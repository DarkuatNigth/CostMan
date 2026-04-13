using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using System.Collections.Concurrent;

namespace CostManagement.Dominio.Reglas
{
    public class MotorProcesoParametro
    {
        private readonly List<string> _lstlbsProcPrim = new List<string>() { "DE", "R6", "R7", "UNI" };
        private readonly ILogger _objLogger;

        private static readonly List<string> _lstProcPrimTiplot = new() { "DE", "R6", "R7", "UNI" };
        private static readonly List<string> _lstNotProcSecun = new() { "DE" };
        private static readonly List<string> _lstNotPresen = new() { "EN1", "SH2" };

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

        /// <summary>
        /// Procesa y asigna las libras para Fresco basándose en múltiples fuentes de datos.
        /// </summary>
        public void AsignarCostoProcesoFrs(
            List<RptGrncLibras> lstLibrasProduccion,
            List<RptCongInd> lstLotOpcon,
            List<ResumenEstiloLbsDto> lstResumenEstiloLbs,
            List<LbsCongelamiento> lstFrsConge,
            List<int> lstCongTunel,
            List<int> lstCongIqf, // Se recibe por si se requiere expandir lógica
            List<int> lstCongBrine, // Se recibe por si se requiere expandir lógica
            List<CopackingLbs> lstCopackingLbs,
            List<ProcesoResultadoDto> lstResultados,
            List<string> lstProdTermConfig,
            List<string> lstDescTotFrescoConfig)
        {
            var dicTotales = new Dictionary<string, decimal>();

            // 1. Recepción y Productos Terminados
            decimal sumRloNetas = lstLibrasProduccion.Sum(obj => obj.dcRloNetas);
            decimal sumRloProCabCol = lstLibrasProduccion.Sum(obj => obj.dcRloProCab + obj.dcRloProCol);

            dicTotales["Recepcion"] = sumRloNetas;
            foreach (var item in lstProdTermConfig.Concat(lstDescTotFrescoConfig))
            {
                dicTotales[item.Trim()] = sumRloProCabCol;
            }

            // 2. Procesos de Tratado (Cocido/Hidratación) e IQF
            foreach (var f in lstLotOpcon)
            {
                string catTratado = !string.IsNullOrEmpty(f.strRecNombre) && f.strRecTipo == "COC" ? "Cocido" :
                                    !string.IsNullOrEmpty(f.strRecNombre) && f.strTratado == "Tratado" ? "Hidratacion" : "OTROS";

                ActualizarDiccionario(dicTotales, catTratado, f.dcLotValAgr);

                if (f.strCongela == "IQF")
                {
                    decimal valorIqf = f.strLotTipo == "VA" ? f.dcLotValAgr : f.dcLotProces;
                    ActualizarDiccionario(dicTotales, "IQF", valorIqf);
                }
            }

            // 3. Descabezado
            decimal totalDescabezado = lstLibrasProduccion.Sum(obj => obj.dcRloProCol) -
                                       lstLibrasProduccion.Where(obj => obj.intRloProcesodest == 2).Sum(obj => obj.dcRloEnviad);
            ActualizarDiccionario(dicTotales, "Descabezado", totalDescabezado);

            // 4. Estilos (Excepto Descabezado)
            foreach (var f in lstResumenEstiloLbs.Where(x => x.strEstilo != "Descabezado"))
            {
                ActualizarDiccionario(dicTotales, f.strEstilo.Trim(), (decimal)f.dcLibrasDecoradas);
            }

            // 5. Congelamiento (Tunel / Brine)
            foreach (var f in lstFrsConge)
            {
                if (lstCongTunel.Contains(f.intProCongel) && f.strProClas03 == "PT")
                    ActualizarDiccionario(dicTotales, "Tunel", f.dcLibras);
                else if (f.blBodEsBrine)
                    ActualizarDiccionario(dicTotales, "Brine", f.dcLibras);
            }

            // 6. Copacking
            ActualizarDiccionario(dicTotales, "C.Copacking", lstCopackingLbs.Sum(x => x.dcLotProces));

            // Mapeo Final a los DTOs
            FinalizarAsignacion(lstResultados, dicTotales);
        }

