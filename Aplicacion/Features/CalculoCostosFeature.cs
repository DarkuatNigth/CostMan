using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Repository.Services;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Aplicacion.DTos;
using CostManagementService.Aplicación.DTos;
using CostManagementService.Dominio.Entidades;
using CostManagementService.Dominio.Reglas;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using static CostManagement.Aplicación.DTos.HaberDistribucionDTO;
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
        private readonly MotorWarren _objMotorWarren;
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
            _objMotorWarren = new MotorWarren(_objLogger);
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
                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                var tareaWarren = _objProcesoParametro.ConsultarParametrosWarren(dtFechaCorte);
                await Task.WhenAll(TareaFrsVal, tareaFrs);
                objDataProceso.lstLiqFresco = await tareaFrs;
                if (objDataProceso.lstLiqFresco == null || !objDataProceso.lstLiqFresco.Any())
                    throw new Exception("No se encontraron liquidaciones de materia prima en el rango de fechas proporcionado.");

                lstYaValorizados = await TareaFrsVal;
                await ObtenerValProceso(dtFechaInicio, objDataProceso);
                //_objMotorProceso.CalcularNuevoCostProcesoSum(objDataProceso);

                _objMotorMergeVal.MergeFrescoValorizado(objDataProceso.lstLiqFresco, lstYaValorizados);



                var tareaInfoProd = _objMateriaPrima.ObtenerInfoProd(
                    objDataProceso.lstLiqFresco.Select(p => p.intCodProd.ToString()).Distinct().ToList()
                    );
                lstLiquidaciones.AddRange(objDataProceso.lstLiqFresco);
                await Task.WhenAll(
                     _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstLiquidaciones),
                     tareaInfoProd
                    );
                objDataProceso.lstInfoProd = await tareaInfoProd;
                _objMotorProceso.AsignarCostosProcesosFresco(objDataProceso);

                // NUEVO: capa Warren PFR EN/SH usando WEN/WSH guardado.
                var parametrosWarren = await tareaWarren;
                _objMotorWarren.AplicarGuardado(objDataProceso.lstLiqFresco, parametrosWarren);
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


        #region Flujo Auditoria Mat Empaque
        private static string NormalizarAudMatEmp(string? valor)
        {
            return (valor ?? string.Empty).Trim();
        }

        private static decimal NormalizarCantidadAudMatEmp(decimal valor)
        {
            return Math.Round(valor, 8);
        }

        private static (
            int Lote,
            string Producto,
            int Item,
            decimal Cantidad)
            CrearClaveAudMatEmp(CostoMatEmpProdXCietunDto item)
        {
            return (
                item.intLiqLote,
                NormalizarAudMatEmp(item.strProCodCor),
                item.intEftItem,
                NormalizarCantidadAudMatEmp(Convert.ToDecimal(item.dbEftCantidad))
            );
        }

        private static (
            int Lote,
            string Producto,
            int Item,
            decimal Cantidad)
            CrearClaveAudMatEmp(CostoMatEmpaDto item)
        {
            return (
                item.intLiqLote,
                NormalizarAudMatEmp(item.strProCodCor),
                item.intEftItem,
                NormalizarCantidadAudMatEmp(Convert.ToDecimal(item.dcEftCantidad ?? 0f))
            );
        }

        private static List<CostoMatEmpProdXCietunDto> ClonarFichaAudMatEmp(
            IEnumerable<CostoMatEmpProdXCietunDto> source)
        {
            return source.Select(x => new CostoMatEmpProdXCietunDto
            {
                intLiqLote = x.intLiqLote,
                intCtuNumero = x.intCtuNumero,
                strProCodCor = x.strProCodCor,
                intEftItem = x.intEftItem,
                strEftGrupo = x.strEftGrupo,
                dbEftCantidad = x.dbEftCantidad,
                dcLibrasXMasters = x.dcLibrasXMasters,
                dcMedCodigo = x.dcMedCodigo,
                strEmbCodigo = x.strEmbCodigo,
                strTipCodigo = x.strTipCodigo,
                dbEmbPeso = x.dbEmbPeso,
                dcCostoDesperdicioBobina = x.dcCostoDesperdicioBobina,
                dbPrecioUnit = 0d,
                dbPrecioUltConsumo = 0d,
                strEstadoFicha = "X",
                dtFechaEgreso = default
            }).ToList();
        }

        private static (
            decimal Precio,
            string Origen,
            DateOnly? Fecha)
            ResolverPrecioAudMatEmp(
                decimal? precioCt,
                DateOnly? fechaCt,
                decimal? precioMov2,
                DateOnly? fechaMov2,
                decimal? precioCompra,
                DateOnly? fechaCompra,
                decimal? precioBodite,
                DateOnly? fechaBodite)
        {
            if ((precioCt ?? 0m) > 0m)
                return (precioCt!.Value, "CT", fechaCt);

            if ((precioMov2 ?? 0m) > 0m)
                return (precioMov2!.Value, "M2", fechaMov2);

            if ((precioCompra ?? 0m) > 0m)
                return (precioCompra!.Value, "CO", fechaCompra);

            if ((precioBodite ?? 0m) > 0m)
                return (precioBodite!.Value, "BO", fechaBodite);

            return (0m, "SP", null);
        }

        private static bool FuentesPrecioDifierenAudMatEmp(params decimal?[] precios)
        {
            var valores = precios
                .Where(x => x.HasValue && x.Value > 0m)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            if (valores.Count < 2)
                return false;

            decimal minimo = valores.Min();
            decimal maximo = valores.Max();

            if (minimo <= 0m)
                return false;

            return ((maximo - minimo) / minimo) > 0.05m;
        }

        public async Task<ResultadoAuditoriaMaterialEmpaqueDto> AuditarMaterialEmpaque(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin,
            string? strProducto = null,
            int? intLote = null,
            bool blSoloErrores = false)
        {
            var resultado = new ResultadoAuditoriaMaterialEmpaqueDto();

            try
            {
                // ============================================================
                // 1. LIQUIDACIONES FRESCO + REPROCESO
                // ============================================================
                var tareaFrs = _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(
                    dtFechaInicio,
                    dtFechaFin,
                    false);

                var tareaRpc = _objMateriaPrima.ObtenerMatPrimValRpcsXRangoFecha(
                    dtFechaInicio,
                    dtFechaFin,
                    false);

                await Task.WhenAll(tareaFrs, tareaRpc);

                var lstFrs = await tareaFrs;
                var lstRpc = await tareaRpc;

                if (!lstFrs.Any() && !lstRpc.Any())
                    return resultado;

                var lstLotesFrs = lstFrs
                    .Select(x => (decimal)x.intLote)
                    .Distinct()
                    .ToList();

                var lstLotesRpc = lstRpc
                    .Select(x => x.dcLotSecuencial)
                    .Distinct()
                    .ToList();

                // ============================================================
                // 2. FICHA TÉCNICA + CATÁLOGOS DE REGLA
                // ============================================================
                var tareaFichaFrs = _objMateriaPrima.ObtenerCostMatEmpFrsProdXLiq(lstLotesFrs);
                var tareaFichaRpc = _objMateriaPrima.ObtenerCostMatEmpRpcProdXLiq(lstLotesRpc);
                var tareaEtiquetas = _objMateriaPrima.ConsultarItemEtiqueta();
                var tareaMasterCajita = _objMateriaPrima.ConsultarItemMasterCajita();

                await Task.WhenAll(
                    tareaFichaFrs,
                    tareaFichaRpc,
                    tareaEtiquetas,
                    tareaMasterCajita);

                var lstFichaFrs = await tareaFichaFrs;
                var lstFichaRpc = await tareaFichaRpc;
                var dictEtiquetas = await tareaEtiquetas;
                var dictMasterCajita = await tareaMasterCajita;

                var hsEtiqueta = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CAM",
            "VE"
        };

                var hsReempaque = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "R3",
            "VR"
        };

                var lstFicha = lstFichaFrs
                    .Concat(lstFichaRpc)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(strProducto))
                {
                    string productoFiltro = NormalizarAudMatEmp(strProducto);
                    lstFicha = lstFicha
                        .Where(x => NormalizarAudMatEmp(x.strProCodCor) == productoFiltro)
                        .ToList();
                }

                if (intLote.HasValue)
                {
                    lstFicha = lstFicha
                        .Where(x => x.intLiqLote == intLote.Value)
                        .ToList();
                }

                if (!lstFicha.Any())
                    return resultado;

                // ============================================================
                // 3. REGLA DE INCLUSIÓN, SIN ELIMINAR ÍTEMS
                // ============================================================
                var dictReglas = new Dictionary<
                    (int Lote, string Producto, int Item, decimal Cantidad),
                    (bool Incluido, string Regla)>();

                foreach (var item in lstFicha)
                {
                    bool incluido = true;
                    string regla = "FICHA_COMPLETA";

                    string tipo = NormalizarAudMatEmp(item.strTipCodigo);
                    string itemKey = item.intEftItem.ToString();

                    if (hsEtiqueta.Contains(tipo))
                    {
                        regla = "SOLO_ETIQUETA";
                        incluido = dictEtiquetas.ContainsKey(itemKey);
                    }
                    else if (hsReempaque.Contains(tipo))
                    {
                        regla = "MASTER_CAJITA";
                        incluido = dictMasterCajita.ContainsKey(itemKey);
                    }

                    dictReglas[CrearClaveAudMatEmp(item)] = (incluido, regla);
                }

                // ============================================================
                // 4. CAPTURAR CADA FUENTE EN LISTAS INDEPENDIENTES
                //    La lista original conserva exclusivamente el precio CT que
                //    ya cargan ObtenerCostMatEmp*ProdXLiq.
                // ============================================================
                var lstMov2 = ClonarFichaAudMatEmp(lstFicha);
                var lstCompra = ClonarFichaAudMatEmp(lstFicha);
                var lstBodite = ClonarFichaAudMatEmp(lstFicha);

                await Task.WhenAll(
                    _objMateriaPrima.ObtenerCostoPromMov2XFichaTecnica(
                        lstMov2,
                        dtFechaInicio,
                        dtFechaFin),

                    _objMateriaPrima.ObtenerCostoPromMov1XFichaTecnica(
                        lstCompra,
                        dtFechaInicio,
                        dtFechaFin),

                    _objMateriaPrima.ObtenerCostoPromBoditeXFichaTecnica(
                        lstBodite,
                        dtFechaInicio,
                        dtFechaFin));

                var dictMov2 = lstMov2
                    .Where(x => (x.dbPrecioUnit ?? 0d) > 0d)
                    .GroupBy(x => x.intEftItem)
                    .ToDictionary(g => g.Key, g => g.First());

                var dictCompra = lstCompra
                    .Where(x => (x.dbPrecioUnit ?? 0d) > 0d)
                    .GroupBy(x => x.intEftItem)
                    .ToDictionary(g => g.Key, g => g.First());

                var dictBodite = lstBodite
                    .Where(x => (x.dbPrecioUnit ?? 0d) > 0d)
                    .GroupBy(x => x.intEftItem)
                    .ToDictionary(g => g.Key, g => g.First());

                // ============================================================
                // 5. LEER LO QUE YA ESTÁ GUARDADO EN COSTOS
                // ============================================================
                var lstLotesAuditoria = lstFicha
                    .Select(x => x.intLiqLote)
                    .Distinct()
                    .ToList();

                var lstActualBD = await _objCostoMaterialEmpaque
                    .ObtenerCostoEmpaqueXLote(lstLotesAuditoria);

                // Evita traer como ME014 productos históricos del mismo lote que no
                // forman parte de la liquidación/ficha que estamos auditando.
                var hsProductosActuales = lstFicha
                    .Select(x => (
                        x.intLiqLote,
                        Producto: NormalizarAudMatEmp(x.strProCodCor)))
                    .ToHashSet();

                lstActualBD = lstActualBD
                    .Where(x => hsProductosActuales.Contains((
                        x.intLiqLote,
                        NormalizarAudMatEmp(x.strProCodCor))))
                    .ToList();

                if (!string.IsNullOrWhiteSpace(strProducto))
                {
                    string productoFiltro = NormalizarAudMatEmp(strProducto);
                    lstActualBD = lstActualBD
                        .Where(x => NormalizarAudMatEmp(x.strProCodCor) == productoFiltro)
                        .ToList();
                }

                var dictDetalleBD = lstActualBD
                    .GroupBy(CrearClaveAudMatEmp)
                    .ToDictionary(g => g.Key, g => g.First());

                var dictCabeceraBD = lstActualBD
                    .GroupBy(x => (
                        x.intLiqLote,
                        Producto: NormalizarAudMatEmp(x.strProCodCor)))
                    .ToDictionary(
                        g => g.Key,
                        g => Convert.ToDecimal(g.First().intTotal));

                // ============================================================
                // 6. DETALLE DE AUDITORÍA
                // ============================================================
                foreach (var item in lstFicha)
                {
                    var banderas = new List<string>();
                    var claveFicha = CrearClaveAudMatEmp(item);
                    var regla = dictReglas[claveFicha];

                    decimal? precioCt = null;
                    DateOnly? fechaCt = null;

                    // La lista base solo llega con CT cargado en este punto.
                    if ((item.dbPrecioUnit ?? 0d) > 0d &&
                        string.Equals(item.strEstadoFicha, "E", StringComparison.OrdinalIgnoreCase))
                    {
                        precioCt = Convert.ToDecimal(item.dbPrecioUnit!.Value);

                        if (item.dtFechaEgreso != default)
                            fechaCt = item.dtFechaEgreso;
                    }

                    dictMov2.TryGetValue(item.intEftItem, out var fuenteMov2);
                    dictCompra.TryGetValue(item.intEftItem, out var fuenteCompra);
                    dictBodite.TryGetValue(item.intEftItem, out var fuenteBodite);

                    decimal? precioMov2 = fuenteMov2?.dbPrecioUnit != null
                        ? Convert.ToDecimal(fuenteMov2.dbPrecioUnit.Value)
                        : null;

                    DateOnly? fechaMov2 = fuenteMov2 != null && fuenteMov2.dtFechaEgreso != default
                        ? fuenteMov2.dtFechaEgreso
                        : null;

                    decimal? precioCompra = fuenteCompra?.dbPrecioUnit != null
                        ? Convert.ToDecimal(fuenteCompra.dbPrecioUnit.Value)
                        : null;

                    DateOnly? fechaCompra = fuenteCompra != null && fuenteCompra.dtFechaEgreso != default
                        ? fuenteCompra.dtFechaEgreso
                        : null;

                    decimal? precioBodite = fuenteBodite?.dbPrecioUnit != null
                        ? Convert.ToDecimal(fuenteBodite.dbPrecioUnit.Value)
                        : null;

                    DateOnly? fechaBodite = fuenteBodite != null && fuenteBodite.dtFechaEgreso != default
                        ? fuenteBodite.dtFechaEgreso
                        : null;

                    var seleccionado = ResolverPrecioAudMatEmp(
                        precioCt,
                        fechaCt,
                        precioMov2,
                        fechaMov2,
                        precioCompra,
                        fechaCompra,
                        precioBodite,
                        fechaBodite);

                    dictDetalleBD.TryGetValue(claveFicha, out var actualBD);

                    if (regla.Incluido && seleccionado.Precio <= 0m)
                        banderas.Add("ME001");

                    decimal librasMaster = Convert.ToDecimal(item.dcLibrasXMasters);
                    decimal cantidadFicha = Convert.ToDecimal(item.dbEftCantidad);

                    if (regla.Incluido && librasMaster <= 0m)
                        banderas.Add("ME002");

                    if (FuentesPrecioDifierenAudMatEmp(
                        precioCt,
                        precioMov2,
                        precioCompra,
                        precioBodite))
                    {
                        banderas.Add("ME004");
                    }

                    if (seleccionado.Fecha.HasValue &&
                        (seleccionado.Fecha.Value < dtFechaInicio ||
                         seleccionado.Fecha.Value > dtFechaFin))
                    {
                        banderas.Add("ME005");
                    }

                    if (regla.Incluido && actualBD == null)
                        banderas.Add("ME015");

                    decimal precioActualBD = actualBD?.dcCosPro ?? 0m;

                    if (regla.Incluido &&
                        actualBD != null &&
                        Math.Abs(precioActualBD - seleccionado.Precio) > 0.0001m)
                    {
                        banderas.Add("ME006");
                    }

                    if (!regla.Incluido && actualBD != null)
                    {
                        if (regla.Regla == "SOLO_ETIQUETA")
                            banderas.Add("ME009");
                        else if (regla.Regla == "MASTER_CAJITA")
                            banderas.Add("ME010");
                    }

                    decimal costoMaster = regla.Incluido
                        ? seleccionado.Precio * cantidadFicha
                        : 0m;

                    decimal costoLibra = regla.Incluido && librasMaster > 0m
                        ? costoMaster / librasMaster
                        : 0m;

                    resultado.lstDetalle.Add(new AuditoriaMaterialEmpaqueDto
                    {
                        intLote = item.intLiqLote,
                        strProducto = NormalizarAudMatEmp(item.strProCodCor),
                        strTipoProceso = string.IsNullOrWhiteSpace(item.strTipCodigo)
                            ? "FRESCO"
                            : item.strTipCodigo.Trim(),
                        intCierreTunel = item.intCtuNumero,

                        intItem = item.intEftItem,
                        strItemDescripcion = actualBD?.strIteDesCor ?? item.intEftItem.ToString(),
                        strGrupoFicha = item.strEftGrupo ?? string.Empty,
                        strLineaActualBD = actualBD?.strLinea ?? string.Empty,
                        strGrupoActualBD = actualBD?.strGrupo ?? string.Empty,

                        dcCantidadFicha = cantidadFicha,
                        dcLibrasMaster = librasMaster,

                        blIncluidoCosto = regla.Incluido,
                        strReglaEmpaque = regla.Regla,

                        dcPrecioCierreTunel = precioCt,
                        dtPrecioCierreTunel = fechaCt,

                        dcPrecioMov2 = precioMov2,
                        dtPrecioMov2 = fechaMov2,

                        dcPrecioCompra = precioCompra,
                        dtPrecioCompra = fechaCompra,

                        dcPrecioBodite = precioBodite,
                        dtPrecioBodite = fechaBodite,

                        dcPrecioSeleccionado = seleccionado.Precio,
                        strOrigenSeleccionado = seleccionado.Origen,
                        dtFechaPrecioSeleccionado = seleccionado.Fecha,

                        blExisteEnBD = actualBD != null,
                        blExisteEnFichaActual = true,
                        dcPrecioActualBD = precioActualBD,
                        dcCantidadActualBD = Convert.ToDecimal(actualBD?.dcEftCantidad ?? 0f),
                        strOrigenActualBD = actualBD?.strEstadoEmpaque ?? string.Empty,

                        dcCostoItemMaster = Math.Round(costoMaster, 8),
                        dcCostoItemLibra = Math.Round(costoLibra, 8),

                        strBandera = string.Join("|", banderas.Distinct()),
                        strObservacion = banderas.Any()
                            ? "REVISAR"
                            : regla.Incluido
                                ? "OK"
                                : "EXCLUIDO_POR_REGLA"
                    });
                }

                // ============================================================
                // 7. DETECTAR ÍTEMS GUARDADOS QUE YA NO EXISTEN EN LA FICHA
                // ============================================================
                var hsFichaActual = lstFicha
                    .Select(CrearClaveAudMatEmp)
                    .ToHashSet();

                foreach (var bd in lstActualBD)
                {
                    var key = CrearClaveAudMatEmp(bd);

                    if (hsFichaActual.Contains(key))
                        continue;

                    resultado.lstDetalle.Add(new AuditoriaMaterialEmpaqueDto
                    {
                        intLote = bd.intLiqLote,
                        strProducto = NormalizarAudMatEmp(bd.strProCodCor),
                        strTipoProceso = "REGISTRO_ANTERIOR",
                        intItem = bd.intEftItem,
                        strItemDescripcion = bd.strIteDesCor ?? bd.intEftItem.ToString(),
                        strGrupoFicha = string.Empty,
                        strLineaActualBD = bd.strLinea ?? string.Empty,
                        strGrupoActualBD = bd.strGrupo ?? string.Empty,
                        dcCantidadFicha = 0m,
                        dcLibrasMaster = Convert.ToDecimal(bd.dcLibxMaster ?? 0f),
                        blIncluidoCosto = false,
                        strReglaEmpaque = "NO_EXISTE_FICHA_ACTUAL",
                        blExisteEnBD = true,
                        blExisteEnFichaActual = false,
                        dcPrecioActualBD = bd.dcCosPro ?? 0m,
                        dcCantidadActualBD = Convert.ToDecimal(bd.dcEftCantidad ?? 0f),
                        strOrigenActualBD = bd.strEstadoEmpaque ?? string.Empty,
                        strBandera = "ME014",
                        strObservacion = "Ítem almacenado en BD que ya no coincide con la ficha técnica actual."
                    });
                }

                // ============================================================
                // 8. LIBRAS POR LOTE + PRODUCTO
                // ============================================================
                var dictLibras = new Dictionary<(int Lote, string Producto), decimal>();

                void SumarLibras(int lote, string producto, decimal libras)
                {
                    var key = (lote, NormalizarAudMatEmp(producto));

                    if (!dictLibras.ContainsKey(key))
                        dictLibras[key] = 0m;

                    dictLibras[key] += libras;
                }

                var hsFrs = lstFichaFrs
                    .Select(x => (x.intLiqLote, NormalizarAudMatEmp(x.strProCodCor)))
                    .ToHashSet();

                var hsRpc = lstFichaRpc
                    .Select(x => (x.intLiqLote, NormalizarAudMatEmp(x.strProCodCor)))
                    .ToHashSet();

                foreach (var frs in lstFrs)
                {
                    string producto = frs.intCodProd?.ToString() ?? string.Empty;
                    var key = (frs.intLote, NormalizarAudMatEmp(producto));

                    if (!hsFrs.Contains(key))
                        continue;

                    SumarLibras(
                        frs.intLote,
                        producto,
                        Convert.ToDecimal(frs.dcLibras));
                }

                foreach (var rpc in lstRpc)
                {
                    int lote = Convert.ToInt32(rpc.dcLotSecuencial);
                    string producto = rpc.intCodProd?.ToString() ?? string.Empty;
                    var key = (lote, NormalizarAudMatEmp(producto));

                    if (!hsRpc.Contains(key))
                        continue;

                    SumarLibras(
                        lote,
                        producto,
                        Convert.ToDecimal(rpc.dcLibras));
                }

                // ============================================================
                // 9. RESUMEN LOTE + PRODUCTO
                // ============================================================
                foreach (var grupo in resultado.lstDetalle
                    .GroupBy(x => new
                    {
                        x.intLote,
                        x.strProducto
                    }))
                {
                    var key = (
                        grupo.Key.intLote,
                        NormalizarAudMatEmp(grupo.Key.strProducto));

                    decimal costoAuditado = grupo
                        .Where(x => x.blExisteEnFichaActual && x.blIncluidoCosto)
                        .Sum(x => x.dcCostoItemLibra);

                    dictCabeceraBD.TryGetValue(key, out decimal costoActual);
                    dictLibras.TryGetValue(key, out decimal libras);

                    decimal diferenciaUnit = costoAuditado - costoActual;

                    var banderas = grupo
                        .Where(x => !string.IsNullOrWhiteSpace(x.strBandera))
                        .SelectMany(x => x.strBandera.Split(
                            '|',
                            StringSplitOptions.RemoveEmptyEntries))
                        .Distinct()
                        .ToList();

                    if (Math.Abs(diferenciaUnit) > 0.0001m)
                        banderas.Add("ME011");

                    banderas = banderas.Distinct().ToList();

                    string tipoProceso = string.Join(",",
                        grupo
                            .Where(x => x.blExisteEnFichaActual)
                            .Select(x => x.strTipoProceso)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Distinct());

                    if (string.IsNullOrWhiteSpace(tipoProceso))
                        tipoProceso = "REGISTRO_ANTERIOR";

                    resultado.lstResumen.Add(new AuditoriaMaterialEmpaqueResumenDto
                    {
                        intLote = grupo.Key.intLote,
                        strProducto = grupo.Key.strProducto,
                        strTipoProceso = tipoProceso,
                        dcLibras = Math.Round(libras, 2),

                        dcCostoUnitarioActualBD = Math.Round(costoActual, 8),
                        dcCostoUnitarioAuditado = Math.Round(costoAuditado, 8),
                        dcDiferenciaUnitario = Math.Round(diferenciaUnit, 8),

                        dcCostoTotalActualBD = Math.Round(costoActual * libras, 2),
                        dcCostoTotalAuditado = Math.Round(costoAuditado * libras, 2),
                        dcDiferenciaDolares = Math.Round(diferenciaUnit * libras, 2),

                        intCantidadItems = grupo.Count(x => x.blExisteEnFichaActual),
                        intCantidadBanderas = banderas.Count,
                        strBanderas = string.Join("|", banderas),
                        strEstado = banderas.Any() ? "REVISAR" : "OK"
                    });
                }

                // ============================================================
                // 10. SOLO ERRORES + ORDEN POR IMPACTO
                // ============================================================
                if (blSoloErrores)
                {
                    var clavesError = resultado.lstResumen
                        .Where(x => x.strEstado == "REVISAR")
                        .Select(x => (x.intLote, x.strProducto))
                        .ToHashSet();

                    resultado.lstResumen = resultado.lstResumen
                        .Where(x => clavesError.Contains((x.intLote, x.strProducto)))
                        .ToList();

                    resultado.lstDetalle = resultado.lstDetalle
                        .Where(x => clavesError.Contains((x.intLote, x.strProducto)))
                        .ToList();
                }

                resultado.lstResumen = resultado.lstResumen
                    .OrderByDescending(x => Math.Abs(x.dcDiferenciaDolares))
                    .ToList();

                resultado.lstDetalle = resultado.lstDetalle
                    .OrderBy(x => x.intLote)
                    .ThenBy(x => x.strProducto)
                    .ThenByDescending(x => x.dcCostoItemLibra)
                    .ToList();

                return resultado;
            }
            catch (Exception ex)
            {
                _objLogger.LogError(
                    $"[CalculoCostosFeature].[AuditarMaterialEmpaque] Ocurrió un error: {ex.Message}");
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
            DataProcesoParam objDataProceso = new();
            List<ParamRectrac> lstInfoRetrac;
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
                var tareaRetrac =  _objMateriaPrima.ObtenerInfoRectractiladoXLote(dtFechaInicio, dtFechaFin);
                var tareaPrecioFrsXMovCam = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(lstLiqLote);
                var tareaOtroProc = _objMateriaPrima.ObtenerConsumoMovLiqOtroProc(lstLiqLote);

                var lstCodProd = objDataProceso.lstLiqRepro.Select(x => x.intCodProd.ToString()).Distinct().ToList();
                await ObtenerValProceso(dtFechaInicio, objDataProceso);
                var TareaTarifaProceso = _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorte);
                var tareaInfoProd = _objMateriaPrima.ObtenerInfoProd(lstCodProd);
                await Task.WhenAll(
                    tareaOtroProc, tareaPrecioFrsXMovCam, tareaFresco, tareaRetrac,
                    _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(objDataProceso.lstLiqRepro),
                    tareaInfoProd
                    );
                objDataProceso.lstInfoRetrac = await tareaRetrac;
                objDataProceso.lstLiqFresco = await tareaFresco;
                objDataProceso.lstProcesoTarifa = await TareaTarifaProceso;
                lstPrecioLiqOtrProc = await tareaOtroProc;
                lstPrecioFrsXMovCam = await tareaPrecioFrsXMovCam;
                objDataProceso.lstInfoProd = await tareaInfoProd;
                _objMotorAsigPrec.AsignarRetractiladoRepro(objDataProceso);
                _objMotorProceso.AsignarCostosProcesosRepro(objDataProceso);
                _objMotorAsigPrec.AsignarCostRecibiXFrsMovCam(objDataProceso);
                var lstLiqLoteInv = objDataProceso.lstLiqRepro.Where(lbsRecProc =>
                        lbsRecProc.strAgrupacion == "1. RECIBIDO" && lbsRecProc.dbCostoXSecuencial == 0)
                    .Select(x => x.intLoteOrigen)
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
                        .ToLookup(v => (v.intLotNumero, v.intLoteUnificado, v.intCodProd, v.intCodTal));

                    foreach (var item in lstMatPrimaRepro.Where(x =>
                        x.strAgrupacion == "2. PROCESADO" &&
                        x.dcCostoTotXLibra == null))
                    {
                        var key = (item.intLotNumero, item.intLoteUnificado,
                                   item.intCodProd, item.intCodTal);

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
                                .GroupBy(p => (p.intLotNumero, p.intLoteUnificado, p.intCodProd))
                                .ToDictionary(g => g.Key, g => g.ToList());

                var dicCtrlProcesados = new ConcurrentDictionary<(int, int, int), byte>();

                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

                // 5. Iterar sobre los grupos creados
                await Parallel.ForEachAsync(gruposPorLote, parallelOptions, async (entry, ct) =>
                {
                    var objKeyLooku = (entry.Key.intLotNumero, entry.Key.intLoteUnificado, entry.Key.intCodProd);
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


        public async Task<List<InfoTalProd>> ObtenerInfoEquiValTalla()
        {
            List<InfoTalProd> lstData;
            try
            {
                lstData = await _objMateriaPrima.ObtenerInfoCodTal();
                return lstData;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerInfoEquiValTalla] Ocurrio un error: {objException.Message}");
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
            List<ParamRectrac> lstInfoRetrac;
            try
            {
                //Se obtiene tipo de proceso cocido para entero
                lstProdCocido = await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Proceso Cocido");
                dtFechaCorteCorr = DateOnly.FromDateTime(dtFechaCorte);
                dtFechaInicio = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, 1);
                dtFechaFin = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, dtFechaCorte.Day);
                var tareaReproCompleto =_objMateriaPrima.ReporteReproPlanRecibProc( dtFechaInicio,dtFechaFin);

                var TareaTarifaProceso = _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorteCorr);

                var tareaFresco =_objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio,dtFechaFin);

                await Task.WhenAll(
                    tareaReproCompleto,
                    TareaTarifaProceso,
                    tareaFresco,
                    ObtenerValProceso(dtFechaInicio, objDataProceso));

                objDataProceso.lstLiqFresco =await tareaFresco;
                objDataProceso.lstLiqReproCompleto = await tareaReproCompleto;
                objDataProceso.lstLiqRepro = objDataProceso.lstLiqReproCompleto.Where(x =>
                string.Equals(  x.strAgrupacion,
                                "2. PROCESADO",
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();

                objDataProceso.lstProcesoTarifa = await TareaTarifaProceso;
                var lstCodProd = objDataProceso.lstLiqRepro.Select(x => x.intCodProd.ToString()).Distinct().ToList();
                objDataProceso.lstInfoRetrac = await _objMateriaPrima.ObtenerInfoRectractiladoXLote(dtFechaInicio, dtFechaFin);
                objDataProceso.lstInfoProd = await _objMateriaPrima.ObtenerInfoProd(lstCodProd);

                _objMotorAsigPrec.AsignarRetractiladoRepro(objDataProceso);
                //if (lstResultados.Any() && lstResultados.All(r => r.dcValor != 0))
                //{
                //    return lstResultados;
                //}
                objDataProceso.lstProdTerm = _objConfig.Value.lstProdTerm;
                objDataProceso.lstDescTotFresco = _objConfig.Value.lstDescTotFresco;
                await _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(objDataProceso.lstLiqFresco);
                await _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(objDataProceso.lstLiqRepro);

                List<string> lstItemCod;

                lstItemCod = MatPrimaReproceso.ObtenerLstItemHidra(objDataProceso.lstLiqRepro);
                var tareaCostPromHidra = await _objMateriaPrima.CostoUltMovXItemCod(lstItemCod, dtFechaInicio, dtFechaFin);

                objDataProceso.dcCostoHidraReproceso = tareaCostPromHidra.Sum(obj => obj.dcConsumoTotal);
                objDataProceso.lstLibrasParticion = new List<LibrasParticionDto>();
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

        public async Task<CostosUnitarios> ObtenerValProcesoFrs(DateOnly dtFechaInicio)
        {
            DataProcesoParam objDataProceso = new();
            ConcurrentDictionary<string, decimal> dictCosto;
            try
            {

                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                await ObtenerValProceso(dtFechaInicio, objDataProceso);

                dictCosto = ProcesoResultadoDto.ConstruirDictProc(objDataProceso.lstProcesoFrs);
                // Reutilizamos tu struct existente para mapear el diccionario
                CostosUnitarios objCostUnit = CostosUnitarios.ExtraerCostosUnitarios(dictCosto);
                return objCostUnit;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerValProceso] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<CostosUnitarios> ObtenerValProcesoRpc(DateOnly dtFechaInicio)
        {
            DataProcesoParam objDataProceso = new();
            ConcurrentDictionary<string, decimal> dictCosto;
            try
            {

                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                await ObtenerValProceso(dtFechaInicio, objDataProceso);

                dictCosto = ProcesoResultadoDto.ConstruirDictProc(objDataProceso.lstProcesoRpc);
                // Reutilizamos tu struct existente para mapear el diccionario
                CostosUnitarios objCostUnit = CostosUnitarios.ExtraerCostosUnitarios(dictCosto);
                return objCostUnit;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerValProceso] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<WarrenResultadoDto> GuardarWarren(GuardarWarrenRequest request)
        {
            try
            {
                int anio = Convert.ToInt32(request.strAnio);
                int mes = Convert.ToInt32(request.strMes);

                DateOnly inicio = new(anio, mes, 1);
                DateOnly fin = new(anio, mes, DateTime.DaysInMonth(anio, mes));

                // ObtenerLiquidacionValorizada deja calculados los componentes y dcCostTotalProc
                // del PFR sin modificar el costo original con Warren.
                var tareaPfr = ObtenerLiquidacionValorizada(inicio, fin);
                var tareaParametros = _objProcesoParametro.ConsultarProcesosFrescoConValores(fin);

                await Task.WhenAll(tareaPfr, tareaParametros);

                var lstPfr = await tareaPfr;
                var lstParametrosPfr = await tareaParametros;

                WarrenResultadoDto resultado = _objMotorWarren.Calcular(
                    lstPfr,
                    lstParametrosPfr,
                    request.dcObjetivoWarren);

                await _objProcesoParametro.RegistrarParametrosWarren(
                    fin,
                    resultado,
                    request.strUsuario);

                return resultado;
            }
            catch (Exception ex)
            {
                ManejoLog<CalculoCostosFeature>.Error(
                    _objLogger,
                    nameof(CalculoCostosFeature),
                    nameof(GuardarWarren),
                    ex);
                throw;
            }
        }

        #endregion

        #region Distribucion Costos Electricos
        public async Task<List<DistribucionCostoDto>> ObtenerDistribucionCostosElectricos(string strAnio, string strMes)
        {
            List<DistribucionCostoDto> lstDistribucionCostos;
            int intAnio, intMes;
            try
            {
                intAnio = Convert.ToInt32(strAnio);
                intMes = Convert.ToInt32(strMes);
                lstDistribucionCostos = await _objProcesoParametro.ConsultarDistribucion(intAnio, intMes);
                return lstDistribucionCostos;
            }
            catch (Exception objException)
            {
                ManejoLog<CalculoCostosFeature>.Error(_objLogger, nameof(CalculoCostosFeature), nameof(ObtenerDistribucionCostosElectricos), objException);
                throw;
            }
        }
        public async Task<bool> RegistrarDistribucionCostosElectricos(List<DistribucionCostoDto> objRegistros)
        {
            bool blEjecucion = false;
            try
            {
                blEjecucion = await _objProcesoParametro.CrearOActualizarDistribucion(objRegistros);
                return blEjecucion;
            }
            catch (Exception objException)
            {
                ManejoLog<CalculoCostosFeature>.Error(_objLogger, nameof(CalculoCostosFeature), nameof(ObtenerDistribucionCostosElectricos), objException);
                throw;
            }
        }

        public async Task<ConsolidadoDTO> GetConsolidadoHaber(int anio)
        {
            ConsolidadoDTO objConsolidado;
            try
            {
                // 1. Traer HABEREs de la BD
                var haber = await _objProcesoParametro.GetHaberesDistribucion(anio);


                // 2. Separar por TIPO: ENLEC y REBAS
                var energiaHaber = haber.Where(h => h.Tipo == "ENLEC").ToList();
                var basuraHaber = haber.Where(h => h.Tipo == "REBAS").ToList();


                // 3. Convertir a ConsolidadoDTO
                objConsolidado = new ConsolidadoDTO
                {
                    Anio = anio,
                    Usd = HaberDistribucionDTO.ConvertirADiccionarioMeses(energiaHaber, h => h.Monto),
                    Kwh = HaberDistribucionDTO.ConvertirADiccionarioMeses(energiaHaber, h => h.ValorKw),
                    Alumbrado = HaberDistribucionDTO.ConvertirADiccionarioMeses(basuraHaber, h => h.Monto)
                };

                return objConsolidado;
            }
            catch (Exception objException)
            {
                ManejoLog<CalculoCostosFeature>.Error(_objLogger, nameof(CalculoCostosFeature), nameof(GetConsolidadoHaber), objException);
                throw;
            }
        }

        #endregion
    }
}
