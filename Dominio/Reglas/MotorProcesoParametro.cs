using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.EF_Core;
using DocumentFormat.OpenXml.Vml;
using System.Collections.Concurrent;

namespace CostManagement.Dominio.Reglas
{
    public class MotorProcesoParametro
    {
        private static readonly HashSet<string> _lstlbsProcPrim = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DE", "R6", "R7", "UNI" };
        private static readonly HashSet<string> _lstlbsProcCostIndDic = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EZP", "PYDTO", "PYD", "P3", "ENTER", "EZ", "P4", "VF", "PYD1", "BD", "EPP", "PYDS", "PYD4" };
        private readonly ILogger _objLogger;

        private static readonly List<string> _lstProcPrimTiplot = new() { "DE", "R6", "R7", "UNI" };
        private static readonly List<string> _lstNotProcSecun = new() { "DE" };
        private static readonly List<string> _lstNotPresen = new() { "EN1", "SH2" };
        private static readonly HashSet<string> _lstCodTarifaProceso = new(StringComparer.OrdinalIgnoreCase) { "CAM", "R1", "R2", "R3", "20", "RS", "VR", "VE", "RCC", "RCC2", "RCB", "ECH", "RERE", "REET" };

        private static readonly HashSet<string> _hsCodEtiqueta = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CAM", "VE" };
        private static readonly HashSet<string> _hsCodReempaque = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "R3", "VR" };
        private static readonly List<string> _lstNotCostDirec = new()
        {
            // Reprocesos
            "R1","R2","R3","RVVL","CDI","LB04","DV","RLL","RPY","REC","RS",
            // Brine
            "B1","B3",
            // Diferencia Pesos
            "BP","BDP",
            // VAG eti – ree
            "VE","VR",
            "CAM"
        };

        private static readonly List<string> _lstNotCostInd = new()
        {
            // Reprocesos
            "R1","R2","R3","RVVL","CDI","LB04","DV","RLL","RPY","REC","RS",
            // Brine
            "B1","B3",
            // Diferencia Pesos
            "BP","BDP",
            // VAG eti – ree
            "VE","VR",
            "CAM"
        };

        private static readonly List<string> _lstNotCostConge = new()
        {
            "CAM","RLL","R1","CDI","R2","REC","LB04","RPY","R3",
            "RS","DV","RVVL","BDP","VE","VR"
        };


        public MotorProcesoParametro(ILogger logger)
        {
            _objLogger = logger;
        }

        #region  Asignacion Procesos
        /// <summary>
        /// Procesa y asigna las libras para Fresco basándose en múltiples fuentes de datos.
        /// </summary>
        public void AsignarCostoProcesoFrs(DataProcesoParam objDataProceso)
        {
            ProcesoResultadoDto objMatEmpaqueFrs;
            try
            {
                var dicTotales = new Dictionary<string, decimal>();

                // 1. Recepción y Productos Terminados
                decimal sumRloNetas = (decimal)objDataProceso.lstLiqFresco.Sum(x => x.dcLibras);
                decimal sumRloProCabCol = (decimal)objDataProceso.lstLiqFresco.Sum(x => x.dcLibras);
                decimal dcSumCopacking = (decimal)objDataProceso.lstLiqFresco.Where(x => x.intCodCopacking > 0 && x.strPlanta != "SONGA").Sum(x => x.dcLibras);
                decimal dcSumMatEmpaque = (decimal)objDataProceso.lstLiqFresco.Sum(x => x.dcCostoTotalMatEmp ?? 0);
                objMatEmpaqueFrs = new ProcesoResultadoDto
                {
                    intCodigo = objDataProceso.lstProcesoFrs
                .LastOrDefault(obj => obj.intCodigo > 0)?.intCodigo ?? 0,
                    intCodDet = 0,
                    strEstado = "AC",
                    strDescripcion = "Material Empaque",
                    blEditable = false,
                    strTipoLote = "RPC",
                    dcValor = dcSumMatEmpaque,
                    dcLibras = sumRloNetas,
                    dcCostUnitario = dcSumMatEmpaque != 0 ? dcSumMatEmpaque / sumRloNetas : 0,
                };
                objDataProceso.lstProcesoFrs.Add(objMatEmpaqueFrs);

                dicTotales["Recepcion"] = sumRloNetas;
                dicTotales["Excedente M.E."] = sumRloProCabCol;
                // 2. Unificación de Productos Terminados y Descuentos (Llaves fijas)
                foreach (var item in objDataProceso.lstProdTerm.Concat(objDataProceso.lstDescTotFresco))
                {
                    dicTotales[item.Trim()] = sumRloProCabCol;
                }

                // 3. Iteración Unificada de lstLiqFresco (Cálculo por condiciones)
                // Asumimos que lstLiqFresco contiene los datos necesarios para clasificar los costos
                foreach (var item in objDataProceso.lstLiqFresco)
                {


                    //if (item.strTipPro == "IQF")
                    //{
                    //    // Validación de seguridad para Contains
                    //    bool esTunelOBrine = !objDataProceso.lstCongTunel.Contains(item.intProCongela) && !item.blBodEsBrine;
                    //    decimal dcLibrasIqf = !esTunelOBrine ? (decimal)item.dcLibras : 0;

                    //    ActualizarDiccionario(dicTotales, "IQF", dcLibrasIqf);
                    //}

                    // --- Lógica de Estilos (Excepto Descabezado) ---
                    if (!string.IsNullOrEmpty(item.strProClas01) && item.strProClas01 == "SC")
                    {
                        ActualizarDiccionario(dicTotales, "Descabezado", (decimal)item.dcLibras);
                    }

                    // --- Lógica de Congelamiento (Tunel / Brine) ---
                    //!lstCongTunel.Contains(liq.intProCongela) && !liq.blBodEsBrine
                    if (objDataProceso.lstCongTunel.Contains(item.intProCongela) && item.strProClas03 == "PT")
                    {
                        ActualizarDiccionario(dicTotales, "Tunel", (decimal)item.dcLibras);
                    }

                    if (item.dcLibrasRetractilado != null)
                    {
                        ActualizarDiccionario(dicTotales, "Retractilado", (decimal)item.dcLibrasRetractilado);
                    }
                    //else if (item.blBodEsBrine)
                    //{
                    //    ActualizarDiccionario(dicTotales, "Brine", (decimal)item.dcLibras);
                    //}

                }

                // 6. Copacking
                ActualizarDiccionario(dicTotales, "C.Copacking",dcSumCopacking );

                // Mapeo Final a los DTOs
                FinalizarAsignacion(objDataProceso.lstProcesoFrs, dicTotales,null);
            }
            catch (Exception objExcep)
            {
                _objLogger.LogInformation("[MotorProcesoParametro].[AsignarCostoProcesoFrs] Error {error}", objExcep.Message);

            }
        }
        //public void AsignarCostoProcesoFrs(DataProcesoParam objDataProceso)
        //{
        //    var dicTotales = new Dictionary<string, decimal>();

