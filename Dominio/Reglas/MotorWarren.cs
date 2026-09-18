using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Warren es una capa paralela al costo productivo normal.
    ///
    /// REGLAS FUNCIONALES:
    /// - Warren aplica únicamente a PFR Entero (EN) y PFR Cola (SH).
    /// - RPC/Reproceso nunca participa en base, porcentaje, ajuste ni unitarios Warren.
    /// - Valor Agregado no se toca: para PFR VA el Total Warren queda igual al Total Proceso.
    /// - dcCostTotalProc permanece intacto.
    /// - dcTotalWarren contiene el nuevo total de procesos posterior al traslado Entero -> Cola.
    /// - dcTotalCostoWarren usa la misma fórmula de dcTotalDolSum, reemplazando dcCostTotalProc
    ///   por dcTotalWarren.
    /// - El ajuste por etapa se convierte a VALOR UNITARIO para retirar de Entero y agregar a Cola.
    /// </summary>
    public class MotorWarren
    {
        private readonly ILogger _logger;

        private sealed record DefProceso(string Descripcion, bool Absorbe);

        private static readonly List<DefProceso> _procesos = new()
        {
            new("Recepcion", true),
            new("Clasificacion", true),
            new("Cajas", true),
            new("Descabezado", true),       // normalmente EN = 0, por tanto absorción = 0
            new("Retractilado", true),
            new("Tunel", true),
            new("C.D.Variables", true),
            new("C.D.Fijos", true),
            new("C.I.Variables", true),
            new("C.I.Fijos", true),
            new("C.Copacking", false),
            new("Excedente M.E.", false)
        };

        public MotorWarren(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calcula los parámetros Warren del período a partir de LiquidacionResultado PFR
        /// ya costeado por MotorProcesoParametro.
        ///
        /// El cálculo NO usa ningún valor RPC.
        /// </summary>
        public WarrenResultadoDto Calcular(
            List<LiquidacionResultado> lstPfr,
            List<ProcesoResultadoDto> lstProcesoPfr,
            decimal objetivoWarren)
        {
            if (lstPfr == null) throw new ArgumentNullException(nameof(lstPfr));
            if (lstProcesoPfr == null) throw new ArgumentNullException(nameof(lstProcesoPfr));
            if (objetivoWarren < 0) throw new ArgumentOutOfRangeException(nameof(objetivoWarren));

            var enteros = lstPfr
                .Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.ENTERO)
                .ToList();

            var colas = lstPfr
                .Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.COLA)
                .ToList();

            decimal librasColaGlobal = colas.Sum(x => (decimal)x.dcLibras);
            decimal costoProcesoColaPfr = colas.Sum(x => x.dcCostTotalProc ?? 0m);
            decimal costoColaActual = librasColaGlobal > 0m
                ? costoProcesoColaPfr / librasColaGlobal
                : 0m;

            decimal diferencia = objetivoWarren - costoColaActual;
            decimal ajusteTotal = diferencia * librasColaGlobal;

            var dictProceso = lstProcesoPfr
                .Where(x => !string.IsNullOrWhiteSpace(x.strDescripcion))
                .GroupBy(x => Normalizar(x.strDescripcion))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var detalle = new List<WarrenProcesoDto>();

            foreach (var def in _procesos)
            {
                dictProceso.TryGetValue(Normalizar(def.Descripcion), out var parametroPfr);

                // Si la etapa no está parametrizada para PFR, no se persiste Warren para ella.
                if (parametroPfr == null) continue;

                decimal montoEntero = enteros.Sum(x => ObtenerMontoProceso(x, def.Descripcion));
                decimal montoCola = colas.Sum(x => ObtenerMontoProceso(x, def.Descripcion));
                decimal lbsEntero = enteros.Sum(x => ObtenerLibrasBaseProceso(x, def.Descripcion));
                decimal lbsCola = colas.Sum(x => ObtenerLibrasBaseProceso(x, def.Descripcion));

                decimal cuEnteroOriginal = lbsEntero > 0m ? montoEntero / lbsEntero : 0m;
                decimal cuColaOriginal = lbsCola > 0m ? montoCola / lbsCola : 0m;

                detalle.Add(new WarrenProcesoDto
                {
                    intCodigo = parametroPfr.intCodigo,
                    strDescripcion = parametroPfr.strDescripcion,
                    dcLibrasEntero = lbsEntero,
                    dcLibrasCola = lbsCola,
                    dcMontoEnteroOriginal = montoEntero,
                    dcMontoColaOriginal = montoCola,
                    dcPorcentajeAbsorcion = 0m,
                    dcAjusteWarren = 0m,
                    dcMontoEnteroWarren = montoEntero,
                    dcMontoColaWarren = montoCola,
                    dcCostoUnitarioEnteroOriginal = cuEnteroOriginal,
                    dcCostoUnitarioColaOriginal = cuColaOriginal,
                    dcUnitarioExtraidoEntero = 0m,
                    dcUnitarioAgregadoCola = 0m,
                    dcCostoUnitarioEnteroWarren = cuEnteroOriginal,
                    dcCostoUnitarioColaWarren = cuColaOriginal,
                    blUsaBaseGlobalCola = false
                });
            }

            // ÚNICA base de absorción permitida: dólares PFR Entero.
            decimal baseAbsorcion = detalle
                .Where(x => EsProcesoAbsorbente(x.strDescripcion))
                .Sum(x => x.dcMontoEnteroOriginal);

            if (ajusteTotal > 0m && baseAbsorcion <= 0m)
                throw new InvalidOperationException("No existe base PFR Entero para absorber Warren.");

            if (ajusteTotal > baseAbsorcion && baseAbsorcion > 0m)
            {
                throw new InvalidOperationException(
                    $"El ajuste Warren ({ajusteTotal:N2}) supera la base PFR Entero ({baseAbsorcion:N2}).");
            }

            foreach (var item in detalle)
            {
                if (!EsProcesoAbsorbente(item.strDescripcion) ||
                    baseAbsorcion <= 0m ||
                    item.dcMontoEnteroOriginal <= 0m)
                {
                    continue;
                }

                // 1) % de participación de la etapa, usando SOLO dólares PFR Entero.
                item.dcPorcentajeAbsorcion = item.dcMontoEnteroOriginal / baseAbsorcion;

                // 2) Dólares que esta etapa debe transferir de Entero hacia Cola.
                item.dcAjusteWarren = item.dcPorcentajeAbsorcion * ajusteTotal;

                // 3) Convertir el ajuste a VALOR UNITARIO que se retira de Entero.
                item.dcUnitarioExtraidoEntero = item.dcLibrasEntero > 0m
                    ? item.dcAjusteWarren / item.dcLibrasEntero
                    : 0m;

                item.dcCostoUnitarioEnteroWarren =
                    item.dcCostoUnitarioEnteroOriginal - item.dcUnitarioExtraidoEntero;

                item.dcMontoEnteroWarren =
                    item.dcMontoEnteroOriginal - item.dcAjusteWarren;

                // 4) Convertir el mismo ajuste a VALOR UNITARIO que se agrega a Cola.
                // Si la etapa no tiene base física Cola, se reparte sobre TODAS las lbs PFR Cola.
                decimal baseCola = item.dcLibrasCola > 0m
                    ? item.dcLibrasCola
                    : librasColaGlobal;

                if (item.dcLibrasCola <= 0m && item.dcAjusteWarren != 0m && librasColaGlobal > 0m)
                {
                    item.dcLibrasCola = librasColaGlobal;
                    item.blUsaBaseGlobalCola = true;
                }

                item.dcUnitarioAgregadoCola = baseCola > 0m
                    ? item.dcAjusteWarren / baseCola
                    : 0m;

                item.dcCostoUnitarioColaWarren =
                    item.dcCostoUnitarioColaOriginal + item.dcUnitarioAgregadoCola;

                item.dcMontoColaWarren =
                    item.dcMontoColaOriginal + item.dcAjusteWarren;
            }

            return new WarrenResultadoDto
            {
                dcObjetivoWarren = objetivoWarren,
                dcCostoColaActual = costoColaActual,
                dcDiferenciaWarren = diferencia,
                dcAjusteTotal = ajusteTotal,
                dcBaseAbsorcionEntero = baseAbsorcion,
                dcLibrasCola = librasColaGlobal,
                lstDetalle = detalle
            };
        }

        /// <summary>
        /// Aplica WEN/WSH guardado a LiquidacionResultado PFR línea a línea.
        ///
        /// En lugar de mezclar montos RPC o escalar factores globales, se calcula el
        /// DELTA UNITARIO por proceso y se aplica únicamente a la base PFR de esa línea.
        /// </summary>
        public void AplicarGuardado(
            List<LiquidacionResultado> lstPfr,
            List<ProcesoResultadoDto> lstParametrosWarren)
        {
            if (lstPfr == null || !lstPfr.Any()) return;

            if (lstParametrosWarren == null || !lstParametrosWarren.Any())
            {
                InicializarSinAjuste(lstPfr);
                return;
            }

            var enteros = lstPfr
                .Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.ENTERO)
                .ToList();

            var colas = lstPfr
                .Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.COLA)
                .ToList();

            var originalEnMonto = _procesos.ToDictionary(
                x => Normalizar(x.Descripcion),
                x => enteros.Sum(liq => ObtenerMontoProceso(liq, x.Descripcion)),
                StringComparer.OrdinalIgnoreCase);

            var originalEnLbs = _procesos.ToDictionary(
                x => Normalizar(x.Descripcion),
                x => enteros.Sum(liq => ObtenerLibrasBaseProceso(liq, x.Descripcion)),
                StringComparer.OrdinalIgnoreCase);

            var originalShMonto = _procesos.ToDictionary(
                x => Normalizar(x.Descripcion),
                x => colas.Sum(liq => ObtenerMontoProceso(liq, x.Descripcion)),
                StringComparer.OrdinalIgnoreCase);

            var originalShLbs = _procesos.ToDictionary(
                x => Normalizar(x.Descripcion),
                x => colas.Sum(liq => ObtenerLibrasBaseProceso(liq, x.Descripcion)),
                StringComparer.OrdinalIgnoreCase);

            var wen = lstParametrosWarren
                .Where(x => string.Equals(x.strTipoLote, "WEN", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrWhiteSpace(x.strDescripcion))
                .GroupBy(x => Normalizar(x.strDescripcion))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var wsh = lstParametrosWarren
                .Where(x => string.Equals(x.strTipoLote, "WSH", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrWhiteSpace(x.strDescripcion))
                .GroupBy(x => Normalizar(x.strDescripcion))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var liq in lstPfr)
            {
                string? particion = ParticionCosteo.ClasificarFrs(liq.strProClas01, liq.strProClas05);
                decimal totalWarren = liq.dcCostTotalProc ?? 0m;

                if (particion == ParticionCosteo.ENTERO)
                {
                    foreach (var def in _procesos)
                    {
                        string key = Normalizar(def.Descripcion);
                        if (!wen.TryGetValue(key, out var guardado)) continue;

                        decimal lbsProceso = originalEnLbs.GetValueOrDefault(key, 0m);
                        if (lbsProceso <= 0m) continue;

                        decimal montoProceso = originalEnMonto.GetValueOrDefault(key, 0m);
                        decimal unitarioOriginal = montoProceso / lbsProceso;
                        decimal unitarioWarren = guardado.dcCostUnitario ?? unitarioOriginal;
                        decimal deltaUnitario = unitarioWarren - unitarioOriginal; // negativo en Entero

                        decimal lbsLinea = ObtenerLibrasBaseProceso(liq, def.Descripcion);
                        if (lbsLinea <= 0m) continue;

                        totalWarren += deltaUnitario * lbsLinea;
                    }
                }
                else if (particion == ParticionCosteo.COLA)
                {
                    foreach (var def in _procesos)
                    {
                        string key = Normalizar(def.Descripcion);
                        if (!wsh.TryGetValue(key, out var guardado)) continue;

                        decimal lbsProceso = originalShLbs.GetValueOrDefault(key, 0m);
                        decimal montoProceso = originalShMonto.GetValueOrDefault(key, 0m);

                        // Proceso con base Cola propia.
                        if (lbsProceso > 0m)
                        {
                            decimal unitarioOriginal = montoProceso / lbsProceso;
                            decimal unitarioWarren = guardado.dcCostUnitario ?? unitarioOriginal;
                            decimal deltaUnitario = unitarioWarren - unitarioOriginal;

                            decimal lbsLinea = ObtenerLibrasBaseProceso(liq, def.Descripcion);
                            if (lbsLinea <= 0m) continue;

                            totalWarren += deltaUnitario * lbsLinea;
                        }
                        // Proceso sin base física Cola: WSH fue guardado usando base global PFR Cola.
                        else if ((guardado.dcCostUnitario ?? 0m) != 0m && guardado.dcLibras > 0m)
                        {
                            decimal extraUnitario = guardado.dcCostUnitario ?? 0m;
                            totalWarren += extraUnitario * (decimal)liq.dcLibras;
                        }
                    }
                }
                // VA PFR: Warren no toca nada. totalWarren permanece igual a dcCostTotalProc.

                liq.dcTotalWarren = Math.Round(totalWarren, 4);
                liq.dcTotalCostoWarren = Math.Round(
                    totalWarren +
                    (liq.dcCostoTotalMatEmp ?? 0m) +
                    (decimal)(liq.dcTotalDol ?? 0d), 4);
            }
        }

        /// <summary>
        /// Si el período todavía no tiene Warren guardado, para PFR las columnas nuevas
        /// son una copia del costo normal. Esto también cubre PFR Valor Agregado sin alterarlo.
        /// </summary>
        public void InicializarSinAjuste(IEnumerable<LiquidacionResultado> lst)
        {
            foreach (var liq in lst)
            {
                liq.dcTotalWarren = liq.dcCostTotalProc ?? 0m;
                liq.dcTotalCostoWarren =
                    (liq.dcCostTotalProc ?? 0m) +
                    (liq.dcCostoTotalMatEmp ?? 0m) +
                    (decimal)(liq.dcTotalDol ?? 0d);
            }
        }

        private static bool EsProcesoAbsorbente(string descripcion)
        {
            var def = _procesos.FirstOrDefault(x =>
                string.Equals(Normalizar(x.Descripcion), Normalizar(descripcion), StringComparison.OrdinalIgnoreCase));
            return def?.Absorbe ?? false;
        }

        private static decimal ObtenerMontoProceso(LiquidacionResultado liq, string descripcion)
        {
            return Normalizar(descripcion) switch
            {
                "RECEPCION" => liq.ProcesoPrimario.dcRecepcion ?? 0m,
                "CLASIFICACION" => liq.ProcesoPrimario.dcClasificacion ?? 0m,
                "CAJAS" => liq.ProcesoPrimario.dcCajas ?? 0m,
                "DESCABEZADO" => liq.ProcesoSecundario.dcDescabezado ?? 0m,
                "RETRACTILADO" => liq.ProcesoPresentacion.dcRetractilado ?? 0m,
                "TUNEL" => liq.ProcesoCongelacion.dcTunel ?? 0m,
                "C.D.VARIABLES" => liq.ProcesoCostFijo.dcCostoVariable ?? 0m,
                "C.D.FIJOS" => liq.ProcesoCostFijo.dcCostoFijo ?? 0m,
                "C.I.VARIABLES" => liq.ProcesoCostIndirecto.dcCostoVariable ?? 0m,
                "C.I.FIJOS" => liq.ProcesoCostIndirecto.dcCostoFijo ?? 0m,
                "C.COPACKING" => liq.dcCostoCopacking ?? 0m,
                "EXCEDENTE M.E." => liq.dcExcedente ?? 0m,
                _ => 0m
            };
        }

        /// <summary>
        /// Devuelve la base física PFR utilizada por el componente de proceso de una línea.
        /// Nunca consulta ni mezcla RPC.
        /// </summary>
        private static decimal ObtenerLibrasBaseProceso(LiquidacionResultado liq, string descripcion)
        {
            decimal monto = ObtenerMontoProceso(liq, descripcion);
            if (monto == 0m) return 0m;

            if (Normalizar(descripcion) == "RETRACTILADO")
                return liq.dcLibrasRetractilado ?? 0m;

            return (decimal)liq.dcLibras;
        }

        private static string Normalizar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            return valor.Trim().ToUpperInvariant()
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U");
        }
    }
}
