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
                decimal dcLibrasFrescoTotal = (decimal)objDataProceso.lstLiqFresco.Sum(x => x.dcLibras);
                decimal sumRloNetas = dcLibrasFrescoTotal;
                decimal sumRloProCabCol = dcLibrasFrescoTotal;
                List<LiquidacionResultado> lstCopacking = objDataProceso.lstLiqFresco
                    .Where(x => x.intCodCopacking > 0 && x.strPlanta != "SONGA")
                    .ToList();
                decimal dcSumCopacking = (decimal)lstCopacking.Sum(x => x.dcLibras);
                decimal dcSumMatEmpaque = (decimal)objDataProceso.lstLiqFresco.Sum(x => x.dcCostoTotalMatEmp ?? 0);
                objMatEmpaqueFrs = new ProcesoResultadoDto
                {
                    intCodigo = objDataProceso.lstProcesoFrs
                .LastOrDefault(obj => obj.intCodigo > 0)?.intCodigo ?? 0,
                    intCodDet = 0,
                    strEstado = "AC",
                    strDescripcion = "Material Empaque",
                    blEditable = false,
                    strTipoLote = "PFR",
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

                    if (item.dcLibrasDecorado != null)
                    {
                        ActualizarDiccionario(dicTotales, "Decorado", (decimal)item.dcLibrasDecorado);
                        objLibras.Acumular("Decorado",
                                ParticionCosteo.ClasificarFrs(item.strProClas01, item.strProClas05), (decimal)item.dcLibrasDecorado);
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
                objLibras.AcumularDesde("C.Copacking", lstCopacking,
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
                //
                // OPTIMIZACIÓN: "Recepción", "Excedente M.E." y la base de
                // "Clasificacion/Cajas" usan exactamente el mismo filtro
                // (strLotTipo=="RE" && _lstlbsProcPrim.Contains(strTipCod)).
                // Antes se evaluaba por separado (incluso sin materializar,
                // re-escaneando lstLiqRepro una vez por cada categoría dentro
                // del foreach de más abajo). Se materializa una sola vez.
                List<MatPrimaReproceso> lstRecep = objDataProceso.lstLiqRepro
                    .Where(x => x.strLotTipo == "RE" && _lstlbsProcPrim.Contains(x.strTipCod))
                    .ToList();
                decimal dcCostProcPrim = (decimal)lstRecep.Sum(x => x.dbLibras);
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
                foreach (var item in objDataProceso.lstProdTerm)
                {
                    if (item.Contains("Clasificacion", StringComparison.OrdinalIgnoreCase))
                    {
                        dicTotales[item.Trim()] = dcCostProcPrim + lbsClasificacionR6;
                        // Primera participación normal
                        objLibras.AcumularDesde(item.Trim(), lstRecep, fnPart, x => (decimal)x.dbLibras);
                        // Segunda participación SOLO de R6
                        objLibras.AcumularDesde(item.Trim(), lstClasificacionR6, fnPart, x => (decimal)x.dbLibras);
                    }
                    else
                    {
                        dicTotales[item.Trim()] = dcCostProcPrim;
                        objLibras.AcumularDesde(item.Trim(), lstRecep, fnPart, x => (decimal)x.dbLibras);
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
                //
                // OPTIMIZACIÓN: antes eran 9 pasadas independientes sobre lstLiqRepro
                // (cada una con su propio .Where().ToList().Sum()). Se consolidan en
                // una sola pasada por lote; las condiciones y los campos sumados por
                // categoría son exactamente los mismos que antes. Las claves se
                // pre-siembran en 0 para preservar el comportamiento cuando ningún
                // lote cae en una categoría (antes la asignación directa "=" dejaba
                // la clave en 0 igual; con ActualizarDiccionario (+=) hace falta
                // sembrarla para que FinalizarAsignacion no la deje sin tocar).
                dicTotales["Cocido"] = 0m;
                dicTotales["Hidratacion"] = 0m;
                dicTotales["Retractilado"] = 0m;
                dicTotales["Pelado"] = 0m;
                dicTotales["Decorado"] = 0m;
                dicTotales["Descabezado"] = 0m;
                dicTotales["IQF"] = 0m;
                dicTotales["Brine"] = 0m;
                dicTotales["Tunel"] = 0m;
                dicTotales["C.Copacking"] = 0m;

                foreach (var item in objDataProceso.lstLiqRepro)
                {
                    string? strParticion = fnPart(item);

                    if (!string.IsNullOrEmpty(item.strRecNombre) && item.strRecTipo == "COC" && item.strLotTipo == "VA")
                    {
                        ActualizarDiccionario(dicTotales, "Cocido", (decimal)item.dbLibras);
                        objLibras.Acumular("Cocido", strParticion, (decimal)item.dbLibras);
                    }

                    if (!string.IsNullOrEmpty(item.strRecNombre) && item.strRecTipo != "COC" && item.strLotTipo == "VA")
                    {
                        ActualizarDiccionario(dicTotales, "Hidratacion", (decimal)item.dbLibras);
                        objLibras.Acumular("Hidratacion", strParticion, (decimal)item.dbLibras);
                    }

                    if (item.blRetractilado)
                    {
                        ActualizarDiccionario(dicTotales, "Retractilado", (decimal)item.dcLibrasRetractilado);
                        objLibras.Acumular("Retractilado", strParticion, (decimal)item.dcLibrasRetractilado);
                    }

                    if (item.blPelado)
                    {
                        ActualizarDiccionario(dicTotales, "Pelado", (decimal)item.dcLibrasPelado);
                        objLibras.Acumular("Pelado", strParticion, (decimal)item.dcLibrasPelado);
                    }

                    if (item.blDecorado)
                    {
                        ActualizarDiccionario(dicTotales, "Decorado", (decimal)item.dbLibras);
                        objLibras.Acumular("Decorado", strParticion, (decimal)item.dbLibras);
                    }

                    if (item.blEsDescabezado)
                    {
                        ActualizarDiccionario(dicTotales, "Descabezado", (decimal)item.dbLibras);
                        objLibras.Acumular("Descabezado", strParticion, (decimal)item.dbLibras);
                    }

                    string strCongeProduc = item.strCongeProduc.Trim();
                    bool blCongelaCosteable = !lstNotCostConge.Contains(item.strTipCod);

                    if (strCongeProduc.Equals("IQF") && blCongelaCosteable)
                    {
                        ActualizarDiccionario(dicTotales, "IQF", (decimal)item.dbLibras);
                        objLibras.Acumular("IQF", strParticion, (decimal)item.dbLibras);
                    }

                    if (strCongeProduc.Equals("BRINE") && blCongelaCosteable)
                    {
                        ActualizarDiccionario(dicTotales, "Brine", (decimal)item.dbLibras);
                        objLibras.Acumular("Brine", strParticion, (decimal)item.dbLibras);
                    }

                    if ((strCongeProduc.Equals("BLOCK") || strCongeProduc.Equals("SEMI IQF")) && blCongelaCosteable)
                    {
                        ActualizarDiccionario(dicTotales, "Tunel", (decimal)item.dbLibras);
                        objLibras.Acumular("Tunel", strParticion, (decimal)item.dbLibras);
                    }

                    if (item.intCodCopacking != 0)
                    {
                        ActualizarDiccionario(dicTotales, "C.Copacking", (decimal)item.dbLibras);
                        objLibras.Acumular("C.Copacking", strParticion, (decimal)item.dbLibras);
                    }
                }


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
                RecalcularLibrasRpcSegunConfiguracion(objDataProceso);
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

        #if false // LEGACY: reemplazado por Flujo RPC Configurado -- 2026-10-01
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
        #endif

        #region Flujo RPC Configurado - Matriz de Costeo

        /// <summary>
        /// Recalcula la base física que alimenta dcCostUnitario para RPC.
        ///
        /// La matriz define si TIPOLIQ participa en el proceso. La condición física
        /// del lote define CUÁNTAS libras participan (pelado, retractilado, etc.).
        ///
        /// Esta combinación evita dos errores:
        /// - incluir un TIPOLIQ deshabilitado aunque físicamente tenga libras;
        /// - habilitar un costo sin que exista evidencia física del proceso.
        /// </summary>
        private void RecalcularLibrasRpcSegunConfiguracion(DataProcesoParam objDataProceso)
        {
            MatrizProcesoParametroRuntime matriz =
                new MatrizProcesoParametroRuntime(objDataProceso.objConfiguracionCostoProductivo);

            List<MatPrimaReproceso> procesados =
                (objDataProceso.lstLiqRepro ?? new List<MatPrimaReproceso>())
                .Where(x => string.Equals(x.strAgrupacion, "2. PROCESADO", StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<MatPrimaReproceso> universo =
                objDataProceso.lstLiqReproCompleto != null &&
                objDataProceso.lstLiqReproCompleto.Count > 0
                    ? objDataProceso.lstLiqReproCompleto
                    : (objDataProceso.lstLiqRepro ?? new List<MatPrimaReproceso>());

            var totales = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var librasParticion = new AcumuladorLibrasParticion();

            foreach (MatPrimaReproceso item in procesados)
            {
                string tipo = NormalizarConfig(item.strTipCod);
                string particion = ParticionCosteo.ClasificarRpc(item.strTipoProducto) ?? string.Empty;

                decimal lbs = (decimal)item.dbLibras;
                decimal lbsPelado = item.dcLibrasPelado.HasValue
                    ? item.dcLibrasPelado.Value
                    : lbs;
                decimal lbsRetractilado = item.dcLibrasRetractilado.HasValue
                    ? item.dcLibrasRetractilado.Value
                    : lbs;

                bool esHidratacion =
                    !string.IsNullOrWhiteSpace(item.strRecNombre) &&
                    !string.Equals(item.strRecTipo, "COC", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.strLotTipo, "VA", StringComparison.OrdinalIgnoreCase);

                bool esCocido =
                    string.Equals(item.strRecTipo, "COC", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.strLotTipo, "VA", StringComparison.OrdinalIgnoreCase);

                string congelamiento = NormalizarConfig(item.strCongeProduc);

                // Procesos con matriz real.
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "LOG", tipo, particion, lbs, true, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "REC", tipo, particion, lbs, true, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "CLA", tipo, particion, lbs, true, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "COD", tipo, particion, lbs, true, false);

                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "DES", tipo, particion, lbs, item.blEsDescabezado, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "PEL", tipo, particion, lbsPelado, item.blPelado, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "HID", tipo, particion, lbs, esHidratacion, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "COC", tipo, particion, lbs, esCocido, false);

                // Decorado y Retractilado conservan sus libras físicas actuales.
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "DEC", tipo, particion, lbs, item.blDecorado, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "RET", tipo, particion, lbsRetractilado, item.blRetractilado, false);

                // PRO/DEP se resuelven con la condición física real del lote.
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "BRI", tipo, particion, lbs, congelamiento == "BRINE", false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "IQF", tipo, particion, lbs, congelamiento == "IQF", false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "TUN", tipo, particion, lbs, congelamiento == "BLOCK" || congelamiento == "SEMI IQF", false);

                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "CDF", tipo, particion, lbs, true, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "CDV", tipo, particion, lbs, true, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "CIF", tipo, particion, lbs, true, false);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "CIV", tipo, particion, lbs, true, false);

                // CAJ y COP no existen actualmente en 03_TIPOS_Y_DRIVERS.sql.
                // Se conserva un fallback explícito y auditable hasta que negocio los
                // incorpore a la matriz. Si aparecen filas en la matriz, ésta manda.
                bool cajaLegacy = TipoCajaLegacy(tipo);
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "CAJ", tipo, particion, lbs, cajaLegacy, true);

                bool copackingLegacy = item.intCodCopacking != 0;
                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "COP", tipo, particion, lbs, copackingLegacy, true);
            }

            // Descongelado usa libras RECIBIDAS PT y regla TAR en la matriz.
            foreach (MatPrimaReproceso item in universo.Where(x =>
                string.Equals(x.strAgrupacion, "1. RECIBIDO", StringComparison.OrdinalIgnoreCase) &&
                x.blEsDescongelado == true &&
                x.dbLibras > 0))
            {
                string tipo = NormalizarConfig(item.strTipCod);
                string particion = ParticionCosteo.ClasificarRpc(item.strTipoProducto) ?? string.Empty;

                AcumularLibrasConfiguradas(matriz, totales, librasParticion, "DSC", tipo, particion, (decimal)item.dbLibras, true, false);
            }

            Dictionary<string, ProcesoResultadoDto> parametroPorDescripcion =
                (objDataProceso.lstProcesoRpc ?? new List<ProcesoResultadoDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.strDescripcion))
                .GroupBy(x => NormalizarTexto(x.strDescripcion), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (ProcesoCostoMantenimientoDto proceso in matriz.ProcesosCatalogo)
            {
                ProcesoResultadoDto parametro;
                decimal total;

                if (!totales.TryGetValue(proceso.strCodigo, out total))
                    total = 0m;

                if (!parametroPorDescripcion.TryGetValue(NormalizarTexto(proceso.strNombre), out parametro))
                {
                    if (Math.Abs(total) > 0.000001m)
                    {
                        _objLogger.LogWarning("[MotorProcesoParametro][Config][BaseCU] Proceso {Pc}/{Nombre} " +
                            "tiene {Libras:N4} lbs configuradas pero no existe fila en " +
                            "lstProcesoRpc. No se puede calcular CU legacy para esa fila.", proceso.strCodigo, proceso.strNombre, total);
                    }
                    continue;
                }

                parametro.dcLibras = Math.Round(total, 4);

                if (string.Equals(proceso.strCodigo, "DSC", StringComparison.OrdinalIgnoreCase))
                {
                    decimal tarifa = ResolverTarifaDescongelado(objDataProceso, parametro);

                    parametro.blEditable = false;
                    parametro.dcCostUnitario = tarifa;
                    parametro.dcValor = Math.Round(total * tarifa, 5);
                }
            }

            // Reemplazar únicamente las filas físicas RPC de procesos catalogados.
            // Material Empaque y otras filas legacy ajenas a tb_procesocosto se conservan.
            HashSet<string> nombresCatalogo = matriz.ProcesosCatalogo
                .Select(x => NormalizarTexto(x.strNombre))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            objDataProceso.lstLibrasParticion =
                objDataProceso.lstLibrasParticion ?? new List<LibrasParticionDto>();

            objDataProceso.lstLibrasParticion.RemoveAll(x =>
                string.Equals(x.strOrigen, "RPC", StringComparison.OrdinalIgnoreCase) &&
                nombresCatalogo.Contains(NormalizarTexto(x.strProceso)));

            objDataProceso.lstLibrasParticion.AddRange(librasParticion.AListaDto("RPC"));

            _objLogger.LogInformation("[MotorProcesoParametro][Config][BaseCU] Base RPC reconstruida desde matriz. " +
                "Procesos={Procesos}; FilasProcesadas={Filas}; ReglasActivas={Reglas}.", totales.Count, procesados.Count, matriz.CantidadReglas);
        }


        /// <summary>
        /// Flujo monetario RPC definitivo.
        ///
        /// Regla general:
        ///     Monto proceso = costo unitario * libras físicas aplicables
        ///
        /// La matriz decide si el proceso pertenece al TIPOLIQ y la evidencia física
        /// decide las libras. Tarifas específicas de producto/talla se conservan.
        /// </summary>
        public void AsignarCostosProcesosRepro(DataProcesoParam objDataProceso)
        {
            if (objDataProceso == null)
                throw new ArgumentNullException(nameof(objDataProceso));

            if (objDataProceso.lstProcesoRpc == null)
                throw new ArgumentNullException(nameof(objDataProceso.lstProcesoRpc));

            if (objDataProceso.lstLiqRepro == null)
                throw new ArgumentNullException(nameof(objDataProceso.lstLiqRepro));

            var matriz = new MatrizProcesoParametroRuntime(objDataProceso.objConfiguracionCostoProductivo);

            ConcurrentDictionary<string, decimal> dicCostos =
                ProcesoResultadoDto.ConstruirDictProc(objDataProceso.lstProcesoRpc);

            CostosUnitarios costos =
                CostosUnitarios.ExtraerCostosUnitarios(dicCostos);

            ConcurrentDictionary<string, string> dicPuente =
                ProcesoResultadoDto.ConstruirDiccionarioPuente(objDataProceso.lstProcesoTarifa ?? new List<ProcesoResultadoDto>());

            ConcurrentDictionary<string, decimal> dicTarifasAdicionales =
                ProcesoResultadoDto.ConstruirDictTarifasPorCodigo(objDataProceso.lstProcesoTarifa ?? new List<ProcesoResultadoDto>(), dicPuente);

            _dicInfoProd = (objDataProceso.lstInfoProd ?? new List<InfoProd>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.strProCodcor))
                .GroupBy(x => x.strProCodcor)
                .ToDictionary(g => g.Key, g => g.First());

            List<MatPrimaReproceso> procesados =
                objDataProceso.lstLiqRepro
                .Where(x => string.Equals(x.strAgrupacion, "2. PROCESADO", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var auditoria = new Dictionary<string, AuditoriaProcesoConfigurado>(StringComparer.OrdinalIgnoreCase);

            foreach (MatPrimaReproceso liq in procesados)
            {
                liq.objInfoProd = _dicInfoProd.GetValueOrDefault(liq.intCodProd.ToString(), null);

                LimpiarCostosProcesoRpc(liq);

                string tipo = NormalizarConfig(liq.strTipCod);
                decimal libras = (decimal)liq.dbLibras;

                liq.dcExcedente = Math.Round(libras * costos.dcExcedente, 2);
                RegistrarAuditoria(auditoria, "EXC", tipo, true, libras, costos.dcExcedente, liq.dcExcedente ?? 0m, "LEGACY_SIN_MATRIZ");

                // Tarifario adicional existente (CAM/R1/R2/etc.).
                // Su tarifa continúa viniendo de lstProcesoTarifa; no se reemplaza por
                // un valor de la matriz porque la matriz solo gobierna procesos de costo.
                decimal tarifaAdicional = dicTarifasAdicionales.GetValueOrDefault(liq.strTipCod, 0m);

                liq.dcTarifaProc = tarifaAdicional != 0m
                    ? Math.Round(libras * tarifaAdicional, 4)
                    : 0m;

                RegistrarAuditoria(auditoria, "TARIFA_ADICIONAL", tipo, tarifaAdicional != 0m, libras, tarifaAdicional, liq.dcTarifaProc ?? 0m,
                    tarifaAdicional != 0m ? "TB_PROCESOCOSTEO/TARIFA" : "SIN_TARIFA");

                // -------------------------------------------------------------
                // PRIMARIOS
                // -------------------------------------------------------------
                AplicarMontoUnitarioConfigurado(matriz, auditoria, "LOG", tipo, libras, costos.dcLogistica, true,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoPrimario.dcLogistica = monto;
                        // MatPrimaReproceso no expone dcLogistica plano; el monto queda
                        // correctamente dentro de ProcesoPrimario y en Total Proceso.
                    });

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "REC", tipo, libras, costos.dcRecepcion, true,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoPrimario.dcRecepcion = monto;
                        liq.dcRecepcion = cu;
                    });

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "CLA", tipo, libras, costos.dcClasificacion, true,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoPrimario.dcClasificacion = monto;
                        liq.dcClasificacion = cu;
                    });

                // CAJ todavía no existe en la matriz de drivers. Si negocio lo agrega,
                // la matriz toma control automáticamente. Hasta entonces conserva el
                // comportamiento histórico DE/R6/R7/UNI.
                bool cajaLegacy = TipoCajaLegacy(tipo);
                AplicarMontoUnitarioConfigurado(matriz, auditoria, "CAJ", tipo, libras, costos.dcCajas, cajaLegacy,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoPrimario.dcCajas = monto;
                        liq.dcCajas = cu;
                    }, true);

                // COD está en tb_procesocosto pero el DTO histórico no posee una celda
                // monetaria independiente de Codificación. Se audita su configuración
                // para no mezclar silenciosamente el costo con Cajas/Clasificación.
                if (matriz.Aplica("COD", tipo, true, false))
                {
                    RegistrarAuditoria(auditoria, "COD", tipo, false, libras, 0m, 0m, "CONFIG_SIN_DESTINO_DTO");
                }

                // -------------------------------------------------------------
                // PRESENTACIÓN — mantener tarifas actuales
                // -------------------------------------------------------------
                AplicarPresentacionConfigurada(liq, tipo, matriz, auditoria);

                // -------------------------------------------------------------
                // CONGELACIÓN
                // -------------------------------------------------------------
                string congelamiento = NormalizarConfig(liq.strCongeProduc);

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "BRI", tipo, libras, costos.dcBrine, congelamiento == "BRINE",
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoCongelacion.dcBrine = monto;
                        liq.dcBrine = cu;
                    });

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "IQF", tipo, libras, costos.dcIQF, congelamiento == "IQF",
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoCongelacion.dcIQF = monto;
                        liq.dcIQF = cu;
                    });

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "TUN", tipo, libras, costos.dcTunel, congelamiento == "BLOCK" || congelamiento == "SEMI IQF",
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoCongelacion.dcTunel = monto;
                        liq.dcTunel = cu;
                    });

                // -------------------------------------------------------------
                // SECUNDARIOS
                // -------------------------------------------------------------
                AplicarMontoUnitarioConfigurado(matriz, auditoria, "DES", tipo, libras, costos.dcDescabezado, liq.blEsDescabezado,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoSecundario.dcDescabezado = monto;
                        liq.dcDescabezado = cu;
                    });

                AplicarPeladoConfigurado(liq, tipo, matriz, auditoria);

                bool tieneRecetaHidratacion =
                    !string.IsNullOrWhiteSpace(liq.strRecNombre) &&
                    !string.Equals(liq.strRecTipo, "COC", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(liq.strLotTipo, "VA", StringComparison.OrdinalIgnoreCase);

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "HID", tipo, libras, costos.dcHidratacion, tieneRecetaHidratacion,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoSecundario.dcHidratacion = monto;
                        liq.dcHidratacion = cu;
                    });

                bool esCocido =
                    string.Equals(liq.strRecTipo, "COC", StringComparison.OrdinalIgnoreCase);

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "COC", tipo, libras, costos.dcCocido, esCocido,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoSecundario.dcCocido = monto;
                        liq.dcCocido = cu;
                    });

                // Descongelado se asigna por lote después del foreach porque su base
                // física está en RECIBIDO y se prorratea sobre PROCESADO.

                // -------------------------------------------------------------
                // COSTOS DIRECTOS / INDIRECTOS
                // -------------------------------------------------------------
                AplicarMontoUnitarioConfigurado(matriz, auditoria, "CDV", tipo, libras, costos.dcCostDirectoVar, true,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoCostFijo.dcCostoVariable = monto;
                        liq.dcCostFijVaria = cu;
                    });

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "CDF", tipo, libras, costos.dcCostDirectoFij, true,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoCostFijo.dcCostoFijo = monto;
                        liq.dcCostFijFijo = cu;
                    });

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "CIV", tipo, libras, costos.dcCostIndirVar, true,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoCostIndirecto.dcCostoVariable = monto;
                        liq.dcCostIndirVaria = cu;
                    });

                AplicarMontoUnitarioConfigurado(matriz, auditoria, "CIF", tipo, libras, costos.dcCostIndirFij, true,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.ProcesoCostIndirecto.dcCostoFijo = monto;
                        liq.dcCostIndirFijo = cu;
                    });

                // COP también es una excepción actual: no tiene fila en la matriz.
                bool copackingFisico = liq.intCodCopacking != 0;
                AplicarMontoUnitarioConfigurado(matriz, auditoria, "COP", tipo, libras, costos.dcCopacking, copackingFisico,
                    delegate(decimal monto, decimal cu)
                    {
                        liq.dcCostoCopacking = monto;
                    }, true);

                // Total preliminar SIN DSC. Se vuelve a calcular después de distribuir
                // Descongelado para evitar el defecto del flujo anterior.
                liq.dcCostTotalProc = CalcularCostoTotalConfiguradoRpc(liq);
            }

            AplicarDescongeladoConfigurado(objDataProceso, procesados, matriz, auditoria);

            // IMPORTANTE: DSC se agregó después del loop. Recalcular TODOS los totales.
            foreach (MatPrimaReproceso liq in procesados)
            {
                liq.dcCostTotalProc = CalcularCostoTotalConfiguradoRpc(liq);
            }

            foreach (AuditoriaProcesoConfigurado item in auditoria.Values
                     .OrderBy(x => x.strProceso)
                     .ThenBy(x => x.strTipo))
            {
                _objLogger.LogInformation("[MotorProcesoParametro][Config][Resumen] Pc={Pc}; Tip={Tip}; " +
                    "Aplicadas={Aplicadas}; Rechazadas={Rechazadas}; Lbs={Lbs:N4}; " +
                    "Monto={Monto:N4}; Fuente={Fuente}.", item.strProceso, item.strTipo, item.intAplicadas, item.intRechazadas, item.dcLibras, item.dcMonto, item.strFuente);
            }
        }


        private void AplicarPresentacionConfigurada(MatPrimaReproceso liq, string tipo, MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            InfoProd info = liq.objInfoProd;

            bool tarifaDecoradoDisponible =
                info != null &&
                info.dcCostoDec > 0m;

            bool condicionDecorado =
                liq.blDecorado &&
                tarifaDecoradoDisponible;

            bool aplicaDecorado = matriz.Aplica("DEC", tipo, condicionDecorado, false);

            decimal lbsDecorado = (decimal)liq.dbLibras;
            decimal tarifaDecorado = info == null ? 0m : info.dcCostoDec;

            decimal montoDecorado = aplicaDecorado
                ? Math.Round(lbsDecorado * tarifaDecorado, 4)
                : 0m;

            liq.ProcesoPresentacion.dcDecorado = montoDecorado;
            liq.dcDecorado = montoDecorado;

            RegistrarAuditoria(auditoria, "DEC", tipo, aplicaDecorado, aplicaDecorado ? lbsDecorado : 0m, tarifaDecorado, montoDecorado, "TARIFA_EMBALAJE_DECORADO");

            bool tarifaRetractiladoDisponible =
                info != null &&
                info.dcCostoRetrac > 0m;

            bool condicionRetractilado =
                liq.blRetractilado &&
                tarifaRetractiladoDisponible;

            bool aplicaRetractilado = matriz.Aplica("RET", tipo, condicionRetractilado, false);

            decimal lbsRetractilado = liq.dcLibrasRetractilado.HasValue
                ? liq.dcLibrasRetractilado.Value
                : (decimal)liq.dbLibras;

            decimal tarifaRetractilado = info == null ? 0m : info.dcCostoRetrac;

            decimal montoRetractilado = aplicaRetractilado
                ? Math.Round(lbsRetractilado * tarifaRetractilado, 4)
                : 0m;

            liq.ProcesoPresentacion.dcRetractilado = montoRetractilado;
            liq.dcRetractilado = montoRetractilado;

            RegistrarAuditoria(auditoria, "RET", tipo, aplicaRetractilado, aplicaRetractilado ? lbsRetractilado : 0m, tarifaRetractilado, montoRetractilado,
                "TARIFA_EMBALAJE_RETRACTILADO");
        }


        private void AplicarPeladoConfigurado(MatPrimaReproceso liq, string tipo, MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            decimal tarifa = 0m;

            if (liq.objInfoProd != null && liq.objInfoProd.lstTarPelado != null)
            {
                var tarifaPelado = liq.objInfoProd.lstTarPelado
                    .FirstOrDefault(x => x.TpTalCodigo == liq.intCodTal);

                if (tarifaPelado != null)
                    tarifa = tarifaPelado.TpTarifa;
            }

            bool condicion =
                liq.blPelado &&
                tarifa > 0m;

            bool aplica = matriz.Aplica("PEL", tipo, condicion, false);

            decimal libras = liq.dcLibrasPelado.HasValue
                ? liq.dcLibrasPelado.Value
                : (decimal)liq.dbLibras;

            decimal monto = aplica
                ? Math.Round(libras * tarifa, 4)
                : 0m;

            liq.ProcesoSecundario.dcPelado = monto;
            liq.dcPelado = monto;

            RegistrarAuditoria(auditoria, "PEL", tipo, aplica, aplica ? libras : 0m, tarifa, monto, "TARIFA_PELADO_TALLA");
        }


        private void AplicarDescongeladoConfigurado(DataProcesoParam objDataProceso, List<MatPrimaReproceso> procesados, MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            List<MatPrimaReproceso> universo =
                objDataProceso.lstLiqReproCompleto != null &&
                objDataProceso.lstLiqReproCompleto.Count > 0
                    ? objDataProceso.lstLiqReproCompleto
                    : (objDataProceso.lstLiqRepro ?? new List<MatPrimaReproceso>());

            ProcesoResultadoDto parametroDsc =
                (objDataProceso.lstProcesoRpc ?? new List<ProcesoResultadoDto>())
                .FirstOrDefault(x => string.Equals(NormalizarTexto(x.strDescripcion), "DESCONGELADO", StringComparison.OrdinalIgnoreCase));

            decimal tarifa = ResolverTarifaDescongelado(objDataProceso, parametroDsc);

            List<MatPrimaReproceso> recibidos = universo
                .Where(x =>
                    string.Equals(x.strAgrupacion, "1. RECIBIDO", StringComparison.OrdinalIgnoreCase) &&
                    x.blEsDescongelado == true &&
                    x.dbLibras > 0)
                .ToList();

            foreach (var grupo in recibidos.GroupBy(x => new { x.intLotNumero, x.intLoteUnificado }))
            {
                // Un mismo lote puede traer más de un TIPOLIQ. Solo se incluyen las
                // líneas cuya matriz DSC/TIPOLIQ esté activa.
                List<MatPrimaReproceso> elegibles = grupo
                    .Where(x => matriz.Aplica("DSC", NormalizarConfig(x.strTipCod), true, false))
                    .ToList();

                decimal lbsRecibidas = (decimal)elegibles.Sum(x => x.dbLibras);
                if (lbsRecibidas <= 0m)
                    continue;

                decimal montoLote = Math.Round(lbsRecibidas * tarifa, 6);

                List<MatPrimaReproceso> salidas = procesados
                    .Where(x => x.intLotNumero == grupo.Key.intLotNumero && x.intLoteUnificado == grupo.Key.intLoteUnificado && x.dbLibras > 0)
                    .ToList();

                decimal lbsSalida = (decimal)salidas.Sum(x => x.dbLibras);
                if (lbsSalida <= 0m)
                    continue;

                decimal asignado = 0m;

                for (int i = 0; i < salidas.Count; i++)
                {
                    MatPrimaReproceso salida = salidas[i];

                    decimal monto = i == salidas.Count - 1
                        ? Math.Round(montoLote - asignado, 6)
                        : Math.Round(montoLote * (decimal)salida.dbLibras / lbsSalida, 6);

                    if (i < salidas.Count - 1)
                        asignado += monto;

                    salida.ProcesoSecundario.dcDescongelado =
                        (salida.ProcesoSecundario.dcDescongelado ?? 0m) + monto;

                    salida.dcDescongelado = tarifa;
                }

                foreach (MatPrimaReproceso origen in elegibles)
                {
                    RegistrarAuditoria(auditoria, "DSC", NormalizarConfig(origen.strTipCod), true, (decimal)origen.dbLibras, tarifa, Math.Round((decimal)origen.dbLibras * tarifa, 6),
                        "TARIFA_DSC_RECIBIDO");
                }
            }
        }


        private void AplicarMontoUnitarioConfigurado(MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria, string proceso, string tipo,
            decimal libras, decimal costoUnitario, bool condicionFisica, Action<decimal, decimal> asignar, bool permitirFallbackSinMatriz = false)
        {
            bool aplica = matriz.Aplica(proceso, tipo, condicionFisica, permitirFallbackSinMatriz);

            int factor = matriz.Factor(proceso, tipo, permitirFallbackSinMatriz);

            decimal monto = aplica && costoUnitario != 0m && libras > 0m
                ? Math.Round(libras * costoUnitario * factor, 4)
                : 0m;

            asignar(monto, aplica ? costoUnitario : 0m);

            string fuente = matriz.Fuente(proceso, tipo, permitirFallbackSinMatriz);

            RegistrarAuditoria(auditoria, proceso, tipo, aplica, aplica ? libras : 0m, costoUnitario, monto, fuente);

            // Log por fila deshabilitado: genera ~16 líneas por liquidación y ralentiza la ejecución.
            //_objLogger.LogDebug("[MotorProcesoParametro][Config][Linea] Pc={Pc}; Tip={Tip}; " +
            //    "CondFisica={Cond}; Aplica={Aplica}; Lbs={Lbs:N4}; CU={CU:N6}; " +
            //    "Factor={Factor}; Monto={Monto:N4}; Fuente={Fuente}.", proceso, tipo, condicionFisica, aplica, libras, costoUnitario, factor, monto, fuente);
        }


        private static void AcumularLibrasConfiguradas(MatrizProcesoParametroRuntime matriz, Dictionary<string, decimal> totales, AcumuladorLibrasParticion particiones, string proceso,
            string tipo, string particion, decimal libras, bool condicionFisica, bool permitirFallbackSinMatriz)
        {
            if (libras <= 0m)
                return;

            if (!matriz.Aplica(proceso, tipo, condicionFisica, permitirFallbackSinMatriz))
                return;

            int factor = matriz.Factor(proceso, tipo, permitirFallbackSinMatriz);

            decimal librasEfectivas = libras * factor;

            decimal actual;
            totales.TryGetValue(proceso, out actual);
            totales[proceso] = actual + librasEfectivas;

            ProcesoCostoMantenimientoDto catalogo = matriz.ObtenerProceso(proceso);
            string nombre = catalogo == null || string.IsNullOrWhiteSpace(catalogo.strNombre)
                ? proceso
                : catalogo.strNombre.Trim();

            particiones.Acumular(nombre, particion, librasEfectivas);
        }


        private static decimal ResolverTarifaDescongelado(DataProcesoParam objDataProceso, ProcesoResultadoDto parametroDsc)
        {
            if (parametroDsc != null)
            {
                decimal cu = parametroDsc.dcCostUnitario ?? 0m;
                if (cu > 0m)
                    return cu;

                if (parametroDsc.dcLibras > 0m && parametroDsc.dcValor > 0m)
                    return parametroDsc.dcValor / parametroDsc.dcLibras;
            }

            // Fallback histórico documentado por el SP actual. Queda en UN solo lugar
            // y se puede eliminar cuando la tarifa DSC sea una columna parametrizable.
            return 0.02m;
        }


        private static decimal CalcularCostoTotalConfiguradoRpc(MatPrimaReproceso liq)
        {
            return
                (liq.ProcesoPrimario.dcLogistica ?? 0m) +
                (liq.ProcesoPrimario.dcRecepcion ?? 0m) +
                (liq.ProcesoPrimario.dcClasificacion ?? 0m) +
                (liq.ProcesoPrimario.dcCajas ?? 0m) +

                (liq.ProcesoPresentacion.dcDecorado ?? 0m) +
                (liq.ProcesoPresentacion.dcRetractilado ?? 0m) +

                (liq.ProcesoCongelacion.dcBrine ?? 0m) +
                (liq.ProcesoCongelacion.dcTunel ?? 0m) +
                (liq.ProcesoCongelacion.dcIQF ?? 0m) +

                (liq.ProcesoSecundario.dcDescabezado ?? 0m) +
                (liq.ProcesoSecundario.dcDescongelado ?? 0m) +
                (liq.ProcesoSecundario.dcPelado ?? 0m) +
                (liq.ProcesoSecundario.dcHidratacion ?? 0m) +
                (liq.ProcesoSecundario.dcCocido ?? 0m) +

                (liq.ProcesoCostFijo.dcCostoVariable ?? 0m) +
                (liq.ProcesoCostFijo.dcCostoFijo ?? 0m) +
                (liq.ProcesoCostIndirecto.dcCostoFijo ?? 0m) +
                (liq.ProcesoCostIndirecto.dcCostoVariable ?? 0m) +

                (liq.dcCostoCopacking ?? 0m) +
                (liq.dcTarifaProc ?? 0m) +
                (liq.dcExcedente ?? 0m);
        }


        private static void LimpiarCostosProcesoRpc(MatPrimaReproceso liq)
        {
            liq.dcExcedente = 0m;
            liq.ProcesoPrimario.dcLogistica = 0m;
            liq.ProcesoPrimario.dcRecepcion = 0m;
            liq.ProcesoPrimario.dcClasificacion = 0m;
            liq.ProcesoPrimario.dcCajas = 0m;

            liq.ProcesoPresentacion.dcDecorado = 0m;
            liq.ProcesoPresentacion.dcRetractilado = 0m;

            liq.ProcesoCongelacion.dcBrine = 0m;
            liq.ProcesoCongelacion.dcIQF = 0m;
            liq.ProcesoCongelacion.dcTunel = 0m;

            liq.ProcesoSecundario.dcDescabezado = 0m;
            liq.ProcesoSecundario.dcDescongelado = 0m;
            liq.ProcesoSecundario.dcPelado = 0m;
            liq.ProcesoSecundario.dcHidratacion = 0m;
            liq.ProcesoSecundario.dcCocido = 0m;

            liq.ProcesoCostFijo.dcCostoVariable = 0m;
            liq.ProcesoCostFijo.dcCostoFijo = 0m;
            liq.ProcesoCostIndirecto.dcCostoVariable = 0m;
            liq.ProcesoCostIndirecto.dcCostoFijo = 0m;

            liq.dcCostoCopacking = 0m;
            liq.dcTarifaProc = 0m;
        }


        private static void RegistrarAuditoria(Dictionary<string, AuditoriaProcesoConfigurado> auditoria, string proceso, string tipo, bool aplica, decimal libras, decimal costoUnitario, decimal monto, string fuente)
        {
            string clave = proceso + "|" + tipo;
            AuditoriaProcesoConfigurado item;

            if (!auditoria.TryGetValue(clave, out item))
            {
                item = new AuditoriaProcesoConfigurado
                {
                    strProceso = proceso, strTipo = tipo, strFuente = fuente
                };
                auditoria[clave] = item;
            }

            if (aplica)
                item.intAplicadas++;
            else
                item.intRechazadas++;

            item.dcLibras += libras;
            item.dcMonto += monto;

            if (costoUnitario != 0m)
                item.dcUltimoCostoUnitario = costoUnitario;

            if (!string.IsNullOrWhiteSpace(fuente))
                item.strFuente = fuente;
        }


        private static bool TipoCajaLegacy(string tipo)
        {
            // Única excepción temporal que todavía no existe en la matriz del script.
            return tipo == "DE" ||
                   tipo == "R6" ||
                   tipo == "R7" ||
                   tipo == "UNI";
        }


        private static string NormalizarConfig(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }


        private static string NormalizarTexto(string value)
        {
            return (value ?? string.Empty)
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                            System.Globalization.UnicodeCategory.NonSpacingMark)
                .Aggregate(new System.Text.StringBuilder(),
                    delegate(System.Text.StringBuilder sb, char c)
                    {
                        sb.Append(c);
                        return sb;
                    })
                .ToString()
                .Trim()
                .ToUpperInvariant();
        }


        private sealed class AuditoriaProcesoConfigurado
        {
            public string strProceso { get; set; }
            public string strTipo { get; set; }
            public int intAplicadas { get; set; }
            public int intRechazadas { get; set; }
            public decimal dcLibras { get; set; }
            public decimal dcMonto { get; set; }
            public decimal dcUltimoCostoUnitario { get; set; }
            public string strFuente { get; set; }

            public AuditoriaProcesoConfigurado()
            {
                strProceso = string.Empty;
                strTipo = string.Empty;
                strFuente = string.Empty;
            }
        }


        /// <summary>
        /// Lookup O(1) de la matriz. Se construye una sola vez por llamada del motor.
        /// </summary>
        private sealed class MatrizProcesoParametroRuntime
        {
            private readonly Dictionary<string, ProcesoCostoAplicacionDto> _dicReglas;
            private readonly Dictionary<string, ProcesoCostoMantenimientoDto> _dicProcesos;
            private readonly HashSet<string> _procesosConMatriz;

            public MatrizProcesoParametroRuntime(ConfiguracionCostoProductivoRuntimeDto configuracion)
            {
                configuracion = configuracion ??
                    new ConfiguracionCostoProductivoRuntimeDto();

                _dicProcesos = (configuracion.lstProcesos ??
                        new List<ProcesoCostoMantenimientoDto>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.strCodigo))
                    .GroupBy(x => NormalizarConfig(x.strCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                _dicReglas = (configuracion.lstAplicaciones ??
                        new List<ProcesoCostoAplicacionDto>())
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.strPcCodigo) &&
                        !string.IsNullOrWhiteSpace(x.strTipoProcesoCodigo))
                    .GroupBy(x => Clave(x.strPcCodigo, x.strTipoProcesoCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

                _procesosConMatriz = (configuracion.lstAplicaciones ??
                        new List<ProcesoCostoAplicacionDto>())
                    .Select(x => NormalizarConfig(x.strPcCodigo))
                    .Where(x => x.Length > 0)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            public IEnumerable<ProcesoCostoMantenimientoDto> ProcesosCatalogo
            {
                get { return _dicProcesos.Values; }
            }

            public int CantidadReglas
            {
                get { return _dicReglas.Count; }
            }

            public ProcesoCostoMantenimientoDto ObtenerProceso(string proceso)
            {
                ProcesoCostoMantenimientoDto item;
                return _dicProcesos.TryGetValue(NormalizarConfig(proceso), out item)
                    ? item
                    : null;
            }

            public bool Aplica(string proceso, string tipo, bool condicionFisica, bool permitirFallbackSinMatriz)
            {
                if (!condicionFisica)
                    return false;

                string pc = NormalizarConfig(proceso);
                string tip = NormalizarConfig(tipo);
                ProcesoCostoAplicacionDto regla;

                if (_dicReglas.TryGetValue(Clave(pc, tip), out regla))
                {
                    string config = NormalizarConfig(regla.strConfig);
                    return config.Length > 0 &&
                           config != "NO" &&
                           config != "NA";
                }

                // Solo se usa para excepciones que el script actual no parametriza
                // (CAJ/COP). Si mañana aparecen filas, nunca entra por aquí.
                return permitirFallbackSinMatriz &&
                       !_procesosConMatriz.Contains(pc);
            }

            public int Factor(string proceso, string tipo, bool permitirFallbackSinMatriz)
            {
                ProcesoCostoAplicacionDto regla;

                if (_dicReglas.TryGetValue(Clave(proceso, tipo), out regla))
                {
                    return string.Equals(NormalizarConfig(regla.strConfig), "X2", StringComparison.OrdinalIgnoreCase)
                        ? 2
                        : 1;
                }

                return permitirFallbackSinMatriz ? 1 : 0;
            }

            public string Fuente(string proceso, string tipo, bool permitirFallbackSinMatriz)
            {
                ProcesoCostoAplicacionDto regla;

                if (_dicReglas.TryGetValue(Clave(proceso, tipo), out regla))
                {
                    return "MATRIZ:" + NormalizarConfig(regla.strConfig);
                }

                return permitirFallbackSinMatriz
                    ? "LEGACY_SIN_MATRIZ"
                    : "SIN_CONFIG";
            }

            private static string Clave(string proceso, string tipo)
            {
                return NormalizarConfig(proceso) + "|" + NormalizarConfig(tipo);
            }
        }

        #endregion


        #region Asignacion Costos Proceso Fresco
        public void AsignarCostosProcesosFresco(DataProcesoParam objDataProceso)
        {
            ConcurrentDictionary<string, decimal> dictCosto;
            try
            {
                if (objDataProceso.lstLiqFresco == null) throw new ArgumentNullException(nameof(objDataProceso.lstLiqFresco));

                if (objDataProceso.lstProcesoFrs == null) throw new ArgumentNullException(nameof(objDataProceso.lstProcesoFrs));

                MatrizProcesoParametroRuntime matriz = new MatrizProcesoParametroRuntime(objDataProceso.objConfiguracionCostoProductivo);
                var auditoria = new Dictionary<string, AuditoriaProcesoConfigurado>(StringComparer.OrdinalIgnoreCase);
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
                        dicUnitariosParticion, matriz, auditoria);
                }

                foreach (AuditoriaProcesoConfigurado item in auditoria.Values.OrderBy(x => x.strProceso).ThenBy(x => x.strTipo))
                {
                    _objLogger.LogInformation("[MotorProcesoParametro][Config][Resumen] Pc={Pc}; Tip={Tip}; " +
                        "Aplicadas={Aplicadas}; Rechazadas={Rechazadas}; Lbs={Lbs:N4}; " +
                        "Monto={Monto:N4}; Fuente={Fuente}.", item.strProceso, item.strTipo, item.intAplicadas, item.intRechazadas, item.dcLibras, item.dcMonto, item.strFuente);
                }
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[AsignarCostosProcesosFresco] : {ex.Message}");
                throw;
            }
        }


        private void AplicarCostosALiquidacion(
            LiquidacionResultado objLiq,
            CostosUnitarios objCostUni,
            List<int> lstCongTunel,
            IReadOnlyDictionary<(string Pc, string Particion), decimal> dicUnitariosParticion,
            MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            AplicarProcesoPrimario(objLiq, objCostUni, dicUnitariosParticion, matriz, auditoria);
            AplicarProcesoPresentacion(objLiq, objCostUni, matriz, auditoria);
            AplicarProcesoCongelacion(objLiq, objCostUni, lstCongTunel, matriz, auditoria);
            AplicarProcesoSecundario(objLiq, objCostUni, matriz, auditoria);
            AplicarCostosDirectos(objLiq, objCostUni, matriz, auditoria);
            AplicarCostosIndirectos(objLiq, objCostUni, matriz, auditoria);
            AplicarCostosCopacking(objLiq, objCostUni, matriz, auditoria);

            objLiq.dcCostTotalProc = CalcularCostoTotal(objLiq);
            objLiq.dcTotalDolSum = (objLiq.dcCostTotalProc ?? 0m) + (objLiq.dcCostoTotalMatEmp ?? 0m) + (decimal) (objLiq.dcTotalDol ?? 0);
            if (objLiq.dcLibras > 0 && objLiq.dcCostoTotXLibra == null)
            {
                objLiq.dcCostoTotXLibra = Math.Truncate((objLiq.dcTotalDolSum / (decimal)objLiq.dcLibras) * 100) / 100;
                objLiq.dcValidador = (decimal)objLiq.dcCostoTotXLibra - (decimal)objLiq.dcPrecioCompra;
            }
        }

        private void AplicarProcesoPrimario(
             LiquidacionResultado objLiq,
             CostosUnitarios c,
             IReadOnlyDictionary<(string Pc, string Particion), decimal> dicUnitariosParticion,
             MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            decimal libras = (decimal)objLiq.dcLibras;

            string particion = ParticionCosteo.ClasificarFrs(
                objLiq.strProClas01,
                objLiq.strProClas05) ?? string.Empty;

            decimal cuLogistica = ObtenerUnitarioParticion(dicUnitariosParticion, "LOG", particion, c.dcLogistica);
            decimal cuRecepcion = ObtenerUnitarioParticion(dicUnitariosParticion, "REC", particion, c.dcRecepcion);
            decimal cuClasificacion = ObtenerUnitarioParticion(dicUnitariosParticion, "CLA", particion, c.dcClasificacion);

            bool aplicaLog = matriz.Aplica("LOG", "PFR", true, false);
            bool aplicaRec = matriz.Aplica("REC", "PFR", true, false);
            bool aplicaCla = matriz.Aplica("CLA", "PFR", true, false);
            bool aplicaCaj = matriz.Aplica("CAJ", "PFR", true, false);
            objLiq.ProcesoPrimario.dcLogistica = aplicaLog ? Math.Round(libras * cuLogistica, 4) : 0m;
            objLiq.ProcesoPrimario.dcRecepcion = aplicaRec ? Math.Round(libras * cuRecepcion, 4) : 0m;
            objLiq.ProcesoPrimario.dcClasificacion = aplicaCla ? Math.Round(libras * cuClasificacion, 4) : 0m;
            objLiq.ProcesoPrimario.dcCajas = aplicaCaj ? Math.Round(libras * c.dcCajas, 4) : 0m;
            RegistrarAuditoriaFresco(matriz, auditoria, "LOG", true, aplicaLog, libras, cuLogistica, objLiq.ProcesoPrimario.dcLogistica ?? 0m);
            RegistrarAuditoriaFresco(matriz, auditoria, "REC", true, aplicaRec, libras, cuRecepcion, objLiq.ProcesoPrimario.dcRecepcion ?? 0m);
            RegistrarAuditoriaFresco(matriz, auditoria, "CLA", true, aplicaCla, libras, cuClasificacion, objLiq.ProcesoPrimario.dcClasificacion ?? 0m);
            RegistrarAuditoriaFresco(matriz, auditoria, "CAJ", true, aplicaCaj, libras, c.dcCajas, objLiq.ProcesoPrimario.dcCajas ?? 0m);

            objLiq.dcExcedente = Math.Round(libras * c.dcExcedente, 2);
            RegistrarAuditoriaFresco(matriz, auditoria, "EXC", true, true, libras, c.dcExcedente, objLiq.dcExcedente ?? 0m, "LEGACY_SIN_MATRIZ");
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

        private void AplicarProcesoPresentacion(LiquidacionResultado liq, CostosUnitarios c,
            MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            InfoProd objInfoProd = _dicInfoProd.GetValueOrDefault(liq.intCodProd.ToString(), null);
            try
            {

                bool blDecorado = objInfoProd != null ? _hshListDecora.Contains(objInfoProd.intProDecora) : false;
                bool blRetractilado = objInfoProd != null ? _hshListRetrac.Contains(objInfoProd.intProRetracti) : false;
                bool aplicaDecorado = matriz.Aplica("DEC", "PFR", blDecorado, false);
                bool aplicaRetractilado = matriz.Aplica("RET", "PFR", blRetractilado, false);

                if (objInfoProd != null)
                {
                    // Mismo patrón que Retractilado: libras propias del lote × tarifa
                    // por embalaje (InfoProd.dcCostoDec, ya resuelta desde
                    // tb_tarifaDecoradosRetractilado). Antes quedaba hardcodeado en 0
                    // porque referenciaba InfoProd.dcCostoDecora, un campo que no existe.
                    liq.ProcesoPresentacion.dcDecorado =
                        aplicaDecorado ? Math.Round((decimal)(liq.dcLibrasDecorado ?? 0) * objInfoProd.dcCostoDec, 4) : 0;

                    liq.ProcesoPresentacion.dcRetractilado =
                        aplicaRetractilado ? Math.Round((decimal)(liq.dcLibrasRetractilado ?? 0) * objInfoProd.dcCostoRetrac , 4)
                        : 0;
                    liq.dcCostRectra = aplicaRetractilado ? liq.ProcesoPresentacion.dcRetractilado : 0;
                }
                else
                {
                    liq.ProcesoPresentacion.dcDecorado = 0;
                    liq.ProcesoPresentacion.dcRetractilado = 0;
                    liq.dcCostRectra = 0;
                }

                liq.dcCostDecorado = Math.Round(c.dcDecorado, 4);
                RegistrarAuditoriaFresco(matriz, auditoria, "DEC", blDecorado, aplicaDecorado, (decimal)(liq.dcLibrasDecorado ?? 0),
                    objInfoProd?.dcCostoDec ?? 0m, liq.ProcesoPresentacion.dcDecorado ?? 0m);
                RegistrarAuditoriaFresco(matriz, auditoria, "RET", blRetractilado, aplicaRetractilado, (decimal)(liq.dcLibrasRetractilado ?? 0),
                    objInfoProd?.dcCostoRetrac ?? 0m, liq.ProcesoPresentacion.dcRetractilado ?? 0m);
            }
            catch (Exception obj)
            {
                _hshInfoProd.Add(objInfoProd);
                Console.WriteLine(obj.Message+ obj.Source);
            }
        }


        private void AplicarProcesoCongelacion(LiquidacionResultado liq, CostosUnitarios c, List<int> lstCongTunel,
            MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            decimal libras = (decimal)liq.dcLibras;

            // Brine
            bool aplicaBrine = matriz.Aplica("BRI", "PFR", liq.blBodEsBrine, false);
            if (aplicaBrine)
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
            bool condicionIqf = !lstCongTunel.Contains(liq.intProCongela) && !liq.blBodEsBrine;
            bool aplicaIqf = matriz.Aplica("IQF", "PFR", condicionIqf, false);
            liq.ProcesoCongelacion.dcIQF = aplicaIqf ? Math.Round(libras * c.dcIQF, 4) : 0;
            liq.dcCostIQF = aplicaIqf ? c.dcIQF : 0;

            // Tunel
            bool condicionTunel = lstCongTunel.Contains(liq.intProCongela) && liq.strProClas03 == "PT";
            bool aplicaTunel = matriz.Aplica("TUN", "PFR", condicionTunel, false);
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
            RegistrarAuditoriaFresco(matriz, auditoria, "BRI", liq.blBodEsBrine, aplicaBrine, libras, c.dcBrine, liq.ProcesoCongelacion.dcBrine ?? 0m);
            RegistrarAuditoriaFresco(matriz, auditoria, "IQF", condicionIqf, aplicaIqf, libras, c.dcIQF, liq.ProcesoCongelacion.dcIQF ?? 0m);
            RegistrarAuditoriaFresco(matriz, auditoria, "TUN", condicionTunel, aplicaTunel, libras, c.dcTunel, liq.ProcesoCongelacion.dcTunel ?? 0m);
        }

        private void AplicarProcesoSecundario(LiquidacionResultado liq, CostosUnitarios c,
            MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            liq.ProcesoSecundario.dcPelado = 0;
            liq.ProcesoSecundario.dcHidratacion = 0;
            liq.ProcesoSecundario.dcCocido = 0;

            liq.dcCostPelado = 0;
            liq.dcCostHidratacion = 0;
            liq.dcCostCocido = 0;

            // Descabezado
            bool aplicaDescabezado = matriz.Aplica("DES", "PFR", liq.strProClas05 == "SH", false);
            if (aplicaDescabezado)
            {
                liq.ProcesoSecundario.dcDescabezado = Math.Round((decimal)liq.dcLibras * c.dcDescabezado, 4);
                liq.dcCostDescabezado = c.dcDescabezado;
            }
            else
            {
                liq.ProcesoSecundario.dcDescabezado = 0;
                liq.dcCostDescabezado = 0;
            }
            RegistrarAuditoriaFresco(matriz, auditoria, "DES", liq.strProClas05 == "SH", aplicaDescabezado,
                (decimal)liq.dcLibras, c.dcDescabezado, liq.ProcesoSecundario.dcDescabezado ?? 0m);
        }

        private void AplicarCostosDirectos(LiquidacionResultado liq, CostosUnitarios c,
            MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            decimal libras = (decimal)liq.dcLibras;

            bool aplicaVariable = matriz.Aplica("CDV", "PFR", true, false);
            bool aplicaFijo = matriz.Aplica("CDF", "PFR", true, false);
            liq.ProcesoCostFijo.dcCostoVariable = aplicaVariable ? Math.Round(libras * c.dcCostDirectoVar, 4) : 0m;
            liq.ProcesoCostFijo.dcCostoFijo = aplicaFijo ? Math.Round(libras * c.dcCostDirectoFij, 4) : 0m;

            liq.dcCostDirVaria = aplicaVariable ? c.dcCostDirectoVar : 0m;
            liq.dcCostDirFij = aplicaFijo ? c.dcCostDirectoFij : 0m;
            RegistrarAuditoriaFresco(matriz, auditoria, "CDV", true, aplicaVariable, libras, c.dcCostDirectoVar, liq.ProcesoCostFijo.dcCostoVariable ?? 0m);
            RegistrarAuditoriaFresco(matriz, auditoria, "CDF", true, aplicaFijo, libras, c.dcCostDirectoFij, liq.ProcesoCostFijo.dcCostoFijo ?? 0m);
        }

        private void AplicarCostosIndirectos(LiquidacionResultado liq, CostosUnitarios c,
            MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            
            decimal libras = (decimal)liq.dcLibras;

            // Nota: Mapeado exactamente como en el código original línea 194-195
            bool aplicaFijo = matriz.Aplica("CIF", "PFR", true, false);
            bool aplicaVariable = matriz.Aplica("CIV", "PFR", true, false);
            liq.ProcesoCostIndirecto.dcCostoFijo = aplicaFijo ? Math.Round(libras * c.dcCostIndirFij, 4) : 0m;
            liq.ProcesoCostIndirecto.dcCostoVariable = aplicaVariable ? Math.Round(libras * c.dcCostIndirVar, 4) : 0m;

            liq.dcCostIndVaria = aplicaVariable ? c.dcCostIndirVar : 0m;
            liq.dcCostIndFij = aplicaFijo ? c.dcCostIndirFij : 0m;
            RegistrarAuditoriaFresco(matriz, auditoria, "CIF", true, aplicaFijo, libras, c.dcCostIndirFij, liq.ProcesoCostIndirecto.dcCostoFijo ?? 0m);
            RegistrarAuditoriaFresco(matriz, auditoria, "CIV", true, aplicaVariable, libras, c.dcCostIndirVar, liq.ProcesoCostIndirecto.dcCostoVariable ?? 0m);
        }

        private void AplicarCostosCopacking(LiquidacionResultado liq, CostosUnitarios c,
            MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria)
        {
            bool condicionCopacking = liq.intCodCopacking != 0 && liq.strPlanta != "SONGA";
            bool aplicaCopacking = matriz.Aplica("COP", "PFR", condicionCopacking, false);
            liq.dcCostoCopacking = liq.intCodCopacking != 0 && liq.strPlanta != "SONGA" && aplicaCopacking
                ? Math.Round((decimal)liq.dcLibras * c.dcCopacking, 4)
                : 0;
            RegistrarAuditoriaFresco(matriz, auditoria, "COP", condicionCopacking, aplicaCopacking,
                (decimal)liq.dcLibras, c.dcCopacking, liq.dcCostoCopacking ?? 0m);
        }

        private void RegistrarAuditoriaFresco(MatrizProcesoParametroRuntime matriz, Dictionary<string, AuditoriaProcesoConfigurado> auditoria,
            string proceso, bool condicionFisica, bool aplica, decimal libras, decimal costoUnitario, decimal monto, string? fuente = null)
        {
            fuente ??= matriz.Fuente(proceso, "PFR", false);
            RegistrarAuditoria(auditoria, proceso, "PFR", aplica, aplica ? libras : 0m, costoUnitario, monto, fuente);
            // En fresco la matriz solo filtra: se conserva la fórmula existente sin multiplicadores.
            // Log por fila deshabilitado: genera ~16 líneas por liquidación y ralentiza la ejecución.
            //_objLogger.LogDebug("[MotorProcesoParametro][Config][Linea] Pc={Pc}; Tip={Tip}; " +
            //    "CondFisica={Cond}; Aplica={Aplica}; Lbs={Lbs:N4}; CU={CU:N6}; " +
            //    "Factor={Factor}; Monto={Monto:N4}; Fuente={Fuente}.", proceso, "PFR", condicionFisica, aplica, libras, costoUnitario, 1, monto, fuente);
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

            // BLOQUE TEMPORAL DE DEBUG (DESCABEZADO CU CONSOLIDADO) — mantener comentado.
            //var lstDebugFrs = lstProcesoFrs.Where(x => x.strDescripcion == "Descabezado").ToList();
            //var lstDebugRpc = lstProcesoRpc.Where(x => x.strDescripcion == "Descabezado").ToList();
        }
        #endregion
    }
}

