using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Repository.Services;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Aplicación.DTos;
using CostManagementService.Dominio.Reglas;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CostManagementService.Dominio.Enums.EnumLiquidacionDto;

namespace CostManagement.Aplicación.Features
{
    public class CalculoCostosFeature
    {

        private readonly IMateriaPrima _objMateriaPrima;
        private readonly ICostoMaterialEmpaque _objCostoMaterialEmpaque;
        private readonly IProcesoParametro _objProcesoParametro;
        private readonly ILogger<CalculoCostosFeature> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        private readonly MotorAsignacionPrecios _objMotorAsigPrec;
        private readonly MotorProrrateo _objMotorProrra;
        private readonly MotorMergeValorizacion _objMotorMergeVal;
        private readonly MotorGrafoTopologico _objMotorGrafo;
        private readonly MotorProcesoParametro _objMotorProceso;
        public CalculoCostosFeature(
            IMateriaPrima objMateriaPrima,
            ICostoMaterialEmpaque objCostoMaterialEmpaque,
            ILogger<CalculoCostosFeature> objLogger,
            IProcesoParametro objProcesoParametro,
            IExcelExportService excelService,
            CostManagementDbContext objCostManagamentDbContext,
            IOptions<ParametrosConfig> objConfig
            )
        {
            _objMateriaPrima = objMateriaPrima;
            _objCostoMaterialEmpaque = objCostoMaterialEmpaque;
            _objProcesoParametro = objProcesoParametro;
            _objLogger = objLogger;
            _objConfig = objConfig;
            _objMotorAsigPrec = new MotorAsignacionPrecios(_objLogger);
            _objMotorProrra = new MotorProrrateo(_objMotorAsigPrec);
            _objMotorGrafo = new MotorGrafoTopologico(_objMotorProrra, _objMotorAsigPrec, _objLogger);
            _objMotorMergeVal = new MotorMergeValorizacion(_objLogger);
            _objMotorProceso = new MotorProcesoParametro(_objLogger);
        }

        #region Flujo Materia Prima Fresco
        public async Task<List<LiquidacionResultado>> ObtenerReporteMateriaPrimaValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<LiquidacionResultado> lstLiquidaciones = new List<LiquidacionResultado>(), lstYaValorizados;
            DataProcesoParam objDataProceso = new();
            try
            {
                var TareaFrsVal = _objMateriaPrima.ObtenerLstMatPrimValorizada(dtFechaInicio, dtFechaFin);
                var tareaReproVal = ObtenerMatPrimRepro(dtFechaInicio, dtFechaFin);

                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                var TareaTarifaProceso = _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorte);
                await Task.WhenAll(TareaFrsVal, tareaReproVal, TareaTarifaProceso);
                await ObtenerValProceso(dtFechaInicio, objDataProceso);
                objDataProceso.lstProcesoTarifa = await TareaTarifaProceso;
                objDataProceso.lstLiqFresco = await _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin);
                objDataProceso.lstLiqFrsRpc = await _objMateriaPrima.ObtenerMatPrimValRpcsXRangoFecha(dtFechaInicio, dtFechaFin);
                objDataProceso.lstLiqRepro = await tareaReproVal;
                lstYaValorizados = await TareaFrsVal;
                if (!objDataProceso.lstLiqFresco.Any() || !objDataProceso.lstLiqFrsRpc.Any())
                    throw new Exception("No se encontraron liquidaciones de materia prima en el rango de fechas proporcionado.");


                // ══════ Merge Fresco: cache BD → datos crudos ══════
                _objMotorMergeVal.MergeFrescoValorizado(objDataProceso.lstLiqFresco, lstYaValorizados);

                // ══════ Merge RPC: intermediario → datos crudos ══════
                _objMotorMergeVal.MergeReproValorizado(objDataProceso.lstLiqFrsRpc, objDataProceso.lstLiqRepro);

