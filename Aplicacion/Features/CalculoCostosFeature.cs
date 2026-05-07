using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagementService.Aplicación.DTos;
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

                if (lstYaValorizados.Any())
                {
                    // 1. Creamos el lookup igual que antes
                    var lookupValorizados = lstYaValorizados
                            .ToLookup(v => $"{v.intLote}-{v.intCodProd}-{v.intLidCodTal}");

                    // 2. Creamos un diccionario de enumeradores. 
                    // Esto mantendrá el "puntero" o posición para cada llave.
                    var enumeradores = lookupValorizados
                            .ToDictionary(group => group.Key, group => group.GetEnumerator());

                    try
                    {
                        foreach (var itemFresco in objDataProceso.lstLiqFresco)
                        {
                            string llave = $"{itemFresco.intLote}-{itemFresco.intCodProd}-{itemFresco.intLidCodTal}";

                            if (enumeradores.TryGetValue(llave, out var enumerador) && enumerador.MoveNext())
                            {
                                var calculado = enumerador.Current;
                                itemFresco.MergeValorizacion(calculado);
                            }
                        }
                    }
                    finally
                    {
                        foreach (var e in enumeradores.Values) e.Dispose();
                    }
                }


                if (objDataProceso.lstLiqRepro.Any())
                {
                    // 1. Creamos el lookup igual que antes
                    var lookupValorizados = objDataProceso.lstLiqRepro
                            .ToLookup(v => $"{v.intLoteUnificado}-{v.intProdCod}-{v.intCodTal}");

                    // 2. Creamos un diccionario de enumeradores. 
                    // Esto mantendrá el "puntero" o posición para cada llave.
                    var enumeradores = lookupValorizados
                            .ToDictionary(group => group.Key, group => group.GetEnumerator());

                    try
                    {
                        foreach (var itemFresco in objDataProceso.lstLiqFrsRpc)
                        {
                            string llave = $"{itemFresco.intLote}-{itemFresco.intCodProd}-{itemFresco.intLidCodTal}";

                            if (enumeradores.TryGetValue(llave, out var enumerador) && enumerador.MoveNext())
                            {
                                var calculado = enumerador.Current;
                                itemFresco.MergeValorizacion(calculado);
                            }
                        }
                    }
                    finally
                    {
                        foreach (var e in enumeradores.Values) e.Dispose();
                    }
                }
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
                if (lstYaValorizados.Any())
                {
                    var lookupValorizados = lstYaValorizados
                            .ToLookup(v => $"{v.intLote}-{v.intCodProd}-{v.intLidCodTal}");

                    var enumeradores = lookupValorizados
                            .ToDictionary(group => group.Key, group => group.GetEnumerator());

                    try
                    {
                        foreach (var itemFresco in objDataProceso.lstLiqFresco)
                        {
                            string llave = $"{itemFresco.intLote}-{itemFresco.intCodProd}-{itemFresco.intLidCodTal}";

                            if (enumeradores.TryGetValue(llave, out var enumerador) && enumerador.MoveNext())
                            {
                                var calculado = enumerador.Current;
                                itemFresco.MergeValorizacion(calculado);
                            }
                        }
                    }
                    finally
                    {
                        foreach (var e in enumeradores.Values) e.Dispose();
                    }
                }


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

        public async Task<List<MatPrimaReproceso>> ObtenerReporteMateriaPrimaReproValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<MatPrimaReproceso> lstReproVal;
            List<string> lstItemCod;
            List<PrecioFrsXMov> lstPrecioLiqOtrProc, lstPrecioFrsXMovCam;
            List<CostoMovArtDto> lstCostPromHidra;
            DataProcesoParam objDataProceso = new();
            try
            {

                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                var tareaReproceso = _objMateriaPrima.ReporteReproPlanRecibProc(dtFechaInicio, dtFechaFin);
                var tareaReproVal = _objMateriaPrima.ObtenerReproValorizada(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(tareaReproceso, tareaReproVal);
                objDataProceso.lstLiqRepro = await tareaReproceso;
                lstReproVal = await tareaReproVal;
                if (!objDataProceso.lstLiqRepro.Any())
                    throw new Exception("No se encontraron Procesos de materia prima Reproceso en el rango de fechas proporcionado.");


                var lstLiqLote = objDataProceso.lstLiqRepro
                        .Select(x => (long)x.intLoteOrigen)
                        .Distinct()
                        .ToList();

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
            List<string> lstItemCod;
            List<PrecioFrsXMov> lstPrecioLiqOtrProc, lstPrecioFrsXMovCam;
            List<CostoMovArtDto> lstCostPromHidra;
            DataProcesoParam objDataProceso = new();
            try
            {
                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                var tareaReproceso = _objMateriaPrima.ReporteReproPlanRecibProc(dtFechaInicio, dtFechaFin);
                //var tareaReproVal = _objMateriaPrima.ObtenerReproValorizada(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(tareaReproceso/*, tareaReproVal*/);
                objDataProceso.lstLiqRepro = await tareaReproceso;
                //lstReproVal = await tareaReproVal;
                if (!objDataProceso.lstLiqRepro.Any())
                    throw new Exception("No se encontraron Procesos de materia prima Reproceso en el rango de fechas proporcionado.");


                var lstLiqLote = objDataProceso.lstLiqRepro
                        .Select(x => (long)x.intLoteOrigen)
                        .Distinct()
                        .ToList();

                lstItemCod = MatPrimaReproceso.ObtenerLstItemHidra(objDataProceso.lstLiqRepro);

                var tareaCostPromHidra = _objMateriaPrima.CostoUltMovXItemCod(lstItemCod, dtFechaInicio, dtFechaFin);
                var tareaFresco = ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin);
                var tareaPrecioFrsXMovCam = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(lstLiqLote);
                var tareaOtroProc = _objMateriaPrima.ObtenerConsumoMovLiqOtroProc(lstLiqLote);
                var TareaTarifaProceso = _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorte);

                await ObtenerValProceso(dtFechaInicio, objDataProceso);
                await Task.WhenAll(tareaCostPromHidra, tareaOtroProc, tareaPrecioFrsXMovCam, tareaFresco, TareaTarifaProceso,
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


                //if (!lstReproVal.Any())
                //{

                //var lstLiqLote = lstMatPrimaReproceso
                //    .Select(x => (long)x.intLoteOrigen)
                //    .Distinct()
                //    .ToList();

                //var tareaFresco = ObtenerReporteMateriaPrimaValorizada(dtFechaInicio, dtFechaFin);
                //var tareaPrecioFrsXMovCam = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(lstLiqLote);
                //var tareaOtroProc = _objMateriaPrima.ObtenerConsumoMovLiqOtroProc(lstLiqLote);
                //await Task.WhenAll(
                //    _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstMatPrimaReproceso),
                //    ObtenerPrecioHidra(lstMatPrimaReproceso, dtFechaInicio, dtFechaFin),
                //    _objProcesoParametro.ObtenerCostosProcesosRepro(lstMatPrimaReproceso, dtFechaFin),
                //    tareaOtroProc, tareaPrecioFrsXMovCam, tareaFresco);
                //lstMatPrimaFresco = await tareaFresco;
                //lstMatPrimaFresco = lstMatPrimaFresco.Where(l => l.strTipoLiq == "LIQ_PFR").ToList();
                //lstPrecioLiqOtrProc = await tareaOtroProc;
                //lstPrecioFrsXMovCam = await tareaPrecioFrsXMovCam;


                //objMotorAsigPrec.AsignarCostRecibiXFrsMovCam(lstMatPrimaReproceso, lstPrecioLiqOtrProc, lstPrecioFrsXMovCam, lstMatPrimaFresco);
                //var lstLiqLoteInv = lstMatPrimaReproceso.Where(lbsRecProc =>
                //        lbsRecProc.strAgrupacion == "1. RECIBIDO" && lbsRecProc.dbCostoXSecuencial == 0)
                //    .Select(x => x.intLoteOrigen)
                //    .Distinct()
                //    .ToList();
                //var lstCodProd = lstMatPrimaReproceso
                //.Select(x => x.intProdCod.ToString())
                //.Distinct()
                //.ToList();

                //var lstPreciosProm = await _objMateriaPrima.ObtenerMatPrimSaldo(lstCodProd);
                //var lstPrecios = await _objMateriaPrima.ObtenerMatPrimSaldo(lstLiqLoteInv);
                //objMotorAsigPrec.EjecutarAsignacionPorSaldo(lstMatPrimaReproceso, lstPreciosProm, lstPrecios);
                //var lstFrsUni = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "UNI").ToList();
                //var lstFrsDir = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "DIR").ToList();
                //objMotorGrafo.CostearTodosLotesEnOrden(lstMatPrimaReproceso, lstMatPrimaFresco, lstFrsUni, lstFrsDir);


                //}
                //else
                //{

                //    await Task.WhenAll(
                //        _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstMatPrimaReproceso),
                //        ObtenerPrecioHidra(lstMatPrimaReproceso, dtFechaInicio, dtFechaFin),
                //        _objProcesoParametro.ObtenerCostosProcesosRepro(lstMatPrimaReproceso, dtFechaFin)
                //        );
                //    var lookupValorizados = lstReproVal
                //        .ToLookup(v => $"{v.intLotNumero}-{v.intLoteUnificado}-{v.intProdCod}-{v.intCodTal}");
                //    var enumeradores = lookupValorizados
                //        .ToDictionary(g => g.Key, g => g.GetEnumerator());

                //    try
                //    {
                //        var itemsAProcesar = lstMatPrimaReproceso
                //            .Where(p => p.strAgrupacion == "2. PROCESADO");

                //        foreach (var itemFresco in itemsAProcesar)
                //        {
                //            string llave = $"{itemFresco.intLotNumero}-{itemFresco.intLoteUnificado}-{itemFresco.intProdCod}-{itemFresco.intCodTal}";

                //            if (enumeradores.TryGetValue(llave, out var enumerador) && enumerador.MoveNext())
                //            {
                //                var calculado = enumerador.Current;
                //                itemFresco.MergeValorizacion(calculado);
                //            }
                //        }
                //    }
                //    finally
                //    {
                //        // Limpieza de recursos de los enumeradores
                //        foreach (var e in enumeradores.Values) e.Dispose();
                //    }
                //}
                _objMotorAsigPrec.RendimientoReproPlanRecibProc(objDataProceso.lstLiqRepro);
                return objDataProceso.lstLiqRepro.Where(l => l.strAgrupacion == "2. PROCESADO").ToList();
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
                var tareaMtpRpcValorizado = ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin);
                var tareaMtpFrsValorizado = ObtenerReporteMateriaPrimaValorizada(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(tareaDiarioCosto, tareaMtpRpcValorizado, tareaMtpFrsValorizado);
                lstMtpFrsValorizado = (await tareaMtpFrsValorizado).Where(x => x.strTipoLiq == "LIQ_PFR").ToList();
                lstMtpRpcValorizado = (await tareaMtpRpcValorizado).Where(x => x.strAgrupacion == "2. PROCESADO" /*x.dbCostoXSecuencial !=0*/).ToList();
                lstDiarioCosto = await tareaDiarioCosto;


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
            try
            {
                lstInvVal = await _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin);
                return lstInvVal;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostoMateriaPrimaFeature].[GenerarInfoMateriaPrimaSaldo] Ocurrio un error: {objException.Message}");
                throw;
            }
        }


        public async Task CrearRegistroInv(RequestDataDto objRequest)
        {

            List<InvValDataDto> lstInvVal;
            try
            {


                lstInvVal = objRequest.lstInvVal;

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
                await ObtenerValProceso(dtFechaInicio, objDataProceso);
                await Task.WhenAll(tareaRepro, TareaTarifaProceso, tareaFresco);
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
                _objLogger.LogError($"[CalculoCostosFeature].[RegistrarParametroProceso] Ocurrio un error: {objException.Message}");
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
