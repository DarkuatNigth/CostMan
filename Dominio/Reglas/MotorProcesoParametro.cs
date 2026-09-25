using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.EF_Core;
using CostManagementService.Aplicacion.DTos;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Serialization;

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
        private static Dictionary<string, InfoProd> _dicInfoProd = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<int> _hshProdCod = new HashSet<int> { 6440, 6761, 5412, 5413, 6372, 5587, 6233, 6437, 5013 };
        private static readonly HashSet<int> _hshListDecora = new HashSet<int> { 2, 3 };
        private static readonly HashSet<int> _hshListRetrac = new HashSet<int> { 3, 4 };
        private const decimal _dcTarifaDescongelado = 0.02m;
        private static HashSet<InfoProd> _hshInfoProd = new HashSet<InfoProd> { };
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
                var objLibras = new AcumuladorLibrasParticion();   // ← NUEVO

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

                dicTotales["Logistica"] = sumRloNetas;
                dicTotales["Recepcion"] = sumRloNetas;
                dicTotales["Excedente M.E."] = sumRloProCabCol;
                // 2. Unificación de Productos Terminados y Descuentos (Llaves fijas)
                foreach (var item in objDataProceso.lstProdTerm.Concat(objDataProceso.lstDescTotFresco))
                {
                    dicTotales[item.Trim()] = sumRloProCabCol;
                    objLibras.AcumularDesde(item.Trim(), objDataProceso.lstLiqFresco,
                        x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05),
                        x => (decimal)x.dcLibras);
                }

                objLibras.AcumularDesde("Logistica", objDataProceso.lstLiqFresco,
                        x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05),
                        x => (decimal)x.dcLibras);
                objLibras.AcumularDesde("Recepcion", objDataProceso.lstLiqFresco,
                        x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05),
                        x => (decimal)x.dcLibras);

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
                        objLibras.Acumular("Descabezado",
                                ParticionCosteo.ClasificarFrs(item.strProClas01, item.strProClas05), (decimal)item.dcLibras);
                    }

                    // --- Lógica de Congelamiento (Tunel / Brine) ---
                    //!lstCongTunel.Contains(liq.intProCongela) && !liq.blBodEsBrine
                    if (objDataProceso.lstCongTunel.Contains(item.intProCongela) && item.strProClas03 == "PT")
                    {
                        ActualizarDiccionario(dicTotales, "Tunel", (decimal)item.dcLibras);
                        objLibras.Acumular("Tunel",
                                    ParticionCosteo.ClasificarFrs(item.strProClas01, item.strProClas05), (decimal)item.dcLibras);
                    }

                    if (item.dcLibrasRetractilado != null)
                    {
                        ActualizarDiccionario(dicTotales, "Retractilado", (decimal)item.dcLibrasRetractilado);
                        objLibras.Acumular("Retractilado",
                                ParticionCosteo.ClasificarFrs(item.strProClas01, item.strProClas05), (decimal)item.dcLibrasRetractilado);
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
                objLibras.AcumularDesde("C.Copacking", objDataProceso.lstLiqFresco.Where(x => x.intCodCopacking > 0 && x.strPlanta != "SONGA"),
                    x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05),
                    x => (decimal)x.dcLibras);
                objLibras.AcumularDesde("Material Empaque", objDataProceso.lstLiqFresco, 
                    x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05), x => (decimal)x.dcLibras);
                // ── NUEVO: exportar matriz ──
                objDataProceso.lstLibrasParticion.AddRange(objLibras.AListaDto("PFR"));
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
                var objLibras = new AcumuladorLibrasParticion();   // ← NUEVO
                List<string> lstNotCostConge = new List<string>() { /*"BP",*/
                        "CAM","RLL","R1","CDI","R2","REC","LB04","RPY","R3","RS","DV","RVVL","BDP","VE","VR"
                    };
                Func<MatPrimaReproceso, string?> fnPart = x => ParticionCosteo.ClasificarRpc(x.strTipoProducto);
                // 1. Costo Proceso Primario (Recepción y Productos Terminados)
                decimal dcCostProcPrim = (decimal)objDataProceso.lstLiqRepro
                    .Where(x => x.strLotTipo == "RE" && _lstlbsProcPrim.Contains(x.strTipCod))
                    .Sum(obj => obj.dbLibras);
                var lstRecep = objDataProceso.lstLiqRepro.Where(x => x.strLotTipo == "RE" && _lstlbsProcPrim.Contains(x.strTipCod)).ToList();
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
                var lstClasificacionR6 = objDataProceso.lstLiqRepro
                    .Where(x => string.Equals(
                        x.strTipCod?.Trim(),
                        "R6",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
                decimal lbsClasificacionR6 = (decimal)lstClasificacionR6.Sum(x => x.dbLibras);
                objDataProceso.lstProcesoRpc.Add(objMatEmpaqueRpc);

                dicTotales["Recepcion"] = dcCostProcPrim;
                dicTotales["Excedente M.E."] = dcCostProcPrim;
                var lstProdTerm = objDataProceso.lstLiqRepro.Where(x => x.strLotTipo == "RE" && _lstlbsProcPrim.Contains(x.strTipCod));
                foreach (var item in objDataProceso.lstProdTerm)
                {
                    if (item.Contains("Clasificacion", StringComparison.OrdinalIgnoreCase))
                    {
                        dicTotales[item.Trim()] = dcCostProcPrim + lbsClasificacionR6;
                        // Primera participación normal
                        objLibras.AcumularDesde(item.Trim(), lstProdTerm, fnPart, x => (decimal)x.dbLibras);
                        // Segunda participación SOLO de R6
                        objLibras.AcumularDesde(item.Trim(), lstClasificacionR6, fnPart, x => (decimal)x.dbLibras);
                    }
                    else
                    {
                        dicTotales[item.Trim()] = dcCostProcPrim;
                        objLibras.AcumularDesde(item.Trim(), lstProdTerm, fnPart, x => (decimal)x.dbLibras);
                    }
                }
                var lstValAggProcPrim = objDataProceso.lstLiqRepro.Where(x => x.strLotTipo == "RE").ToList();
                lstValAggProcPrim.AddRange(objDataProceso.lstLiqRepro.Where(x => _lstlbsProcCostIndDic.Contains(x.strTipCod)));

                foreach (var item in objDataProceso.lstDescTotFresco)
                {
                    dicTotales[item.Trim()] = dclbsValAgg + dcCostProcPrim;
                    objLibras.AcumularDesde(item.Trim(), lstValAggProcPrim,fnPart, x => (decimal)x.dbLibras);
                }
                objLibras.AcumularDesde("Recepcion", lstRecep, fnPart, x => (decimal)x.dbLibras);
                objLibras.AcumularDesde("Material Empaque", objDataProceso.lstLiqRepro, fnPart, x => (decimal)x.dbLibras);

                // 2. Filtros específicos de Reproceso
                var fCocido = objDataProceso.lstLiqRepro.Where(x => !string.IsNullOrEmpty(x.strRecNombre) && x.strRecTipo == "COC" && x.strLotTipo == "VA").ToList();
                dicTotales["Cocido"] = (decimal)objDataProceso.lstLiqRepro.Where(x => !string.IsNullOrEmpty(x.strRecNombre) && x.strRecTipo == "COC" && x.strLotTipo == "VA").Sum(obj => obj.dbLibras);
                objLibras.AcumularDesde("Cocido", fCocido, fnPart, x => (decimal)x.dbLibras);

                var fHidra = objDataProceso.lstLiqRepro.Where(x => !string.IsNullOrEmpty(x.strRecNombre) && x.strRecTipo != "COC" && x.strLotTipo == "VA").ToList();
                dicTotales["Hidratacion"] = (decimal)fHidra.Sum(o => o.dbLibras);
                objLibras.AcumularDesde("Hidratacion", fHidra, fnPart, x => (decimal)x.dbLibras);

                var fRetra = objDataProceso.lstLiqRepro.Where(x => x.blRetractilado).ToList();
                dicTotales["Retractilado"] = (decimal)fRetra.Sum(o => o.dcLibrasRetractilado);
                objLibras.AcumularDesde("Retractilado", fRetra, fnPart, x => (decimal)x.dcLibrasRetractilado);

                var fPelado = objDataProceso.lstLiqRepro.Where(o => o.blPelado).ToList();
                dicTotales["Pelado"] = (decimal)fPelado.Sum(o => o.dcLibrasPelado);
                objLibras.AcumularDesde("Pelado", fPelado, fnPart, x => (decimal)x.dcLibrasPelado);

                var fDecor = objDataProceso.lstLiqRepro.Where(o => o.blDecorado).ToList();
                dicTotales["Decorado"] = (decimal)fDecor.Sum(o => o.dbLibras);
                objLibras.AcumularDesde("Decorado", fDecor, fnPart, x => (decimal)x.dbLibras);

                var fDesc = objDataProceso.lstLiqRepro.Where(o => o.blEsDescabezado).ToList();
                dicTotales["Descabezado"] = (decimal)fDesc.Sum(o => o.dbLibras);
                objLibras.AcumularDesde("Descabezado", fDesc, fnPart, x => (decimal)x.dbLibras);

                var fIqf = objDataProceso.lstLiqRepro.Where(x => x.strCongeProduc.Trim().Equals("IQF") && !lstNotCostConge.Contains(x.strTipCod)).ToList();
                dicTotales["IQF"] = (decimal)fIqf.Sum(o => o.dbLibras);
                objLibras.AcumularDesde("IQF", fIqf, fnPart, x => (decimal)x.dbLibras);

                var fBrine = objDataProceso.lstLiqRepro.Where(x => x.strCongeProduc.Trim().Equals("BRINE") && !lstNotCostConge.Contains(x.strTipCod)).ToList();
                dicTotales["Brine"] = (decimal)fBrine.Sum(o => o.dbLibras);
                objLibras.AcumularDesde("Brine", fBrine, fnPart, x => (decimal)x.dbLibras);

                var fTunel = objDataProceso.lstLiqRepro.Where(x => (x.strCongeProduc.Trim().Equals("BLOCK") || x.strCongeProduc.Trim().Equals("SEMI IQF")) && !lstNotCostConge.Contains(x.strTipCod)).ToList();
                dicTotales["Tunel"] = (decimal)fTunel.Sum(o => o.dbLibras);
                objLibras.AcumularDesde("Tunel", fTunel, fnPart, x => (decimal)x.dbLibras);

                var dcLstCopacking = objDataProceso.lstLiqRepro.Where(x => x.intCodCopacking != 0);
                dicTotales["C.Copacking"] = (decimal)dcLstCopacking.Sum(obj => obj.dbLibras);
                objLibras.AcumularDesde("C.Copacking", dcLstCopacking, fnPart, x => (decimal)x.dbLibras);


                var lstRecibidosDescongelado =objDataProceso.lstLiqReproCompleto.Where(x =>
                                string.Equals(x.strAgrupacion,"1. RECIBIDO",StringComparison.OrdinalIgnoreCase)
                                && x.blEsDescongelado == true).ToList();

                decimal lbsDescongelado = (decimal)lstRecibidosDescongelado.Sum(x => x.dbLibras);

                dicTotales["Descongelado"] = lbsDescongelado;

                ProcesoResultadoDto? procesoDescongelado =
                    objDataProceso.lstProcesoRpc.FirstOrDefault(
                        x => string.Equals(
                            x.strDescripcion?.Trim(),
                            "Descongelado",
                            StringComparison.OrdinalIgnoreCase));

                if (procesoDescongelado != null)
                {
                    procesoDescongelado.blEditable = false;
                    procesoDescongelado.dcLibras = Math.Round(lbsDescongelado, 2);
                    procesoDescongelado.dcValor =
                        Math.Round(lbsDescongelado * _dcTarifaDescongelado, 4);
                    procesoDescongelado.dcCostUnitario = _dcTarifaDescongelado;
                }
                // Mapeo Final
                FinalizarAsignacion(objDataProceso.lstProcesoRpc, dicTotales, objDataProceso.dcCostoHidraReproceso);
                objDataProceso.lstLibrasParticion.AddRange(objLibras.AListaDto("RPC"));
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
                _dicInfoProd = objDataProceso.lstInfoProd
                        .GroupBy(obj => obj.strProCodcor)
                        .ToDictionary(g => g.Key, g => g.First());
                foreach (var objLiq in lstLiqRepro)
                    AplicarCostosALiquidacion(objLiq, objCostUnit, dicTarifasPorCodigo);


                AplicarDescongeladoGlobal(objDataProceso, lstLiqRepro, _dcTarifaDescongelado);
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
            liq.objInfoProd = _dicInfoProd.GetValueOrDefault(liq.intCodProd.ToString(), null);
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

        private static void AplicarDescongeladoGlobal( DataProcesoParam objDataProceso,List<MatPrimaReproceso> lstProcesados, decimal tarifa)
        {
            // En ObtenerParametroProceso se conserva RECIBIDO+PROCESADO aquí.
            // En otros flujos, lstLiqRepro puede ya contener ambos; se deja fallback.
            List<MatPrimaReproceso> universo =
                objDataProceso.lstLiqReproCompleto != null &&
                objDataProceso.lstLiqReproCompleto.Any()
                    ? objDataProceso.lstLiqReproCompleto
                    : (objDataProceso.lstLiqRepro ?? new List<MatPrimaReproceso>());

            var recibidos = universo
                .Where(x =>
                    x.strAgrupacion == "1. RECIBIDO" &&
                    x.strProClas03 == "PT" &&
                    x.blEsDescongelado == true &&
                    x.dbLibras > 0)
                .ToList();

            var salidas = lstProcesados
                .Where(x =>
                    (x.strProClas03 == "PT" || x.strProClas03 == "PP") &&
                    x.dbLibras > 0)
                .ToList();

            foreach (var grupo in recibidos.GroupBy(
                x => (x.intLotNumero, x.intLoteUnificado)))
            {
                decimal lbsRecibidas =  (decimal)grupo.Sum(x => x.dbLibras);

                decimal montoLote =  Math.Round(lbsRecibidas * tarifa, 6);

                List<MatPrimaReproceso> salidasLote =  salidas.Where(x =>x.intLotNumero == grupo.Key.intLotNumero && x.intLoteUnificado == grupo.Key.intLoteUnificado).ToList();
                decimal lbsSalida = (decimal)salidasLote.Sum(x => x.dbLibras);

                if (lbsSalida <= 0m)
                    continue;

                decimal asignado = 0m;

                for (int i = 0; i < salidasLote.Count; i++)
                {
                    MatPrimaReproceso salida = salidasLote[i];

                    decimal montoSalida = i == salidasLote.Count - 1 ? Math.Round(montoLote - asignado, 6)
                            : Math.Round( montoLote * (decimal)salida.dbLibras / lbsSalida,
                                6);

                    if (i < salidasLote.Count - 1)
                        asignado += montoSalida;

                    salida.ProcesoSecundario.dcDescongelado =
                        (salida.ProcesoSecundario.dcDescongelado ?? 0m)
                        + montoSalida;
                }
            }
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
            List<LoteRpcKeyXProdTal> objProd = new List<LoteRpcKeyXProdTal> { new LoteRpcKeyXProdTal( 5177, 25), new LoteRpcKeyXProdTal(5412, 25) };
            try
            {
                if (_lstNotPresen.Contains(liq.strTipCod)) return;
                if(liq.strTipCod != "CAM" && liq.intCodProd == 7052)
                {
                    liq.blDecorado = liq.blDecorado /*&& objInfoProd != null */&& (_hshListDecora.Contains(liq.objInfoProd.intProDecora) || !_hshListDecora.Contains(liq.objInfoProd.intProDecora));
                }
                //InfoProd objInfoProd = _dicInfoProd.GetValueOrDefault(liq.intCodProd.ToString(), null);
                //liq.blDecorado = liq.blDecorado && objInfoProd == null ? liq.blDecorado && _hshListDecora.Contains(objInfoProd.intProDecora) : false;
                liq.blDecorado = liq.blDecorado && liq.objInfoProd != null && (_hshListDecora.Contains(liq.objInfoProd.intProDecora) || !_hshListDecora.Contains(liq.objInfoProd.intProDecora));
                liq.blRetractilado = liq.blRetractilado && liq.objInfoProd != null ? liq.blRetractilado && _hshListRetrac.Contains(liq.objInfoProd.intProRetracti) : false;
                liq.ProcesoPresentacion.dcDecorado = 
                    liq.blDecorado? Math.Round((decimal)liq.dbLibras * liq.objInfoProd.dcCostoDec, 4)//Math.Round((decimal)liq.dbLibras * c.dcDecorado, 4) 
                        : 0;
                if (objProd.Contains(liq.objProdTalKey))
                {
                    var db = Convert.ToDouble(liq.objInfoProd.dcCostoDec);
                    var dbKg = Convert.ToDouble(liq.objInfoProd.dcCostoDec) * 2.2046;
                    var dblbs = dbKg  / 2.2046;
                    var objDb = liq.dbLibras * Convert.ToDouble(liq.objInfoProd.dcCostoDec);
                }
                if (liq.objInfoProd != null)
                {
                    decimal dcLbsRetrac = liq.blRetractilado && liq.dcLibrasRetractilado.HasValue ? liq.dcLibrasRetractilado.Value : (decimal)liq.dbLibras;
                    liq.ProcesoPresentacion.dcRetractilado =
                        liq.blRetractilado ? Math.Round(dcLbsRetrac * liq.objInfoProd.dcCostoRetrac, 4) //Math.Round((decimal)liq.dcLibrasRetractilado * c.dcRetractilado, 4) 
                        : 0;
                    liq.dcRetractilado = liq.blRetractilado ? liq.ProcesoPresentacion.dcRetractilado : 0;
                }

                liq.dcDecorado = liq.blDecorado ? liq.ProcesoPresentacion.dcDecorado : 0;
            }
            catch (Exception obj)
            {
                _hshInfoProd.Add(liq.objInfoProd);
                Console.WriteLine(obj.Message + obj.Source);
            }
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
            try
            {
                if (liq.strTipCod == "EZ" && liq.intCodProd == 3861)
                {
                    Console.WriteLine(liq.intCodProd);
                }
                decimal libras = (decimal)liq.dbLibras;
                if (liq.objInfoProd != null && liq.blPelado)
                {
                    var objTarifaPelado = liq.objInfoProd.lstTarPelado.FirstOrDefault(x => x.TpTalCodigo == liq.intCodTal);
                    if (objTarifaPelado != null)
                    {
                        liq.ProcesoSecundario.dcPelado = Math.Round((decimal)liq.dcLibrasPelado * objTarifaPelado.TpTarifa, 4);
                        liq.dcPelado = liq.blPelado ? liq.ProcesoSecundario.dcPelado : 0;
                    }
                }
                // Pelado
                //liq.ProcesoSecundario.dcPelado = liq.blPelado
                //    ? Math.Round((decimal)liq.dcLibrasPelado * c.dcPelado, 4) : 0;
                //liq.dcPelado = liq.blPelado ? c.dcPelado : 0;

                // Hidratación (sal + hidra)
                decimal dcValorHidra = (decimal)liq.dbLibras * c.dcHidratacion;

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
            catch(Exception obj)
            {
                _hshInfoProd.Add(liq.objInfoProd);
                Console.WriteLine(obj.Message + obj.Source);
            }
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
            objLiq.ProcesoCostIndirecto.dcCostoFijo = Math.Round(dcLibras * objCostUni.dcCostIndirFij, 4);
            objLiq.ProcesoCostIndirecto.dcCostoVariable = Math.Round(dcLibras * objCostUni.dcCostIndirVar, 4);
            objLiq.dcCostIndirFijo = objCostUni.dcCostIndirFij;
            objLiq.dcCostIndirVaria = objCostUni.dcCostIndirVar;
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
            (liq.ProcesoSecundario.dcDescongelado ?? 0) +
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
                Dictionary<(string Pc, string Particion), decimal> dicUnitariosParticion =
                    ConstruirUnitariosFrescoParticion(objDataProceso);

                foreach (var liq in objDataProceso.lstLiqFresco)
                {
                    AplicarCostosALiquidacion(
                        liq,
                        objCostUnit,
                        objDataProceso.lstCongTunel,
                        dicUnitariosParticion);
                }
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[AsignarCostosProcesosFresco] : {ex.Message}");
                throw;
            }
        }


        private static void AplicarCostosALiquidacion(
            LiquidacionResultado objLiq,
            CostosUnitarios objCostUni,
            List<int> lstCongTunel,
            IReadOnlyDictionary<(string Pc, string Particion), decimal> dicUnitariosParticion)
        {
            AplicarProcesoPrimario(objLiq, objCostUni, dicUnitariosParticion);
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

        private static void AplicarProcesoPrimario(
             LiquidacionResultado objLiq,
             CostosUnitarios c,
             IReadOnlyDictionary<(string Pc, string Particion), decimal> dicUnitariosParticion)
        {
            decimal libras = (decimal)objLiq.dcLibras;

            string particion = ParticionCosteo.ClasificarFrs(
                objLiq.strProClas01,
                objLiq.strProClas05) ?? string.Empty;

            decimal cuLogistica = ObtenerUnitarioParticion(dicUnitariosParticion, "LOG", particion, c.dcLogistica);
            decimal cuRecepcion = ObtenerUnitarioParticion(dicUnitariosParticion, "REC", particion, c.dcRecepcion);
            decimal cuClasificacion = ObtenerUnitarioParticion(dicUnitariosParticion, "CLA", particion, c.dcClasificacion);

            objLiq.ProcesoPrimario.dcLogistica = Math.Round(libras * cuLogistica, 4);
            objLiq.ProcesoPrimario.dcRecepcion = Math.Round(libras * cuRecepcion, 4);
            objLiq.ProcesoPrimario.dcClasificacion = Math.Round(libras * cuClasificacion, 4);
            objLiq.ProcesoPrimario.dcCajas = Math.Round(libras * c.dcCajas, 4);
        }

        private static Dictionary<(string Pc, string Particion), decimal>ConstruirUnitariosFrescoParticion(DataProcesoParam objDataProceso)
        {
            var resultado = new Dictionary<(string Pc, string Particion), decimal>();
            List<LiquidacionResultado> fresco = objDataProceso.lstLiqFresco ?? new();

            decimal lbsEn = fresco
                .Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.ENTERO)
                .Sum(x => (decimal)x.dcLibras);
            decimal lbsCo = fresco
                .Where(x => ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05) == ParticionCosteo.COLA)
                .Sum(x => (decimal)x.dcLibras);

            foreach (CostoProcesoParticionDto item in objDataProceso.lstCostoProcesoParticion ?? new())
            {
                string pc = (item.strPcCodigo ?? string.Empty).Trim().ToUpperInvariant();
                if (pc is not ("LOG" or "REC" or "CLA")) continue;

                resultado[(pc, ParticionCosteo.ENTERO)] = lbsEn > 0m
                    ? item.dcDolaresEntero / lbsEn : 0m;
                resultado[(pc, ParticionCosteo.COLA)] = lbsCo > 0m
                    ? item.dcDolaresCola / lbsCo : 0m;
            }

            return resultado;
        }

        private static decimal ObtenerUnitarioParticion(
            IReadOnlyDictionary<(string Pc, string Particion), decimal> dic,
            string pc,
            string particion,
            decimal fallback)
        {
            return dic.TryGetValue((pc, particion), out decimal valor) && valor != 0m
                ? valor
                : fallback;
        }

        private static void AplicarProcesoPresentacion(LiquidacionResultado liq, CostosUnitarios c)
        {
            InfoProd objInfoProd = _dicInfoProd.GetValueOrDefault(liq.intCodProd.ToString(), null);
            try
            {

                bool blDecorado = objInfoProd != null ? _hshListDecora.Contains(objInfoProd.intProDecora) : false;
                bool blRetractilado = objInfoProd != null ? _hshListRetrac.Contains(objInfoProd.intProRetracti) : false;
                //bool blDecorado = _dicInfoProd.TryGetValue(liq.intCodProd.ToString(), out var infoProd) && _hshListDecora.Contains(infoProd.intProDecora);
                //bool blRetractilado = _dicInfoProd.TryGetValue(liq.intCodProd.ToString(), out var infoProd2) && _hshListRetrac.Contains(infoProd2.intProRetracti);
                liq.ProcesoPresentacion.dcDecorado = //blDecorado ? Math.Round((decimal)liq.dcLibrasDecorado * (objInfoProd.dcCostoDecora / 2.2406m), 4)//Math.Round((decimal)liq.dcLibrasDecorado * c.dcDecorado, 4) 
                    //: 
                    0;

                if (objInfoProd != null)
                {
                    liq.ProcesoPresentacion.dcRetractilado =
                        blRetractilado ? Math.Round((decimal)(liq.dcLibrasRetractilado ?? 0) * objInfoProd.dcCostoRetrac , 4) //Math.Round((decimal)liq.dcLibrasRetractilado * c.dcRetractilado, 4) 
                        : 0;
                    liq.dcCostRectra = blRetractilado ? liq.ProcesoPresentacion.dcRetractilado : 0;
                }

                liq.dcCostDecorado = Math.Round(c.dcDecorado, 4);
                //liq.dcCostRectra = Math.Round(c.dcRetractilado, 4);
            }
            catch (Exception obj)
            {
                _hshInfoProd.Add(objInfoProd);
                Console.WriteLine(obj.Message+ obj.Source);
            }
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
            liq.ProcesoCostIndirecto.dcCostoFijo = Math.Round(libras * c.dcCostIndirFij, 4);
            liq.ProcesoCostIndirecto.dcCostoVariable = Math.Round(libras * c.dcCostIndirVar, 4);

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
            (liq.ProcesoPrimario.dcLogistica ?? 0) +
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
            // ============================================================
            // PROCESOS TARIFA
            //
            // Estos CU ya vienen determinados por tarifa.
            // Este método NO debe alterarlos.
            // ============================================================

            var procesosTarifa =
                new HashSet<string>(
                    new[]
                    {
                "Descabezado",
                "Descongelado"
                    },
                    StringComparer.OrdinalIgnoreCase);


            static string NormalizarDescripcion(
                string? value)
            {
                return value?.Trim() ??
                    string.Empty;
            }


            bool EsTarifa(
                ProcesoResultadoDto item)
            {
                return procesosTarifa.Contains(
                    NormalizarDescripcion(
                        item.strDescripcion));
            }


            // ============================================================
            // LOOKUP PFR
            // ============================================================

            var dictFrs =
                lstProcesoFrs
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.strDescripcion))
                    .GroupBy(
                        x =>
                            x.strDescripcion.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);


            // ============================================================
            // NO EDITABLES
            //
            // Compartir CU entre PFR/RPC SOLO si NO es tarifa.
            // ============================================================

            foreach (
                var itemRPC
                in lstProcesoRpc.Where(
                    x => !x.blEditable))
            {
                // Descabezado / Descongelado conservan
                // exclusivamente su tarifa original.
                if (EsTarifa(itemRPC))
                {
                    continue;
                }


                string desc =
                    NormalizarDescripcion(
                        itemRPC.strDescripcion);


                dictFrs.TryGetValue(
                    desc,
                    out var itemFRS);


                decimal librasFRS =
                    itemFRS?.dcLibras ??
                    0m;


                decimal librasRPC =
                    itemRPC.dcLibras;


                decimal librasTotal =
                    librasFRS +
                    librasRPC;


                decimal valorFRS =
                    itemFRS?.dcValor ??
                    0m;


                decimal valorRPC =
                    itemRPC.dcValor;


                decimal valorTotal =
                    valorFRS +
                    valorRPC;


                decimal cuExacto =
                    librasTotal > 0m
                        ? valorTotal /
                          librasTotal
                        : 0m;


                itemRPC.dcCostUnitario =
                    cuExacto;


                if (itemFRS != null)
                {
                    itemFRS.dcCostUnitario =
                        cuExacto;
                }
            }


            // ============================================================
            // DESCRIPCIONES RPC
            // ============================================================

            var descripcionesRpc =
                new HashSet<string>(
                    lstProcesoRpc
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.strDescripcion))
                        .Select(x =>
                            x.strDescripcion.Trim()),
                    StringComparer.OrdinalIgnoreCase);


            // ============================================================
            // SOLO PFR
            // ============================================================

            foreach (
                var itemFRS
                in lstProcesoFrs.Where(
                    x =>
                        !x.blEditable &&
                        !descripcionesRpc.Contains(
                            x.strDescripcion.Trim())))
            {
                // Una tarifa jamás se recalcula aquí.
                if (EsTarifa(itemFRS))
                {
                    continue;
                }


                itemFRS.dcCostUnitario =
                    itemFRS.dcLibras > 0m
                        ? itemFRS.dcValor /
                          itemFRS.dcLibras
                        : 0m;
            }


            // ============================================================
            // EDITABLES RPC
            // ============================================================

            foreach (
                var itemRPC
                in lstProcesoRpc.Where(
                    x => x.blEditable))
            {
                if (EsTarifa(itemRPC))
                {
                    continue;
                }


                itemRPC.dcCostUnitario =
                    itemRPC.dcLibras > 0m
                        ? itemRPC.dcValor /
                          itemRPC.dcLibras
                        : 0m;
            }


            // ============================================================
            // EDITABLES PFR
            // ============================================================

            foreach (
                var itemFRS
                in lstProcesoFrs.Where(
                    x => x.blEditable))
            {
                if (EsTarifa(itemFRS))
                {
                    continue;
                }


                itemFRS.dcCostUnitario =
                    itemFRS.dcLibras > 0m
                        ? itemFRS.dcValor /
                          itemFRS.dcLibras
                        : 0m;
            }
        }
        #endregion
    }
}

