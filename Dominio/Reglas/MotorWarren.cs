using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagementService.Aplicacion.DTos;
using Microsoft.Extensions.Logging;

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
            new("Logistica", true),
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
        /// DIAGNÓSTICO (solo log): detalle de un cálculo Warren (snapshot o cierre) para ver objetivo, ajuste y unitarios por proceso.
        /// </summary>
        public static void RegistrarDiagnosticoResultado(ILogger logger, string strOrigen, WarrenResultadoDto objWarren)
        {
            if (logger == null || objWarren == null) return;
            decimal dcUnitarioColaFinal = objWarren.dcLibrasCola > 0m ? (objWarren.dcCostoColaActual * objWarren.dcLibrasCola + objWarren.dcAjusteTotal) / objWarren.dcLibrasCola : 0m;
            logger.LogInformation("[DiagWarren][{Origen}] Objetivo={Objetivo} CostoColaActual={CostoColaActual} AjusteTotal={Ajuste} BaseAbsorcionEntero={Base} LibrasPfrCola={Libras} UnitarioColaFinal={UnitarioFinal}", strOrigen, objWarren.dcObjetivoWarren, objWarren.dcCostoColaActual, objWarren.dcAjusteTotal, objWarren.dcBaseAbsorcionEntero, objWarren.dcLibrasCola, dcUnitarioColaFinal);
            foreach (WarrenProcesoDto objItem in objWarren.lstDetalle)
                logger.LogInformation("[DiagWarren][{Origen}] Proceso={Proceso} LbsEN={LbsEn} LbsSH={LbsSh} MontoEN={MontoEn} MontoSH={MontoSh} %Abs={Pct} Ajuste={Ajuste} UnitExtraidoEN={UnitExt} UnitAgregadoSH={UnitAgr} UnitOrigEN={UnitOrigEn} UnitWarrenEN={UnitWarEn} UnitOrigSH={UnitOrigSh} UnitWarrenSH={UnitWarSh} BaseGlobalCola={BaseGlobal}", strOrigen, objItem.strDescripcion, objItem.dcLibrasEntero, objItem.dcLibrasCola, objItem.dcMontoEnteroOriginal, objItem.dcMontoColaOriginal, objItem.dcPorcentajeAbsorcion, objItem.dcAjusteWarren, objItem.dcUnitarioExtraidoEntero, objItem.dcUnitarioAgregadoCola, objItem.dcCostoUnitarioEnteroOriginal, objItem.dcCostoUnitarioEnteroWarren, objItem.dcCostoUnitarioColaOriginal, objItem.dcCostoUnitarioColaWarren, objItem.blUsaBaseGlobalCola);
            decimal dcAjusteDistribuido = objWarren.lstDetalle.Sum(x => x.dcAjusteWarren);
            if (Math.Abs(dcAjusteDistribuido - objWarren.dcAjusteTotal) > 0.01m || Math.Abs(dcUnitarioColaFinal - objWarren.dcObjetivoWarren) > 0.0001m)
                logger.LogWarning("[DiagWarren][{Origen}] Resultado Warren no cuadra: AjusteDistribuido={Distribuido} AjusteTotal={AjusteTotal} UnitarioColaFinal={UnitarioFinal} Objetivo={Objetivo}", strOrigen, dcAjusteDistribuido, objWarren.dcAjusteTotal, dcUnitarioColaFinal, objWarren.dcObjetivoWarren);
        }

        /// <summary>
        /// Calcula los parámetros Warren del período a partir de LiquidacionResultado PFR
        /// ya costeado por MotorProcesoParametro.
        ///
        /// El cálculo NO usa ningún valor RPC.
        /// </summary>
        [Obsolete("No usar para ParamProc actual. El cálculo del cierre debe usar MotorWarrenCierre " +
            "o MotorWarrenSnapshot, porque el costo normal de Cola incluye PFR + RPC.")]
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
        /// Aplica el monto WAD firmado sobre la base física PFR; sin WAD conserva el cálculo legado.
        /// </summary>
        public void AplicarGuardado(List<LiquidacionResultado> lstPfr, List<ProcesoResultadoDto> lstParametrosWarren)
        {
            if (lstPfr == null || lstPfr.Count == 0) return;
            var dicWad = (lstParametrosWarren ?? new List<ProcesoResultadoDto>())
                .Where(x => string.Equals(x.strTipoLote, "WAD", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrWhiteSpace(x.strDescripcion))
                .GroupBy(x => Normalizar(x.strDescripcion))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            if (dicWad.Count == 0)
            {
                AplicarGuardadoLegacy(lstPfr, lstParametrosWarren);
                RegistrarDiagnosticoConservacion(lstPfr);
                return;
            }

            var lstEnteros = lstPfr.Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.ENTERO).ToList();
            var lstColas = lstPfr.Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.COLA).ToList();
            foreach (var objLiq in lstPfr) objLiq.dcTotalWarren = objLiq.dcCostTotalProc ?? 0m;
            decimal dcSumaEntero = 0m, dcSumaCola = 0m;
            foreach (var objDef in _procesos)
            {
                if (!dicWad.TryGetValue(Normalizar(objDef.Descripcion), out var objWad)) continue;
                decimal dcAjuste = objWad.dcValor; // Se conserva el signo del monto guardado.
                if (dcAjuste == 0m) continue;
                var lstBaseEntero = lstEnteros.Where(x => ObtenerLibrasBaseProceso(x, objDef.Descripcion) > 0m).ToList();
                var lstBaseCola = lstColas.Where(x => ObtenerLibrasBaseProceso(x, objDef.Descripcion) > 0m).ToList();
                bool blBaseGlobalCola = lstBaseCola.Count == 0;
                if (blBaseGlobalCola) lstBaseCola = lstColas;
                // ANTERIOR: solo advertía y permitía aplicar un lado sin contrapartida.
                // if (lstBaseEntero.Count == 0)
                // {
                //     // ACTIVAR DESPUÉS DE REVISIÓN DE LOGS: throw new InvalidOperationException($"No existe base PFR Entero para aplicar WAD de {objDef.Descripcion} ({dcAjuste}).");
                //     _logger?.LogWarning("[DiagWarren][Aplicar] Rama=WAD Proceso={Proceso} Ajuste={Ajuste}: sin base PFR Entero.", objDef.Descripcion, dcAjuste);
                // }
                decimal dcBaseEntero = lstBaseEntero.Sum(x => ObtenerLibrasBaseProceso(x, objDef.Descripcion));
                decimal dcBaseCola = lstBaseCola.Sum(x => blBaseGlobalCola ? (decimal)x.dcLibras : ObtenerLibrasBaseProceso(x, objDef.Descripcion));
                // Validar ambas bases antes de aplicar cualquiera de los dos lados.
                if (dcBaseEntero <= 0m || dcBaseCola <= 0m)
                {
                    _logger?.LogWarning("[DiagWarren][Aplicar] WAD no aplicado Proceso={Proceso} Ajuste={Ajuste} BaseEntero={BaseEntero} BaseCola={BaseCola}", objDef.Descripcion, dcAjuste, dcBaseEntero, dcBaseCola);
                    // ACTIVAR DESPUÉS DE REVISIÓN DE LOGS: throw new InvalidOperationException($"No existen ambas bases para aplicar WAD de {objDef.Descripcion} ({dcAjuste}): BaseEntero={dcBaseEntero}, BaseCola={dcBaseCola}.");
                    continue;
                }
                decimal dcAntesEntero = lstEnteros.Sum(x => x.dcTotalWarren ?? 0m);
                decimal dcAntesCola = lstColas.Sum(x => x.dcTotalWarren ?? 0m);
                AplicarMontoExacto(lstBaseEntero, x => ObtenerLibrasBaseProceso(x, objDef.Descripcion), -dcAjuste);
                AplicarMontoExacto(lstBaseCola, x => blBaseGlobalCola ? (decimal)x.dcLibras : ObtenerLibrasBaseProceso(x, objDef.Descripcion), dcAjuste);
                decimal dcDeltaEntero = lstEnteros.Sum(x => x.dcTotalWarren ?? 0m) - dcAntesEntero;
                decimal dcDeltaCola = lstColas.Sum(x => x.dcTotalWarren ?? 0m) - dcAntesCola;
                dcSumaEntero += dcDeltaEntero;
                dcSumaCola += dcDeltaCola;
                RegistrarDiagnosticoAplicacion(objDef.Descripcion, dcAjuste, dcDeltaEntero, dcDeltaCola, blBaseGlobalCola);
            }
            foreach (var objLiq in lstPfr)
            {
                // WAD ya contiene el total exacto: no redondear por línea. VA conserva su costo de proceso.
                // LEGADO: liq.dcTotalWarren = Math.Round(totalWarren, 4); // Se conserva solo en la rama sin WAD.
                decimal dcTotalWarren = objLiq.dcTotalWarren ?? 0m;
                objLiq.dcTotalCostoWarren = Math.Round(dcTotalWarren + (objLiq.dcCostoTotalMatEmp ?? 0m) + (decimal)(objLiq.dcTotalDol ?? 0d), 4);
                objLiq.dcCostoTotXLibraWarren = objLiq.ObtenerCostoXLibraParaReproceso();
            }
            _logger?.LogInformation("[DiagWarren][Aplicar] Rama=WAD TOTAL EN={SumaE} SH={SumaS} Neto={Neto}", dcSumaEntero, dcSumaCola, dcSumaEntero + dcSumaCola);
            RegistrarDiagnosticoConservacion(lstPfr);
        }

        private static void AplicarMontoExacto(List<LiquidacionResultado> lstLineas, Func<LiquidacionResultado, decimal> fnObtenerPeso, decimal dcMontoTotal)
        {
            // ANTERIOR: salidas silenciosas; el llamador ahora valida ambas bases antes del reparto.
            // if (lstLineas.Count == 0) return;
            decimal dcPesoTotal = lstLineas.Sum(fnObtenerPeso);
            // if (dcPesoTotal <= 0m) return;
            if (dcPesoTotal <= 0m) throw new InvalidOperationException("AplicarMontoExacto requiere una base con peso total positivo.");
            decimal dcMontoAplicado = 0m;
            for (int intIndice = 0; intIndice < lstLineas.Count; intIndice++)
            {
                var objLiq = lstLineas[intIndice];
                // La última línea absorbe el residuo decimal del reparto.
                decimal dcMonto = intIndice == lstLineas.Count - 1 ? dcMontoTotal - dcMontoAplicado : dcMontoTotal * fnObtenerPeso(objLiq) / dcPesoTotal;
                objLiq.dcTotalWarren = (objLiq.dcTotalWarren ?? 0m) + dcMonto;
                dcMontoAplicado += dcMonto;
            }
        }

        private void RegistrarDiagnosticoAplicacion(string strProceso, decimal dcAjuste, decimal dcDeltaEntero, decimal dcDeltaCola, bool blBaseGlobalCola)
        {
            _logger?.LogInformation("[DiagWarren][Aplicar] Rama=WAD Proceso={Proceso} Ajuste={Ajuste} EN: delta={DeltaE} | SH: BaseGlobalCola={BaseGlobal} delta={DeltaS} | Neto={Neto}", strProceso, dcAjuste, dcDeltaEntero, blBaseGlobalCola, dcDeltaCola, dcDeltaEntero + dcDeltaCola);
        }

        private void RegistrarDiagnosticoConservacion(List<LiquidacionResultado> lstPfr)
        {
            decimal dcDeltaGlobal = lstPfr.Sum(x => (x.dcTotalWarren ?? 0m) - (x.dcCostTotalProc ?? 0m));
            if (Math.Abs(dcDeltaGlobal) > 0.01m)
            {
                // ACTIVAR DESPUÉS DE REVISIÓN DE LOGS: throw new InvalidOperationException($"Warren no conserva: Delta={dcDeltaGlobal}.");
                _logger?.LogWarning("[DiagWarren][Aplicar] Warren no conserva: Delta={Delta}", dcDeltaGlobal);
            }
        }

        /// <summary>
        /// Aplica WEN/WSH guardado a LiquidacionResultado PFR línea a línea.
        ///
        /// En lugar de mezclar montos RPC o escalar factores globales, se calcula el
        /// DELTA UNITARIO por proceso y se aplica únicamente a la base PFR de esa línea.
        /// </summary>
        // LEGADO: algoritmo original sin cambios para períodos que no tienen WAD.
        private void AplicarGuardadoLegacy(
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

            RegistrarDiagnosticoAplicacion(colas, originalEnMonto, originalEnLbs, originalShMonto, originalShLbs, wen, wsh);

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
                liq.dcCostoTotXLibraWarren = liq.ObtenerCostoXLibraParaReproceso();
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
                liq.dcCostoTotXLibraWarren = liq.ObtenerCostoXLibraParaReproceso();
            }
        }

        private void RegistrarDiagnosticoAplicacion(
            List<LiquidacionResultado> lstColas,
            Dictionary<string, decimal> dicOriginalEnMonto, Dictionary<string, decimal> dicOriginalEnLbs,
            Dictionary<string, decimal> dicOriginalShMonto, Dictionary<string, decimal> dicOriginalShLbs,
            Dictionary<string, ProcesoResultadoDto> dicWen, Dictionary<string, ProcesoResultadoDto> dicWsh)
        {
            if (_logger == null || (dicWen.Count == 0 && dicWsh.Count == 0)) return;
            decimal dcLibrasColaGlobal = lstColas.Sum(x => (decimal)x.dcLibras);
            decimal dcSumaE = 0m, dcSumaS = 0m;
            var lstNetos = new List<(string strProceso, decimal dcNeto)>();
            foreach (var objDef in _procesos)
            {
                string strKey = Normalizar(objDef.Descripcion);
                dicWen.TryGetValue(strKey, out var objWen);
                dicWsh.TryGetValue(strKey, out var objWsh);
                decimal dcLbsE = dicOriginalEnLbs.GetValueOrDefault(strKey, 0m);
                decimal dcMontoE = dicOriginalEnMonto.GetValueOrDefault(strKey, 0m);
                decimal dcLbsS = dicOriginalShLbs.GetValueOrDefault(strKey, 0m);
                decimal dcMontoS = dicOriginalShMonto.GetValueOrDefault(strKey, 0m);
                decimal dcUnitOrigE = dcLbsE > 0m ? dcMontoE / dcLbsE : 0m;
                decimal dcUnitOrigS = dcLbsS > 0m ? dcMontoS / dcLbsS : 0m;
                decimal dcUnitWen = objWen?.dcCostUnitario ?? dcUnitOrigE;
                decimal dcUnitWsh = objWsh?.dcCostUnitario ?? dcUnitOrigS;
                decimal dcDeltaE = objWen != null && dcLbsE > 0m ? (dcUnitWen - dcUnitOrigE) * dcLbsE : 0m;
                decimal dcDeltaS = 0m;
                string strRama = "SIN_AJUSTE";
                if (objWsh != null)
                {
                    if (dcLbsS > 0m)
                    {
                        strRama = "BASE_PROPIA";
                        dcDeltaS = (dcUnitWsh - dcUnitOrigS) * dcLbsS;
                    }
                    else if ((objWsh.dcCostUnitario ?? 0m) != 0m && objWsh.dcLibras > 0m)
                    {
                        strRama = "BASE_GLOBAL";
                        dcDeltaS = (objWsh.dcCostUnitario ?? 0m) * dcLibrasColaGlobal;
                    }
                }
                decimal dcNeto = dcDeltaE + dcDeltaS;
                dcSumaE += dcDeltaE;
                dcSumaS += dcDeltaS;
                lstNetos.Add((objDef.Descripcion, dcNeto));
                _logger.LogInformation("[DiagWarren][Aplicar] Proceso={Proceso} EN: lbs={LbsE} monto={MontoE} unitOrig={UnitOrigE} unitWEN={UnitWen} paramLbs={ParamLbsE} delta={DeltaE} | SH: rama={Rama} lbs={LbsS} monto={MontoS} unitOrig={UnitOrigS} unitWSH={UnitWsh} paramLbs={ParamLbsS} delta={DeltaS} | Neto={Neto}", objDef.Descripcion, dcLbsE, dcMontoE, dcUnitOrigE, dcUnitWen, objWen?.dcLibras ?? 0m, dcDeltaE, strRama, dcLbsS, dcMontoS, dcUnitOrigS, dcUnitWsh, objWsh?.dcLibras ?? 0m, dcDeltaS, dcNeto);
            }
            decimal dcNetoTotal = dcSumaE + dcSumaS;
            _logger.LogInformation("[DiagWarren][Aplicar] TOTAL EN={SumaE} SH={SumaS} Neto={Neto}", dcSumaE, dcSumaS, dcNetoTotal);
            if (Math.Abs(dcNetoTotal) > 0.01m)
                _logger.LogWarning("[DiagWarren][Aplicar] Warren no conserva: Neto={Neto}. Procesos con |Neto| > 0.01: {Lista}", dcNetoTotal, string.Join(", ", lstNetos.Where(x => Math.Abs(x.dcNeto) > 0.01m).OrderByDescending(x => Math.Abs(x.dcNeto)).Select(x => $"{x.strProceso}={x.dcNeto}")));
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
                "LOGISTICA" => liq.ProcesoPrimario.dcLogistica ?? 0m,
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