        //    // 1. Recepción y Productos Terminados
        //    decimal sumRloNetas = objDataProceso.lstLibrasProduccion.Sum(obj => obj.dcRloNetas);
        //    decimal sumRloProCabCol = objDataProceso.lstLibrasProduccion.Sum(obj => obj.dcRloProCab + obj.dcRloProCol);

        //    dicTotales["Recepcion"] = sumRloNetas;
        //    dicTotales["Excedente M.E."] = sumRloProCabCol;
        //    foreach (var item in objDataProceso.lstProdTerm.Concat(objDataProceso.lstDescTotFresco))
        //    {
        //        dicTotales[item.Trim()] = sumRloProCabCol;
        //    }

        //    // 2. Procesos de Tratado (Cocido/Hidratación) e IQF
        //    foreach (var f in objDataProceso.lstLotOpcon)
        //    {
        //        string catTratado = !string.IsNullOrEmpty(f.strRecNombre) && f.strRecTipo == "COC" ? "Cocido" :
        //                            !string.IsNullOrEmpty(f.strRecNombre) && f.strTratado == "Tratado" ? "Hidratacion" : "OTROS";

        //        ActualizarDiccionario(dicTotales, catTratado, f.dcLotValAgr);

        //        if (f.strCongela == "IQF")
        //        {
        //            decimal valorIqf = f.strLotTipo == "VA" ? f.dcLotValAgr : f.dcLotProces;
        //            ActualizarDiccionario(dicTotales, "IQF", valorIqf);
        //        }
        //    }

        //    // 3. Descabezado
        //    decimal totalDescabezado = objDataProceso.lstLibrasProduccion.Sum(obj => obj.dcRloProCol) -
        //                               objDataProceso.lstLibrasProduccion.Where(obj => obj.intRloProcesodest == 2).Sum(obj => obj.dcRloEnviad);
        //    ActualizarDiccionario(dicTotales, "Descabezado", totalDescabezado);

        //    // 4. Estilos (Excepto Descabezado)
        //    foreach (var f in objDataProceso.lstResumenEstiloLbs.Where(x => x.strEstilo != "Descabezado"))
        //    {
        //        ActualizarDiccionario(dicTotales, f.strEstilo.Trim(), (decimal)f.dcLibrasDecoradas);
        //    }

        //    // 5. Congelamiento (Tunel / Brine)
        //    foreach (var f in objDataProceso.lstFrsConge)
        //    {
        //        if (objDataProceso.lstCongTunel.Contains(f.intProCongel) && f.strProClas03 == "PT")
        //            ActualizarDiccionario(dicTotales, "Tunel", f.dcLibras);
        //        else if (f.blBodEsBrine)
        //            ActualizarDiccionario(dicTotales, "Brine", f.dcLibras);
        //    }

        //    // 6. Copacking
        //    ActualizarDiccionario(dicTotales, "C.Copacking", objDataProceso.lstCopackingLbs.Sum(x => x.dcLotProces));