        /// <summary>
        /// Procesa y asigna las libras para Reproceso.
        /// </summary>
        public void AsignarCostoProcesoRpc(
            List<MatPrimaReproceso> lstReprocesos,
            List<int> lstCongTunel,
            List<int> lstCongIqf,
            List<int> lstCongBrine,
            List<ProcesoResultadoDto> lstResultados,
            List<string> lstProdTermConfig,
            List<string> lstDescTotFrescoConfig)
        {
            var dicTotales = new Dictionary<string, decimal>();
            List<string> lstNotCostConge = new List<string>() { /*"BP",*/
                    "CAM","RLL","R1","CDI","R2","REC","LB04","RPY","R3","RS","DV","RVVL","BDP","VE","VR"
                };
            // 1. Costo Proceso Primario (Recepción y Productos Terminados)
            decimal dcCostProcPrim = (decimal)lstReprocesos
                .Where(x => x.strAgrupacion == "2. PROCESADO" && x.strLotTipo == "RE" && _lstlbsProcPrim.Contains(x.strTipCod))
                .Sum(obj => obj.dbLibras);
            var dclbsValAgg = (decimal)lstReprocesos.Where(x => x.strAgrupacion == "2. PROCESADO" && x.strLotTipo == "VA" && String.IsNullOrEmpty(x.strRecNombre)).Sum(obj => obj.dbLibras);

            dicTotales["Recepcion"] = dcCostProcPrim;
            foreach (var item in lstProdTermConfig)
            {
                dicTotales[item.Trim()] = dcCostProcPrim;
            }
            foreach (var item in lstDescTotFrescoConfig)
            {
                dicTotales[item.Trim()] = dclbsValAgg + dcCostProcPrim;
            }
            // 2. Filtros específicos de Reproceso
            dicTotales["Cocido"] = (decimal)lstReprocesos.Where(x => x.strAgrupacion == "2. PROCESADO" && !string.IsNullOrEmpty(x.strRecNombre) && x.strRecTipo == "COC" && x.strLotTipo == "VA").Sum(obj => obj.dbLibras);
            dicTotales["Hidratacion"] = (decimal)lstReprocesos.Where(x => x.strAgrupacion == "2. PROCESADO" && !string.IsNullOrEmpty(x.strRecNombre) && x.strRecTipo != "COC" && x.strLotTipo == "VA").Sum(obj => obj.dbLibras);
            dicTotales["Retractilado"] = (decimal)lstReprocesos.Where(x => x.strAgrupacion == "2. PROCESADO" && x.blRetractilado).Sum(obj => obj.dbLibras);
            dicTotales["Pelado"] = (decimal)lstReprocesos.Where(obj => obj.strAgrupacion == "2. PROCESADO" && obj.blPelado).Sum(obj => obj.dbLibras);
            dicTotales["Decorado"] = (decimal)lstReprocesos.Where(obj => obj.strAgrupacion == "2. PROCESADO" && obj.blDecorado).Sum(obj => obj.dbLibras);
            dicTotales["Descabezado"] = (decimal)lstReprocesos.Where(obj => obj.strAgrupacion == "2. PROCESADO" && obj.blEsDescabezado).Sum(obj => obj.dbLibras);
            dicTotales["IQF"] = (decimal)lstReprocesos
                .Where(x =>
                        x.strAgrupacion == "2. PROCESADO" && x.strCongeProduc.Trim().Equals("IQF") &&
                        !lstNotCostConge.Contains(x.strTipCod)
                        ).Sum(obj => obj.dbLibras);
            dicTotales["Brine"] = (decimal)lstReprocesos.Where(x =>
            x.strAgrupacion == "2. PROCESADO" &&
            x.strCongeProduc.Trim().Equals("BRINE") &&
            !lstNotCostConge.Contains(x.strTipCod)).Sum(obj => obj.dbLibras);
            dicTotales["Tunel"] = (decimal)lstReprocesos.Where(x =>
            x.strAgrupacion == "2. PROCESADO" &&
            (x.strCongeProduc.Trim().Equals("BLOCK") || x.strCongeProduc.Trim().Equals("SEMI IQF")) &&
            !lstNotCostConge.Contains(x.strTipCod)
            ).Sum(obj => obj.dbLibras);
            dicTotales["C.Copacking"] = (decimal)lstReprocesos.Where(x => x.strAgrupacion == "2. PROCESADO" && x.intCodCopacking != 0).Sum(obj => obj.dbLibras);

            // Mapeo Final
            FinalizarAsignacion(lstResultados, dicTotales);
        }

        private void ActualizarDiccionario(Dictionary<string, decimal> dic, string llave, decimal valor)
        {
            if (dic.ContainsKey(llave))
                dic[llave] += valor;
            else
                dic[llave] = valor;
        }

        private void FinalizarAsignacion(List<ProcesoResultadoDto> resultados, Dictionary<string, decimal> totales)
        {
            foreach (var con in resultados)
            {
                if (totales.TryGetValue(con.strDescripcion.Trim(), out decimal totalLibras))
                {
                    con.dcLibras = Math.Round(totalLibras, 2);
                }
            }
        }

