using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Dominio.Reglas
{
    public sealed class MotorDistribucionCostoProductivo
    {
        private readonly ILogger _objLogger;
        private static readonly HashSet<string> _hshProcesosCajas = new(StringComparer.OrdinalIgnoreCase) { "DE", "R6", "R7", "UNI" };
        private static readonly HashSet<string> _hshProcesosFrescoRecibido = new(StringComparer.OrdinalIgnoreCase) { "LOG", "REC", "CLA" };
        private static readonly HashSet<string> _hshProcesosPfrLibrasRecibidas = new(StringComparer.OrdinalIgnoreCase) { "LOG", "REC", "CLA" };

        public MotorDistribucionCostoProductivo(ILogger objLogger) => _objLogger = objLogger;

        public CostoProductivoDriversDto Construir(
            DataProcesoParam objDataProceso,
            IReadOnlyCollection<MatPrimaReproceso> lstRpcCompleto,
            IReadOnlyCollection<ProcesoCostoAplicacionDto> lstAplicaciones)
        {
            var resultado = new CostoProductivoDriversDto();
            List<LiquidacionResultado> lstFrs = objDataProceso.lstLiqFresco ?? new();
            List<MatPrimaReproceso> lstRpc = (lstRpcCompleto ?? Array.Empty<MatPrimaReproceso>()).ToList();
            List<MatPrimaReproceso> lstProcesado = lstRpc
                .Where(x => string.Equals(x.strAgrupacion, "2. PROCESADO", StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<string> procesosRecibido = lstAplicaciones
                .Where(x => _hshProcesosFrescoRecibido.Contains(Normalizar(x.strPcCodigo)))
                .Where(x => string.Equals(Normalizar(x.strTipoProcesoCodigo), "PFR", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.intFactor > 0)
                .Select(x => Normalizar(x.strPcCodigo))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string pc in procesosRecibido)
            {
                DriverFrescoRecibidoDto resumen = ConstruirDriverFrescoRecibido(pc, lstFrs);
                resultado.lstFrescoRecibido.Add(resumen);
                Acumular(resultado.lstDrivers, pc, "PFR", ParticionCosteo.ENTERO, resumen.dcLibrasRecibidasEntero, 1);
                Acumular(resultado.lstDrivers, pc, "PFR", ParticionCosteo.COLA, resumen.dcLibrasRecibidasCola, 1);
            }

            var directas = lstAplicaciones
                .Where(x => x.intFactor > 0)
                .Where(x => !_hshProcesosFrescoRecibido.Contains(Normalizar(x.strPcCodigo)))
                .Where(x => !string.Equals(x.strPcCodigo, "CAJ", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.Equals(x.strPcCodigo, "DSC", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (ProcesoCostoAplicacionDto app in directas)
            {
                string pc = Normalizar(app.strPcCodigo);
                string tip = Normalizar(app.strTipoProcesoCodigo);
                int factor = app.intFactor;
                if (tip == "PFR")
                {
                    foreach (LiquidacionResultado item in lstFrs)
                    {
                        string? particion = ParticionCosteo.ClasificarFrs(item.strProClas01, item.strProClas05);
                        decimal libras = ObtenerLibrasFrs(pc, item);
                        Acumular(resultado.lstDrivers, pc, tip, particion, libras, factor);
                    }
                    continue;
                }

                foreach (MatPrimaReproceso item in lstProcesado.Where(
                    x => string.Equals(Normalizar(x.strTipCod), tip, StringComparison.OrdinalIgnoreCase)))
                {
                    string? particion = ParticionCosteo.ClasificarRpc(item.strTipoProducto);
                    decimal libras = ObtenerLibrasRpc(pc, item);
                    Acumular(resultado.lstDrivers, pc, tip, particion, libras, factor);
                }
            }

            foreach (MatPrimaReproceso item in lstProcesado.Where(x => _hshProcesosCajas.Contains(Normalizar(x.strTipCod))))
            {
                string? particion = ParticionCosteo.ClasificarRpc(item.strTipoProducto);
                if (particion is not (ParticionCosteo.ENTERO or ParticionCosteo.COLA)) continue;
                Acumular(resultado.lstDrivers, "CAJ", Normalizar(item.strTipCod), particion, (decimal)item.dbLibras, 1);
            }

            resultado.objDescongelado = ConstruirDescongelado(lstRpc, lstAplicaciones);
            return resultado;
        }

        public DistribucionMontoProductoDto DistribuirMonto(
            string strPcCodigo,
            decimal dcMonto,
            CostoProductivoDriversDto objDrivers,
            string strCuentaFallback)
        {
            string pc = Normalizar(strPcCodigo);
            DriverFrescoRecibidoDto? driverRecibido = objDrivers.lstFrescoRecibido
                .FirstOrDefault(x => string.Equals(x.strPcCodigo, pc, StringComparison.OrdinalIgnoreCase));
            if (driverRecibido != null)
                return DistribuirPorFrescoRecibido(dcMonto, driverRecibido, strCuentaFallback);

            List<DriverProcesoProductoDto> drivers = objDrivers.lstDrivers
                .Where(x => string.Equals(x.strPcCodigo, pc, StringComparison.OrdinalIgnoreCase)).ToList();
            decimal pesoEntero = drivers.Where(x => x.strParticionCodigo == ParticionCosteo.ENTERO).Sum(x => x.dcPeso);
            decimal pesoCola = drivers.Where(x => x.strParticionCodigo == ParticionCosteo.COLA).Sum(x => x.dcPeso);
            decimal pesoVag = drivers.Where(x => x.strParticionCodigo is ParticionCosteo.ENTERO_VA or ParticionCosteo.COLA_VA).Sum(x => x.dcPeso);
            decimal pesoTotal = pesoEntero + pesoCola + pesoVag;
            if (pesoTotal <= 0m) return DistribuirFallback(dcMonto, strCuentaFallback);

            return new DistribucionMontoProductoDto
            {
                dcEntero = Math.Round(dcMonto * pesoEntero / pesoTotal, 4),
                dcCola = Math.Round(dcMonto * pesoCola / pesoTotal, 4),
                dcValorAgregado = 0m,
                dcLibrasDriver = drivers.Sum(x => x.dcLibras),
                dcPesoDriver = pesoTotal,
                blUsoDriver = true,
                strOrigen = "DRIVER_LIBRAS"
            }.ConResiduo(dcMonto, pesoVag, pesoTotal);
        }

        public List<CostoProductivoProcesoOrigenDto> ConstruirResumenProcesosOrigen(
            IEnumerable<CostoProductivoCuentaDto> lstCuentas,
            CostoProductivoDriversDto objDrivers,
            IReadOnlyCollection<ProcesoCostoAplicacionDto> lstAplicaciones)
        {
            var nombres = (lstAplicaciones ?? Array.Empty<ProcesoCostoAplicacionDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.strPcCodigo))
                .GroupBy(x => Normalizar(x.strPcCodigo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => string.IsNullOrWhiteSpace(g.First().strPcNombre) ? g.Key : g.First().strPcNombre.Trim(),
                    StringComparer.OrdinalIgnoreCase);

            var dict = new Dictionary<(string Pc, string Origen), CostoProductivoProcesoOrigenDto>();

            CostoProductivoProcesoOrigenDto Get(string pc, string origen, string nombre)
            {
                var key = (pc, origen);
                if (!dict.TryGetValue(key, out CostoProductivoProcesoOrigenDto? dto))
                {
                    dto = new CostoProductivoProcesoOrigenDto { strPcCodigo = pc, strProceso = nombre, strOrigen = origen };
                    dict[key] = dto;
                }
                return dto;
            }

            void AgregarMonto(string pc, string nombre, string particion, decimal monto)
            {
                if (Math.Abs(monto) <= 0.0000001m) return;
                if (_hshProcesosFrescoRecibido.Contains(pc))
                {
                    AcumularMonto(Get(pc, "PFR", nombre), particion, monto);
                    return;
                }
                if (string.Equals(pc, "DSC", StringComparison.OrdinalIgnoreCase))
                {
                    AcumularMonto(Get(pc, "RPC", nombre), particion, monto);
                    return;
                }

                List<DriverProcesoProductoDto> driversParticion = objDrivers.lstDrivers
                    .Where(x => string.Equals(Normalizar(x.strPcCodigo), pc, StringComparison.OrdinalIgnoreCase))
                    .Where(x => EsMismaParticionResumen(x.strParticionCodigo, particion)).ToList();
                decimal pesoPfr = driversParticion
                    .Where(x => string.Equals(Normalizar(x.strTipCodigo), "PFR", StringComparison.OrdinalIgnoreCase)).Sum(x => x.dcPeso);
                decimal pesoRpc = driversParticion
                    .Where(x => !string.Equals(Normalizar(x.strTipCodigo), "PFR", StringComparison.OrdinalIgnoreCase)).Sum(x => x.dcPeso);
                decimal pesoTotal = pesoPfr + pesoRpc;
                if (pesoTotal <= 0m) return;

                decimal montoPfr = pesoPfr > 0m ? Math.Round(monto * pesoPfr / pesoTotal, 5) : 0m;
                decimal montoRpc = Math.Round(monto - montoPfr, 5);
                if (pesoPfr > 0m) AcumularMonto(Get(pc, "PFR", nombre), particion, montoPfr);
                if (pesoRpc > 0m) AcumularMonto(Get(pc, "RPC", nombre), particion, montoRpc);
            }

            foreach (CostoProductivoCuentaDto row in lstCuentas ?? Array.Empty<CostoProductivoCuentaDto>())
            {
                string pc = Normalizar(row.strPcCodigo).ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(pc) || pc == "MPE") continue;
                string nombre = nombres.GetValueOrDefault(pc, string.IsNullOrWhiteSpace(row.strEtapa) ? pc : row.strEtapa.Trim());
                AgregarMonto(pc, nombre, ParticionCosteo.ENTERO, row.dcMontoEnteroPersistencia);
                AgregarMonto(pc, nombre, ParticionCosteo.COLA, row.dcMontoColaPersistencia);
                AgregarMonto(pc, nombre, "VA", row.dcMontoVagPersistencia);
            }

            foreach (var grupo in objDrivers.lstDrivers.GroupBy(x => new
            {
                Pc = Normalizar(x.strPcCodigo).ToUpperInvariant(),
                Origen = string.Equals(Normalizar(x.strTipCodigo), "PFR", StringComparison.OrdinalIgnoreCase) ? "PFR" : "RPC",
                Particion = ParticionResumen(x.strParticionCodigo)
            }))
            {
                if (string.IsNullOrWhiteSpace(grupo.Key.Pc) || grupo.Key.Pc == "MPE") continue;
                string nombre = nombres.GetValueOrDefault(grupo.Key.Pc, grupo.Key.Pc);
                CostoProductivoProcesoOrigenDto dto = Get(grupo.Key.Pc, grupo.Key.Origen, nombre);
                AcumularLibras(dto, grupo.Key.Particion, grupo.Sum(x => x.dcLibras));
            }

            foreach (DriverFrescoRecibidoDto driver in objDrivers.lstFrescoRecibido)
            {
                string pc = Normalizar(driver.strPcCodigo).ToUpperInvariant();
                string nombre = nombres.GetValueOrDefault(pc, pc);
                CostoProductivoProcesoOrigenDto dto = Get(pc, "PFR", nombre);
                dto.dcLibrasEntero = driver.dcLibrasProcesadasEntero;
                dto.dcLibrasCola = driver.dcLibrasProcesadasCola;
                dto.dcLibrasValorAgregado = 0m;
            }

            if (dict.TryGetValue(("DSC", "RPC"), out CostoProductivoProcesoOrigenDto? dsc))
            {
                decimal tarifa = objDrivers.objDescongelado.dcTarifa > 0m ? objDrivers.objDescongelado.dcTarifa : 0.02m;
                dsc.dcLibrasEntero = dsc.dcDolaresEntero / tarifa;
                dsc.dcLibrasCola = dsc.dcDolaresCola / tarifa;
                dsc.dcLibrasValorAgregado = dsc.dcDolaresValorAgregado / tarifa;
            }

            foreach (CostoProductivoProcesoOrigenDto dto in dict.Values) dto.Recalcular();
            return dict.Values
                .Where(x => x.dcTotalDolares != 0m || x.dcTotalLibras != 0m)
                .OrderBy(x => x.strProceso).ThenBy(x => x.strOrigen).ToList();
        }

        private static string ParticionResumen(string? particion) =>
            particion is ParticionCosteo.ENTERO ? ParticionCosteo.ENTERO :
            particion is ParticionCosteo.COLA ? ParticionCosteo.COLA : "VA";

        private static bool EsMismaParticionResumen(string? driver, string resumen) =>
            string.Equals(ParticionResumen(driver), ParticionResumen(resumen), StringComparison.OrdinalIgnoreCase);

        private static void AcumularMonto(CostoProductivoProcesoOrigenDto dto, string particion, decimal monto)
        {
            switch (ParticionResumen(particion))
            {
                case ParticionCosteo.ENTERO: dto.dcDolaresEntero += monto; break;
                case ParticionCosteo.COLA: dto.dcDolaresCola += monto; break;
                default: dto.dcDolaresValorAgregado += monto; break;
            }
        }

        private static void AcumularLibras(CostoProductivoProcesoOrigenDto dto, string particion, decimal libras)
        {
            switch (ParticionResumen(particion))
            {
                case ParticionCosteo.ENTERO: dto.dcLibrasEntero += libras; break;
                case ParticionCosteo.COLA: dto.dcLibrasCola += libras; break;
                default: dto.dcLibrasValorAgregado += libras; break;
            }
        }

        private static DriverFrescoRecibidoDto ConstruirDriverFrescoRecibido(string pc, IEnumerable<LiquidacionResultado> lstFrs)
        {
            var resultado = new DriverFrescoRecibidoDto { strPcCodigo = pc };
            foreach (LiquidacionResultado item in lstFrs)
            {
                string? particion = ClasificarFrescoEnteroCola(item.strProClas01);
                if (particion == null) continue;
                decimal lbsRecibidas = item.dcLbsReciXRend;
                decimal lbsProcesadas = (decimal)item.dcLibras;
                if (particion == ParticionCosteo.ENTERO)
                {
                    if (lbsRecibidas > 0m) resultado.dcLibrasRecibidasEntero += lbsRecibidas;
                    if (lbsProcesadas > 0m) resultado.dcLibrasProcesadasEntero += lbsProcesadas;
                }
                else if (particion == ParticionCosteo.COLA)
                {
                    if (lbsRecibidas > 0m) resultado.dcLibrasRecibidasCola += lbsRecibidas;
                    if (lbsProcesadas > 0m) resultado.dcLibrasProcesadasCola += lbsProcesadas;
                }
            }
            return resultado;
        }

        private static string? ClasificarFrescoEnteroCola(string? strProClas01) =>
            Normalizar(strProClas01).ToUpperInvariant() switch
            {
                "CC" => ParticionCosteo.ENTERO,
                "SC" => ParticionCosteo.COLA,
                _ => null
            };

        private static DistribucionMontoProductoDto DistribuirPorFrescoRecibido(decimal monto, DriverFrescoRecibidoDto driver, string cuentaFallback)
        {
            decimal lbsRecEntero = driver.dcLibrasRecibidasEntero;
            decimal lbsRecCola = driver.dcLibrasRecibidasCola;
            decimal lbsRecTotal = lbsRecEntero + lbsRecCola;
            if (lbsRecTotal <= 0m) return DistribuirFallback(monto, cuentaFallback);
            decimal costoUnitarioRecibido = monto / lbsRecTotal;
            decimal montoEntero = Math.Round(lbsRecEntero * costoUnitarioRecibido, 4);
            decimal montoCola = Math.Round(monto - montoEntero, 4);
            decimal costoUnitarioEntero = driver.dcLibrasProcesadasEntero > 0m ? montoEntero / driver.dcLibrasProcesadasEntero : 0m;
            decimal costoUnitarioCola = driver.dcLibrasProcesadasCola > 0m ? montoCola / driver.dcLibrasProcesadasCola : 0m;
            return new DistribucionMontoProductoDto
            {
                dcEntero = montoEntero,
                dcCola = montoCola,
                dcValorAgregado = 0m,
                dcLibrasDriver = lbsRecTotal,
                dcPesoDriver = lbsRecTotal,
                blUsoDriver = true,
                strOrigen = "FRESCO_LBS_RECIBIDAS_REND",
                dcLibrasRecibidasEntero = lbsRecEntero,
                dcLibrasRecibidasCola = lbsRecCola,
                dcLibrasProcesadasEntero = driver.dcLibrasProcesadasEntero,
                dcLibrasProcesadasCola = driver.dcLibrasProcesadasCola,
                dcCostoUnitarioRecibido = costoUnitarioRecibido,
                dcCostoUnitarioEntero = costoUnitarioEntero,
                dcCostoUnitarioCola = costoUnitarioCola
            };
        }

        public CostoProductivoCuentaDto? AplicarReclasificacionDescongelado(
            List<CostoProductivoCuentaDto> lstCuentas,
            DescongeladoResultadoDto objDescongelado,
            CostoProductivoDerivadoConfigDto objConfigDescongelado)
        {
            const string CUENTA_FUENTE_DESCONGELADO = "50303050104";
            decimal montoDescongelado = Math.Round(objDescongelado.dcMontoAsignado, 4);
            if (montoDescongelado <= 0m) return null;

            List<CostoProductivoCuentaDto> lstFuente = lstCuentas
                .Where(x => !x.blEsDerivado && string.Equals(Normalizar(x.strCuenta), CUENTA_FUENTE_DESCONGELADO, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!lstFuente.Any())
                throw new InvalidOperationException($"No se encontró la cuenta fuente {CUENTA_FUENTE_DESCONGELADO} para realizar la reclasificación de Descongelado.");

            decimal montoDisponible = Math.Round(lstFuente.Sum(x => x.dcMontoEntero + x.dcMontoCola + x.dcMontoVag), 4);
            if (montoDisponible <= 0m)
            {
                decimal montoSong = Math.Round(lstFuente.Sum(x => x.dcMontoCuenta), 4);
                throw new InvalidOperationException($"La cuenta {CUENTA_FUENTE_DESCONGELADO} existe, pero no tiene monto distribuido disponible. Monto contable SONG: {montoSong:N4}.");
            }
            if (montoDisponible + 0.01m < montoDescongelado)
                throw new InvalidOperationException($"El costo de Descongelado ({montoDescongelado:N4}) supera el monto distribuido disponible de la cuenta {CUENTA_FUENTE_DESCONGELADO} ({montoDisponible:N4}).");

            decimal acumuladoRetirado = 0m;
            for (int i = 0; i < lstFuente.Count; i++)
            {
                CostoProductivoCuentaDto row = lstFuente[i];
                decimal montoFila = Math.Round(row.dcMontoEntero + row.dcMontoCola + row.dcMontoVag, 4);
                if (montoFila <= 0m) continue;
                decimal ajusteFila;
                if (i == lstFuente.Count - 1)
                    ajusteFila = Math.Round(montoDescongelado - acumuladoRetirado, 4);
                else
                {
                    ajusteFila = Math.Round(montoDescongelado * montoFila / montoDisponible, 4);
                    acumuladoRetirado += ajusteFila;
                }
                if (ajusteFila <= 0m) continue;
                ReducirDistribucionPorReclasificacion(row, ajusteFila);
                row.dcReclasificadoDescongelado = Math.Round(row.dcReclasificadoDescongelado + ajusteFila, 4);
                row.RecalcularTotales();
            }

            decimal enteroDescongelado = Math.Round(objDescongelado.dcEntero, 4);
            decimal colaDescongelado = Math.Round(objDescongelado.dcCola, 4);
            decimal vagDescongelado = Math.Round(objDescongelado.dcValorAgregado, 4);
            decimal residuoDescongelado = Math.Round(montoDescongelado - enteroDescongelado - colaDescongelado - vagDescongelado, 4);
            if (residuoDescongelado != 0m)
            {
                if (vagDescongelado != 0m) vagDescongelado = Math.Round(vagDescongelado + residuoDescongelado, 4);
                else if (colaDescongelado != 0m) colaDescongelado = Math.Round(colaDescongelado + residuoDescongelado, 4);
                else enteroDescongelado = Math.Round(enteroDescongelado + residuoDescongelado, 4);
            }

            objConfigDescongelado ??= new CostoProductivoDerivadoConfigDto();
            string ecCodigo = string.IsNullOrWhiteSpace(objConfigDescongelado.strEcCodigo) ? "PS" : objConfigDescongelado.strEcCodigo.Trim();
            string etapaGeneral = string.IsNullOrWhiteSpace(objConfigDescongelado.strEtapaGeneral) ? "Proceso Secundario" : objConfigDescongelado.strEtapaGeneral.Trim();
            string pcCodigo = string.IsNullOrWhiteSpace(objConfigDescongelado.strPcCodigo) ? "DSC" : objConfigDescongelado.strPcCodigo.Trim();
            string proceso = string.IsNullOrWhiteSpace(objConfigDescongelado.strProcesoCosto) ? "Descongelado" : objConfigDescongelado.strProcesoCosto.Trim();

            var derivado = new CostoProductivoCuentaDto
            {
                strOrigen = "DERIVADO",
                strEtapa = proceso,
                strGrupoEtapa = etapaGeneral,
                strEcCodigo = ecCodigo,
                strPcCodigo = pcCodigo,
                strCuenta = string.Empty,
                blEncontradaSong = false,
                strTipo = string.IsNullOrWhiteSpace(objConfigDescongelado.strTipo) ? "TARIFA" : objConfigDescongelado.strTipo.Trim(),
                strTipoCosto = string.IsNullOrWhiteSpace(objConfigDescongelado.strTipoCosto) ? "VARIABLE" : objConfigDescongelado.strTipoCosto.Trim(),
                strTipo3 = "Descongelado",
                strAgrupacionCentro = objConfigDescongelado.strAgrupacionCentro?.Trim() ?? string.Empty,
                strGrupoCentro = objConfigDescongelado.strGrupoCentro?.Trim() ?? string.Empty,
                strClasificacionMonto = "DESCONGELADO",
                strOrigenDistribucion = "RECLASIFICACION_50303050104",
                strCentroCodigo = string.IsNullOrWhiteSpace(objConfigDescongelado.strCentroCodigo) ? "50303" : objConfigDescongelado.strCentroCodigo.Trim(),
                strCentroCosto = string.IsNullOrWhiteSpace(objConfigDescongelado.strCentroCosto) ? "VALOR AGREGADO" : objConfigDescongelado.strCentroCosto.Trim(),
                strSubcentroCodigo = string.IsNullOrWhiteSpace(objConfigDescongelado.strSubcentroCodigo) ? "5030307" : objConfigDescongelado.strSubcentroCodigo.Trim(),
                strSubcentroCosto = string.IsNullOrWhiteSpace(objConfigDescongelado.strSubcentroCosto) ? "DESCONGELADO" : objConfigDescongelado.strSubcentroCosto.Trim(),
                strRubro = "TARIFA DESCONGELADO",
                strAuxiliar = $"Libras PT recibidas {objDescongelado.dcLibrasRecibidasPt:N2} x {objDescongelado.dcTarifa:N4} | Reclasificado desde {CUENTA_FUENTE_DESCONGELADO}",
                strNaturaleza = "D",
                dcDebe = 0m,
                dcCredito = 0m,
                dcMontoFuenteSong = 0m,
                dcFactorAsignacion = 1m,
                dcMontoCuenta = 0m,
                dcMontoEntero = enteroDescongelado,
                dcMontoCola = colaDescongelado,
                dcMontoVag = vagDescongelado,
                dcLibrasDriver = objDescongelado.dcLibrasRecibidasPt,
                dcPesoDriver = montoDescongelado,
                strProcesosAplicables = objDescongelado.strTiposAplicables,
                intCantidadDestinos = string.IsNullOrWhiteSpace(objDescongelado.strTiposAplicables)
                    ? 0 : objDescongelado.strTiposAplicables.Split(',', StringSplitOptions.RemoveEmptyEntries).Length
            };

            if (string.IsNullOrWhiteSpace(derivado.strAgrupacionCentro) || string.IsNullOrWhiteSpace(derivado.strGrupoCentro))
                throw new InvalidOperationException("La configuración derivada de Descongelado no tiene AGRUPACION/GRUPO.");
            derivado.RecalcularTotales();
            decimal totalDerivado = Math.Round(derivado.dcMontoEntero + derivado.dcMontoCola + derivado.dcMontoVag, 4);
            if (Math.Abs(totalDerivado - montoDescongelado) > 0.01m)
                throw new InvalidOperationException($"Descuadre en reclasificación de Descongelado. Retirado: {montoDescongelado:N4}; Derivado: {totalDerivado:N4}.");
            return derivado;
        }

        private static void ReducirDistribucionPorReclasificacion(CostoProductivoCuentaDto row, decimal dcAjuste)
        {
            dcAjuste = Math.Round(dcAjuste, 4);
            if (dcAjuste <= 0m) return;
            decimal enteroOriginal = row.dcMontoEntero;
            decimal colaOriginal = row.dcMontoCola;
            decimal vagOriginal = row.dcMontoVag;
            decimal totalOriginal = Math.Round(enteroOriginal + colaOriginal + vagOriginal, 4);
            if (totalOriginal <= 0m)
                throw new InvalidOperationException($"La cuenta {row.strCuenta} del proceso {row.strEtapa} pertenece al pool 50303 pero no tiene monto distribuido.");
            if (dcAjuste > totalOriginal + 0.01m)
                throw new InvalidOperationException($"El ajuste de Descongelado ({dcAjuste:N4}) supera el monto distribuido de la cuenta {row.strCuenta} ({totalOriginal:N4}).");
            decimal nuevoTotal = Math.Round(totalOriginal - dcAjuste, 4);
            if (nuevoTotal < 0m) nuevoTotal = 0m;
            decimal nuevoEntero = enteroOriginal != 0m ? Math.Round(nuevoTotal * enteroOriginal / totalOriginal, 4) : 0m;
            decimal nuevoCola = colaOriginal != 0m ? Math.Round(nuevoTotal * colaOriginal / totalOriginal, 4) : 0m;
            decimal nuevoVag = vagOriginal != 0m ? Math.Round(nuevoTotal * vagOriginal / totalOriginal, 4) : 0m;
            decimal residuo = Math.Round(nuevoTotal - nuevoEntero - nuevoCola - nuevoVag, 4);
            if (vagOriginal != 0m) nuevoVag = Math.Round(nuevoVag + residuo, 4);
            else if (colaOriginal != 0m) nuevoCola = Math.Round(nuevoCola + residuo, 4);
            else nuevoEntero = Math.Round(nuevoEntero + residuo, 4);
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
                .Where(x => string.Equals(Normalizar(x.strPcCodigo), pc, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.intFactor > 0 || x.blEsTarifa)
                .GroupBy(x => Normalizar(x.strTipoProcesoCodigo), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First()).ToList();
            var pesosPorTipo = (objDrivers?.lstDrivers ?? new List<DriverProcesoProductoDto>())
                .Where(x => string.Equals(Normalizar(x.strPcCodigo), pc, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => Normalizar(x.strTipCodigo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.dcPeso), StringComparer.OrdinalIgnoreCase);
            var nombresPorCodigo = aplicaciones
                .Where(x => !string.IsNullOrWhiteSpace(x.strTipoProcesoCodigo))
                .ToDictionary(x => Normalizar(x.strTipoProcesoCodigo),
                    x => string.IsNullOrWhiteSpace(x.strTipoProceso) ? Normalizar(x.strTipoProcesoCodigo) : x.strTipoProceso.Trim(),
                    StringComparer.OrdinalIgnoreCase);
            var codigos = aplicaciones.Select(x => Normalizar(x.strTipoProcesoCodigo))
                .Concat(pesosPorTipo.Where(x => x.Value > 0m).Select(x => x.Key))
                .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var trabajo = codigos.Select(codigo => new
            {
                Codigo = codigo,
                Proceso = nombresPorCodigo.TryGetValue(codigo, out string? nombre) ? nombre : codigo,
                Peso = pesosPorTipo.TryGetValue(codigo, out decimal peso) ? peso : 0m
            }).Where(x => x.Peso > 0m).ToList();
            decimal pesoTotal = trabajo.Sum(x => x.Peso);
            if (pesoTotal <= 0m || dcMontoDistribuir == 0m) return new();
            var resultado = new List<CostoProductivoProcesoAplicableDetalleDto>();
            decimal acumulado = 0m;
            for (int i = 0; i < trabajo.Count; i++)
            {
                var actual = trabajo[i];
                decimal dolares = i == trabajo.Count - 1 ? Math.Round(dcMontoDistribuir - acumulado, 4) : Math.Round(dcMontoDistribuir * actual.Peso / pesoTotal, 4);
                if (i < trabajo.Count - 1) acumulado += dolares;
                if (Math.Abs(dolares) <= 0.0000001m) continue;
                resultado.Add(new CostoProductivoProcesoAplicableDetalleDto
                {
                    strCodigoProceso = actual.Codigo,
                    strProceso = actual.Proceso,
                    dcDolares = dolares
                });
            }
            return resultado;
        }

        private DescongeladoResultadoDto ConstruirDescongelado(List<MatPrimaReproceso> lstRpc, IReadOnlyCollection<ProcesoCostoAplicacionDto> lstAplicaciones)
        {
            var tarifas = lstAplicaciones
                .Where(x => string.Equals(x.strPcCodigo, "DSC", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.blEsTarifa).ToList();
            decimal tarifa = tarifas.Select(x => x.dcTarifa ?? 0m).FirstOrDefault(x => x > 0m);
            if (tarifa <= 0m) tarifa = 0.02m;
            HashSet<string> tipos = tarifas.Select(x => Normalizar(x.strTipoProcesoCodigo))
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<MatPrimaReproceso> recibidos = lstRpc
                .Where(x => string.Equals(x.strAgrupacion, "1. RECIBIDO", StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.strProClas03, "PT", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.blEsDescongelado == true)
                .Where(x => tipos.Count == 0 || tipos.Contains(Normalizar(x.strTipCod))).ToList();
            List<MatPrimaReproceso> procesados = lstRpc
                .Where(x => string.Equals(x.strAgrupacion, "2. PROCESADO", StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.strProClas03, "PT", StringComparison.OrdinalIgnoreCase) || string.Equals(x.strProClas03, "PP", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.dbLibras > 0).ToList();
            var resultado = new DescongeladoResultadoDto { dcTarifa = tarifa, strTiposAplicables = string.Join(", ", tipos.OrderBy(x => x)) };
            foreach (var grupo in recibidos.GroupBy(x => (x.intLotNumero, x.intLoteUnificado)))
            {
                decimal lbsRec = grupo.Sum(x => (decimal)x.dbLibras);
                if (lbsRec <= 0m) continue;
                resultado.intLotesElegibles++;
                resultado.dcLibrasRecibidasPt += lbsRec;
                decimal montoLote = Math.Round(lbsRec * tarifa, 6);
                List<MatPrimaReproceso> salidas = procesados
                    .Where(x => x.intLotNumero == grupo.Key.intLotNumero && x.intLoteUnificado == grupo.Key.intLoteUnificado).ToList();
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
                    decimal montoSalida = i == salidas.Count - 1 ? Math.Round(montoLote - asignadoLote, 6) : Math.Round(montoLote * (decimal)salida.dbLibras / lbsSalida, 6);
                    if (i < salidas.Count - 1) asignadoLote += montoSalida;
                    string? part = ParticionCosteo.ClasificarRpc(salida.strTipoProducto);
                    switch (part)
                    {
                        case ParticionCosteo.ENTERO: resultado.dcEntero += montoSalida; break;
                        case ParticionCosteo.COLA: resultado.dcCola += montoSalida; break;
                        case ParticionCosteo.ENTERO_VA:
                        case ParticionCosteo.COLA_VA: resultado.dcValorAgregado += montoSalida; break;
                        default: resultado.dcMontoSinAsignar += montoSalida; break;
                    }
                }
            }
            resultado.dcMontoTotal = Math.Round(resultado.dcLibrasRecibidasPt * tarifa, 6);
            resultado.dcMontoAsignado = Math.Round(resultado.dcEntero + resultado.dcCola + resultado.dcValorAgregado, 6);
            resultado.dcMontoSinAsignar = Math.Round(resultado.dcMontoTotal - resultado.dcMontoAsignado, 6);
            return resultado;
        }

        private static decimal ObtenerLibrasFrs(string pc, LiquidacionResultado item)
        {
            if (_hshProcesosPfrLibrasRecibidas.Contains(pc)) return item.dcLbsReciXRend > 0m ? item.dcLbsReciXRend : 0m;
            if (string.Equals(pc, "RET", StringComparison.OrdinalIgnoreCase))
                return item.dcLibrasRetractilado.GetValueOrDefault() > 0m ? item.dcLibrasRetractilado.GetValueOrDefault() : (decimal)item.dcLibras;
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

        private static void Acumular(List<DriverProcesoProductoDto> lst, string pc, string tip, string? particion, decimal libras, int factor)
        {
            if (string.IsNullOrWhiteSpace(particion) || libras <= 0m || factor <= 0) return;
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

        private static DistribucionMontoProductoDto DistribuirFallback(decimal monto, string cuenta)
        {
            string c = (cuenta ?? string.Empty).Trim();
            if (c.StartsWith("50301", StringComparison.OrdinalIgnoreCase))
                return new DistribucionMontoProductoDto { dcEntero = monto, strOrigen = "FALLBACK_CUENTA", blUsoDriver = false };
            if (c.StartsWith("50302", StringComparison.OrdinalIgnoreCase))
                return new DistribucionMontoProductoDto { dcCola = monto, strOrigen = "FALLBACK_CUENTA", blUsoDriver = false };
            if (c.StartsWith("50303", StringComparison.OrdinalIgnoreCase))
                return new DistribucionMontoProductoDto { dcValorAgregado = monto, strOrigen = "FALLBACK_CUENTA", blUsoDriver = false };
            return new DistribucionMontoProductoDto { strOrigen = "SIN_DRIVER", blUsoDriver = false };
        }

        private static string Normalizar(string? v) => (v ?? string.Empty).Trim();
    }

    internal static class DistribucionMontoProductoExtensions
    {
        public static DistribucionMontoProductoDto ConResiduo(this DistribucionMontoProductoDto dto, decimal monto, decimal pesoVag, decimal pesoTotal)
        {
            dto.dcValorAgregado = Math.Round(monto - dto.dcEntero - dto.dcCola, 4);
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