        //    // Mapeo Final a los DTOs
        //    FinalizarAsignacion(objDataProceso.lstProcesoFrs, dicTotales);
        //}
        /// <summary>
        /// Procesa y asigna las libras para Reproceso.
        /// </summary>
        public void AsignarCostoProcesoRpc(DataProcesoParam objDataProceso)
        {
            ProcesoResultadoDto objMatEmpaqueRpc;
            try
            {
            var dicTotales = new Dictionary<string, decimal>();
            List<string> lstNotCostConge = new List<string>() { /*"BP",*/
                    "CAM","RLL","R1","CDI","R2","REC","LB04","RPY","R3","RS","DV","RVVL","BDP","VE","VR"
                };
            // 1. Costo Proceso Primario (Recepción y Productos Terminados)
            decimal dcCostProcPrim = (decimal)objDataProceso.lstLiqRepro
                .Where(x => x.strLotTipo == "RE" && _lstlbsProcPrim.Contains(x.strTipCod))
                .Sum(obj => obj.dbLibras);
            var dclbsValAgg = (decimal)objDataProceso.lstLiqRepro.Where(x => _lstlbsProcCostIndDic.Contains(x.strTipCod)).Sum(obj => obj.dbLibras);
                decimal dcSumMatEmpaque = (decimal)objDataProceso.lstLiqRepro.Sum(x => x.dcCostoTotalMatEmp ?? 0);
                objMatEmpaqueRpc = new ProcesoResultadoDto
                {
                    intCodigo = objDataProceso.lstProcesoRpc
                .LastOrDefault(obj => obj.intCodigo > 0)?.intCodigo ?? 0,
                    intCodDet = 0,
                    strEstado = "AC",
                    strDescripcion = "Material Empaque",
                    blEditable = false,
                    strTipoLote = "RPC",
                    dcValor = dcSumMatEmpaque,
                    dcLibras = dcCostProcPrim,
                    dcCostUnitario = dcSumMatEmpaque != 0 ? dcSumMatEmpaque / dcCostProcPrim : 0,
                };
                objDataProceso.lstProcesoRpc.Add(objMatEmpaqueRpc);

            dicTotales["Recepcion"] = dcCostProcPrim;
            dicTotales["Excedente M.E."] = dcCostProcPrim;
            foreach (var item in objDataProceso.lstProdTerm)
            {
                dicTotales[item.Trim()] = dcCostProcPrim;
            }
            foreach (var item in objDataProceso.lstDescTotFresco)
            {
                dicTotales[item.Trim()] = dclbsValAgg + dcCostProcPrim;
            }

            // 2. Filtros específicos de Reproceso
                dicTotales["Cocido"] = (decimal)objDataProceso.lstLiqRepro.Where(x => !string.IsNullOrEmpty(x.strRecNombre) && x.strRecTipo == "COC" && x.strLotTipo == "VA").Sum(obj => obj.dbLibras);
                dicTotales["Hidratacion"] = (decimal)objDataProceso.lstLiqRepro.Where(x => !string.IsNullOrEmpty(x.strRecNombre) && x.strRecTipo != "COC" && x.strLotTipo == "VA").Sum(obj => obj.dbLibras);
                dicTotales["Retractilado"] = (decimal)objDataProceso.lstLiqRepro.Where(x => x.blRetractilado).Sum(obj => obj.dcLibrasRetractilado);
                dicTotales["Pelado"] = (decimal)objDataProceso.lstLiqRepro.Where(obj => obj.blPelado).Sum(obj => obj.dcLibrasPelado);
                dicTotales["Decorado"] = (decimal)objDataProceso.lstLiqRepro.Where(obj => obj.blDecorado).Sum(obj => obj.dbLibras);
                dicTotales["Descabezado"] = (decimal)objDataProceso.lstLiqRepro.Where(obj => obj.blEsDescabezado).Sum(obj => obj.dbLibras);
                dicTotales["IQF"] = (decimal)objDataProceso.lstLiqRepro.Where(x => x.strCongeProduc.Trim().Equals("IQF") && !lstNotCostConge.Contains(x.strTipCod)).Sum(obj => obj.dbLibras);
                dicTotales["Brine"] = (decimal)objDataProceso.lstLiqRepro.Where(x => x.strCongeProduc.Trim().Equals("BRINE") && !lstNotCostConge.Contains(x.strTipCod)).Sum(obj => obj.dbLibras);
                dicTotales["Tunel"] = (decimal)objDataProceso.lstLiqRepro.Where(x => (x.strCongeProduc.Trim().Equals("BLOCK") || x.strCongeProduc.Trim().Equals("SEMI IQF")) && !lstNotCostConge.Contains(x.strTipCod)).Sum(obj => obj.dbLibras);
                dicTotales["C.Copacking"] = (decimal)objDataProceso.lstLiqRepro.Where(x => x.intCodCopacking != 0).Sum(obj => obj.dbLibras);
            

            // Mapeo Final
            FinalizarAsignacion(objDataProceso.lstProcesoRpc, dicTotales, objDataProceso.dcCostoHidraReproceso);
        }
            catch (Exception ex)
            {
                _objLogger.LogError("[MotorProcesoParametro].[AsignarCostoProcesoRpc] Error {error}", ex.Message);
                throw;
            }
        }

        public void AsignarCostoProcesoTarifa(DataProcesoParam objDataProceso)
        {
            // Usamos StringComparer.OrdinalIgnoreCase para evitar problemas de mayúsculas/minúsculas
            var dicTotales = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // 1. Construir el puente traductor (Código -> Descripción)
            var dictPuente = ProcesoResultadoDto.ConstruirDiccionarioPuente(objDataProceso.lstProcesoTarifa);
            var lstLiqReproFiltrada = objDataProceso.lstLiqRepro.Where(l => objDataProceso.lstProcesoTarifa.Any(x => x.strCodTip == l.strTipCod) && !string.IsNullOrWhiteSpace(l.strTipCod)).ToList();
            foreach (var liq in lstLiqReproFiltrada)
            {
                if (dictPuente.TryGetValue(liq.strTipDescri, out string descriProceso))
                {
                    // Encontramos la descripción (ej. "ETIQUETEOS"). Sumamos las libras.
                    ActualizarDiccionario(dicTotales, liq.strTipDescri, (decimal)liq.dbLibras);
                }
            }

            FinalizarAsignacion(objDataProceso.lstProcesoTarifa, dicTotales, null);
        }