        public void AsignarCostosProcesosRepro(List<ProcesoResultadoDto> lstProcesos, List<MatPrimaReproceso> lstLiquidaciones)
        {
            List<MatPrimaReproceso> lstLiqRepro;
            CostosUnitarios objCostUnit;
            ConcurrentDictionary<string, decimal> dictCosto;
            try
            {
                dictCosto = ProcesoResultadoDto.ConstruirDictProc(lstProcesos);

                objCostUnit = CostosUnitarios.ExtraerCostosUnitarios(dictCosto);

                lstLiqRepro = lstLiquidaciones
                    .Where(obj => obj.strAgrupacion == "2. PROCESADO").ToList();

                foreach (var objLiq in lstLiqRepro)
                    AplicarCostosALiquidacion(objLiq, objCostUnit);
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
                                        CostosUnitarios c)
        {
            AplicarProcesoPrimario(liq, c);
            AplicarProcesoPresentacion(liq, c);
            AplicarProcesoCongelacion(liq, c);
            AplicarProcesoSecundario(liq, c);
            AplicarCostosDirectos(liq, c);
            AplicarCostosIndirectos(liq, c);
            AplicarCopacking(liq, c);
            liq.dcCostTotalProc = CalcularCostoTotal(liq);
        }

        private static void AplicarProcesoPrimario(MatPrimaReproceso liq, CostosUnitarios c)
        {
            if (!_lstProcPrimTiplot.Contains(liq.strTipCod)) return;

            decimal libras = (decimal)liq.dbLibras;
            liq.ProcesoPrimario.dcRecepcion = Math.Round(libras * c.dcRecepcion, 4);
            liq.ProcesoPrimario.dcClasificacion = Math.Round(libras * c.dcClasificacion, 4);
            liq.ProcesoPrimario.dcCodificacion = Math.Round(libras * c.dcCodificacion, 4);

            liq.dcRecepcion = c.dcRecepcion;
            liq.dcClasificacion = c.dcClasificacion;
            liq.dcCodificacion = c.dcCodificacion;
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
            decimal valorSal = ((liq.dcCthSallbs ?? 0m) * (liq.dcRecPorSal ?? 0m)) / 2.2046m;
            decimal valorHidra = (liq.dcCthHidlbs ?? 0m) * (liq.dcValorHidra ?? 0m);
            decimal costHidraSal = valorSal * (liq.dcValorSal ?? 0m);
            decimal totalHidra = costHidraSal + valorHidra;

            bool tieneReceta = !string.IsNullOrEmpty(liq.strRecNombre);
            liq.ProcesoSecundario.dcHidratacion = tieneReceta ? Math.Round(totalHidra, 4) : 0;
            liq.dcHidratacion = tieneReceta ? totalHidra : 0;

            // Descabezado
            liq.ProcesoSecundario.dcDescabezado = liq.blEsDescabezado
                ? Math.Round(libras * c.dcDescabezado, 4) : 0;
            liq.dcDescabezado = liq.blEsDescabezado ? c.dcDescabezado : 0;

            // Cocido
            bool esCocido = !string.IsNullOrEmpty(liq.strRecTipo) && liq.strRecTipo == "COC";
            liq.ProcesoSecundario.dcCocido = esCocido ? Math.Round(libras * c.dcCocido, 4) : 0;
            liq.dcCocido = esCocido ? Math.Round(libras * c.dcCocido, 4) : 0;
        }

        private static void AplicarCostosDirectos(MatPrimaReproceso liq, CostosUnitarios c)
        {
            bool aplica = (!_lstNotCostDirec.Contains(liq.strTipCod)
                           && string.IsNullOrEmpty(liq.strRecNombre)
                           && liq.strLotTipo == "VA")
                          || _lstProcPrimTiplot.Contains(liq.strTipCod);

            if (!aplica) return;

            decimal libras = (decimal)liq.dbLibras;
            liq.ProcesoCostFijo.dcCostoVariable = Math.Round(libras * c.dcCostDirectoVar, 4);
            liq.ProcesoCostFijo.dcCostoFijo = Math.Round(libras * c.dcCostDirectoFij, 4);
            liq.dcCostFijVaria = c.dcCostDirectoVar;
            liq.dcCostFijFijo = c.dcCostDirectoFij;
        }

        private static void AplicarCostosIndirectos(MatPrimaReproceso objLiq, CostosUnitarios objCostUni)
        {
            bool blAplica = (!_lstNotCostInd.Contains(objLiq.strTipCod)
                           && string.IsNullOrEmpty(objLiq.strRecNombre)
                           && objLiq.strLotTipo == "VA")
                          || _lstProcPrimTiplot.Contains(objLiq.strTipCod);

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

        private static decimal CalcularCostoTotal(MatPrimaReproceso liq) =>
            // Proceso Primario
            (liq.ProcesoPrimario.dcRecepcion ?? 0) +
            (liq.ProcesoPrimario.dcClasificacion ?? 0) +
            (liq.ProcesoPrimario.dcCodificacion ?? 0) +
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
            (liq.dcCostoCopacking ?? 0);
    
    }
}

