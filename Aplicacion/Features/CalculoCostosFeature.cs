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
        private readonly IExcelExportService _objExcelService;
        private readonly CostManagementDbContext _objCostManagamentDbContext;
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
            _objCostManagamentDbContext = objCostManagamentDbContext;
            _objExcelService = excelService;
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
            List<LiquidacionResultado> lstLiquidaciones = new List<LiquidacionResultado>();
            List<LiquidacionResultado> lstMatPrimaFresco, lstMatPrimaReproceso, 
                lstYaValorizados;
            List<MatPrimaReproceso> lstMatRepro;
            decimal dcCostoTot, dcCostTotalProc, dcCostoTotalMatEmp,dcTotalDol;
            List<CostoMovArtDto> lstCostPromHidra;
            List<ProcesoResultadoDto> lstProcesos;
            try
            {
                var TareaFrsVal = _objMateriaPrima.ObtenerLstMatPrimValorizada(dtFechaInicio, dtFechaFin);
                var tareaReproVal = ObtenerMatPrimRepro(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(TareaFrsVal, tareaReproVal);
                lstMatPrimaFresco = await _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin);
                lstMatPrimaReproceso = await _objMateriaPrima.ObtenerMatPrimValRpcsXRangoFecha(dtFechaInicio, dtFechaFin);
                lstMatRepro = await tareaReproVal;
                lstYaValorizados = await TareaFrsVal;

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
                        foreach (var itemFresco in lstMatPrimaFresco)
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


                if (lstMatRepro.Any())
                {
                    // 1. Creamos el lookup igual que antes
                    var lookupValorizados = lstMatRepro
                            .ToLookup(v => $"{v.intLoteUnificado}-{v.intProdCod}-{v.intCodTal}");

                    // 2. Creamos un diccionario de enumeradores. 
                    // Esto mantendrá el "puntero" o posición para cada llave.
                    var enumeradores = lookupValorizados
                            .ToDictionary(group => group.Key, group => group.GetEnumerator());

                    try
                    {
                        foreach (var itemFresco in lstMatPrimaReproceso)
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

                lstLiquidaciones.AddRange(lstMatPrimaFresco);
                lstLiquidaciones.AddRange(lstMatPrimaReproceso);
                if (lstLiquidaciones.Count == 0)
                    throw new Exception("No se encontraron liquidaciones de materia prima en el rango de fechas proporcionado.");
                await Task.WhenAll(
                     _objProcesoParametro.ObtenerCostosProcesosMatPrimPFR(lstMatPrimaFresco, dtFechaFin),
                     _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstLiquidaciones)
                    );

                return lstLiquidaciones;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en reporte: {objException.Message}");
                throw;
            }
        }

        public async Task<List<LiquidacionResultado>> ObtenerLiquidadaValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<LiquidacionResultado> lstLiquidaciones = new List<LiquidacionResultado>();
            List<LiquidacionResultado> lstMatPrimaFresco, lstMatPrimaReproceso,
                lstYaValorizados;
            decimal dcCostoTot, dcCostTotalProc, dcCostoTotalMatEmp, dcTotalDol;
            try
            {
                var TareaFrsVal = _objMateriaPrima.ObtenerLstMatPrimValorizada(dtFechaInicio, dtFechaFin);

                await Task.WhenAll(TareaFrsVal);
                lstMatPrimaFresco = await _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin);

                lstYaValorizados = await TareaFrsVal;

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
                        foreach (var itemFresco in lstMatPrimaFresco)
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


                lstLiquidaciones.AddRange(lstMatPrimaFresco);
                if (lstLiquidaciones.Count == 0)
                    throw new Exception("No se encontraron liquidaciones de materia prima en el rango de fechas proporcionado.");
                await Task.WhenAll(
                     _objProcesoParametro.ObtenerCostosProcesosMatPrimPFR(lstMatPrimaFresco, dtFechaFin),
                     _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstLiquidaciones)
                    );

                foreach (var itemFresco in lstMatPrimaFresco.Where(obj => obj.strTipoLiq == "LIQ_PFR"))
                {

                    if (itemFresco.dcLibras > 0 && itemFresco.dcCostoTotXLibra == null)
                    {
                        dcCostTotalProc = (itemFresco.dcCostTotalProc ?? 0m);
                        dcCostoTotalMatEmp = (itemFresco.dcCostoTotalMatEmp ?? 0m);
                        dcTotalDol = (decimal)itemFresco.dcTotalDol;
                        dcCostoTot = dcCostTotalProc + dcCostoTotalMatEmp + dcTotalDol;
                        itemFresco.dcCostoTotXLibra = Math.Truncate((dcCostoTot / (decimal)itemFresco.dcLibras) * 100) / 100;
                    }
                }
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
                //lstMatPrimaFresco = await ObtenerReporteMateriaPrimaValorizada(dtFechaInicio, dtFechaFin);
                lstMatPrimaFresco = await ObtenerLiquidadaValorizada(objRequest.dtFechaInicio, objRequest.dtFechaFin);
                var gruposPorLote = lstMatPrimaFresco
                                .GroupBy(p => p.intLote)
                                .ToDictionary(g => g.Key, g => g.ToList());
                var controlProcesados = new ConcurrentDictionary<int, byte>();

                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

                // 5. Iterar sobre los grupos creados
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
                    .Select(l => (decimal)l.intLote)
                    .Distinct()
                    .ToList()!;
                var objCostMatEmpFrs = _objMateriaPrima.ObtenerCostMatEmpFrsProdXLiq(lstNumLoteFrs);
                var objCostMatEmpRpc = _objMateriaPrima.ObtenerCostMatEmpRpcProdXLiq(lstNumLoteRpc);
                await Task.WhenAll(objCostMatEmpFrs, objCostMatEmpRpc);
                lstCostMatEmpFrs = await objCostMatEmpFrs;
                lstCostMatEmpRpc = await objCostMatEmpRpc;
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
            try
            {
                lstResultado = await _objCostoMaterialEmpaque.ObtenerCostoEmpaqueXRangoFecha(dtFechaInicio, dtFechaFin);
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
            List<MatPrimaReproceso> lstMatPrimaReproceso, lstReproVal;
            List<string> lstItemCod;
            List<LiquidacionResultado> lstMatPrimaFresco;
            List<PrecioFrsXMov> lstPrecioLiqOtrProc, lstPrecioFrsXMovCam;
            List<CostoMovArtDto> lstCostPromHidra;
            List<ProcesoResultadoDto> lstProcesos;
            try
            {

                var tareaReproceso = _objMateriaPrima.ReporteReproPlanRecibProc(dtFechaInicio, dtFechaFin);
                var tareaReproVal = _objMateriaPrima.ObtenerReproValorizada(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(tareaReproceso, tareaReproVal);
                lstMatPrimaReproceso = await tareaReproceso;
                lstReproVal = await tareaReproVal;
                if (!lstMatPrimaReproceso.Any())
                    throw new Exception("No se encontraron Procesos de materia prima Reproceso en el rango de fechas proporcionado.");


                var lstLiqLote = lstMatPrimaReproceso
                        .Select(x => (long)x.intLoteOrigen)
                        .Distinct()
                        .ToList();

                lstItemCod = MatPrimaReproceso.ObtenerLstItemHidra(lstMatPrimaReproceso);

                var tareaCostPromHidra = _objMateriaPrima.CostoUltMovXItemCod(lstItemCod, dtFechaInicio, dtFechaFin);
                var tareaProcesos = _objProcesoParametro.ObtenerProcesosReproConValores(dtFechaFin);
                var tareaFresco = ObtenerLiquidadaValorizada(dtFechaInicio, dtFechaFin);
                var tareaPrecioFrsXMovCam = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(lstLiqLote);
                var tareaOtroProc = _objMateriaPrima.ObtenerConsumoMovLiqOtroProc(lstLiqLote);
                await Task.WhenAll(tareaCostPromHidra, tareaProcesos, tareaOtroProc, tareaPrecioFrsXMovCam, tareaFresco,
                    _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstMatPrimaReproceso)
                    );
                lstCostPromHidra = await tareaCostPromHidra;
                lstProcesos = await tareaProcesos;
                lstMatPrimaFresco = await tareaFresco;
                lstMatPrimaFresco = lstMatPrimaFresco.Where(l => l.strTipoLiq == "LIQ_PFR").ToList();
                lstPrecioLiqOtrProc = await tareaOtroProc;
                lstPrecioFrsXMovCam = await tareaPrecioFrsXMovCam;
                _objMotorAsigPrec.AsignarCostHidra(lstCostPromHidra, lstMatPrimaReproceso);
                _objMotorProceso.AsignarCostosProcesosRepro(lstProcesos, lstMatPrimaReproceso);
                _objMotorAsigPrec.AsignarCostRecibiXFrsMovCam(lstMatPrimaReproceso, lstPrecioLiqOtrProc, lstPrecioFrsXMovCam, lstMatPrimaFresco);
                var lstLiqLoteInv = lstMatPrimaReproceso.Where(lbsRecProc =>
                        lbsRecProc.strAgrupacion == "1. RECIBIDO" && lbsRecProc.dbCostoXSecuencial == 0)
                    .Select(x => x.intLoteOrigen)
                    .Distinct()
                    .ToList();
                var lstCodProd = lstMatPrimaReproceso
                .Select(x => x.intProdCod.ToString())
                .Distinct()
                .ToList();

                var lstPreciosProm = await _objMateriaPrima.ObtenerMatPrimSaldo(lstCodProd);
                var lstPrecios = await _objMateriaPrima.ObtenerMatPrimSaldo(lstLiqLoteInv);
                _objMotorAsigPrec.EjecutarAsignacionPorSaldo(lstMatPrimaReproceso, lstPreciosProm, lstPrecios);
                var lstFrsUni = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "UNI").ToList();
                var lstFrsDir = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "DIR").ToList();
                _objMotorGrafo.CostearTodosLotesEnOrden(lstMatPrimaReproceso, lstMatPrimaFresco, lstFrsUni, lstFrsDir);


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
                _objMotorAsigPrec.RendimientoReproPlanRecibProc(lstMatPrimaReproceso);
                return lstMatPrimaReproceso/*.Where(l => l.strAgrupacion == "2. PROCESADO").ToList()*/;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerReporteMateriaPrimaReproValorizada] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<List<MatPrimaReproceso>> ObtenerMatPrimRepro(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<MatPrimaReproceso> lstMatPrimaReproceso, lstReproVal;
            List<string> lstItemCod;
            List<LiquidacionResultado> lstMatPrimaFresco;
            List<PrecioFrsXMov> lstPrecioLiqOtrProc, lstPrecioFrsXMovCam;
            List<CostoMovArtDto> lstCostPromHidra;
            List<ProcesoResultadoDto> lstProcesos;
            try
            {

                var tareaReproceso = _objMateriaPrima.ReporteReproPlanRecibProc(dtFechaInicio, dtFechaFin);
                //var tareaReproVal = _objMateriaPrima.ObtenerReproValorizada(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(tareaReproceso/*, tareaReproVal*/);
                lstMatPrimaReproceso = await tareaReproceso;
                //lstReproVal = await tareaReproVal;
                if (!lstMatPrimaReproceso.Any())
                    throw new Exception("No se encontraron Procesos de materia prima Reproceso en el rango de fechas proporcionado.");


                var lstLiqLote = lstMatPrimaReproceso
                        .Select(x => (long)x.intLoteOrigen)
                        .Distinct()
                        .ToList();

                lstItemCod = MatPrimaReproceso.ObtenerLstItemHidra(lstMatPrimaReproceso);

                var tareaCostPromHidra = _objMateriaPrima.CostoUltMovXItemCod(lstItemCod, dtFechaInicio, dtFechaFin);
                var tareaProcesos = _objProcesoParametro.ObtenerProcesosReproConValores(dtFechaFin);
                var tareaFresco = ObtenerLiquidadaValorizada(dtFechaInicio, dtFechaFin);
                var tareaPrecioFrsXMovCam = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(lstLiqLote);
                var tareaOtroProc = _objMateriaPrima.ObtenerConsumoMovLiqOtroProc(lstLiqLote);
                await Task.WhenAll(tareaCostPromHidra, tareaProcesos, tareaOtroProc, tareaPrecioFrsXMovCam, tareaFresco,
                    _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstMatPrimaReproceso)
                    );
                lstCostPromHidra = await tareaCostPromHidra;
                lstProcesos = await tareaProcesos;
                lstMatPrimaFresco = await tareaFresco;
                lstMatPrimaFresco = lstMatPrimaFresco.Where(l => l.strTipoLiq == "LIQ_PFR").ToList();
                lstPrecioLiqOtrProc = await tareaOtroProc;
                lstPrecioFrsXMovCam = await tareaPrecioFrsXMovCam;
                _objMotorAsigPrec.AsignarCostHidra(lstCostPromHidra, lstMatPrimaReproceso);
                _objMotorProceso.AsignarCostosProcesosRepro(lstProcesos, lstMatPrimaReproceso);
                _objMotorAsigPrec.AsignarCostRecibiXFrsMovCam(lstMatPrimaReproceso, lstPrecioLiqOtrProc, lstPrecioFrsXMovCam, lstMatPrimaFresco);
                var lstLiqLoteInv = lstMatPrimaReproceso.Where(lbsRecProc =>
                        lbsRecProc.strAgrupacion == "1. RECIBIDO" && lbsRecProc.dbCostoXSecuencial == 0)
                    .Select(x => x.intLoteOrigen)
                    .Distinct()
                    .ToList();
                var lstCodProd = lstMatPrimaReproceso
                .Select(x => x.intProdCod.ToString())
                .Distinct()
                .ToList();

                var lstPreciosProm = await _objMateriaPrima.ObtenerMatPrimSaldo(lstCodProd);
                var lstPrecios = await _objMateriaPrima.ObtenerMatPrimSaldo(lstLiqLoteInv);
                _objMotorAsigPrec.EjecutarAsignacionPorSaldo(lstMatPrimaReproceso, lstPreciosProm, lstPrecios);
                var lstFrsUni = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "UNI").ToList();
                var lstFrsDir = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "DIR").ToList();
                _objMotorGrafo.CostearTodosLotesEnOrden(lstMatPrimaReproceso, lstMatPrimaFresco, lstFrsUni, lstFrsDir);


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
                _objMotorAsigPrec.RendimientoReproPlanRecibProc(lstMatPrimaReproceso);
                return lstMatPrimaReproceso.Where(l => l.strAgrupacion == "2. PROCESADO").ToList();
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
                                .Where(p => p.strAgrupacion == "2. PROCESADO" )
                                .GroupBy(p => (p.intLotNumero,p.intLoteUnificado, p.intProdCod))
                                .ToDictionary(g => g.Key, g => g.ToList());

                var dicCtrlProcesados = new ConcurrentDictionary<(int,int,int), byte>();

                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

                // 5. Iterar sobre los grupos creados
                await Parallel.ForEachAsync(gruposPorLote, parallelOptions, async (entry, ct) =>
                {
                    var objKeyLooku = (entry.Key.intLotNumero,entry.Key.intLoteUnificado, entry.Key.intProdCod);
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
            List<DateOnly> lstData ;
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
                    .Select(y => new DataProcesoParamDto{ intId = y, strDescripcion = y.ToString(), strTipoData = "ANIO" })
                    .ToList();
                listaAños.Insert(0, new DataProcesoParamDto { intId = 0, strDescripcion = "", strTipoData = "ANIO" });

                // 2. Meses
                string[] nombresMeses = { "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
                                  "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE" };

                var listaMeses = nombresMeses
                    .Select((nombre, index) => new DataProcesoParamDto {
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
                lstInvVal = await  _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin);
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

        public async Task<List<ProcesoResultadoDto>> ObtenerParametroProceso(DateTime dtFechaCorte)
        {
            List<int> lstCongTunel,lstCongIqf, lstCongBrine;
            List<string> lstProdCocido;
            List<string> lstCodTplotIQF = new List<string>() { "VA","IQ"};
            List<ProcesoResultadoDto> lstResultados = new List<ProcesoResultadoDto>(), 
                lstFrs, LstRpc;
            DateOnly dtFechaCorteCorr, dtFechaInicio, dtFechaFin;
            List<RptGrncLibras> lstLibrasProduccion;
            List<LbsCongelamiento> lstFrsConge;
            List<CopackingLbs> lstCopackingLbs;
            List<ResumenEstiloLbsDto> lstResumenEstiloLbs;
            List<MatPrimaReproceso> lstReprocesos;
            List<RptCongInd> lstLotOpcon/* , lstlotCongInd*/;
            MotorProcesoParametro objMotorProceso = new MotorProcesoParametro(_objLogger);
            try
            {
                //Se obtienen las formas de congelamiento desde el catalogoDet  
                lstCongTunel = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento Tunel")//ConsultarCatalogoXCab(_objConfig.Value.intTunelCabeceraId)
                    ).Select(int.Parse).ToList();
                lstCongIqf = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento Brine")//ConsultarCatalogoXCab(_objConfig.Value.intIqfCabId)
                    ).Select(int.Parse).ToList();
                lstCongBrine = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento IQF")
                    //await ConsultarCatalogoXCab(_objConfig.Value.intBrineCabId)
                    ).Select(int.Parse).ToList();
                //Se obtiene tipo de proceso cocido para entero
                lstProdCocido = await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Proceso Cocido");//await ConsultarCatalogoXCab(_objConfig.Value.intProdCocidoCabId); 
                //lstProdCocido = await _objProcesoParametro.ConsultarCatalogoXCab(_objConfig.Value.intProdCocidoCabId);
                dtFechaCorteCorr = DateOnly.FromDateTime(dtFechaCorte);
                dtFechaInicio = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, 1);
                dtFechaFin = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, dtFechaCorte.Day);
                var tareaRepro =  _objMateriaPrima.ReporteReproPlanRecibProc(dtFechaInicio, dtFechaFin);

                var tareaResultadosFrs = _objProcesoParametro.ObtenerProcesosFrescoConValores(dtFechaCorteCorr);
                var tareaResultadosRpc = _objProcesoParametro.ObtenerProcesosReproConValores(dtFechaCorteCorr);
                await Task.WhenAll(tareaResultadosFrs, tareaResultadosRpc, tareaRepro);
                lstReprocesos = await tareaRepro;
                lstFrs = await tareaResultadosFrs;
                LstRpc = await tareaResultadosRpc;

                //if (lstResultados.Any() && lstResultados.All(r => r.dcValor != 0))
                //{
                //    return lstResultados;
                //}
                // Se obtienen los datos de las diferentes fuentes para el cálculo
                var tareaFrsConge = _objMateriaPrima.ObtenerMatPrimFrsCongeXRangoFecha(dtFechaInicio, dtFechaFin);
                var tareaLibrasProduccion = _objMateriaPrima.ObtenerProduccionXRangoFecha(dtFechaInicio, dtFechaFin);
                var tareaCopackingLbs = _objMateriaPrima.ObtenerCopackingLbsXRangoFecha(dtFechaInicio, dtFechaFin);
                var tareaResumenEstiloLbs = _objMateriaPrima.ObtenerResumenEstiloLbsXRangoFecha(dtFechaInicio, dtFechaFin);
                var tareaLotOpcon = _objMateriaPrima.ObtenerTipProcXRangoFecha(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(tareaFrsConge, tareaLibrasProduccion, tareaCopackingLbs, tareaResumenEstiloLbs, tareaLotOpcon/*, tareaLotCongInd*/);
                lstFrsConge = await tareaFrsConge;
                lstLibrasProduccion = await tareaLibrasProduccion;
                lstCopackingLbs = await tareaCopackingLbs;
                lstResumenEstiloLbs = await tareaResumenEstiloLbs;
                lstLotOpcon = await tareaLotOpcon;

                // Llamada para Fresco
                objMotorProceso.AsignarCostoProcesoFrs(
                    lstLibrasProduccion,
                    lstLotOpcon,
                    lstResumenEstiloLbs,
                    lstFrsConge,
                    lstCongTunel,
                    lstCongIqf,
                    lstCongBrine,
                    lstCopackingLbs,
                    lstFrs,
                    _objConfig.Value.lstProdTerm,
                    _objConfig.Value.lstDescTotFresco);

                // Llamada para Reproceso
                objMotorProceso.AsignarCostoProcesoRpc(
                    lstReprocesos,
                    lstCongTunel,
                    lstCongIqf,
                    lstCongBrine,
                    LstRpc,
                    _objConfig.Value.lstProdTerm,
                    _objConfig.Value.lstDescTotFresco);
                lstResultados.AddRange(lstFrs);
                lstResultados.AddRange(LstRpc);

                return lstResultados;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerDataProcesoParametro] Ocurrio un error: {objException.Message}");
                throw;
            }
        }
        public async Task<bool> RegistrarParamProcPfr(List<ProcesoResultadoDto> lstDetalle, DateTime dtFechaCorte, string strUsario)
        {
            try
            {
                DateOnly dtFechaCorteCorr = DateOnly.FromDateTime(dtFechaCorte);
                bool blRegistroExitoso = await _objProcesoParametro.RegistrarParamCosteoPfr(lstDetalle, dtFechaCorteCorr, strUsario);
                return blRegistroExitoso;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[RegistrarParametroProceso] Ocurrio un error: {objException.Message}");
                throw;
            }
        }
    }


}