        public void SumarizarLibrasNoEditable(DataProcesoParam objDataProceso)
        {
            var todasLasEtiquetasEditables = objDataProceso.lstProcesoFrs
                .Concat(objDataProceso.lstProcesoRpc)
                .Where(x => !x.blEditable)
                .ToList();

            if (!todasLasEtiquetasEditables.Any()) return;

            var dicTotalesSumarizados = todasLasEtiquetasEditables
                .GroupBy(x => x.strDescripcion.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => (decimal)x.dcLibras)
                );
            ActualizarListaConTotales(objDataProceso.lstProcesoFrs, dicTotalesSumarizados);
            ActualizarListaConTotales(objDataProceso.lstProcesoRpc, dicTotalesSumarizados);
        }
        #endregion

        #region Asignacion Costos Proceso Reproceso
        public void AsignarCostosProcesosRepro(DataProcesoParam objDataProceso)
        {
            List<MatPrimaReproceso> lstLiqRepro;
            CostosUnitarios objCostUnit;
            ConcurrentDictionary<string, decimal> dictCosto, dicTarifasPorCodigo;
            ConcurrentDictionary<string, string> dicTiplot;
            try
            {
                if (objDataProceso.lstProcesoRpc == null) throw new ArgumentNullException(nameof(objDataProceso.lstProcesoRpc));
                if (objDataProceso.lstLiqRepro == null) throw new ArgumentNullException(nameof(objDataProceso.lstLiqRepro));
                if (objDataProceso.lstProcesoTarifa == null) throw new ArgumentNullException(nameof(objDataProceso.lstProcesoTarifa));
                dictCosto = ProcesoResultadoDto.ConstruirDictProc(objDataProceso.lstProcesoRpc);
                dicTiplot = ProcesoResultadoDto.ConstruirDiccionarioPuente(objDataProceso.lstProcesoTarifa);
                objCostUnit = CostosUnitarios.ExtraerCostosUnitarios(dictCosto);
                dicTarifasPorCodigo = ProcesoResultadoDto.ConstruirDictTarifasPorCodigo(objDataProceso.lstProcesoTarifa, dicTiplot);
                lstLiqRepro = objDataProceso.lstLiqRepro.Where(l => l.strAgrupacion == "2. PROCESADO").ToList();
                foreach (var objLiq in lstLiqRepro)
                    AplicarCostosALiquidacion(objLiq, objCostUnit, dicTarifasPorCodigo);
            }
            catch (Exception ex)
            {
                _objLogger.LogError(
                    "[ProcesoParametro].[ObtenerCostosProcesosMatPrimPFR] : {Mensaje}",
                    ex.Message);
                throw;
            }
        }
        private static void AplicarCostosALiquidacion(
                                        MatPrimaReproceso liq,
                                        CostosUnitarios c,
                                        ConcurrentDictionary<string, decimal> dicTarifasPorCodigo)
        {
            AplicarCostoTarifa(liq, dicTarifasPorCodigo);
            AplicarProcesoPrimario(liq, c);
            AplicarProcesoPresentacion(liq, c);
            AplicarProcesoCongelacion(liq, c);
            AplicarProcesoSecundario(liq, c);
            AplicarCostosDirectos(liq, c);
            AplicarCostosIndirectos(liq, c);
            AplicarCopacking(liq, c);
            liq.dcCostTotalProc = CalcularCostoTotal(liq);
        }

        private static void AplicarProcesoPrimario(MatPrimaReproceso objLiq, CostosUnitarios c)
        {
            if (!_lstProcPrimTiplot.Contains(objLiq.strTipCod)) return;

            decimal libras = (decimal)objLiq.dbLibras;
            objLiq.ProcesoPrimario.dcRecepcion = Math.Round(libras * c.dcRecepcion, 4);
            objLiq.ProcesoPrimario.dcClasificacion = Math.Round(libras * c.dcClasificacion, 4);
            objLiq.ProcesoPrimario.dcCajas = Math.Round(libras * c.dcCajas, 4);
            objLiq.dcExcedente = Math.Round((decimal)objLiq.dbLibras * c.dcExcedente, 2);

            objLiq.dcRecepcion = c.dcRecepcion;
            objLiq.dcClasificacion = c.dcClasificacion;
            objLiq.dcCajas = c.dcCajas;
        }

        private static void AplicarProcesoPresentacion(MatPrimaReproceso liq, CostosUnitarios c)
        {
            if (_lstNotPresen.Contains(liq.strTipCod)) return;

            liq.ProcesoPresentacion.dcDecorado =
                liq.blDecorado ? Math.Round((decimal)liq.dbLibras * c.dcDecorado, 4) : 0;
            liq.ProcesoPresentacion.dcRetractilado =
                liq.blRetractilado ? Math.Round((decimal)liq.dcLibrasRetractilado * c.dcRetractilado, 4) : 0;

            liq.dcDecorado = liq.blDecorado ? c.dcDecorado : 0;
            liq.dcRetractilado = liq.blRetractilado ? c.dcRetractilado : 0;
        }

        private static void AplicarProcesoCongelacion(MatPrimaReproceso liq, CostosUnitarios c)
        {
            if (_lstNotCostConge.Contains(liq.strTipCod)) return;

            string conge = liq.strCongeProduc.Trim();
            decimal libras = (decimal)liq.dbLibras;

            liq.ProcesoCongelacion.dcBrine = conge == "BRINE"
                ? Math.Round(libras * c.dcBrine, 4) : 0;
            liq.dcBrine = conge == "BRINE" ? c.dcBrine : 0;

            liq.ProcesoCongelacion.dcTunel = conge is "BLOCK" or "SEMI IQF"
                ? Math.Round(libras * c.dcTunel, 4) : 0;
            liq.dcTunel = conge is "BLOCK" or "SEMI IQF" ? c.dcTunel : 0;

            liq.ProcesoCongelacion.dcIQF = conge == "IQF"
                ? Math.Round(libras * c.dcIQF, 4) : 0;
            liq.dcIQF = conge == "IQF" ? c.dcIQF : 0;
        }

        private static void AplicarProcesoSecundario(MatPrimaReproceso liq, CostosUnitarios c)
        {
            decimal libras = (decimal)liq.dbLibras;

            // Pelado
            liq.ProcesoSecundario.dcPelado = liq.blPelado
                ? Math.Round((decimal)liq.dcLibrasPelado * c.dcPelado, 4) : 0;
            liq.dcPelado = liq.blPelado ? c.dcPelado : 0;

            // Hidratación (sal + hidra)
            //decimal valorSal = ((liq.dcCthSallbs ?? 0m) * (liq.dcRecPorSal ?? 0m)) / 2.2046m;
            //decimal valorHidra = (liq.dcCthHidlbs ?? 0m) * (liq.dcValorHidra ?? 0m);
            //decimal costHidraSal = valorSal * (liq.dcValorSal ?? 0m);
            //decimal totalHidra = costHidraSal + valorHidra;
            decimal dcValorHidra = (decimal)liq.dbLibras * c.dcHidratacion ;

            bool tieneReceta = !string.IsNullOrEmpty(liq.strRecNombre) && liq.strRecTipo != "COC" && liq.strLotTipo == "VA";
            liq.ProcesoSecundario.dcHidratacion = tieneReceta ? Math.Round(dcValorHidra, 4) : 0;
            liq.dcHidratacion = tieneReceta ? dcValorHidra : 0;
            // Descabezado
            liq.ProcesoSecundario.dcDescabezado = liq.blEsDescabezado
                ? Math.Round(libras * c.dcDescabezado, 4) : 0;
            liq.dcDescabezado = liq.blEsDescabezado ? c.dcDescabezado : 0;

            // Cocido
            bool esCocido = !string.IsNullOrEmpty(liq.strRecTipo) && liq.strRecTipo == "COC";
            liq.ProcesoSecundario.dcCocido = esCocido ? Math.Round(libras * c.dcCocido, 4) : 0;
            liq.dcCocido = esCocido ? Math.Round(libras * c.dcCocido, 4) : 0;
        }

        private static void AplicarCostosDirectos(MatPrimaReproceso objLiq, CostosUnitarios c)
        {
            bool blAplica = _lstlbsProcPrim.Contains(objLiq.strTipCod) || _lstlbsProcCostIndDic.Contains(objLiq.strTipCod);
            //objLiq.strLotTipo == "RE" && _lstlbsProcPrim.Contains(objLiq.strTipCod) ||
            //        objLiq.strLotTipo == "VA" && String.IsNullOrEmpty(objLiq.strRecNombre);
            //(!_lstNotCostDirec.Contains(liq.strTipCod)
            //           && liq.strLotTipo == "VA")
            //          || _lstProcPrimTiplot.Contains(liq.strTipCod);

            if (!blAplica) return;

            decimal libras = (decimal)objLiq.dbLibras;
            objLiq.ProcesoCostFijo.dcCostoVariable = Math.Round(libras * c.dcCostDirectoVar, 4);
            objLiq.ProcesoCostFijo.dcCostoFijo = Math.Round(libras * c.dcCostDirectoFij, 4);
            objLiq.dcCostFijVaria = c.dcCostDirectoVar;
            objLiq.dcCostFijFijo = c.dcCostDirectoFij;
        }

        private static void AplicarCostosIndirectos(MatPrimaReproceso objLiq, CostosUnitarios objCostUni)
        {

            bool blAplica = _lstlbsProcPrim.Contains(objLiq.strTipCod) || _lstlbsProcCostIndDic.Contains(objLiq.strTipCod);
            //objLiq.strLotTipo == "RE" && _lstlbsProcPrim.Contains(objLiq.strTipCod) ||
            //        objLiq.strLotTipo == "VA" && String.IsNullOrEmpty(objLiq.strRecNombre);
            //(!_lstNotCostDirec.Contains(liq.strTipCod)
            //           && liq.strLotTipo == "VA")
            //          || _lstProcPrimTiplot.Contains(liq.strTipCod);

            if (!blAplica) return;

            decimal dcLibras = (decimal)objLiq.dbLibras;
            objLiq.ProcesoCostIndirecto.dcCostoFijo = Math.Round(dcLibras * objCostUni.dcCostIndirVar, 4);
            objLiq.ProcesoCostIndirecto.dcCostoVariable = Math.Round(dcLibras * objCostUni.dcCostIndirFij, 4);
            objLiq.dcCostIndirFijo = objCostUni.dcCostIndirVar;
            objLiq.dcCostIndirVaria = objCostUni.dcCostIndirFij;
        }

        private static void AplicarCopacking(MatPrimaReproceso liq, CostosUnitarios c)
        {
            liq.dcCostoCopacking = liq.intCodCopacking != 0
                ? Math.Round((decimal)liq.dbLibras * c.dcCopacking, 4)
                : 0;
        }

        private static void AplicarCostoTarifa(MatPrimaReproceso objLiq, ConcurrentDictionary<string, decimal> dicTarifasPorCodigo)
        {
            objLiq.dcTarifaProc = Math.Round((decimal)objLiq.dbLibras * dicTarifasPorCodigo.GetValueOrDefault(objLiq.strTipCod, 0m), 2);
        }


        private static decimal CalcularCostoTotal(MatPrimaReproceso liq) =>
            // Proceso Primario
            (liq.ProcesoPrimario.dcRecepcion ?? 0) +
            (liq.ProcesoPrimario.dcClasificacion ?? 0) +
            (liq.ProcesoPrimario.dcCajas ?? 0) +
            // Proceso Presentación
            (liq.ProcesoPresentacion.dcDecorado ?? 0) +
            (liq.ProcesoPresentacion.dcRetractilado ?? 0) +
            // Proceso Congelación
            (liq.ProcesoCongelacion.dcBrine ?? 0) +
            (liq.ProcesoCongelacion.dcTunel ?? 0) +
            (liq.ProcesoCongelacion.dcIQF ?? 0) +
            // Proceso Secundario
            (liq.ProcesoSecundario.dcPelado ?? 0) +
            (liq.ProcesoSecundario.dcHidratacion ?? 0) +
            (liq.ProcesoSecundario.dcDescabezado ?? 0) +
            (liq.ProcesoSecundario.dcCocido ?? 0) +
            // Costos Directos
            (liq.ProcesoCostFijo.dcCostoVariable ?? 0) +
            (liq.ProcesoCostFijo.dcCostoFijo ?? 0) +
            // Costos Indirectos
            (liq.ProcesoCostIndirecto.dcCostoFijo ?? 0) +
            (liq.ProcesoCostIndirecto.dcCostoVariable ?? 0) +
            // Copacking
            (liq.dcCostoCopacking ?? 0) +
            //Tarifa Proceso
            (liq.dcTarifaProc  ?? 0) +
            // Premios
            //(liq.dcCertificado ?? 0) +
            (liq.dcExcedente ?? 0);
        #endregion

        #region Asignacion Costos Proceso Fresco
        public void AsignarCostosProcesosFresco(DataProcesoParam objDataProceso)
        {
            ConcurrentDictionary<string, decimal> dictCosto;
            try
            {
                if (objDataProceso.lstLiqFresco == null) throw new ArgumentNullException(nameof(objDataProceso.lstLiqFresco));

                if (objDataProceso.lstProcesoFrs == null) throw new ArgumentNullException(nameof(objDataProceso.lstProcesoFrs));

                dictCosto = ProcesoResultadoDto.ConstruirDictProc(objDataProceso.lstProcesoFrs);
                // Reutilizamos tu struct existente para mapear el diccionario
                CostosUnitarios objCostUnit = CostosUnitarios.ExtraerCostosUnitarios(dictCosto);

                foreach (var liq in objDataProceso.lstLiqFresco)
                {
                    AplicarCostosALiquidacion(liq, objCostUnit, objDataProceso.lstCongTunel);
                }
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[AsignarCostosProcesosFresco] : {ex.Message}");
                throw;
            }
        }

        private static void AplicarCostosALiquidacion(LiquidacionResultado objLiq, CostosUnitarios objCostUni, List<int> lstCongTunel)
        {
            AplicarProcesoPrimario(objLiq, objCostUni);
            AplicarProcesoPresentacion(objLiq, objCostUni);
            AplicarProcesoCongelacion(objLiq, objCostUni, lstCongTunel);
            AplicarProcesoSecundario(objLiq, objCostUni);
            AplicarCostosDirectos(objLiq, objCostUni);
            AplicarCostosIndirectos(objLiq, objCostUni);
            AplicarCostosCopacking(objLiq, objCostUni);

            objLiq.dcCostTotalProc = CalcularCostoTotal(objLiq);
            objLiq.dcTotalDolSum = (objLiq.dcCostTotalProc ?? 0m) + (objLiq.dcCostoTotalMatEmp ?? 0m) + (decimal) (objLiq.dcTotalDol ?? 0);
            if (objLiq.dcLibras > 0 && objLiq.dcCostoTotXLibra == null)
            {
                objLiq.dcCostoTotXLibra = Math.Truncate((objLiq.dcTotalDolSum / (decimal)objLiq.dcLibras) * 100) / 100;
                objLiq.dcValidador = (decimal)objLiq.dcCostoTotXLibra - (decimal)objLiq.dcPrecioCompra;
            }
        }

        private static void AplicarProcesoPrimario(LiquidacionResultado objLiq, CostosUnitarios c)
        {
            decimal libras = (decimal)objLiq.dcLibras;

            objLiq.ProcesoPrimario.dcRecepcion = Math.Round(libras * c.dcRecepcion, 4);
            objLiq.ProcesoPrimario.dcClasificacion = Math.Round(libras * c.dcClasificacion, 4);
            objLiq.ProcesoPrimario.dcCajas = Math.Round(libras * c.dcCajas, 4);
            objLiq.dcExcedente = Math.Round((decimal)objLiq.dcLibras * c.dcExcedente, 2);

            objLiq.dcCostRecepcion = c.dcRecepcion;
            objLiq.dcCostClasificacion = c.dcClasificacion;
            objLiq.dcCostCajas = c.dcCajas;
        }

        private static void AplicarProcesoPresentacion(LiquidacionResultado liq, CostosUnitarios c)
        {
            liq.ProcesoPresentacion.dcDecorado = Math.Round((decimal)liq.dcLibrasDecorado * c.dcDecorado, 4);
            liq.ProcesoPresentacion.dcRetractilado = Math.Round((decimal)(liq.dcLibrasRetractilado ?? 0) * c.dcRetractilado, 4);

            liq.dcCostDecorado = Math.Round(c.dcDecorado, 4);
            liq.dcCostRectra = Math.Round(c.dcRetractilado, 4);
        }


        private static void AplicarProcesoCongelacion(LiquidacionResultado liq, CostosUnitarios c, List<int> lstCongTunel)
        {
            decimal libras = (decimal)liq.dcLibras;

            // Brine
            if (liq.blBodEsBrine)
            {
                liq.ProcesoCongelacion.dcBrine = Math.Round(libras * c.dcBrine, 4);
                liq.dcCostBrine = c.dcBrine;
            }
            else
            {
                liq.ProcesoCongelacion.dcBrine = 0;
                liq.dcCostBrine = 0;
            }

            // IQF
            bool aplicaIqf = !lstCongTunel.Contains(liq.intProCongela) && !liq.blBodEsBrine;
            liq.ProcesoCongelacion.dcIQF = aplicaIqf ? Math.Round(libras * c.dcIQF, 4) : 0;
            liq.dcCostIQF = aplicaIqf ? c.dcIQF : 0;

            // Tunel
            bool aplicaTunel = lstCongTunel.Contains(liq.intProCongela) && liq.strProClas03 == "PT";
            if (aplicaTunel)
            {
                liq.ProcesoCongelacion.dcTunel = Math.Round(libras * c.dcTunel, 4);
                liq.dcCostTunel = c.dcTunel;
            }
            else
            {
                liq.ProcesoCongelacion.dcTunel = 0;
                liq.dcCostTunel = 0;
            }
        }

        private static void AplicarProcesoSecundario(LiquidacionResultado liq, CostosUnitarios c)
        {
            liq.ProcesoSecundario.dcPelado = 0;
            liq.ProcesoSecundario.dcHidratacion = 0;
            liq.ProcesoSecundario.dcCocido = 0;

            liq.dcCostPelado = 0;
            liq.dcCostHidratacion = 0;
            liq.dcCostCocido = 0;

            // Descabezado
            if (liq.strProClas05 == "SH")
            {
                liq.ProcesoSecundario.dcDescabezado = Math.Round((decimal)liq.dcLibras * c.dcDescabezado, 4);
                liq.dcCostDescabezado = c.dcDescabezado;
            }
            else
            {
                liq.ProcesoSecundario.dcDescabezado = 0;
                liq.dcCostDescabezado = 0;
            }
        }

        private static void AplicarCostosDirectos(LiquidacionResultado liq, CostosUnitarios c)
        {
            decimal libras = (decimal)liq.dcLibras;

            liq.ProcesoCostFijo.dcCostoVariable = Math.Round(libras * c.dcCostDirectoVar, 4);
            liq.ProcesoCostFijo.dcCostoFijo = Math.Round(libras * c.dcCostDirectoFij, 4);

            liq.dcCostDirVaria = c.dcCostDirectoVar;
            liq.dcCostDirFij = c.dcCostDirectoFij;
        }

        private static void AplicarCostosIndirectos(LiquidacionResultado liq, CostosUnitarios c)
        {
            
            decimal libras = (decimal)liq.dcLibras;

            // Nota: Mapeado exactamente como en el código original línea 194-195
            liq.ProcesoCostIndirecto.dcCostoFijo = Math.Round(libras * c.dcCostIndirVar, 4);
            liq.ProcesoCostIndirecto.dcCostoVariable = Math.Round(libras * c.dcCostIndirFij, 4);

            liq.dcCostIndVaria = c.dcCostIndirVar;
            liq.dcCostIndFij = c.dcCostIndirFij;
        }

        private static void AplicarCostosCopacking(LiquidacionResultado liq, CostosUnitarios c)
        {
            liq.dcCostoCopacking = liq.intCodCopacking != 0 && liq.strPlanta != "SONGA"
                ? Math.Round((decimal)liq.dcLibras * c.dcCopacking, 4)
                : 0;
        }
        private static decimal CalcularCostoTotal(LiquidacionResultado liq) =>
            // Proceso Primario
            (liq.ProcesoPrimario.dcRecepcion ?? 0) +
            (liq.ProcesoPrimario.dcClasificacion ?? 0) +
            (liq.ProcesoPrimario.dcCajas ?? 0) +
            // Proceso Presentación
            (liq.ProcesoPresentacion.dcDecorado ?? 0) +
            (liq.ProcesoPresentacion.dcRetractilado ?? 0) +
            // Proceso Congelación
            (liq.ProcesoCongelacion.dcBrine ?? 0) +
            (liq.ProcesoCongelacion.dcTunel ?? 0) +
            (liq.ProcesoCongelacion.dcIQF ?? 0) +
            // Proceso Secundario
            (liq.ProcesoSecundario.dcPelado ?? 0) +
            (liq.ProcesoSecundario.dcHidratacion ?? 0) +
            (liq.ProcesoSecundario.dcDescabezado ?? 0) +
            (liq.ProcesoSecundario.dcCocido ?? 0) +
            // Costos Directos
            (liq.ProcesoCostFijo.dcCostoVariable ?? 0) +
            (liq.ProcesoCostFijo.dcCostoFijo ?? 0) +
            // Costos Indirectos
            (liq.ProcesoCostIndirecto.dcCostoFijo ?? 0) +
            (liq.ProcesoCostIndirecto.dcCostoVariable ?? 0) +
            // Copacking
            (liq.dcCostoCopacking ?? 0) +
            //Tarifa Proceso
            (liq.dcTarifaProc ?? 0) +
            // Excedente
            (liq.dcExcedente ?? 0);
        #endregion

        #region Metodos Auxiliares para Asignacion Costos Proceso


        // Método auxiliar para limpiar y actualizar los valores consolidados
        private void ActualizarListaConTotales(List<ProcesoResultadoDto> lstProcResult, Dictionary<string, decimal> totales)
        {

            foreach (var item in lstProcResult.Where(x => !x.blEditable))
            {
                if (totales.TryGetValue(item.strDescripcion.Trim(), out decimal totalLibras))
                {
                    item.dcLibras = totalLibras;
                    // Cálculo: Dólares Totales / Libras Totales
                    item.dcCostUnitario = Math.Round(item.dcValor / totalLibras,4);
                }
            }
        }
        private void ActualizarDiccionario(Dictionary<string, decimal> dic, string llave, decimal valor)
        {
            if (dic.ContainsKey(llave))
                dic[llave] += valor;
            else
                dic[llave] = valor;
        }

        private void FinalizarAsignacion(List<ProcesoResultadoDto> resultados, Dictionary<string, decimal> totales, decimal? dcValorHidra)
        {
            foreach (var con in resultados)
            {
                if (totales.TryGetValue(con.strDescripcion.Trim(), out decimal totalLibras))
                {
                    if (con.strDescripcion.Trim() == "Hidratacion" && con.dcValor == 0)
                    {
                        con.dcValor = (decimal)(con.dcValor + dcValorHidra);
                    }
                    con.dcLibras = Math.Round(totalLibras, 2);
                }
            }
        }

        /// <summary>
        /// Recalcula dcCostUnitario exacto para todos los procesos no editables
        /// usando la lógica proporcional: cu = (ValorFRS + ValorRPC) / (LibrasFRS + LibrasRPC)
        /// Modifica directamente los DTOs para que CostosUnitarios.ExtraerCostosUnitarios
        /// ya reciba los valores con precisión completa, sin pasar por numeric(18,5) de BD.
        /// </summary>
        public void RecalcularCostosUnitariosExactos(
            List<ProcesoResultadoDto> lstProcesoFrs,
            List<ProcesoResultadoDto> lstProcesoRpc)
        {
            // Construir lookup de FRS por descripción para acceso O(1)
            var dictFrs = lstProcesoFrs
                .Where(x => !string.IsNullOrWhiteSpace(x.strDescripcion))
                .ToDictionary(
                    x => x.strDescripcion.Trim(),
                    x => x,
                    StringComparer.OrdinalIgnoreCase);

            // Iterar sobre RPC y recalcular CU exacto para los no editables
            foreach (var itemRPC in lstProcesoRpc.Where(x => !x.blEditable))
            {
                string desc = itemRPC.strDescripcion.Trim();

                dictFrs.TryGetValue(desc, out var itemFRS);

                double librasFRS = (double)(itemFRS?.dcLibras ?? 0);
                double librasRPC = (double)(itemRPC.dcLibras);
                double librasTotal = librasFRS + librasRPC;

                if (librasTotal == 0) continue;

                double valorFRS = (double)(itemFRS?.dcValor ?? 0);
                double valorRPC = (double)(itemRPC.dcValor);
                double valorTotal = valorFRS + valorRPC;

                decimal cuExacto = (decimal)(valorTotal / librasTotal);

                // Actualizar RPC
                itemRPC.dcCostUnitario = cuExacto;

                // Actualizar FRS con el mismo CU exacto
                if (itemFRS != null)
                    itemFRS.dcCostUnitario = cuExacto;
            }

            // Procesar procesos que solo existen en FRS (sin contraparte en RPC)
            var descripcionesRpc = new HashSet<string>(
                lstProcesoRpc.Select(x => x.strDescripcion.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var itemFRS in lstProcesoFrs.Where(x => !x.blEditable
                && !descripcionesRpc.Contains(x.strDescripcion.Trim())))
            {
                if (itemFRS.dcLibras == 0) continue;
                itemFRS.dcCostUnitario = itemFRS.dcValor / itemFRS.dcLibras;
            }


            // ── EDITABLES: CU = dcValor / dcLibras propio, sin mezclar FRS y RPC ──
            // Cada registro editable es independiente — su CU corresponde
            // únicamente a su propio valor y sus propias libras.
            foreach (var itemRPC in lstProcesoRpc.Where(x => x.blEditable))
            {
                if (itemRPC.dcLibras == 0) continue;
                itemRPC.dcCostUnitario = itemRPC.dcValor / itemRPC.dcLibras;
            }

            foreach (var itemFRS in lstProcesoFrs.Where(x => x.blEditable))
            {
                if (itemFRS.dcLibras == 0) continue;
                itemFRS.dcCostUnitario = itemFRS.dcValor / itemFRS.dcLibras;
            }
        }
        #endregion
    }
}

