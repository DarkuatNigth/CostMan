using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Motor de distribución del auxiliar contable hacia producto.
    ///
    /// Reglas V3:
    /// - SI = libras x 1.
    /// - X2 = libras + esas mismas libras una vez más (libras x 2).
    /// - CAJAS: solo segundo proceso DE/R6/R7/UNI y solo particiones Entero/Cola.
    /// - DESCONGELADO: libras RECIBIDAS PT elegibles x tarifa 0.02.
    ///   El monto del lote se reparte a sus salidas PT/PP por participación de libras.
    /// </summary>
    public sealed class MotorDistribucionCostoProductivo
    {
        private readonly ILogger _objLogger;

        private static readonly HashSet<string> _hshProcesosCajas =
            new(StringComparer.OrdinalIgnoreCase) { "DE", "R6", "R7", "UNI" };

        public MotorDistribucionCostoProductivo(ILogger objLogger)
        {
            _objLogger = objLogger;
        }

        public CostoProductivoDriversDto Construir(
            DataProcesoParam objDataProceso,
            IReadOnlyCollection<MatPrimaReproceso> lstRpcCompleto,
            IReadOnlyCollection<ProcesoCostoAplicacionDto> lstAplicaciones)
        {
            var resultado = new CostoProductivoDriversDto();

            List<LiquidacionResultado> lstFrs =
                objDataProceso.lstLiqFresco ?? new List<LiquidacionResultado>();

            List<MatPrimaReproceso> lstRpc =
                (lstRpcCompleto ?? Array.Empty<MatPrimaReproceso>()).ToList();

            List<MatPrimaReproceso> lstProcesado = lstRpc
                .Where(x => string.Equals(
                    x.strAgrupacion, "2. PROCESADO",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var directas = lstAplicaciones
                .Where(x => x.intFactor > 0)
                .Where(x => !string.Equals(x.strPcCodigo, "CAJ", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.Equals(x.strPcCodigo, "DSC", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Aplicaciones SI/X2 provenientes de niveles de costeo.
            foreach (ProcesoCostoAplicacionDto app in directas)
            {
                string pc = Normalizar(app.strPcCodigo);
                string tip = Normalizar(app.strTipoProcesoCodigo);
                int factor = app.intFactor;

                if (tip == "PFR")
                {
                    foreach (LiquidacionResultado item in lstFrs)
                    {
                        string? particion = ParticionCosteo.ClasificarFrs(
                            item.strProClas01, item.strProClas05);

                        decimal libras = ObtenerLibrasFrs(pc, item);
                        Acumular(resultado.lstDrivers, pc, tip, particion, libras, factor);
                    }
                    continue;
                }

                foreach (MatPrimaReproceso item in lstProcesado.Where(
                    x => string.Equals(Normalizar(x.strTipCod), tip,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    string? particion = ParticionCosteo.ClasificarRpc(item.strTipoProducto);
                    decimal libras = ObtenerLibrasRpc(pc, item);
                    Acumular(resultado.lstDrivers, pc, tip, particion, libras, factor);
                }
            }

            // CAJAS NO utiliza fresco. Solo segundo proceso y solo Entero/Cola.
            foreach (MatPrimaReproceso item in lstProcesado.Where(
                x => _hshProcesosCajas.Contains(Normalizar(x.strTipCod))))
            {
                string? particion = ParticionCosteo.ClasificarRpc(item.strTipoProducto);

                if (particion is not (ParticionCosteo.ENTERO or ParticionCosteo.COLA))
                    continue;

                Acumular(
                    resultado.lstDrivers,
                    "CAJ",
                    Normalizar(item.strTipCod),
                    particion,
                    (decimal)item.dbLibras,
                    1);
            }

            resultado.objDescongelado =
                ConstruirDescongelado(lstRpc, lstAplicaciones);

            return resultado;
        }

        public DistribucionMontoProductoDto DistribuirMonto(
            string strPcCodigo,
            decimal dcMonto,
            CostoProductivoDriversDto objDrivers,
            string strCuentaFallback)
        {
            string pc = Normalizar(strPcCodigo);

            List<DriverProcesoProductoDto> drivers = objDrivers.lstDrivers
                .Where(x => string.Equals(x.strPcCodigo, pc, StringComparison.OrdinalIgnoreCase))
                .ToList();

            decimal pesoEntero = drivers
                .Where(x => x.strParticionCodigo == ParticionCosteo.ENTERO)
                .Sum(x => x.dcPeso);

            decimal pesoCola = drivers
                .Where(x => x.strParticionCodigo == ParticionCosteo.COLA)
                .Sum(x => x.dcPeso);

            decimal pesoVag = drivers
                .Where(x => x.strParticionCodigo is
                    ParticionCosteo.ENTERO_VA or ParticionCosteo.COLA_VA)
                .Sum(x => x.dcPeso);

            decimal pesoTotal = pesoEntero + pesoCola + pesoVag;

            if (pesoTotal <= 0m)
                return DistribuirFallback(dcMonto, strCuentaFallback);

            return new DistribucionMontoProductoDto
            {
                dcEntero = Math.Round(dcMonto * pesoEntero / pesoTotal, 4),
                dcCola = Math.Round(dcMonto * pesoCola / pesoTotal, 4),
                dcValorAgregado = 0m, // se calcula abajo para absorber residuo
                dcLibrasDriver = drivers.Sum(x => x.dcLibras),
                dcPesoDriver = pesoTotal,
                blUsoDriver = true,
                strOrigen = "DRIVER_LIBRAS"
            }.ConResiduo(dcMonto, pesoVag, pesoTotal);
        }

        /// <summary>
        /// Reclasifica el costo de Descongelado tomando el valor
        /// exclusivamente de la cuenta contable 50303050104.
        ///
        /// Reglas:
        /// - Descongelado NO posee cuenta contable propia.
        /// - El monto se calcula por tarifa: Libras PT recibidas x 0.02.
        /// - El monto calculado se retira de la distribución de 50303050104.
        /// - dcMontoCuenta de 50303050104 NO se modifica porque representa
        ///   el saldo original obtenido desde SONG.
        /// - El mismo monto retirado se incorpora como fila derivada DSC.
        /// - El costo general permanece sin cambios.
        /// </summary>
        public CostoProductivoCuentaDto? AplicarReclasificacionDescongelado(
            List<CostoProductivoCuentaDto> lstCuentas,
            DescongeladoResultadoDto objDescongelado,
            CostoProductivoDerivadoConfigDto objConfigDescongelado)
        {
            const string CUENTA_FUENTE_DESCONGELADO = "50303050104";

            decimal montoDescongelado =
                Math.Round(
                    objDescongelado.dcMontoAsignado,
                    4);

            if (montoDescongelado <= 0m)
                return null;

            /*
             * ============================================================
             * 1. LOCALIZAR CUENTA FUENTE
             * ============================================================
             *
             * Ya NO buscamos todas las 50303.
             *
             * Descongelado sale exclusivamente de:
             *
             *      50303050104
             *
             * Puede existir más de una fila si en algún momento la cuenta
             * está dividida mediante factor/configuración, por eso usamos
             * List<> y no FirstOrDefault().
             */
            List<CostoProductivoCuentaDto> lstFuente =
                lstCuentas
                    .Where(x =>
                        !x.blEsDerivado
                        &&
                        string.Equals(
                            Normalizar(x.strCuenta),
                            CUENTA_FUENTE_DESCONGELADO,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (!lstFuente.Any())
            {
                throw new InvalidOperationException(
                    $"No se encontró la cuenta fuente " +
                    $"{CUENTA_FUENTE_DESCONGELADO} para realizar " +
                    $"la reclasificación de Descongelado.");
            }

            /*
             * ============================================================
             * 2. MONTO REAL DISPONIBLE DE LA CUENTA
             * ============================================================
             *
             * No usamos solamente dcMontoVag.
             *
             * La cuenta ya pudo haber pasado por DistribuirMonto()
             * y tener participación en:
             *
             *      Entero
             *      Cola
             *      Valor Agregado
             *
             * Lo importante es cuánto dinero de la cuenta sigue distribuido.
             */
            decimal montoDisponible =
                Math.Round(
                    lstFuente.Sum(x =>
                        x.dcMontoEntero
                        + x.dcMontoCola
                        + x.dcMontoVag),
                    4);

            if (montoDisponible <= 0m)
            {
                decimal montoSong =
                    Math.Round(
                        lstFuente.Sum(x => x.dcMontoCuenta),
                        4);

                throw new InvalidOperationException(
                    $"La cuenta {CUENTA_FUENTE_DESCONGELADO} existe, " +
                    $"pero no tiene monto distribuido disponible. " +
                    $"Monto contable SONG: {montoSong:N4}.");
            }

            if (montoDisponible + 0.01m < montoDescongelado)
            {
                throw new InvalidOperationException(
                    $"El costo de Descongelado ({montoDescongelado:N4}) " +
                    $"supera el monto distribuido disponible de la cuenta " +
                    $"{CUENTA_FUENTE_DESCONGELADO} ({montoDisponible:N4}).");
            }

            /*
             * ============================================================
             * 3. RETIRAR EL MONTO DE 50303050104
             * ============================================================
             *
             * Si la cuenta aparece en varias configuraciones,
             * el retiro se distribuye proporcionalmente.
             *
             * IMPORTANTE:
             *
             * ReducirDistribucionPorReclasificacion()
             * ya mantiene la proporción Entero / Cola / VAG.
             */
            decimal acumuladoRetirado = 0m;

            for (int i = 0; i < lstFuente.Count; i++)
            {
                CostoProductivoCuentaDto row =
                    lstFuente[i];

                decimal montoFila =
                    Math.Round(
                        row.dcMontoEntero
                        + row.dcMontoCola
                        + row.dcMontoVag,
                        4);

                if (montoFila <= 0m)
                    continue;

                decimal ajusteFila;

                /*
                 * La última fila absorbe diferencias de redondeo.
                 */
                if (i == lstFuente.Count - 1)
                {
                    ajusteFila =
                        Math.Round(
                            montoDescongelado
                            - acumuladoRetirado,
                            4);
                }
                else
                {
                    ajusteFila =
                        Math.Round(
                            montoDescongelado
                            * montoFila
                            / montoDisponible,
                            4);

                    acumuladoRetirado +=
                        ajusteFila;
                }

                if (ajusteFila <= 0m)
                    continue;

                /*
                 * Reduce:
                 *
                 * dcMontoEntero
                 * dcMontoCola
                 * dcMontoVag
                 *
                 * manteniendo la proporción que ya había calculado
                 * DistribuirMonto().
                 */
                ReducirDistribucionPorReclasificacion(
                    row,
                    ajusteFila);

                /*
                 * Dejamos evidencia de cuánto salió de la cuenta
                 * para formar Descongelado.
                 */
                row.dcReclasificadoDescongelado =
                    Math.Round(
                        row.dcReclasificadoDescongelado
                        + ajusteFila,
                        4);

                /*
                 * NO modificar:
                 *
                 * row.dcMontoCuenta
                 *
                 * porque debe conservar el monto contable original.
                 */
                row.RecalcularTotales();
            }

            /*
             * ============================================================
             * 4. AJUSTAR DISTRIBUCIÓN DEL DERIVADO
             * ============================================================
             *
             * ConstruirDescongelado() trabaja con precisión de 6 decimales.
             * Nuestra distribución contable trabaja con 4.
             *
             * Aseguramos:
             *
             * Entero + Cola + VAG = montoDescongelado
             */
            decimal enteroDescongelado =
                Math.Round(
                    objDescongelado.dcEntero,
                    4);

            decimal colaDescongelado =
                Math.Round(
                    objDescongelado.dcCola,
                    4);

            decimal vagDescongelado =
                Math.Round(
                    objDescongelado.dcValorAgregado,
                    4);

            decimal residuoDescongelado =
                Math.Round(
                    montoDescongelado
                    - enteroDescongelado
                    - colaDescongelado
                    - vagDescongelado,
                    4);

            /*
             * El residuo se agrega a una partición que realmente
             * haya participado.
             */
            if (residuoDescongelado != 0m)
            {
                if (vagDescongelado != 0m)
                {
                    vagDescongelado =
                        Math.Round(
                            vagDescongelado
                            + residuoDescongelado,
                            4);
                }
                else if (colaDescongelado != 0m)
                {
                    colaDescongelado =
                        Math.Round(
                            colaDescongelado
                            + residuoDescongelado,
                            4);
                }
                else
                {
                    enteroDescongelado =
                        Math.Round(
                            enteroDescongelado
                            + residuoDescongelado,
                            4);
                }
            }

            /*
             * ============================================================
             * 5. CONFIGURACIÓN DEL DERIVADO
             * ============================================================
             */
            objConfigDescongelado ??=
                new CostoProductivoDerivadoConfigDto();

            string ecCodigo =
                string.IsNullOrWhiteSpace(
                    objConfigDescongelado.strEcCodigo)
                    ? "PS"
                    : objConfigDescongelado
                        .strEcCodigo
                        .Trim();

            string etapaGeneral =
                string.IsNullOrWhiteSpace(
                    objConfigDescongelado.strEtapaGeneral)
                    ? "Proceso Secundario"
                    : objConfigDescongelado
                        .strEtapaGeneral
                        .Trim();

            string pcCodigo =
                string.IsNullOrWhiteSpace(
                    objConfigDescongelado.strPcCodigo)
                    ? "DSC"
                    : objConfigDescongelado
                        .strPcCodigo
                        .Trim();

            string proceso =
                string.IsNullOrWhiteSpace(
                    objConfigDescongelado.strProcesoCosto)
                    ? "Descongelado"
                    : objConfigDescongelado
                        .strProcesoCosto
                        .Trim();

            /*
             * ============================================================
             * 6. CREAR FILA DERIVADA
             * ============================================================
             *
             * NO existe cuenta SONG propia para Descongelado.
             *
             * La cuenta 50303050104 es únicamente la fuente monetaria
             * de la reclasificación.
             */
            var derivado =
                new CostoProductivoCuentaDto
                {
                    strOrigen =
                        "DERIVADO",

                    strEtapa =
                        proceso,

                    strGrupoEtapa =
                        etapaGeneral,

                    strEcCodigo =
                        ecCodigo,

                    strPcCodigo =
                        pcCodigo,

                    /*
                     * Descongelado no tiene cuenta contable propia.
                     */
                    strCuenta =
                        string.Empty,

                    blEncontradaSong =
                        false,

                    /*
                     * Configuración proveniente de BASE ACTUALIZADA /
                     * SP de configuración.
                     */
                    strTipo =
                        string.IsNullOrWhiteSpace(
                            objConfigDescongelado.strTipo)
                            ? "TARIFA"
                            : objConfigDescongelado
                                .strTipo
                                .Trim(),

                    strTipoCosto =
                        string.IsNullOrWhiteSpace(
                            objConfigDescongelado.strTipoCosto)
                            ? "VARIABLE"
                            : objConfigDescongelado
                                .strTipoCosto
                                .Trim(),

                    strTipo3 =
                        "Descongelado",

                    strAgrupacionCentro =
                        objConfigDescongelado
                            .strAgrupacionCentro
                            ?.Trim()
                        ?? string.Empty,

                    strGrupoCentro =
                        objConfigDescongelado
                            .strGrupoCentro
                            ?.Trim()
                        ?? string.Empty,

                    strClasificacionMonto =
                        "DESCONGELADO",

                    /*
                     * Para trazabilidad queda explícito de dónde
                     * salió el dinero.
                     */
                    strOrigenDistribucion =
                        "RECLASIFICACION_50303050104",

                    /*
                     * Centro / Subcentro conceptual del proceso.
                     */
                    strCentroCodigo =
                        string.IsNullOrWhiteSpace(
                            objConfigDescongelado.strCentroCodigo)
                            ? "50303"
                            : objConfigDescongelado
                                .strCentroCodigo
                                .Trim(),

                    strCentroCosto =
                        string.IsNullOrWhiteSpace(
                            objConfigDescongelado.strCentroCosto)
                            ? "VALOR AGREGADO"
                            : objConfigDescongelado
                                .strCentroCosto
                                .Trim(),

                    strSubcentroCodigo =
                        string.IsNullOrWhiteSpace(
                            objConfigDescongelado.strSubcentroCodigo)
                            ? "5030307"
                            : objConfigDescongelado
                                .strSubcentroCodigo
                                .Trim(),

                    strSubcentroCosto =
                        string.IsNullOrWhiteSpace(
                            objConfigDescongelado.strSubcentroCosto)
                            ? "DESCONGELADO"
                            : objConfigDescongelado
                                .strSubcentroCosto
                                .Trim(),

                    strRubro =
                        "TARIFA DESCONGELADO",

                    /*
                     * Aquí sí mostramos claramente la fuente monetaria.
                     */
                    strAuxiliar =
                        $"Libras PT recibidas " +
                        $"{objDescongelado.dcLibrasRecibidasPt:N2} " +
                        $"x {objDescongelado.dcTarifa:N4} | " +
                        $"Reclasificado desde {CUENTA_FUENTE_DESCONGELADO}",

                    strNaturaleza =
                        "D",

                    /*
                     * No representa un nuevo movimiento SONG.
                     */
                    dcDebe =
                        0m,

                    dcCredito =
                        0m,

                    dcMontoFuenteSong =
                        0m,

                    dcFactorAsignacion =
                        1m,

                    /*
                     * No existe nueva cuenta contable.
                     */
                    dcMontoCuenta =
                        0m,

                    /*
                     * Distribución que ya calculó
                     * ConstruirDescongelado().
                     */
                    dcMontoEntero =
                        enteroDescongelado,

                    dcMontoCola =
                        colaDescongelado,

                    dcMontoVag =
                        vagDescongelado,

                    dcLibrasDriver =
                        objDescongelado
                            .dcLibrasRecibidasPt,

                    dcPesoDriver =
                        montoDescongelado,

                    strProcesosAplicables =
                        objDescongelado
                            .strTiposAplicables,

                    intCantidadDestinos =
                        string.IsNullOrWhiteSpace(
                            objDescongelado.strTiposAplicables)
                            ? 0
                            : objDescongelado
                                .strTiposAplicables
                                .Split(
                                    ',',
                                    StringSplitOptions
                                        .RemoveEmptyEntries)
                                .Length
                };

            /*
             * ============================================================
             * 7. VALIDAR AGRUPACIÓN / GRUPO
             * ============================================================
             */
            if (string.IsNullOrWhiteSpace(
                    derivado.strAgrupacionCentro)
                ||
                string.IsNullOrWhiteSpace(
                    derivado.strGrupoCentro))
            {
                throw new InvalidOperationException(
                    "La configuración derivada de Descongelado " +
                    "no tiene AGRUPACION/GRUPO.");
            }

            derivado.RecalcularTotales();

            /*
             * ============================================================
             * 8. CONTROL FINAL
             * ============================================================
             *
             * Lo que retiramos de 50303050104 debe ser exactamente
             * lo que aparece como Descongelado.
             */
            decimal totalDerivado =
                Math.Round(
                    derivado.dcMontoEntero
                    + derivado.dcMontoCola
                    + derivado.dcMontoVag,
                    4);

            if (Math.Abs(
                    totalDerivado
                    - montoDescongelado) > 0.01m)
            {
                throw new InvalidOperationException(
                    $"Descuadre en reclasificación de Descongelado. " +
                    $"Retirado: {montoDescongelado:N4}; " +
                    $"Derivado: {totalDerivado:N4}.");
            }

            return derivado;
        }

        /// <summary>
        /// Reduce el monto distribuido de una fila manteniendo la proporción
        /// Entero/Cola/VAG calculada previamente por el driver.
        /// </summary>
        private static void ReducirDistribucionPorReclasificacion(
            CostoProductivoCuentaDto row,
            decimal dcAjuste)
        {
            dcAjuste = Math.Round(dcAjuste, 4);
            if (dcAjuste <= 0m) return;

            decimal enteroOriginal = row.dcMontoEntero;
            decimal colaOriginal = row.dcMontoCola;
            decimal vagOriginal = row.dcMontoVag;

            decimal totalOriginal = Math.Round(
                enteroOriginal + colaOriginal + vagOriginal,
                4);

            if (totalOriginal <= 0m)
            {
                throw new InvalidOperationException(
                    $"La cuenta {row.strCuenta} del proceso {row.strEtapa} " +
                    "pertenece al pool 50303 pero no tiene monto distribuido.");
            }

            if (dcAjuste > totalOriginal + 0.01m)
            {
                throw new InvalidOperationException(
                    $"El ajuste de Descongelado ({dcAjuste:N4}) supera el " +
                    $"monto distribuido de la cuenta {row.strCuenta} ({totalOriginal:N4}).");
            }

            decimal nuevoTotal = Math.Round(totalOriginal - dcAjuste, 4);
            if (nuevoTotal < 0m) nuevoTotal = 0m;

            decimal nuevoEntero = enteroOriginal != 0m
                ? Math.Round(nuevoTotal * enteroOriginal / totalOriginal, 4)
                : 0m;

            decimal nuevoCola = colaOriginal != 0m
                ? Math.Round(nuevoTotal * colaOriginal / totalOriginal, 4)
                : 0m;

            decimal nuevoVag = vagOriginal != 0m
                ? Math.Round(nuevoTotal * vagOriginal / totalOriginal, 4)
                : 0m;

            decimal residuo = Math.Round(
                nuevoTotal - nuevoEntero - nuevoCola - nuevoVag,
                4);

            if (vagOriginal != 0m)
                nuevoVag = Math.Round(nuevoVag + residuo, 4);
            else if (colaOriginal != 0m)
                nuevoCola = Math.Round(nuevoCola + residuo, 4);
            else
                nuevoEntero = Math.Round(nuevoEntero + residuo, 4);

            row.dcMontoEntero = nuevoEntero;
            row.dcMontoCola = nuevoCola;
            row.dcMontoVag = nuevoVag;
        }

        public List<CostoProductivoProcesoAplicableDetalleDto> ConstruirDetalleProcesosAplicables(
    string strPcCodigo,
    decimal dcMontoDistribuir,
    CostoProductivoDriversDto objDrivers,
    IReadOnlyCollection<ProcesoCostoAplicacionDto> lstAplicaciones)
        {
            string pc = Normalizar(strPcCodigo);
            dcMontoDistribuir = Math.Round(dcMontoDistribuir, 4);

            var aplicaciones = (lstAplicaciones ?? Array.Empty<ProcesoCostoAplicacionDto>())
                .Where(x => string.Equals(
                    Normalizar(x.strPcCodigo),
                    pc,
                    StringComparison.OrdinalIgnoreCase))
                .GroupBy(
                    x => Normalizar(x.strTipoProcesoCodigo),
                    StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var pesosPorTipo = (objDrivers?.lstDrivers ?? new List<DriverProcesoProductoDto>())
                .Where(x => string.Equals(
                    Normalizar(x.strPcCodigo),
                    pc,
                    StringComparison.OrdinalIgnoreCase))
                .GroupBy(
                    x => Normalizar(x.strTipCodigo),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.dcPeso),
                    StringComparer.OrdinalIgnoreCase);

            var nombresPorCodigo = aplicaciones
                .Where(x => !string.IsNullOrWhiteSpace(x.strTipoProcesoCodigo))
                .ToDictionary(
                    x => Normalizar(x.strTipoProcesoCodigo),
                    x => string.IsNullOrWhiteSpace(x.strTipoProceso)
                        ? Normalizar(x.strTipoProcesoCodigo)
                        : x.strTipoProceso.Trim(),
                    StringComparer.OrdinalIgnoreCase);

            // Unión: configuración + drivers reales. Así CAJ también puede aparecer
            // aunque su driver haya sido construido manualmente.
            var codigos = aplicaciones
                .Select(x => Normalizar(x.strTipoProcesoCodigo))
                .Concat(pesosPorTipo.Keys)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var trabajo = codigos
                .Select(codigo => new
                {
                    Codigo = codigo,
                    Proceso = nombresPorCodigo.TryGetValue(codigo, out string? nombre)
                        ? nombre
                        : codigo,
                    Peso = pesosPorTipo.TryGetValue(codigo, out decimal peso)
                        ? peso
                        : 0m
                })
                .ToList();

            decimal pesoTotal = trabajo.Sum(x => x.Peso);

            var resultado = trabajo
                .Select(x => new CostoProductivoProcesoAplicableDetalleDto
                {
                    strCodigoProceso = x.Codigo,
                    strProceso = x.Proceso,
                    dcDolares = 0m
                })
                .ToList();

            // Sin driver real no inventamos dólares por proceso.
            if (pesoTotal <= 0m || dcMontoDistribuir == 0m)
                return resultado;

            var indicesConPeso = trabajo
                .Select((x, i) => new { Item = x, Index = i })
                .Where(x => x.Item.Peso > 0m)
                .ToList();

            decimal acumulado = 0m;

            for (int i = 0; i < indicesConPeso.Count; i++)
            {
                var actual = indicesConPeso[i];
                decimal dolares;

                if (i == indicesConPeso.Count - 1)
                {
                    dolares = Math.Round(dcMontoDistribuir - acumulado, 4);
                }
                else
                {
                    dolares = Math.Round(
                        dcMontoDistribuir * actual.Item.Peso / pesoTotal,
                        4);
                    acumulado += dolares;
                }

                resultado[actual.Index].dcDolares = dolares;
            }

            return resultado;
        }


        private DescongeladoResultadoDto ConstruirDescongelado(
            List<MatPrimaReproceso> lstRpc,
            IReadOnlyCollection<ProcesoCostoAplicacionDto> lstAplicaciones)
        {
            var tarifas = lstAplicaciones
                .Where(x => string.Equals(x.strPcCodigo, "DSC", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.blEsTarifa)
                .ToList();

            decimal tarifa = tarifas
                .Select(x => x.dcTarifa ?? 0m)
                .FirstOrDefault(x => x > 0m);

            if (tarifa <= 0m) tarifa = 0.02m;

            HashSet<string> tipos = tarifas
                .Select(x => Normalizar(x.strTipoProcesoCodigo))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<MatPrimaReproceso> recibidos = lstRpc
                .Where(x => string.Equals(x.strAgrupacion, "1. RECIBIDO",
                    StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.strProClas03, "PT",
                    StringComparison.OrdinalIgnoreCase))
                .Where(x => x.blEsDescongelado == true)
                .Where(x => tipos.Count == 0 || tipos.Contains(Normalizar(x.strTipCod)))
                .ToList();

            List<MatPrimaReproceso> procesados = lstRpc
                .Where(x => string.Equals(x.strAgrupacion, "2. PROCESADO",
                    StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.strProClas03, "PT", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(x.strProClas03, "PP", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.dbLibras > 0)
                .ToList();

            var resultado = new DescongeladoResultadoDto
            {
                dcTarifa = tarifa,
                strTiposAplicables = string.Join(", ", tipos.OrderBy(x => x))
            };

            var gruposRec = recibidos.GroupBy(x => (x.intLotNumero, x.intLoteUnificado));

            foreach (var grupo in gruposRec)
            {
                decimal lbsRec = grupo.Sum(x => (decimal)x.dbLibras);
                if (lbsRec <= 0m) continue;

                resultado.intLotesElegibles++;
                resultado.dcLibrasRecibidasPt += lbsRec;

                decimal montoLote = Math.Round(lbsRec * tarifa, 6);

                List<MatPrimaReproceso> salidas = procesados
                    .Where(x => x.intLotNumero == grupo.Key.intLotNumero
                             && x.intLoteUnificado == grupo.Key.intLoteUnificado)
                    .ToList();

                decimal lbsSalida = salidas.Sum(x => (decimal)x.dbLibras);

                if (lbsSalida <= 0m)
                {
                    resultado.intLotesSinSalida++;
                    resultado.dcMontoSinAsignar += montoLote;
                    continue;
                }

                decimal asignadoLote = 0m;
                for (int i = 0; i < salidas.Count; i++)
                {
                    MatPrimaReproceso salida = salidas[i];

                    decimal montoSalida = i == salidas.Count - 1
                        ? Math.Round(montoLote - asignadoLote, 6)
                        : Math.Round(montoLote * (decimal)salida.dbLibras / lbsSalida, 6);

                    if (i < salidas.Count - 1) asignadoLote += montoSalida;

                    string? part = ParticionCosteo.ClasificarRpc(salida.strTipoProducto);
                    switch (part)
                    {
                        case ParticionCosteo.ENTERO:
                            resultado.dcEntero += montoSalida;
                            break;
                        case ParticionCosteo.COLA:
                            resultado.dcCola += montoSalida;
                            break;
                        case ParticionCosteo.ENTERO_VA:
                        case ParticionCosteo.COLA_VA:
                            resultado.dcValorAgregado += montoSalida;
                            break;
                        default:
                            resultado.dcMontoSinAsignar += montoSalida;
                            break;
                    }
                }
            }

            resultado.dcMontoTotal =
                Math.Round(resultado.dcLibrasRecibidasPt * tarifa, 6);

            resultado.dcMontoAsignado =
                Math.Round(resultado.dcEntero + resultado.dcCola +
                           resultado.dcValorAgregado, 6);

            // Incluye lotes sin salida + salidas sin partición.
            resultado.dcMontoSinAsignar =
                Math.Round(resultado.dcMontoTotal - resultado.dcMontoAsignado, 6);

            return resultado;
        }

        private static decimal ObtenerLibrasFrs(string pc, LiquidacionResultado item)
        {
            if (pc == "RET")
                return item.dcLibrasRetractilado.GetValueOrDefault() > 0m
                    ? item.dcLibrasRetractilado.GetValueOrDefault()
                    : (decimal)item.dcLibras;

            return (decimal)item.dcLibras;
        }

        private static decimal ObtenerLibrasRpc(string pc, MatPrimaReproceso item)
        {
            if (pc == "PEL")
            {
                decimal pelado = item.dcLibrasPelado.GetValueOrDefault();
                return pelado > 0m ? pelado : (decimal)item.dbLibras;
            }

            if (pc == "RET")
            {
                decimal retra = item.dcLibrasRetractilado.GetValueOrDefault();
                return retra > 0m ? retra : (decimal)item.dbLibras;
            }

            return (decimal)item.dbLibras;
        }

        private static void Acumular(
            List<DriverProcesoProductoDto> lst,
            string pc,
            string tip,
            string? particion,
            decimal libras,
            int factor)
        {
            if (string.IsNullOrWhiteSpace(particion) || libras <= 0m || factor <= 0)
                return;

            // X2: las libras normales + las mismas libras una vez más.
            decimal peso = libras + (libras * (factor - 1));

            lst.Add(new DriverProcesoProductoDto
            {
                strPcCodigo = pc,
                strTipCodigo = tip,
                strParticionCodigo = particion,
                strParticion = ParticionCosteo.Descripcion(particion),
                dcLibras = libras,
                intFactor = factor,
                dcPeso = peso
            });
        }

        private static DistribucionMontoProductoDto DistribuirFallback(
            decimal monto,
            string cuenta)
        {
            string c = (cuenta ?? string.Empty).Trim();

            if (c.StartsWith("50301", StringComparison.OrdinalIgnoreCase))
                return new DistribucionMontoProductoDto
                {
                    dcEntero = monto,
                    strOrigen = "FALLBACK_CUENTA",
                    blUsoDriver = false
                };

            if (c.StartsWith("50302", StringComparison.OrdinalIgnoreCase))
                return new DistribucionMontoProductoDto
                {
                    dcCola = monto,
                    strOrigen = "FALLBACK_CUENTA",
                    blUsoDriver = false
                };

            if (c.StartsWith("50303", StringComparison.OrdinalIgnoreCase))
                return new DistribucionMontoProductoDto
                {
                    dcValorAgregado = monto,
                    strOrigen = "FALLBACK_CUENTA",
                    blUsoDriver = false
                };

            return new DistribucionMontoProductoDto
            {
                strOrigen = "SIN_DRIVER",
                blUsoDriver = false
            };
        }

        private static string Normalizar(string? v) => (v ?? string.Empty).Trim();
    }


    internal static class DistribucionMontoProductoExtensions
    {
        public static DistribucionMontoProductoDto ConResiduo(
            this DistribucionMontoProductoDto dto,
            decimal monto,
            decimal pesoVag,
            decimal pesoTotal)
        {
            // VAG absorbe el residuo de redondeo para que Entero+Cola+VAG=monto.
            dto.dcValorAgregado = Math.Round(
                monto - dto.dcEntero - dto.dcCola, 4);

            // Si no existe VAG pero el residuo solo proviene del redondeo,
            // se lo deja en la última partición con peso.
            if (pesoVag <= 0m && Math.Abs(dto.dcValorAgregado) > 0m)
            {
                if (dto.dcCola != 0m)
                {
                    dto.dcCola += dto.dcValorAgregado;
                    dto.dcValorAgregado = 0m;
                }
                else
                {
                    dto.dcEntero += dto.dcValorAgregado;
                    dto.dcValorAgregado = 0m;
                }
            }
            return dto;
        }
    }
}