                await Task.WhenAll(
                     _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstLiquidaciones)
                    );

                _objMotorProceso.AsignarCostosProcesosFresco(objDataProceso);
                _objMotorProceso.AsignarCostosProcesosRepro(objDataProceso);
                lstLiquidaciones.AddRange(objDataProceso.lstLiqFresco);
                lstLiquidaciones.AddRange(objDataProceso.lstLiqFrsRpc);

                return lstLiquidaciones;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en reporte: {objException.Message}");
                throw;
            }
        }

        public async Task<List<LiquidacionResultado>> ObtenerLiquidacionValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<LiquidacionResultado> lstLiquidaciones = new List<LiquidacionResultado>();
            List<LiquidacionResultado> lstYaValorizados;
            DataProcesoParam objDataProceso = new();
            try
            {
                var TareaFrsVal = _objMateriaPrima.ObtenerLstMatPrimValorizada(dtFechaInicio, dtFechaFin);
                var tareaFrs = _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(TareaFrsVal, tareaFrs);
                objDataProceso.lstLiqFresco = await tareaFrs;
                if (objDataProceso.lstLiqFresco == null || !objDataProceso.lstLiqFresco.Any())
                    throw new Exception("No se encontraron liquidaciones de materia prima en el rango de fechas proporcionado.");

                lstYaValorizados = await TareaFrsVal;
                await ObtenerValProceso(dtFechaInicio, objDataProceso);
                //_objMotorProceso.CalcularNuevoCostProcesoSum(objDataProceso);

                _objMotorMergeVal.MergeFrescoValorizado(objDataProceso.lstLiqFresco, lstYaValorizados);



                lstLiquidaciones.AddRange(objDataProceso.lstLiqFresco);
                await Task.WhenAll(
                     _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstLiquidaciones)
                    );
                _objMotorProceso.AsignarCostosProcesosFresco(objDataProceso);

                return lstLiquidaciones;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en reporte: {objException.Message}");
                throw;
            }
        }

        public async Task RegistrarMpFrsValorizada(RequestMatPrimDto objRequest)
        {
            List<LiquidacionResultado> lstMatPrimaFresco;
            try
            {
                lstMatPrimaFresco = await ObtenerLiquidacionValorizada(objRequest.dtFechaInicio, objRequest.dtFechaFin);
                var gruposPorLote = lstMatPrimaFresco
                                .GroupBy(p => p.intLote)
                                .ToDictionary(g => g.Key, g => g.ToList());
                var controlProcesados = new ConcurrentDictionary<int, byte>();

                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

                await Parallel.ForEachAsync(gruposPorLote, parallelOptions, async (entry, ct) =>
                {
                    int loteId = entry.Key;
                    List<LiquidacionResultado> lineasFichaTecnica = entry.Value;
                    try
                    {
                        // Control de duplicados (opcional si gruposPorLote ya es único)
                        if (!controlProcesados.TryAdd(loteId, 0)) return;

                        _objLogger.LogInformation($"Procesando Lote {loteId}. Líneas: {lineasFichaTecnica.Count}");

                        if (lineasFichaTecnica.Any())
                        {
                            await _objMateriaPrima.GuardarMatPrimValorizada(
                                lineasFichaTecnica, objRequest);
                        }
                    }
                    catch (Exception ex)
                    {
                        _objLogger.LogError($"Error en Lote {loteId}: {ex.Message}");
                        // No lanzamos throw aquí para que el Parallel continúe con los demás lotes
                    }
                });
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostoMateriaPrimaFeature].[RegistrarMpFrsValorizada] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        #endregion

        #region Flujo Material Empaque
        public async Task ProcesarDataMaterialEmpaque(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<LiquidacionResultado> lstMatPrimaFresco, lstMatPrimaReproceso;
            List<LiquidacionResultado> lstLiquidaciones = new List<LiquidacionResultado>();
            List<CostoMatEmpaDto> lstCostoMatEmpaque = new List<CostoMatEmpaDto>();
            List<CostoMatEmpProdXCietunDto> lstCostosEmpaque = new List<CostoMatEmpProdXCietunDto>(),
                lstCostMatEmpFrs, lstCostMatEmpRpc;
            ConcurrentDictionary<string, string> dictItemsEtiqueta, dictItemsMasterCaj;
            try
            {
                lstMatPrimaFresco = await _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin, false);

                lstMatPrimaReproceso = await _objMateriaPrima.ObtenerMatPrimValRpcsXRangoFecha(dtFechaInicio, dtFechaFin, false);

                lstLiquidaciones.AddRange(lstMatPrimaFresco);
                lstLiquidaciones.AddRange(lstMatPrimaReproceso);
                if (lstLiquidaciones.Count == 0)
                    throw new Exception("No se encontraron liquidaciones de materia prima en el rango de fechas proporcionado.");

                List<decimal> lstNumLoteFrs =
                    lstMatPrimaFresco
                    .Select(l => (decimal)l.intLote)
                    .Distinct()
                    .ToList()!;
                List<decimal> lstNumLoteRpc =
                    lstMatPrimaReproceso
                    .Select(l => l.dcLotSecuencial)
                    .Distinct()
                    .ToList()!;
                var objCostMatEmpFrs = _objMateriaPrima.ObtenerCostMatEmpFrsProdXLiq(lstNumLoteFrs);
                var objCostMatEmpRpc = _objMateriaPrima.ObtenerCostMatEmpRpcProdXLiq(lstNumLoteRpc);
                var taskItemsEti = _objMateriaPrima.ConsultarItemEtiqueta();       // ET sin metales
                var taskItemsMstCaj = _objMateriaPrima.ConsultarItemMasterCajita();   // CM + CP
                await Task.WhenAll(objCostMatEmpFrs, objCostMatEmpRpc, taskItemsEti, taskItemsMstCaj);
                lstCostMatEmpFrs = await objCostMatEmpFrs;
                dictItemsEtiqueta = await taskItemsEti;    // solo ET
                dictItemsMasterCaj = await taskItemsMstCaj; // CM + CP
                lstCostMatEmpRpc = await objCostMatEmpRpc;
                var hsCodEtiqueta = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CAM", "VE" };
                var hsCodReempaque = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "R3", "VR" };

                // ── Segmentación real cruzando contra los ítems válidos ──
                lstCostMatEmpRpc = lstCostMatEmpRpc.Where(c =>
                {
                    var itemKey = c.intEftItem.ToString();

                    if (hsCodEtiqueta.Contains(c.strTipCodigo))
                        return dictItemsEtiqueta.ContainsKey(itemKey);

                    if (hsCodReempaque.Contains(c.strTipCodigo))
                        return dictItemsMasterCaj.ContainsKey(itemKey);

                    return true;
                }).ToList();
                lstCostosEmpaque.AddRange(lstCostMatEmpFrs);
                lstCostosEmpaque.AddRange(lstCostMatEmpRpc);
                await Task.WhenAll(
                    _objMateriaPrima.ObtenerCostoPromBoditeXFichaTecnica(lstCostosEmpaque, dtFechaInicio, dtFechaFin),
                    _objMateriaPrima.ObtenerCostoPromMov1XFichaTecnica(lstCostosEmpaque, dtFechaInicio, dtFechaFin),
                    _objMateriaPrima.ObtenerCostoUltConsuMov2XFichaTecnica(lstCostosEmpaque, dtFechaInicio, dtFechaFin));
                await ProcesarYGuardarCostosEmpaque(lstCostosEmpaque, "SISTEMA", "SERVER");
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ProcesarDataMaterialEmpaque] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<List<CostoMatEmpaDto>> ObtenerReporteMaterialEmpaqueValorizado(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<CostoMatEmpaDto> lstResultado = new List<CostoMatEmpaDto>();
            DataProcesoParam objDataProceso = new();
            try
            {
                var tareaMatPrimaFresco = _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin, false);
                var tareaMatPrimaReproceso = _objMateriaPrima.ObtenerMatPrimValRpcsXRangoFecha(dtFechaInicio, dtFechaFin, false);
                await Task.WhenAll(tareaMatPrimaFresco, tareaMatPrimaReproceso);
                objDataProceso.lstLiqFresco = await tareaMatPrimaFresco;
                objDataProceso.lstLiqFrsRpc = await tareaMatPrimaReproceso;
                objDataProceso.lstLotesFrsRpc = objDataProceso.lstLiqFrsRpc
                                        .Select(l => (int)l.dcLotSecuencial)
                                        .Distinct()
                                        .Concat(objDataProceso.lstLiqFresco.Select(l => l.intLote).Distinct())
                                        .ToList();
                lstResultado = await _objCostoMaterialEmpaque.ObtenerCostoEmpaqueXLote(objDataProceso.lstLotesFrsRpc);
                if (!lstResultado.Any())
                    throw new Exception("No se encontraron datos Costo Material Empaque en el rango de fechas proporcionado.");

                return lstResultado;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostoMateriaPrimaFeature].[ObtenerReporteMaterialEmpaqueValorizado] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task ProcesarYGuardarCostosEmpaque(List<CostoMatEmpProdXCietunDto> lstCostosEmpaque, string usuario, string equipo)
        {
            // Este diccionario servirá para que solo UN hilo procese cada combinación única
            var procesados = new ConcurrentDictionary<(int, string), byte>();

            try
            {
                var gruposPorProductoYLiq = lstCostosEmpaque
                                            .GroupBy(p => new { p.intLiqLote, p.strProCodCor })
                                            .ToDictionary(
                                                g => (g.Key.intLiqLote, g.Key.strProCodCor),
                                                g => g.ToList() // Aquí están las 12 líneas de la ficha técnica
                                            );

                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

                await Parallel.ForEachAsync(gruposPorProductoYLiq.Keys, parallelOptions, async (objLlave, ct) =>
                {
                    try
                    {

                        // CLAVE DE UNICIDAD: (Lote, Código)
                        if (!procesados.TryAdd((objLlave.intLiqLote, objLlave.strProCodCor), 0)) return;
                        // Obtenemos las líneas (ítems de ficha técnica) para este producto en este lote
                        var lineasFichaTecnica = gruposPorProductoYLiq[objLlave];
                        _objLogger.LogInformation($"Ingreso Lote {objLlave.intLiqLote}, Código {objLlave.strProCodCor}");
                        if (lineasFichaTecnica.Any())
                        {
                            await _objCostoMaterialEmpaque.CrearCostoEmpaqueCompleto(
                                lineasFichaTecnica,
                                usuario,
                                equipo
                            );
                        }

                    }
                    catch (Exception ex)
                    {
                        _objLogger.LogError($"Error en Lote {objLlave.intLiqLote}, Código {objLlave.strProCodCor}: {ex.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _objLogger.LogCritical($"Error masivo: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Flujo Mat Prima Reproceso

        public async Task<List<MatPrimaReproceso>> ObtenerReporteMateriaPrimaReproValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin, enmTipoConsulta objEnmTip = enmTipoConsulta.ConFront)
        {
            //List<MatPrimaReproceso> lstReproVal;
            List<string> lstItemCod;
            List<PrecioFrsXMov> lstPrecioLiqOtrProc, lstPrecioFrsXMovCam;
            List<CostoMovArtDto> lstCostPromHidra;
            DataProcesoParam objDataProceso = new();
            try
            {
                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                objDataProceso.lstLiqRepro = await ObtenerMatPrimRepro(dtFechaInicio, dtFechaFin);
                var lstLiqLote = objDataProceso.lstLiqRepro
                        .Select(x => (long)x.intLoteOrigen)
                        .Distinct()
                        .ToList();
                if (objEnmTip == enmTipoConsulta.ConBack && objDataProceso.lstLiqRepro.Any(x => x.dcCostoTotXLibra != null))
                {
                    return objDataProceso.lstLiqRepro;
                }
                lstItemCod = MatPrimaReproceso.ObtenerLstItemHidra(objDataProceso.lstLiqRepro);

                var tareaFresco = ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin);
                var tareaCostPromHidra = _objMateriaPrima.CostoUltMovXItemCod(lstItemCod, dtFechaInicio, dtFechaFin);
                var tareaPrecioFrsXMovCam = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(lstLiqLote);
                var tareaOtroProc = _objMateriaPrima.ObtenerConsumoMovLiqOtroProc(lstLiqLote);

                await ObtenerValProceso(dtFechaInicio, objDataProceso);
                var TareaTarifaProceso = _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorte);
                await Task.WhenAll(tareaCostPromHidra, tareaOtroProc, tareaPrecioFrsXMovCam, tareaFresco,
                    _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(objDataProceso.lstLiqRepro)
                    );
                lstCostPromHidra = await tareaCostPromHidra;
                objDataProceso.lstLiqFresco = await tareaFresco;
                objDataProceso.lstProcesoTarifa = await TareaTarifaProceso;
                lstPrecioLiqOtrProc = await tareaOtroProc;
                lstPrecioFrsXMovCam = await tareaPrecioFrsXMovCam;
                _objMotorAsigPrec.AsignarCostHidra(lstCostPromHidra, objDataProceso.lstLiqRepro);
                _objMotorProceso.AsignarCostosProcesosRepro(objDataProceso);
                _objMotorAsigPrec.AsignarCostRecibiXFrsMovCam(lstPrecioLiqOtrProc, lstPrecioFrsXMovCam, objDataProceso);
                var lstLiqLoteInv = objDataProceso.lstLiqRepro.Where(lbsRecProc =>
                        lbsRecProc.strAgrupacion == "1. RECIBIDO" && lbsRecProc.dbCostoXSecuencial == 0)
                    .Select(x => x.intLoteOrigen)
                    .Distinct()
                    .ToList();
                var lstCodProd = objDataProceso.lstLiqRepro
                .Select(x => x.intProdCod.ToString())
                .Distinct()
                .ToList();

                var lstPreciosProm = await _objMateriaPrima.ObtenerMatPrimSaldo(lstCodProd);
                var lstPrecios = await _objMateriaPrima.ObtenerMatPrimSaldo(lstLiqLoteInv);
                _objMotorAsigPrec.EjecutarAsignacionPorSaldo(objDataProceso.lstLiqRepro, lstPreciosProm, lstPrecios);
                var lstFrsUni = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "UNI").ToList();
                var lstFrsDir = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "DIR").ToList();
                _objMotorGrafo.CostearTodosLotesEnOrden(objDataProceso, lstFrsUni, lstFrsDir);
                _objMotorAsigPrec.RendimientoReproPlanRecibProc(objDataProceso.lstLiqRepro);
                return objDataProceso.lstLiqRepro/*.Where(l => l.strAgrupacion == "2. PROCESADO").ToList()*/;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerReporteMateriaPrimaReproValorizada] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<List<MatPrimaReproceso>> ObtenerMatPrimRepro(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<MatPrimaReproceso> lstMatPrimaRepro, lstReproVal;
            try
            {
                // 1. Datos crudos (estructura, libras, agrupación)
                var tareaReproceso = _objMateriaPrima.ReporteReproPlanRecibProc(dtFechaInicio, dtFechaFin);

                // 2. Cache desde tb_materiaPrimaReproValorizada
                var tareaReproVal = _objMateriaPrima.ObtenerReproValorizada(dtFechaInicio, dtFechaFin);

                await Task.WhenAll(tareaReproceso, tareaReproVal);

                lstMatPrimaRepro = await tareaReproceso;
                lstReproVal = await tareaReproVal;

                if (!lstMatPrimaRepro.Any())
                    throw new Exception(
                        "No se encontraron Procesos de materia prima Reproceso " +
                        "en el rango de fechas proporcionado.");

                // 3. Merge: asignar dcCostoTotXLibra desde cache a ítems PROCESADO
                if (lstReproVal.Any())
                {
                    var lookupCache = lstReproVal
                        .Where(v => v.dcCostoTotXLibra != null && v.dcCostoTotXLibra > 0)
                        .ToLookup(v => (v.intLotNumero, v.intLoteUnificado, v.intProdCod, v.intCodTal));

                    foreach (var item in lstMatPrimaRepro.Where(x =>
                        x.strAgrupacion == "2. PROCESADO" &&
                        x.dcCostoTotXLibra == null))
                    {
                        var key = (item.intLotNumero, item.intLoteUnificado,
                                   item.intProdCod, item.intCodTal);

                        var cached = lookupCache[key].FirstOrDefault();
                        if (cached != null)
                        {
                            item.dcCostoTotXLibra = cached.dcCostoTotXLibra;
                        }
                    }

                    _objLogger.LogInformation(
                        $"[ObtenerMatPrimRepro] Cache: {lookupCache.Count} claves, " +
                        $"aplicadas a ítems PROCESADO.");
                }
                return lstMatPrimaRepro/*.Where(l => l.strAgrupacion == "2. PROCESADO").ToList()*/;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerReporteMateriaPrimaReproValorizada] Ocurrio un error: {objException.Message}");
                throw;
            }
        }



        public async Task RegistrarMpRprValorizado(RequestMatPrimDto objRequest)
        {
            List<MatPrimaReproceso> lstMatPrimaReproceso;
            try
            {
                lstMatPrimaReproceso = await ObtenerReporteMateriaPrimaReproValorizada(objRequest.dtFechaInicio, objRequest.dtFechaFin);
                var gruposPorLote = lstMatPrimaReproceso
                                .Where(p => p.strAgrupacion == "2. PROCESADO")
                                .GroupBy(p => (p.intLotNumero, p.intLoteUnificado, p.intProdCod))
                                .ToDictionary(g => g.Key, g => g.ToList());

                var dicCtrlProcesados = new ConcurrentDictionary<(int, int, int), byte>();

                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

                // 5. Iterar sobre los grupos creados
                await Parallel.ForEachAsync(gruposPorLote, parallelOptions, async (entry, ct) =>
                {
                    var objKeyLooku = (entry.Key.intLotNumero, entry.Key.intLoteUnificado, entry.Key.intProdCod);
                    List<MatPrimaReproceso> lineasFichaTecnica = entry.Value;
                    try
                    {
                        // Control de duplicados (opcional si gruposPorLote ya es único)
                        if (!dicCtrlProcesados.TryAdd(objKeyLooku, 0)) return;
                        _objLogger.LogInformation($"Procesando Lote {objKeyLooku}. Líneas: {lineasFichaTecnica.Count}");

                        if (lineasFichaTecnica.Any())
                        {
                            await _objMateriaPrima.GuardarReproValorizado(
                                lineasFichaTecnica, objRequest);
                        }
                    }
                    catch (Exception ex)
                    {
                        _objLogger.LogError($"Error en Lote {objKeyLooku}: {ex.Message}");
                        // No lanzamos throw aquí para que el Parallel continúe con los demás lotes
                    }
                });
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostoMateriaPrimaFeature].[RegistrarMpFrsValorizada] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        #endregion


        public async Task<List<DiarioCosto>> ObtenerDiarioCostoAsync(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<LiquidacionResultado> lstMtpFrsValorizado;
            List<MatPrimaReproceso> lstMtpRpcValorizado;
            List<DiarioCosto> lstDiarioCosto;
            try
            {
                var tareaDiarioCosto = _objMateriaPrima.ObtenerMovimientosAsync(dtFechaInicio, dtFechaFin);

                // ══════ CAMBIO: usar intermediario liviano en lugar del pipeline completo ══════
                var tareaMtpRpcValorizado = ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin, enmTipoConsulta.ConBack);
                var tareaMtpFrsValorizado = ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin);

                await Task.WhenAll(tareaDiarioCosto, tareaMtpRpcValorizado, tareaMtpFrsValorizado);
                lstDiarioCosto = await tareaDiarioCosto;
                lstMtpFrsValorizado = (await tareaMtpFrsValorizado)
                    .Where(x => x.strTipoLiq == "LIQ_PFR").ToList();

                // dcCostoTotXLibra ya viene del intermediario (desde cache BD)
                lstMtpRpcValorizado = (await tareaMtpRpcValorizado)
                    .Where(x => x.strAgrupacion == "2. PROCESADO")
                    .ToList();


                _objMotorAsigPrec.AsignarCostoSaldo(lstDiarioCosto, lstMtpFrsValorizado, lstMtpRpcValorizado);

                return lstDiarioCosto;

            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerDiarioCostoAsync] Ocurrio un error: {objException.Message}");
                throw;
            }
        }


        public async Task<List<DateOnly>> ObtenerDataFechaCorte()
        {
            List<DateOnly> lstData;
            try
            {
                lstData = await _objMateriaPrima.ConsultarFechaCorteInv();
                return lstData;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerDataFechaCorte] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public List<DataProcesoParamDto> ObtenerDataProcesoParametro()
        {
            List<DataProcesoParamDto> lstData = new List<DataProcesoParamDto>();
            int currentYear = DateTime.Now.Year;
            try
            {
                var listaAños = Enumerable.Range(currentYear - 3, 4)
                    .Select(y => new DataProcesoParamDto { intId = y, strDescripcion = y.ToString(), strTipoData = "ANIO" })
                    .ToList();
                listaAños.Insert(0, new DataProcesoParamDto { intId = 0, strDescripcion = "", strTipoData = "ANIO" });

                // 2. Meses
                string[] nombresMeses = { "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
                                  "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE" };

                var listaMeses = nombresMeses
                    .Select((nombre, index) => new DataProcesoParamDto
                    {
                        intId = index + 1,
                        strDescripcion = nombre,
                        strTipoData = "MES"
                    })
                    .ToList();
                listaMeses.Insert(0, new DataProcesoParamDto { intId = 0, strDescripcion = "", strTipoData = "MES" });
                lstData.AddRange(listaAños);
                lstData.AddRange(listaMeses);
                return lstData;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerDataProcesoParametro] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<List<InventarioVal>> ObtenerInventarioValorizado(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<InventarioVal> lstInvVal = new List<InventarioVal>();
            List<DiarioCosto> lstCuadre = new List<DiarioCosto>();
            try
            {
                DateOnly dtFechaCorteAnterior;

                if (dtFechaInicio.Year == dtFechaFin.Year
                    && dtFechaInicio.Month == dtFechaFin.Month)
                {
                    // FechaInicio es el 1ro del mes → restamos 1 día → último día mes anterior
                    dtFechaCorteAnterior = dtFechaInicio.AddDays(-1);
                }
                else
                {
                    // Tomamos el mes de dtFechaFin, le restamos 1 mes, último día
                    var dtMesAnterior = dtFechaFin.AddMonths(-1);
                    dtFechaCorteAnterior = new DateOnly(
                        dtMesAnterior.Year,
                        dtMesAnterior.Month,
                        DateTime.DaysInMonth(dtMesAnterior.Year, dtMesAnterior.Month));
                }
                var tareaInvInicial = _objMateriaPrima.ConsultarInvValBodite(dtFechaCorteAnterior, "I");
                var tareaInvFinal = _objMateriaPrima.ConsultarInvValBodite(dtFechaFin, "F");
                var tareaDiarioCosto = ObtenerDiarioCostoAsync(dtFechaInicio, dtFechaFin);
                var tareaInvValorado =  _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(tareaInvInicial, tareaInvFinal, tareaDiarioCosto, tareaInvValorado);
                var lstInvInicial = await tareaInvInicial;
                var lstInvFinal = await tareaInvFinal;
                lstCuadre = await tareaDiarioCosto;
                var lstInvValSubido = await tareaInvValorado;


                if (lstInvValSubido.Any())
                {
                    lstInvVal = lstInvValSubido;
                }
                else
                {
                    lstInvVal.AddRange(lstInvInicial);
                }

                if (lstInvFinal.Any())
                {
                    lstInvVal.AddRange(lstInvFinal);
                }
                //lstInvVal.AddRange(InventarioVal.GenerarSaldoFinal(lstCuadre));
                _objMotorMergeVal.MergeInventarioValorizado(lstInvVal, lstCuadre);
                return lstInvVal;
            }
            catch (Exception objException)
            {
                _objLogger.LogError(
                    $"[CalculoCostoMateriaPrimaFeature].[ObtenerInventarioValorizado] " +
                    $"Ocurrio un error: {objException.Message}");
                throw;
            }
        }


        public async Task CrearRegistroInv(RequestDataDto objRequest)
        {

            List<InvValDataDto> lstInvVal;
            try
            {
                lstInvVal = objRequest.lstInvVal;

                var primerDiaMesActual = new DateOnly(objRequest.dtFechaCorte.Year, objRequest.dtFechaCorte.Month, 1);

                // Al restar un día, retrocedemos automáticamente al último día del mes anterior
                objRequest.dtFechaCorte = primerDiaMesActual.AddDays(-1);
                await _objMateriaPrima.ObtenerDatosProd(lstInvVal, objRequest.dtFechaCorte.ToString("yyyy/MM/dd"));
                await _objMateriaPrima.CrearInvMatPrimExcel(lstInvVal, objRequest);
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostoMateriaPrimaFeature].[GenerarInfoMateriaPrimaSaldo] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        #region Flujo procesos
        public async Task<DataProcesoParam> ObtenerParametroProceso(DateTime dtFechaCorte)
        {
            List<string> lstProdCocido;
            DateOnly dtFechaCorteCorr, dtFechaInicio, dtFechaFin;
            MotorProcesoParametro objMotorProceso = new MotorProcesoParametro(_objLogger);
            DataProcesoParam objDataProceso = new DataProcesoParam();
            try
            {
                //Se obtiene tipo de proceso cocido para entero
                lstProdCocido = await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Proceso Cocido");
                dtFechaCorteCorr = DateOnly.FromDateTime(dtFechaCorte);
                dtFechaInicio = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, 1);
                dtFechaFin = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, dtFechaCorte.Day);
                var tareaRepro = _objMateriaPrima.ReporteReproPlanProc(dtFechaInicio, dtFechaFin);
                var TareaTarifaProceso = _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorteCorr);
                var tareaFresco =  _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin);
                //await ObtenerValProceso(dtFechaInicio, objDataProceso);
                await Task.WhenAll(tareaRepro, TareaTarifaProceso, tareaFresco, ObtenerValProceso(dtFechaInicio, objDataProceso));
                objDataProceso.lstLiqFresco = await tareaFresco;
                objDataProceso.lstLiqRepro = await tareaRepro;
                objDataProceso.lstProcesoTarifa = await TareaTarifaProceso;

                //if (lstResultados.Any() && lstResultados.All(r => r.dcValor != 0))
                //{
                //    return lstResultados;
                //}
                objDataProceso.lstProdTerm = _objConfig.Value.lstProdTerm;
                objDataProceso.lstDescTotFresco = _objConfig.Value.lstDescTotFresco;

                // Llamada para Fresco
                objMotorProceso.AsignarCostoProcesoFrs(objDataProceso);
                // Llamada para Reproceso
                objMotorProceso.AsignarCostoProcesoRpc(objDataProceso);
                // Llamada para Tarifario
                objMotorProceso.AsignarCostoProcesoTarifa(objDataProceso);
                //Sumrizamos libras 
                //objMotorProceso.SumarizarLibrasNoEditable(objDataProceso);
                return objDataProceso;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerDataProcesoParametro] Ocurrio un error: {objException.Message}");
                throw;
            }
        }
        public async Task<bool> RegistrarParamProcPfr( DateTime dtFechaCorte, GuardarParametrosRequest objParam)
        {
            try
            {
                DateOnly dtFechaCorteCorr = DateOnly.FromDateTime(dtFechaCorte);
                bool blRegistroExitoso = await _objProcesoParametro.RegistrarParamCosteoPfr(dtFechaCorteCorr, objParam);
                return blRegistroExitoso;
            }
            catch (Exception objException)
            {
                ManejoLog<CalculoCostosFeature>.Error(_objLogger, nameof(CalculoCostosFeature), nameof(RegistrarParamProcPfr), objException);
                throw;
            }
        }

        private async Task ObtenerValProceso(DateOnly dtFechaInicio, DataProcesoParam objDataProceso)
        {
            try
            {

                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);

                //Se obtienen las formas de congelamiento desde el catalogoDet  
                objDataProceso.lstCongTunel = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento Tunel")).Select(int.Parse).ToList();
                objDataProceso.lstCongIqf = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento Brine")).Select(int.Parse).ToList();
                objDataProceso.lstCongBrine = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento IQF")).Select(int.Parse).ToList();
                var tareaResultadosFrs = _objProcesoParametro.ConsultarProcesosFrescoConValores(dtFechaCorte);
                var tareaResultadosRpc = _objProcesoParametro.ConsultarProcesosReproConValores(dtFechaCorte);
                await Task.WhenAll(tareaResultadosFrs, tareaResultadosRpc);
                objDataProceso.lstProcesoFrs = await tareaResultadosFrs;
                objDataProceso.lstProcesoRpc = await tareaResultadosRpc;
                _objMotorProceso.RecalcularCostosUnitariosExactos(
                   objDataProceso.lstProcesoFrs,
                   objDataProceso.lstProcesoRpc);
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerValProceso] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        #endregion


    }
}
