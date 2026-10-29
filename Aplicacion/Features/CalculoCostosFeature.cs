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

        #region Cierre mensual valorizado
        // leer los parámetros persistidos; sin recibir snapshot ni usar cachés de valorización.
        public async Task<CierreValorizacionContexto> ObtenerContextoValorizacionCierreAsync(int intAnio, int intMes)
        {
            DateOnly dtCorte = new(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));
            var ctx = new CierreValorizacionContexto { blIgnorarCache = true };
            ctx.lstProcesoFrs = await _objProcesoParametro.ConsultarProcesosFrescoConValores(dtCorte);
            ctx.lstProcesoRpc = await _objProcesoParametro.ConsultarProcesosReproConValores(dtCorte);
            ctx.lstCostoProcesoParticion = await _objProcesoParametro.ConsultarCostoProcesoParticion(intAnio, intMes);
            ctx.lstParametrosWarren = await _objProcesoParametro.ConsultarParametrosWarren(dtCorte);
            return ctx;
        }

        public async Task<CierreValorizacionDto> ValorizarCierreAsync(int intAnio, int intMes, CierreValorizacionContexto ctx)
        {
            ArgumentNullException.ThrowIfNull(ctx);
            if (ctx.objConfiguracion == null || !ctx.blIgnorarCache)
                throw new InvalidOperationException("El cierre requiere configuración propia y cálculo sin caché.");
            DateOnly dtInicio = new(intAnio, intMes, 1);
            DateOnly dtFin = new(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));
            var objResultado = new CierreValorizacionDto();
            objResultado.lstAdvertencias.AddRange(ctx.lstAdvertencias);
            #region Solape y medicion del cierre
            // RPC carga con factories propias; parametros y motores esperan fresco.
            // using var objTiempoCierre = MedicionTiempos.IniciarMetodo(_objLogger, "ValorizarCierreAsync", () => objResultado.lstFresco.Count + objResultado.lstRepro.Count);
            var tareaFrescoCierre = MedicionTiempos.MedirAsync("cierre-fresco", () => ObtenerLiquidacionValorizada(dtInicio, dtFin, ctx), x => x.Count);
            var tareaReproCierre = MedicionTiempos.MedirAsync("cierre-repro", () => ObtenerReporteMateriaPrimaReproTiempoReal(
                dtInicio, dtFin, enmTipoConsulta.ConFront, ctx: ctx, tareaFrescoCierre: tareaFrescoCierre), x => x.Count);
            // WhenAll espera y observa ambas ramas incluso si una falla. Todavia no hay escritura.
            await Task.WhenAll(tareaFrescoCierre, tareaReproCierre);
            var lstFresco = await tareaFrescoCierre;
            var lstRepro = (await tareaReproCierre).Where(x => x.strAgrupacion == "2. PROCESADO").ToList();
            // using var objTiempoArmado = MedicionTiempos.IniciarTramo("cierre-armado-cabeceras-detalles", () => objResultado.lstFresco.Count + objResultado.lstRepro.Count);
            #endregion
            if (lstFresco.Count == 0 && lstRepro.Count == 0)
                throw new InvalidOperationException("No existen líneas de producción para valorizar el período; no se modificó el cierre.");
            // validar todas las tarifas antes de construir el cierre; agrupar fallos.
            ValidarCodigosTarifaCierre(lstRepro, ctx.dicEtapas);
            decimal dcDiferenciaComponentes = 0m, dcDiferenciaDetalles = 0m;
            decimal dcNetoWarren = lstFresco.Sum(x => (x.dcTotalWarren ?? x.dcCostTotalProc ?? 0m) - (x.dcCostTotalProc ?? 0m));
            if (Math.Abs(dcNetoWarren) > 0.01m)
                throw new InvalidOperationException($"El ajuste neto Warren de fresco no concilia: {dcNetoWarren:N5}.");

            foreach (var objLiq in lstFresco)
            {
                string strLinea = $"Fresco lote {objLiq.intLote}, producto {objLiq.intCodProd}, talla {objLiq.intLidCodTal}";
                string strProducto = objLiq.intCodProd.ToString();
                if (!int.TryParse(strProducto, out _)) throw new InvalidOperationException($"{strLinea}: producto no numérico.");
                if (!objLiq.dtFechaLote.HasValue || objLiq.dtFechaLote.Value < dtInicio || objLiq.dtFechaLote.Value > dtFin)
                    throw new InvalidOperationException($"{strLinea}: fecha fuera del período de cierre.");
                if (!objLiq.dcCostoTotXLibra.HasValue || !objLiq.dcTotalDol.HasValue)
                    throw new InvalidOperationException($"{strLinea}: valorización incompleta.");
                string strParticion = ParticionDetalleCierre(ParticionCosteo.ClasificarFrs(objLiq.strProClas01, objLiq.strProClas05), strLinea);
                bool blWarren = (strParticion is "EN" or "CO") && ctx.lstParametrosWarren.Count > 0;
                if (blWarren && (!objLiq.dcCostoTotXLibraWarren.HasValue || !objLiq.dcTotalCostoWarren.HasValue))
                    throw new InvalidOperationException($"{strLinea}: falta valorización Warren.");
                var objLinea = new CierreFrescoLineaDto
                {
                    objCabecera = new TbMateriaPrimaFrescoValorizada
                    {
                        MfEmpCodigo = 1, MfPaCodigo = 1, MfFecha = objLiq.dtFechaLote.Value,
                        MfRloNumero = objLiq.intLote, MfProCodcor = strProducto, MfTalCodigo = checked((short)objLiq.intLidCodTal),
                        MfBodCodigo = objLiq.strBodCod, MfMedCodigo = checked((byte)objLiq.dcMedCodigo), MfEmbCodigo = objLiq.strEmbCodigo,
                        MfMasters = (decimal)objLiq.dcMasters, MfLibras = (decimal)objLiq.dcLibras,
                        MfCostoUnitario = objLiq.dcCostoTotXLibra.Value, MfCostoTotal = (decimal)objLiq.dcTotalDol.Value,
                        MfCostoUnitarioWarren = blWarren ? Math.Round(objLiq.dcCostoTotXLibraWarren.Value, 4, MidpointRounding.AwayFromZero) : null,
                        MfCostoTotalWarren = blWarren ? objLiq.dcTotalCostoWarren : null, MfEstado = "AC"
                    }
                };
                var dicComponentes = ComponentesCierre(objLiq.ProcesoPrimario, objLiq.ProcesoPresentacion, objLiq.ProcesoCongelacion,
                    objLiq.ProcesoSecundario, objLiq.ProcesoCostFijo, objLiq.ProcesoCostIndirecto, objLiq.dcCostoCopacking, false, objLiq.ProcesosAdicionales);
                objLinea.lstDetalles = ConstruirDetallesCierre(dicComponentes, objLiq.dcExcedente ?? 0m, objLiq.dcTarifaProc ?? 0m,
                    objLiq.dcCostTotalProc ?? 0m, objLinea.objCabecera.MfLibras, strParticion, "PFR", strLinea, ctx.dicEtapas,
                    objResultado.lstAdvertencias, ref dcDiferenciaComponentes, ref dcDiferenciaDetalles);
                objResultado.lstFresco.Add(objLinea);
            }
            foreach (var objLiq in lstRepro)
            {
                string strLinea = $"Reproceso lote {objLiq.intLotNumero}/{objLiq.intLoteUnificado}, producto {objLiq.intCodProd}, talla {objLiq.intCodTal}";
                string strProducto = objLiq.intCodProd.ToString();
                if (!int.TryParse(strProducto, out _)) throw new InvalidOperationException($"{strLinea}: producto no numérico.");
                DateOnly dtFecha = DateOnly.FromDateTime(objLiq.dtLotFecha);
                if (dtFecha < dtInicio || dtFecha > dtFin) throw new InvalidOperationException($"{strLinea}: fecha fuera del período de cierre.");
                if (!objLiq.dcCostoTotXLibra.HasValue) throw new InvalidOperationException($"{strLinea}: valorización incompleta.");
                // Misma regla de MotorDistribucionCostoProductivo: clasificación resuelta de producto, sin inferir por lote.
                string strParticion = ParticionDetalleCierre(ParticionCosteo.ClasificarRpc(objLiq.strTipoProducto), strLinea);
                string strTipo = (objLiq.strTipCod ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(strTipo)) throw new InvalidOperationException($"{strLinea}: falta el tipo de proceso.");
                var objLinea = new CierreReproLineaDto
                {
                    objCabecera = new TbMateriaPrimaReproValorizada
                    {
                        MrEmpCodigo = 1, MrPaCodigo = 1, MrFecha = dtFecha, MrLotNumero = objLiq.intLotNumero,
                        MrRloNumeroUnificado = objLiq.intLoteUnificado, MrProCodcor = strProducto, MrTalCodigo = checked((short)objLiq.intCodTal),
                        MrBodCodigo = objLiq.strBodCod ?? "0", MrMedCodigo = checked((byte)objLiq.intMedCodigo), MrEmbCodigo = objLiq.strEmbCodigo ?? "",
                        MrMasters = (decimal)objLiq.dbMasters, MrLibras = (decimal)objLiq.dbLibras,
                        MrCostoUnitario = objLiq.dcCostoTotXLibra.Value, MrCostoTotal = objLiq.dbCostoTotal, MrEstado = "AC"
                    }
                };
                var dicComponentes = ComponentesCierre(objLiq.ProcesoPrimario, objLiq.ProcesoPresentacion, objLiq.ProcesoCongelacion,
                    objLiq.ProcesoSecundario, objLiq.ProcesoCostFijo, objLiq.ProcesoCostIndirecto, objLiq.dcCostoCopacking, true, objLiq.ProcesosAdicionales);
                objLinea.lstDetalles = ConstruirDetallesCierre(dicComponentes, objLiq.dcExcedente ?? 0m, objLiq.dcTarifaProc ?? 0m,
                    objLiq.dcCostTotalProc ?? 0m, objLinea.objCabecera.MrLibras, strParticion, strTipo, strLinea, ctx.dicEtapas,
                    objResultado.lstAdvertencias, ref dcDiferenciaComponentes, ref dcDiferenciaDetalles, blRepro: true);
                objResultado.lstRepro.Add(objLinea);
            }
            if (objResultado.lstFresco.Count != ctx.intFrescoEsperados || objResultado.lstRepro.Count != ctx.intReproEsperados)
                throw new InvalidOperationException("La cantidad de cabeceras no corresponde a las líneas crudas del período.");
            if (Math.Abs(dcDiferenciaComponentes) > 0.01m || Math.Abs(dcDiferenciaDetalles) > 0.01m)
                throw new InvalidOperationException($"La valorización no concilia globalmente: componentes {dcDiferenciaComponentes:N5}, detalles {dcDiferenciaDetalles:N5}.");
            objResultado.lstAdvertencias.Add("Reproceso: las ediciones manuales no pueden distinguirse en la tabla compartida de motivos (H9); se recalculan los unitarios.");
            return objResultado;
        }

        private static string ParticionDetalleCierre(string? strParticion, string strLinea) => strParticion switch
        {
            ParticionCosteo.ENTERO => "EN", ParticionCosteo.COLA => "CO",
            ParticionCosteo.ENTERO_VA or ParticionCosteo.COLA_VA => "VA",
            _ => throw new InvalidOperationException($"{strLinea}: partición no clasificable por la regla existente.")
        };

        private static Dictionary<string, decimal> ComponentesCierre(ProcesoPrimarioDto objPrimario, ProcesoPresentacionDto objPresentacion,
            ProcesoCongelacionDto objCongelacion, ProcesoSecundarioDto objSecundario, ProcesoCostDirectoDto objDirecto,
            // ProcesoCostIndirectoDto objIndirecto, decimal? dcCopacking, bool blRepro)
            ProcesoCostIndirectoDto objIndirecto, decimal? dcCopacking, bool blRepro, Dictionary<string, decimal>? dicAdicionales = null)
        {
            // acumulado, sin emitir un log por fila.
            // using var objTiempo = MedicionTiempos.IniciarAcumulado("ComponentesCierre");
            var dicMontos = new Dictionary<string, decimal>
            {
                ["LOG"] = objPrimario.dcLogistica ?? 0m, ["REC"] = objPrimario.dcRecepcion ?? 0m,
                ["CLA"] = objPrimario.dcClasificacion ?? 0m, ["CAJ"] = objPrimario.dcCajas ?? 0m,
                ["DEC"] = objPresentacion.dcDecorado ?? 0m, ["RET"] = objPresentacion.dcRetractilado ?? 0m,
                ["BRI"] = objCongelacion.dcBrine ?? 0m, ["IQF"] = objCongelacion.dcIQF ?? 0m, ["TUN"] = objCongelacion.dcTunel ?? 0m,
                ["DES"] = objSecundario.dcDescabezado ?? 0m, ["PEL"] = objSecundario.dcPelado ?? 0m,
                ["HID"] = objSecundario.dcHidratacion ?? 0m, ["COC"] = objSecundario.dcCocido ?? 0m,
                ["CDV"] = objDirecto.dcCostoVariable ?? 0m, ["CDF"] = objDirecto.dcCostoFijo ?? 0m,
                ["CIV"] = objIndirecto.dcCostoVariable ?? 0m, ["CIF"] = objIndirecto.dcCostoFijo ?? 0m, ["COP"] = dcCopacking ?? 0m
            };
            if (blRepro) dicMontos["DSC"] = objSecundario.dcDescongelado ?? 0m;
            // el total ya incluye estos procesos; persistirlos para conciliar el cierre.
            if (dicAdicionales != null)
                foreach (var objAdicional in dicAdicionales)
                    dicMontos[objAdicional.Key] = objAdicional.Value;
            return dicMontos;
        }

        // reservar todas las claves, incluso componentes de importe cero.
        // Todas las filas de esta línea/partición se emiten desde dicMontos: no puede duplicarse la tarifa.
        private static string? ElegirCodigoTarifaCierre(string strTipo, Dictionary<string, decimal> dicMontos,
            Dictionary<string, string> dicEtapas)
        {
            strTipo = CierreDetalleDto.NormalizarCodigo(strTipo);
            bool Disponible(string strCodigo) => strCodigo.Length > 0 && !CierreDetalleDto.EsComponente(strCodigo)
                && !dicMontos.Keys.Any(x => CierreDetalleDto.NormalizarCodigo(x) == strCodigo)
                && dicEtapas.TryGetValue(strCodigo, out var strEtapa) && !string.IsNullOrWhiteSpace(strEtapa);
            return Disponible(strTipo) ? strTipo : Disponible("TAR") ? "TAR" : null;
        }

        private static string MensajeCodigosTarifaCierre(IEnumerable<string> lstTipos, Dictionary<string, string> dicEtapas)
        {
            var lstCodigos = lstTipos.Select(CierreDetalleDto.NormalizarCodigo).Distinct().OrderBy(x => x).ToList();
            var lstFaltantes = lstCodigos.Append("TAR").Distinct().Where(x => !dicEtapas.ContainsKey(x)).ToList();
            return $"No se puede guardar la tarifa. Procesos afectados: {string.Join(", ", lstCodigos)}. " +
                $"Códigos faltantes en costos.tb_procesocosto: {(lstFaltantes.Count == 0 ? "ninguno" : string.Join(", ", lstFaltantes))}. " +
                "Registre esos códigos en costos.tb_procesocosto o ejecute 05_insert_proceso_tarifa.sql; " +
                "el destino debe tener pc_ecCodigo y no colisionar con componentes de la línea.";
        }

        private static void ValidarCodigosTarifaCierre(List<MatPrimaReproceso> lstRepro, Dictionary<string, string> dicEtapas)
        {
            var lstFallos = lstRepro.Where(x => (x.dcTarifaProc ?? 0m) != 0m).Where(x =>
                ElegirCodigoTarifaCierre(x.strTipCod, ComponentesCierre(x.ProcesoPrimario, x.ProcesoPresentacion,
                    x.ProcesoCongelacion, x.ProcesoSecundario, x.ProcesoCostFijo, x.ProcesoCostIndirecto,
                    x.dcCostoCopacking, true, x.ProcesosAdicionales), dicEtapas) == null).Select(x => x.strTipCod).ToList();
            if (lstFallos.Count > 0) throw new InvalidOperationException(MensajeCodigosTarifaCierre(lstFallos, dicEtapas));
        }

        private static List<CierreDetalleDto> ConstruirDetallesCierre(Dictionary<string, decimal> dicMontos, decimal dcExcedente,
            decimal dcTarifa, decimal dcTotalProceso, decimal dcLibras, string strParticion, string strTipo, string strLinea,
            Dictionary<string, string> dicEtapas, List<string> lstAdvertencias, ref decimal dcGlobalComponentes, ref decimal dcGlobalDetalles,
            bool blRepro = false)
        {
            // acumulado, sin emitir un log por fila.
            // using var objTiempo = MedicionTiempos.IniciarAcumulado("ConstruirDetallesCierre");
            // elegir antes de emitir filas o modificar advertencias/acumuladores.
            strTipo = CierreDetalleDto.NormalizarCodigo(strTipo);
            bool blGuardarTarifa = blRepro && dcTarifa != 0m;
            string? strCodigoTarifa = blGuardarTarifa ? ElegirCodigoTarifaCierre(strTipo, dicMontos, dicEtapas) : null;
            if (blGuardarTarifa && strCodigoTarifa == null)
                throw new InvalidOperationException(MensajeCodigosTarifaCierre(new[] { strTipo }, dicEtapas));
            var lstDetalles = new List<CierreDetalleDto>();
            decimal dcResidual = dcExcedente + (blGuardarTarifa ? 0m : dcTarifa);
            if (dcExcedente != 0m) lstAdvertencias.Add($"{strLinea}: Excedente sin detalle, residual {dcExcedente:N5}.");
            if (dcTarifa != 0m && !blGuardarTarifa) lstAdvertencias.Add($"{strLinea}: Tarifa adicional sin detalle, residual {dcTarifa:N5}.");
            var lstMontos = dicMontos.Where(x => x.Value != 0m);
            if (blGuardarTarifa) lstMontos = lstMontos.Append(new KeyValuePair<string, decimal>(strCodigoTarifa!, dcTarifa));
            foreach (var objMonto in lstMontos)
            {
                if (!dicEtapas.TryGetValue(objMonto.Key, out string? strEtapa))
                {
                    dcResidual += objMonto.Value;
                    lstAdvertencias.Add($"{strLinea}: {objMonto.Key} sin fila en tb_procesocosto, residual {objMonto.Value:N5}.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(strEtapa)) throw new InvalidOperationException($"{strLinea}: {objMonto.Key} no tiene pc_ecCodigo.");
                decimal dcTotal = Math.Round(objMonto.Value, 5);
                lstDetalles.Add(new CierreDetalleDto
                {
                    strEtapaCodigo = strEtapa, strParticion = strParticion, strPcCodigo = objMonto.Key, strToCodigo = strTipo,
                    dcLibras = Math.Round(dcLibras, 5), dcCostoTotal = dcTotal, dcCostoUnitario = dcLibras == 0m ? 0m : Math.Round(dcTotal / dcLibras, 5)
                });
            }
            decimal dcDiferenciaComponentes = dicMontos.Values.Sum() + dcExcedente + dcTarifa - dcTotalProceso;
            decimal dcDiferenciaDetalles = lstDetalles.Sum(x => x.dcCostoTotal) + dcResidual - dcTotalProceso;
            if (Math.Abs(dcDiferenciaComponentes) > 0.0001m || Math.Abs(dcDiferenciaDetalles) > 0.0001m)
                throw new InvalidOperationException($"{strLinea}: no concilia el proceso; componentes {dcDiferenciaComponentes:N5}, detalles y residual {dcDiferenciaDetalles:N5}.");
            dcGlobalComponentes += dcDiferenciaComponentes; dcGlobalDetalles += dcDiferenciaDetalles;
            return lstDetalles;
        }
        #endregion

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

                // mismo esquema en todas las filas, tras valorizar fresco/RPC.
                ReporteMateriaPrimaHelper.Preparar(lstLiquidaciones, objDataProceso.objConfiguracionCostoProductivo);
                return lstLiquidaciones;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en reporte: {objException.Message}");
                throw;
            }
        }

        public async Task<List<LiquidacionResultado>> ObtenerLiquidacionValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin, CierreValorizacionContexto? ctx = null)
        {
            List<LiquidacionResultado> lstLiquidaciones = new List<LiquidacionResultado>();
            List<LiquidacionResultado> lstYaValorizados;
            DataProcesoParam objDataProceso = new();
            #region Medicion
            // using var objMedicionTotal = MedicionTiempos.IniciarMetodo(_objLogger, "fresco", () => objDataProceso.lstLiqFresco?.Count);
            #endregion
            try
            {
                var TareaFrsVal = MedicionTiempos.MedirAsync("cache-lectura", () => ctx == null ? _objMateriaPrima.ObtenerLstMatPrimValorizada(dtFechaInicio, dtFechaFin) : Task.FromResult(new List<LiquidacionResultado>()), objResultado => objResultado?.Count);
                var tareaFrs = MedicionTiempos.MedirAsync("cargador-crudo-SP", () => _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio, dtFechaFin), objResultado => objResultado?.Count);
                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                var tareaWarren = MedicionTiempos.MedirAsync("Warren-parametros", () => ctx == null ? _objProcesoParametro.ConsultarParametrosWarren(dtFechaCorte) : Task.FromResult(ctx.lstParametrosWarren), objResultado => objResultado?.Count);
                await Task.WhenAll(TareaFrsVal, tareaFrs);
                // entrada propia y orden total antes del merge 1:1 y de las colas de empaque.
                objDataProceso.lstLiqFresco = EntradaCosteo.OrdenarFresco(EntradaCosteo.Copiar(await tareaFrs));
                if (ctx != null) ctx.intFrescoEsperados = objDataProceso.lstLiqFresco?.Count ?? 0;
                // un tipo sin líneas permite anular sus cabeceras anteriores.
                if (ctx != null && objDataProceso.lstLiqFresco?.Count == 0) return lstLiquidaciones;
                if (objDataProceso.lstLiqFresco == null || !objDataProceso.lstLiqFresco.Any())
                    throw new Exception("No se encontraron liquidaciones de materia prima en el rango de fechas proporcionado.");

                lstYaValorizados = EntradaCosteo.OrdenarFresco(await TareaFrsVal);
                // await ObtenerValProceso(dtFechaInicio, objDataProceso);
                await MedicionTiempos.MedirAsync("parametros", () => ObtenerValProceso(dtFechaInicio, objDataProceso, ctx), () => (objDataProceso.lstProcesoFrs?.Count ?? 0) + (objDataProceso.lstProcesoRpc?.Count ?? 0));
                //_objMotorProceso.CalcularNuevoCostProcesoSum(objDataProceso);

                // _objMotorMergeVal.MergeFrescoValorizado(objDataProceso.lstLiqFresco, lstYaValorizados);
                // using var objTiempoCacheMerge = MedicionTiempos.IniciarTramo("cache-merge", () => objDataProceso.lstLiqFresco.Count);
                if (ctx == null) _objMotorMergeVal.MergeFrescoValorizado(objDataProceso.lstLiqFresco, lstYaValorizados);
                // objTiempoCacheMerge.Dispose();

                var tareaInfoProd = _objMateriaPrima.ObtenerInfoProd(
                    objDataProceso.lstLiqFresco.Select(p => p.intCodProd.ToString()).Distinct().ToList()
                    );
                lstLiquidaciones.AddRange(objDataProceso.lstLiqFresco);
                await Task.WhenAll(
                     MedicionTiempos.MedirAsync("empaque-Costos", () => _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(lstLiquidaciones), () => lstLiquidaciones.Count),
                     tareaInfoProd
                    );
                objDataProceso.lstInfoProd = EntradaCosteo.Ordenar(await tareaInfoProd);
                // using var objTiempoMotoresProceso = MedicionTiempos.IniciarTramo("motores-proceso", () => objDataProceso.lstLiqFresco.Count);
                _objMotorProceso.AsignarCostosProcesosFresco(objDataProceso);
                // objTiempoMotoresProceso.Dispose();

                // NUEVO: capa Warren PFR EN/SH usando WEN/WSH guardado.
                var parametrosWarren = EntradaCosteo.Ordenar(await tareaWarren);
                // using var objTiempoWarrenAplicacion = MedicionTiempos.IniciarTramo("Warren-aplicacion", () => objDataProceso.lstLiqFresco.Count);
                _objMotorWarren.AplicarGuardado(objDataProceso.lstLiqFresco, parametrosWarren, objDataProceso.objConfiguracionCostoProductivo);
                // objTiempoWarrenAplicacion.Dispose();
                // vista de reportes compartida por JSON y Excel.
                ReporteMateriaPrimaHelper.Preparar(lstLiquidaciones, objDataProceso.objConfiguracionCostoProductivo);
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

                var lstErrores = new ConcurrentBag<Exception>(); // propagar los errores al finalizar todos los lotes.
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
                        lstErrores.Add(ex); // conservar el error para el AggregateException.
                    }
                });
                if (!lstErrores.IsEmpty) throw new AggregateException("Falló el registro de uno o más lotes.", lstErrores);
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

        #region Lectura de cierre guardado
        // selección atómica del rango. Nunca entregar el candidato parcialmente reconstruido.
        internal static async Task<List<MatPrimaReproceso>> SeleccionarOrigenRepro(enmOrigenDatos objOrigen, bool blCierre,
            Func<Task<(List<MatPrimaReproceso>? lstFilas, string? strMotivo)>> objLeer,
            Func<Task<List<MatPrimaReproceso>>> objTiempoReal, Action<string> objRegistrar)
        {
            if (objOrigen == enmOrigenDatos.Auto && !blCierre)
            {
                try
                {
                    var objResultado = await objLeer();
                    if (objResultado.strMotivo == null && objResultado.lstFilas != null)
                        return objResultado.lstFilas;
                    objRegistrar(objResultado.strMotivo ?? "CIERRE_SIN_RESULTADO");
                }
                catch (Exception objError)
                {
                    objRegistrar($"ERROR_LECTURA: {objError.GetType().Name}: {objError.Message}");
                }
            }
            return await objTiempoReal();
        }

        private async Task<(List<MatPrimaReproceso>? lstFilas, string? strMotivo)> IntentarLeerCierreRepro(
            DateOnly dtInicio, DateOnly dtFin, Action<List<MatPrimaReproceso>> objConservarCrudas)
        {
            // using var objMedicion = MedicionTiempos.IniciarMetodo(_objLogger, "repro-cierre", () => null);
            string? strMotivo = ValidarRangoCierreRepro(dtInicio, dtFin);
            if (strMotivo != null) return (null, strMotivo);
            var objCierre = await MedicionTiempos.MedirAsync("cierre-cabeceras-detalles-cobertura",
                () => _objMateriaPrima.ObtenerCierreReproGuardado(dtInicio, dtFin), x => x.lstLineas.Count);
            strMotivo = ValidarCoberturaCierreRepro(dtInicio, dtFin, objCierre.lstParametros,
                objCierre.lstDistribuciones, objCierre.lstLineas, null);
            if (strMotivo != null) return (null, strMotivo);
            // este cargador ya incluye ObtenerInfoPlanProc; no usar merge ni fresco anidado.
            var lstCrudas = await MedicionTiempos.MedirAsync("cierre-cargador-crudo",
                () => _objMateriaPrima.ReporteReproPlanRecibProc(dtInicio, dtFin), x => x.Count);
            objConservarCrudas(lstCrudas);
            using (MedicionTiempos.IniciarTramo("cierre-validacion-cobertura"))
                strMotivo = ValidarCoberturaCierreRepro(dtInicio, dtFin, objCierre.lstParametros,
                    objCierre.lstDistribuciones, objCierre.lstLineas, lstCrudas);
            if (strMotivo != null) return (null, strMotivo);
            // using var objConstruccion = MedicionTiempos.IniciarTramo("cierre-emparejamiento-reconstruccion");
            var objParejas = EmparejarReproDesdeCierre(lstCrudas, objCierre.lstLineas);
            if (objParejas.strMotivo != null) return (null, objParejas.strMotivo);
            // copia local; ningún dato del intento puede contaminar el fallback.
            var lstCandidatas = EntradaCosteo.Copiar(lstCrudas);
            strMotivo = ConstruirReproDesdeCierre(lstCandidatas, objParejas.lstEmparejadas);
            // La persistencia vigente no permite certificar tarifa/secuencial: el constructor lo informa.
            // No costear empaque ni reconstruir RECIBIDO para un candidato ya rechazado.
            return (strMotivo == null ? lstCandidatas : null, strMotivo);
        }

        internal static string? ValidarRangoCierreRepro(DateOnly dtInicio, DateOnly dtFin)
            => dtInicio > dtFin || dtInicio.Day != 1 || dtFin.Day != DateTime.DaysInMonth(dtFin.Year, dtFin.Month)
                ? "RANGO_NO_MESES_COMPLETOS" : null;

        internal static string? ValidarCoberturaCierreRepro(DateOnly dtInicio, DateOnly dtFin,
            List<TbParametroCosteo> lstParametros, List<DateOnly> lstDistribuciones,
            List<CierreReproLineaDto> lstLineas, List<MatPrimaReproceso>? lstCrudas)
        {
            string? strMotivo = ValidarRangoCierreRepro(dtInicio, dtFin);
            if (strMotivo != null) return strMotivo;
            var dicParametros = lstParametros.Where(x => x.PcEstado != "AN").ToLookup(x => (x.PcFecha.Year, x.PcFecha.Month));
            var hshDistribuciones = lstDistribuciones.Select(x => (x.Year, x.Month)).ToHashSet();
            var dicCabeceras = lstLineas.Where(x => x.objCabecera.MrEstado == "AC")
                .ToLookup(x => (x.objCabecera.MrFecha.Year, x.objCabecera.MrFecha.Month));
            var dicCrudas = (lstCrudas ?? new()).Where(x => x.strAgrupacion == "2. PROCESADO")
                .ToLookup(x => (x.dtLotFecha.Year, x.dtLotFecha.Month));
            for (var dtMes = dtInicio; ; dtMes = dtMes.AddMonths(1))
            {
                var objMes = (dtMes.Year, dtMes.Month);
                var lstParametrosMes = dicParametros[objMes].ToList();
                if (lstParametrosMes.Count == 0 || lstParametrosMes.Any(x => x.PcEstado != "CE"))
                    return $"PARAMETROS_NO_CE mes={dtMes:yyyy-MM}";
                if (!hshDistribuciones.Contains(objMes)) return $"DISTRIBUCION_NO_AC mes={dtMes:yyyy-MM}";
                var lstCabecerasMes = dicCabeceras[objMes].ToList();
                if (lstCabecerasMes.Count == 0) return $"SIN_CABECERAS_AC mes={dtMes:yyyy-MM}";
                if (lstCrudas != null && lstCabecerasMes.Count != dicCrudas[objMes].Count()) return $"CONTEO_PROCESADO mes={dtMes:yyyy-MM}";
                foreach (var objLinea in lstCabecerasMes)
                {
                    // la cabecera NO guarda Total Proceso. Ausencia de detalles no demuestra proceso=0.
                    if (objLinea.lstDetalles.Count == 0)
                        return $"SIN_DETALLES_AC_PROCESO_CERO_NO_DEMOSTRABLE mes={dtMes:yyyy-MM} mr_id={objLinea.objCabecera.MrId}";
                }
                if (dtMes.Year == dtFin.Year && dtMes.Month == dtFin.Month) break;
            }
            if (lstCrudas != null && lstCrudas.Any(x => DateOnly.FromDateTime(x.dtLotFecha) < dtInicio || DateOnly.FromDateTime(x.dtLotFecha) > dtFin))
                return "FILAS_FUERA_RANGO";
            return null;
        }

        // clave decimal a escala persistida; MrId resuelve gemelas sin reordenar el cargador.
        internal static (List<CierreReproLineaDto> lstEmparejadas, string? strMotivo) EmparejarReproDesdeCierre(
            List<MatPrimaReproceso> lstCrudas, List<CierreReproLineaDto> lstGuardadas)
        {
            var dicColas = lstGuardadas.Where(x => x.objCabecera.MrEstado == "AC").OrderBy(x => x.objCabecera.MrId)
                .GroupBy(x => (x.objCabecera.MrLotNumero, x.objCabecera.MrRloNumeroUnificado,
                    x.objCabecera.MrProCodcor, (int)x.objCabecera.MrTalCodigo, x.objCabecera.MrLibras,
                    x.objCabecera.MrMasters, x.objCabecera.MrBodCodigo, (int)x.objCabecera.MrMedCodigo, x.objCabecera.MrEmbCodigo))
                .ToDictionary(x => x.Key, x => new Queue<CierreReproLineaDto>(x));
            var lstParejas = new List<CierreReproLineaDto>();
            foreach (var objFila in lstCrudas.Where(x => x.strAgrupacion == "2. PROCESADO"))
            {
                var objClave = (objFila.intLotNumero, objFila.intLoteUnificado, objFila.intCodProd.ToString(), objFila.intCodTal,
                    Math.Round((decimal)objFila.dbLibras, 5), Math.Round((decimal)objFila.dbMasters, 5),
                    objFila.strBodCod ?? "0", objFila.intMedCodigo, objFila.strEmbCodigo ?? "");
                if (!dicColas.TryGetValue(objClave, out var objCola) || objCola.Count == 0)
                    return (new(), $"CLAVE_SIN_CABECERA lote={objFila.intLotNumero}/{objFila.intLoteUnificado} producto={objFila.intCodProd} talla={objFila.intCodTal}");
                var objLinea = objCola.Dequeue();
                if (objLinea.objCabecera.MrFecha != DateOnly.FromDateTime(objFila.dtLotFecha))
                    return (new(), $"FECHA_CABECERA_DISTINTA mr_id={objLinea.objCabecera.MrId}");
                lstParejas.Add(objLinea);
            }
            return dicColas.Values.Any(x => x.Count != 0)
                ? (new(), "CABECERAS_SIN_FILA_CRUDA") : (lstParejas, null);
        }

        // reconstruir solo valores demostrables. El candidato NO es una salida publicable.
        internal static string? ConstruirReproDesdeCierre(List<MatPrimaReproceso> lstFilas, List<CierreReproLineaDto> lstParejas)
        {
            var lstProcesados = lstFilas.Where(x => x.strAgrupacion == "2. PROCESADO").ToList();
            if (lstProcesados.Count == 0 || lstProcesados.Count != lstParejas.Count) return "CONTEO_PROCESADO";
            for (int intIndice = 0; intIndice < lstProcesados.Count; intIndice++)
            {
                var objFila = lstProcesados[intIndice];
                var objLinea = lstParejas[intIndice];
                var dicMontos = objLinea.lstDetalles.GroupBy(x => CierreDetalleDto.NormalizarCodigo(x.strPcCodigo))
                    .ToDictionary(x => x.Key, x => x.Sum(d => d.dcCostoTotal));
                // comprobar el crudo antes de reemplazar su tarifa por el detalle.
                if ((objFila.dcTarifaProc ?? 0m) != 0m && !dicMontos.Keys.Any(x => CierreDetalleDto.EsTarifa(x, objFila.strTipCod)))
                    return $"CIERRE_ANTIGUO_SIN_TAR mes={objLinea.objCabecera.MrFecha:yyyy-MM} mr_id={objLinea.objCabecera.MrId}";
                var dicDestinos = new Dictionary<string, Action<decimal>>
                {
                    ["LOG"] = dc => objFila.ProcesoPrimario.dcLogistica = dc,
                    ["REC"] = dc => objFila.dcRecepcion = objFila.ProcesoPrimario.dcRecepcion = dc,
                    ["CLA"] = dc => objFila.dcClasificacion = objFila.ProcesoPrimario.dcClasificacion = dc,
                    ["CAJ"] = dc => objFila.dcCajas = objFila.ProcesoPrimario.dcCajas = dc,
                    ["DEC"] = dc => objFila.dcDecorado = objFila.ProcesoPresentacion.dcDecorado = dc,
                    ["RET"] = dc => objFila.dcRetractilado = objFila.ProcesoPresentacion.dcRetractilado = dc,
                    ["BRI"] = dc => objFila.dcBrine = objFila.ProcesoCongelacion.dcBrine = dc,
                    ["IQF"] = dc => objFila.dcIQF = objFila.ProcesoCongelacion.dcIQF = dc,
                    ["TUN"] = dc => objFila.dcTunel = objFila.ProcesoCongelacion.dcTunel = dc,
                    ["DES"] = dc => objFila.dcDescabezado = objFila.ProcesoSecundario.dcDescabezado = dc,
                    ["DSC"] = dc => objFila.dcDescongelado = objFila.ProcesoSecundario.dcDescongelado = dc,
                    ["PEL"] = dc => objFila.dcPelado = objFila.ProcesoSecundario.dcPelado = dc,
                    ["HID"] = dc => objFila.dcHidratacion = objFila.ProcesoSecundario.dcHidratacion = dc,
                    ["COC"] = dc => objFila.dcCocido = objFila.ProcesoSecundario.dcCocido = dc,
                    ["CDV"] = dc => objFila.dcCostFijVaria = objFila.ProcesoCostFijo.dcCostoVariable = dc,
                    ["CDF"] = dc => objFila.dcCostFijFijo = objFila.ProcesoCostFijo.dcCostoFijo = dc,
                    ["CIV"] = dc => objFila.dcCostIndirVaria = objFila.ProcesoCostIndirecto.dcCostoVariable = dc,
                    ["CIF"] = dc => objFila.dcCostIndirFijo = objFila.ProcesoCostIndirecto.dcCostoFijo = dc,
                    ["COP"] = dc => objFila.dcCostoCopacking = dc,
                    ["TAR"] = dc => objFila.dcTarifaProc = dc,
                    ["PMIO"] = dc => { }   // control del premio; no es componente de costo de la fila
                };
                if (dicMontos.Keys.Any(x => !dicDestinos.ContainsKey(x) && !CierreDetalleDto.EsTarifa(x, objFila.strTipCod))) return $"COMPONENTE_DESCONOCIDO mr_id={objLinea.objCabecera.MrId}";
                if (objLinea.lstDetalles.Any(x => CierreDetalleDto.NormalizarCodigo(x.strToCodigo) != CierreDetalleDto.NormalizarCodigo(objFila.strTipCod))) return $"TIPO_PROCESO_DISTINTO mr_id={objLinea.objCabecera.MrId}";
                foreach (var objDestino in dicDestinos) objDestino.Value(dicMontos.GetValueOrDefault(objDestino.Key));
                // sumar TAR y el proceso propio solo si no es componente conocido.
                objFila.dcTarifaProc = dicMontos.Where(x => CierreDetalleDto.EsTarifa(x.Key, objFila.strTipCod)).Sum(x => x.Value);
                objFila.dbCostoTotal = objLinea.objCabecera.MrCostoTotal;
                objFila.dcCostoTotXLibra = objLinea.objCabecera.MrCostoUnitario;
                objFila.dcExcedente = 0m;
            }
            // AsignarPrecio redondea precio y total independientemente; el cociente no lo invierte.
            // El detalle de proceso propio o TAR ya conserva tarifa. Los componentes a cinco decimales no certifican el total original exacto.
            // El detalle conserva libras totales de cabecera, no las físicas de RET/PEL/DEC.
            var objPrimera = lstParejas[0].objCabecera;
            return $"CAMPOS_NO_DERIVABLES mes={objPrimera.MrFecha:yyyy-MM} mr_id={objPrimera.MrId} " +
                "dbCostoXSecuencial,dcCostTotalProc,dcTotalDolSum,dcValidador; " +
                "precio y total redondeados independientemente; precisión original de componentes no persistida; " +
                "libras físicas/trazabilidad del costeo no certificadas";
        }

        // núcleo puro §5.3. Solo admite fuentes previamente certificadas; no propaga ni recostea.
        // La compuerta anterior impide invocarlo en producción hasta poder certificar los campos faltantes.
        internal static string? ReconstruirCostoRecibido(List<MatPrimaReproceso> lstFilas,
            List<TbMateriaPrimaFrescoValorizada> lstFresco)
        {
            // using var objTiempo = MedicionTiempos.IniciarTramo("cierre-reconstruccion-recibido");
            var lstActivos = lstFresco.Where(x => x.MfEstado == "AC").ToList();
            var hshFresco = lstActivos.Select(x => x.MfRloNumero).ToHashSet();
            var dicFresco = lstActivos.ToLookup(x => (x.MfRloNumero, x.MfProCodcor, (int)x.MfTalCodigo));
            var dicRepro = lstFilas.Where(x => x.strAgrupacion == "2. PROCESADO")
                .ToLookup(x => (x.intLoteUnificado, x.intCodProd, x.intCodTal));
            var hshRepro = lstFilas.Where(x => x.strAgrupacion == "2. PROCESADO").Select(x => x.intLoteUnificado).ToHashSet();
            // Preparar primero: un lote que no concilia no debe dejar costos parcialmente asignados.
            var lstAsignaciones = new List<(MatPrimaReproceso objFila, decimal dcPrecio, decimal dcTotal)>();
            foreach (var objLote in lstFilas.GroupBy(x => (x.intLotNumero, x.intLoteUnificado)))
            {
                var lstProcesados = objLote.Where(x => x.strAgrupacion == "2. PROCESADO").ToList();
                if (lstProcesados.Count == 0) return $"RECIBIDO_SIN_PROCESADO lote={objLote.Key}";
                decimal dcTotal = lstProcesados.Sum(x => x.dbCostoTotal), dcResuelto = 0m;
                var lstResiduales = new List<MatPrimaReproceso>();
                foreach (var objFila in objLote.Where(x => x.strAgrupacion == "1. RECIBIDO"))
                {
                    if (objFila.dbLibras < 0) return $"LIBRAS_RECIBIDO_NEGATIVAS lote={objLote.Key}";
                    decimal dcPrecio;
                    if (hshFresco.Contains(objFila.intLoteOrigen))
                    {
                        var lstFuente = dicFresco[(objFila.intLoteOrigen, objFila.intCodProd.ToString(), objFila.intCodTal)].ToList();
                        // MfCostoTotal es MP, NO el total completo requerido por GenerarDiccionarioCostoXTallaParaReproceso.
                        if (lstFuente.Count == 0 || lstFuente.Any(x => !x.MfCostoTotalWarren.HasValue) || lstFuente.Sum(x => x.MfLibras) <= 0m)
                            return $"FRESCO_SIN_TOTAL_COMPLETO lote={objLote.Key} origen={objFila.intLoteOrigen}";
                        dcPrecio = Math.Round(lstFuente.Sum(x => x.MfCostoTotalWarren!.Value) / lstFuente.Sum(x => x.MfLibras), 4, MidpointRounding.AwayFromZero);
                    }
                    else if (hshRepro.Contains(objFila.intLoteOrigen))
                    {
                        var lstFuente = dicRepro[(objFila.intLoteOrigen, objFila.intCodProd, objFila.intCodTal)].ToList();
                        if (lstFuente.Count == 0 || lstFuente.Select(x => x.dbCostoXSecuencial).Distinct().Count() != 1)
                            return $"REPRO_ORIGEN_AMBIGUO lote={objLote.Key} origen={objFila.intLoteOrigen}";
                        dcPrecio = lstFuente[0].dbCostoXSecuencial;
                    }
                    else { lstResiduales.Add(objFila); continue; }
                    decimal dcMonto = Math.Round(dcPrecio * (decimal)objFila.dbLibras, 2);
                    dcResuelto += dcMonto;
                    lstAsignaciones.Add((objFila, dcPrecio, dcMonto));
                }
                decimal dcResidual = dcTotal - dcResuelto;
                decimal dcLibras = lstResiduales.Sum(x => (decimal)x.dbLibras);
                if (dcResidual < -0.01m || (dcLibras == 0m && Math.Abs(dcResidual) > 0.01m))
                    return $"RECIBIDO_NO_CONCILIA lote={objLote.Key} diferencia={dcResidual}";
                decimal dcRepartido = 0m;
                foreach (var objFila in lstResiduales)
                {
                    decimal dcPrecio = dcLibras == 0m ? 0m : dcResidual / dcLibras;
                    decimal dcMonto = Math.Round(dcPrecio * (decimal)objFila.dbLibras, 2);
                    dcRepartido += dcMonto;
                    lstAsignaciones.Add((objFila, Math.Round(dcPrecio, 4), dcMonto));
                }
                if (Math.Abs(dcTotal - dcResuelto - dcRepartido) > 0.01m)
                    return $"RECIBIDO_NO_CONCILIA lote={objLote.Key} diferencia={dcTotal - dcResuelto - dcRepartido}";
            }
            foreach (var objAsignacion in lstAsignaciones)
            {
                objAsignacion.objFila.dbCostoXSecuencial = objAsignacion.dcPrecio;
                objAsignacion.objFila.dbCostoTotal = objAsignacion.dcTotal;
            }
            return null;
        }
        #endregion

        #region Flujo Mat Prima Reproceso

        // parámetro al final para conservar todos los llamadores existentes.
        public async Task<List<MatPrimaReproceso>> ObtenerReporteMateriaPrimaReproValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin,
            enmTipoConsulta objEnmTip = enmTipoConsulta.ConFront, List<LiquidacionResultado>? liqFrs = null,
            CierreValorizacionContexto? ctx = null, enmOrigenDatos objOrigenDatos = enmOrigenDatos.Auto)
        {
            List<MatPrimaReproceso>? lstCrudas = null;
            return await SeleccionarOrigenRepro(objOrigenDatos, ctx != null,
                () => IntentarLeerCierreRepro(dtFechaInicio, dtFechaFin, lstFilas => lstCrudas = lstFilas),
                () => ObtenerReporteMateriaPrimaReproTiempoReal(dtFechaInicio, dtFechaFin, objEnmTip, liqFrs, ctx, lstCrudas),
                strMotivo => _objLogger.LogInformation(
                    "[LECTURA-CIERRE] Rango={Inicio}/{Fin} Motivo={Motivo}; origen=tiempo-real",
                    dtFechaInicio, dtFechaFin, strMotivo));
        }

        private async Task<List<MatPrimaReproceso>> ObtenerReporteMateriaPrimaReproTiempoReal(DateOnly dtFechaInicio, DateOnly dtFechaFin,
            // contexto del cierre; el flujo GET conserva su comportamiento.
            enmTipoConsulta objEnmTip = enmTipoConsulta.ConFront, List<LiquidacionResultado>? liqFrs = null, CierreValorizacionContexto? ctx = null,
            List<MatPrimaReproceso>? lstCrudas = null, Task<List<LiquidacionResultado>>? tareaFrescoCierre = null)
        {
            //List<MatPrimaReproceso> lstReproVal;
            List<string> lstItemCod;
            List<PrecioFrsXMov> lstPrecioLiqOtrProc, lstPrecioFrsXMovCam;
            DataProcesoParam objDataProceso = new();
            #region Medicion
            // using var objMedicionTotal = MedicionTiempos.IniciarMetodo(_objLogger, "repro", () => objDataProceso.lstLiqRepro?.Count);
            #endregion
            // observar todas las cargas iniciadas, incluso si falla otra carga.
            var lstCargas = new List<Task>();
            try
            {
                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);
                // contexto del cierre; el flujo GET conserva su comportamiento.
                // objDataProceso.lstLiqRepro = await ObtenerMatPrimRepro(dtFechaInicio, dtFechaFin);
                // cada repositorio crea su contexto con IDbContextFactory/ManejoContext.
                // ConBack y cierre conservan sus salidas tempranas sin cargas especulativas.
                bool blCargaAnticipada = ctx == null && objEnmTip != enmTipoConsulta.ConBack;
                var tareaRepro = MedicionTiempos.MedirAsync("cargador-cache-merge", () => ObtenerMatPrimRepro(dtFechaInicio, dtFechaFin, ctx != null, lstCrudas), objResultado => objResultado?.Count);
                lstCargas.Add(tareaRepro);
                var tareaFresco = blCargaAnticipada ? CargarFresco() : null;
                if (tareaFresco != null) lstCargas.Add(tareaFresco);
                var tareaInven = blCargaAnticipada ? CargarInventario() : null;
                if (tareaInven != null) lstCargas.Add(tareaInven);
                var tareaParametros = blCargaAnticipada ? CargarParametros() : null;
                if (tareaParametros != null) lstCargas.Add(tareaParametros);
                var TareaTarifaProceso = blCargaAnticipada ? CargarTarifa() : null;
                if (TareaTarifaProceso != null) lstCargas.Add(TareaTarifaProceso);
                objDataProceso.lstLiqRepro = await tareaRepro;
                // no escribir ctx mientras fresco lo utiliza.
                if (ctx != null && objDataProceso.lstLiqRepro.Count == 0)
                {
                    if (tareaFrescoCierre != null) await tareaFrescoCierre;
                    ctx.intReproEsperados = 0;
                    return objDataProceso.lstLiqRepro;
                }
                var lstLiqLote = objDataProceso.lstLiqRepro
                        .Select(x => (long)x.intLoteOrigen)
                        .Distinct()
                        .ToList();
                if (ctx == null && objEnmTip == enmTipoConsulta.ConBack && objDataProceso.lstLiqRepro.Any(x => x.dcCostoTotXLibra != null)
                    && objDataProceso.lstLiqRepro.Where(x => x.strAgrupacion == "2. PROCESADO").All(x => x.dcTotalDolSum != 0m))
                {
                    return objDataProceso.lstLiqRepro;
                }
                lstItemCod = MatPrimaReproceso.ObtenerLstItemHidra(objDataProceso.lstLiqRepro);

                tareaFresco ??= CargarFresco();
                lstCargas.Add(tareaFresco);
                var tareaRetrac = MedicionTiempos.MedirAsync("retractilado", () => _objMateriaPrima.ObtenerInfoRectractiladoXLote(dtFechaInicio, dtFechaFin), x => x.Count);
                var tareaPrecioFrsXMovCam = MedicionTiempos.MedirAsync("precios-MovCam", () => _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(lstLiqLote), objResultado => objResultado?.Count);
                var tareaOtroProc = MedicionTiempos.MedirAsync("precios-otros-procesos", () => _objMateriaPrima.ObtenerConsumoMovLiqOtroProc(lstLiqLote), objResultado => objResultado?.Count);
                tareaInven ??= CargarInventario();
                var lstCodProd = objDataProceso.lstLiqRepro.Select(x => x.intCodProd.ToString()).Distinct().ToList();
                // contexto del cierre; el flujo GET conserva su comportamiento.
                // await ObtenerValProceso(dtFechaInicio, objDataProceso);
                // ctx comparte listas mutables de parametros con fresco.
                // Esperar fresco solo dentro de esta tarea permite solapar las demas lecturas.
                tareaParametros ??= CargarParametrosTrasFresco();
                TareaTarifaProceso ??= CargarTarifa();
                var tareaInfoProd = MedicionTiempos.MedirAsync("productos-repro", () => _objMateriaPrima.ObtenerInfoProd(lstCodProd), x => x.Count);
                // saldo por producto solo depende del catalogo de productos crudos.
                var tareaPreciosProm = MedicionTiempos.MedirAsync("saldo-productos", () => _objMateriaPrima.ObtenerMatPrimSaldo(lstCodProd), x => x.Count);
                // empaque escribe en una copia exclusiva, nunca en la entrada compartida.
                var tareaEmpaque = CargarEmpaque(objDataProceso.lstLiqRepro);
                lstCargas.AddRange(new Task[] { tareaEmpaque, tareaPreciosProm, tareaParametros, TareaTarifaProceso, tareaOtroProc, tareaPrecioFrsXMovCam, tareaRetrac, tareaInfoProd, tareaInven });
                await Task.WhenAll(
                    tareaPreciosProm, tareaParametros, TareaTarifaProceso, tareaOtroProc, tareaPrecioFrsXMovCam, tareaFresco, tareaRetrac,
                    tareaEmpaque,
                    tareaInfoProd, tareaInven
                    );
                // funciones locales sin estado de DbContext compartido.
                // copiar el fresco definitivo solo despues de completar su valorizacion.
                Task<List<LiquidacionResultado>> CargarFresco() => MedicionTiempos.MedirAsync(
                    "ObtenerLiquidacionValorizada-o-fresco-proporcionado", async () =>
                    {
                        if (tareaFrescoCierre != null)
                            return EntradaCosteo.OrdenarFresco(EntradaCosteo.Copiar(await tareaFrescoCierre));
                        return liqFrs != null
                            ? EntradaCosteo.OrdenarFresco(EntradaCosteo.Copiar(liqFrs))
                            : await ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin, ctx);
                    }, x => x?.Count);
                Task<List<InventarioVal>> CargarInventario() => MedicionTiempos.MedirAsync("inventario", () => _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin), x => x?.Count);
                async Task<DataProcesoParam> CargarParametros()
                {
                    // la tarea solo escribe en su propio resultado.
                    var parametros = new DataProcesoParam();
                    await MedicionTiempos.MedirAsync("parametros", () => ObtenerValProceso(dtFechaInicio, parametros, ctx), () => (parametros.lstProcesoFrs?.Count ?? 0) + (parametros.lstProcesoRpc?.Count ?? 0));
                    return parametros;
                }
                async Task<List<MatPrimaReproceso>> CargarEmpaque(List<MatPrimaReproceso> entrada)
                {
                    var copia = EntradaCosteo.Copiar(entrada);
                    await MedicionTiempos.MedirAsync("empaque-Costos", () => _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(copia), () => copia.Count);
                    return copia;
                }
                Task<List<ProcesoResultadoDto>> CargarTarifa() => MedicionTiempos.MedirAsync("ConsultarProcesoTarifa", () => _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorte), x => x?.Count);
                async Task<DataProcesoParam> CargarParametrosTrasFresco()
                {
                    if (ctx != null) await tareaFresco;
                    return await CargarParametros();
                }
                // fresco ya termino; contexto fuera del solape.
                if (ctx != null) ctx.intReproEsperados = objDataProceso.lstLiqRepro.Count(x => x.strAgrupacion == "2. PROCESADO");
                // reunir resultados secuencialmente antes de cualquier motor RPC.
                var filasRepro = objDataProceso.lstLiqRepro;
                objDataProceso = await tareaParametros;
                objDataProceso.lstLiqRepro = filasRepro;
                var filasEmpaque = await tareaEmpaque;
                for (int i = 0; i < filasRepro.Count; i++)
                {
                    filasRepro[i].dcCostoMatEmpaque = filasEmpaque[i].dcCostoMatEmpaque;
                    filasRepro[i].dcCostoTotalMatEmp = filasEmpaque[i].dcCostoTotalMatEmp;
                }
                objDataProceso.lstInvenVal = EntradaCosteo.Ordenar(await tareaInven);
                objDataProceso.lstInfoRetrac = EntradaCosteo.Ordenar(await tareaRetrac);
                objDataProceso.lstLiqFresco = await tareaFresco;
                bool blWarrenAplicado = objDataProceso.lstLiqFresco.Any(x => x.dcTotalCostoWarren.HasValue &&
                    Math.Abs(x.dcTotalCostoWarren.Value - x.dcTotalDolSum) > 0.0001m);
                if (blWarrenAplicado)
                {
                    // El costo guardado de RPC corresponde a una base anterior.
                    // Se anula SOLO EN MEMORIA para regenerar el costo final de PROCESADO.
                    foreach (var objItem in objDataProceso.lstLiqRepro.Where(x =>
                        string.Equals(x.strAgrupacion, "2. PROCESADO", StringComparison.OrdinalIgnoreCase)))
                        objItem.dcCostoTotXLibra = null;
                    _objLogger.LogInformation("[Warren->RPC] Warren detectado. " +
                        "Se invalida en memoria el costo cacheado de PROCESADO " +
                        "para recalcular Reproceso con el costo PFR Warren.");
                }
                objDataProceso.lstProcesoTarifa = EntradaCosteo.Ordenar(await TareaTarifaProceso);
                lstPrecioLiqOtrProc = EntradaCosteo.Ordenar(await tareaOtroProc);
                lstPrecioFrsXMovCam = EntradaCosteo.Ordenar(await tareaPrecioFrsXMovCam);
                objDataProceso.lstInfoProd = EntradaCosteo.Ordenar(await tareaInfoProd);
                // using var objTiempoAsignacionRetractilado = MedicionTiempos.IniciarTramo("asignacion-retractilado", () => objDataProceso.lstLiqRepro.Count);
                _objMotorAsigPrec.AsignarRetractiladoRepro(objDataProceso);
                // objTiempoAsignacionRetractilado.Dispose();
                // using var objTiempoMotoresProceso = MedicionTiempos.IniciarTramo("motores-proceso", () => objDataProceso.lstLiqRepro.Count);
                _objMotorProceso.AsignarCostosProcesosRepro(objDataProceso);
                // objTiempoMotoresProceso.Dispose();
                // using var objTiempoAsignacionPreciosSaldos = MedicionTiempos.IniciarTramo("asignacion-precios-saldos", () => objDataProceso.lstLiqRepro.Count);
                _objMotorAsigPrec.AsignarCostRecibiXFrsMovCam(objDataProceso);
                var lstLiqLoteInv = objDataProceso.lstLiqRepro.Where(lbsRecProc =>
                        lbsRecProc.strAgrupacion == "1. RECIBIDO" && lbsRecProc.dbCostoXSecuencial == 0)
                    .Select(x => x.intLoteOrigen)
                    .Distinct()
                    .ToList();

                var lstPreciosProm = await tareaPreciosProm;
                var lstPrecios = await _objMateriaPrima.ObtenerMatPrimSaldo(lstLiqLoteInv);
                _objMotorAsigPrec.EjecutarAsignacionPorSaldo(objDataProceso.lstLiqRepro, lstPreciosProm, lstPrecios, objDataProceso.dicNivelesCosteoRuntime);
                var lstFrsUni = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "UNI").ToList();
                var lstFrsDir = lstPrecioFrsXMovCam.Where(p => p.strTrcTipo == "DIR").ToList();
                // objTiempoAsignacionPreciosSaldos.Dispose();
                // using var objTiempoGrafoCosteoTotal = MedicionTiempos.IniciarTramo("grafo-costeo-total", () => objDataProceso.lstLiqRepro.Count);
                _objMotorGrafo.CostearTodosLotesEnOrden(objDataProceso, lstFrsUni, lstFrsDir);
                // objTiempoGrafoCosteoTotal.Dispose();
                _objMotorAsigPrec.RendimientoReproPlanRecibProc(objDataProceso.lstLiqRepro, objDataProceso.dicNivelesCosteoRuntime);
                // _objMotorAsigPrec.RegistrarResumenPrecision();
                // incluye ceros en RECIBIDO y procesos sin aplicación/CU.
                ReporteMateriaPrimaHelper.Preparar(objDataProceso.lstLiqRepro, objDataProceso.objConfiguracionCostoProductivo);
                return objDataProceso.lstLiqRepro/*.Where(l => l.strAgrupacion == "2. PROCESADO").ToList()*/;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[CalculoCostosFeature].[ObtenerReporteMateriaPrimaReproValorizada] Ocurrio un error: {objException.Message}");
                throw;
            }
            finally
            {
                // no dejar contextos activos al salir por error; preservar la excepcion original.
                try { await Task.WhenAll(lstCargas); }
                catch { /* Las cargas se observan en el flujo principal; no reemplazar su excepcion. */ }
            }
        }

        public async Task<List<MatPrimaReproceso>> ObtenerMatPrimRepro(DateOnly dtFechaInicio, DateOnly dtFechaFin, bool blIgnorarCache = false,
            List<MatPrimaReproceso>? lstCrudas = null)
        {
            List<MatPrimaReproceso> lstMatPrimaRepro, lstReproVal;
            try
            {
                // 1. Datos crudos (estructura, libras, agrupación)
                // reutilizar la lectura cruda del intento histórico; la copia/orden existentes siguen abajo.
                var tareaReproceso = MedicionTiempos.MedirAsync("cargador-crudo-ReporteReproPlanRecibProc", () =>
                    lstCrudas != null ? Task.FromResult(lstCrudas) : _objMateriaPrima.ReporteReproPlanRecibProc(dtFechaInicio, dtFechaFin), objResultado => objResultado?.Count);

                // 2. Cache desde tb_materiaPrimaReproValorizada
                // contexto del cierre; el flujo GET conserva su comportamiento.
                // var tareaReproVal = _objMateriaPrima.ObtenerReproValorizada(dtFechaInicio, dtFechaFin);
                var tareaReproVal = MedicionTiempos.MedirAsync("cache-lectura", () => blIgnorarCache ? Task.FromResult(new List<MatPrimaReproceso>()) : _objMateriaPrima.ObtenerReproValorizada(dtFechaInicio, dtFechaFin), objResultado => objResultado?.Count);

                await Task.WhenAll(tareaReproceso, tareaReproVal);

                // ordenar antes de FirstOrDefault, colas y motores; copiar las filas mutables.
                lstMatPrimaRepro = EntradaCosteo.OrdenarRepro(EntradaCosteo.Copiar(await tareaReproceso));
                lstReproVal = EntradaCosteo.OrdenarRepro(await tareaReproVal);

                // La validación de procesos se aplica a los GET; el cierre permite resultados vacíos.
                if (!blIgnorarCache && !lstMatPrimaRepro.Any())
                    throw new Exception(
                        "No se encontraron Procesos de materia prima Reproceso " +
                        "en el rango de fechas proporcionado.");

                // 3. Merge: asignar dcCostoTotXLibra desde cache a ítems PROCESADO
                // using var objTiempoCacheMerge = MedicionTiempos.IniciarTramo("cache-merge", () => lstMatPrimaRepro.Count);
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
                // DiagnosticoPrecision.Reproceso(_objLogger, "MergeIntermediarioRPC", lstMatPrimaRepro);
                // objTiempoCacheMerge.Dispose();
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
                                // .GroupBy(p => (p.intLotNumero, p.intLoteUnificado, p.intCodProd))
                                .GroupBy(p => (p.intLotNumero, p.intLoteUnificado, p.intCodProd, p.intCodTal))
                                .ToDictionary(g => g.Key, g => g.ToList());

                var dicCtrlProcesados = new ConcurrentDictionary<(int, int, int, int), byte>();

                var lstErrores = new ConcurrentBag<Exception>(); // propagar los errores al finalizar todos los lotes.
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 15 };

                // 5. Iterar sobre los grupos creados
                await Parallel.ForEachAsync(gruposPorLote, parallelOptions, async (entry, ct) =>
                {
                    var objKeyLooku = (entry.Key.intLotNumero, entry.Key.intLoteUnificado, entry.Key.intCodProd, entry.Key.intCodTal);
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
                        lstErrores.Add(ex); // conservar el error para el AggregateException.
                    }
                });
                if (!lstErrores.IsEmpty) throw new AggregateException("Falló el registro de uno o más lotes.", lstErrores);
            }
            catch (Exception objException)
            {
                // _objLogger.LogError($"[CalculoCostoMateriaPrimaFeature].[RegistrarMpFrsValorizada] Ocurrio un error: {objException.Message}");
                _objLogger.LogError($"[CalculoCostosFeature].[RegistrarMpRprValorizado] Ocurrio un error: {objException.Message}");
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

        // valorar el saldo final con el mismo costo global del libro.
        public async Task<List<InventarioVal>> ObtenerInventarioValorizado(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            var trInvVal = ObtenerInventarioValorizadoBase(dtFechaInicio, dtFechaFin);
            var trDetalle = ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
            await Task.WhenAll(trInvVal, trDetalle);
            var lstBase = await trInvVal;
            var lstDetalle = await trDetalle;
            var costosInv = MotorValoracionCostoVenta.CostosUnitariosInventario(lstBase);
            var dicCosto = MotorValoracionCostoVenta.CostoPorProdTallaDesdeDetalle(
                lstDetalle.Select(x => new FilaValoracion(x.strTipoLiq, x.strTipo, x.intLote, x.intCodProd, x.intCodTalla,
                    x.strAgrupacion ?? "", x.strCodMov ?? "", x.dcLibras, x.dcCostoTot)), costosInv);
            MotorValoracionCostoVenta.ValorarInventarioFinal(lstBase, dicCosto);
            return lstBase;
        }

        public async Task<List<InventarioVal>> ObtenerInventarioValorizadoBase(DateOnly dtFechaInicio, DateOnly dtFechaFin)
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
            #region Medicion
            // using var objMedicionTotal = MedicionTiempos.IniciarMetodo(_objLogger, "param-proc", () => (objDataProceso.lstLiqFresco?.Count ?? 0) + (objDataProceso.lstLiqReproCompleto?.Count ?? 0));
            #endregion
            try
            {
                //Se obtiene tipo de proceso cocido para entero
                lstProdCocido = await MedicionTiempos.MedirAsync("parametros-catalogo-cocido", () => _objProcesoParametro.ConsultarCatalogoXDes("Tipo Proceso Cocido"), objResultado => objResultado?.Count);
                dtFechaCorteCorr = DateOnly.FromDateTime(dtFechaCorte);
                dtFechaInicio = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, 1);
                dtFechaFin = new DateOnly(dtFechaCorte.Year, dtFechaCorte.Month, dtFechaCorte.Day);

                var TareaTarifaProceso = MedicionTiempos.MedirAsync("ConsultarProcesoTarifa", () => _objProcesoParametro.ConsultarProcesoTarifa(dtFechaCorteCorr), objResultado => objResultado?.Count);

                var tareaFresco =MedicionTiempos.MedirAsync("cargador-crudo-fresco-SP", () => _objMateriaPrima.ObtenerMatPrimValFrsXRangoFecha(dtFechaInicio,dtFechaFin), objResultado => objResultado?.Count);
                var tareaReproCompleto = MedicionTiempos.MedirAsync("ObtenerReporteMateriaPrimaReproValorizada", () => ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin), objResultado => objResultado?.Count);

                await Task.WhenAll(
                    tareaReproCompleto,
                    TareaTarifaProceso,
                    tareaFresco,
                    MedicionTiempos.MedirAsync("parametros", () => ObtenerValProceso(dtFechaInicio, objDataProceso), () => (objDataProceso.lstProcesoFrs?.Count ?? 0) + (objDataProceso.lstProcesoRpc?.Count ?? 0)));

                objDataProceso.lstLiqFresco =await tareaFresco;
                objDataProceso.lstProcesoTarifa = await TareaTarifaProceso;
                //if (lstResultados.Any() && lstResultados.All(r => r.dcValor != 0))
                //{
                //    return lstResultados;
                //}

                objDataProceso.lstLiqReproCompleto = await tareaReproCompleto;
                objDataProceso.lstLiqRepro = objDataProceso.lstLiqReproCompleto.Where(x =>string.Equals(x.strAgrupacion,"2. PROCESADO", StringComparison.OrdinalIgnoreCase)).ToList();
                objDataProceso.lstProdTerm = _objConfig.Value.lstProdTerm;
                objDataProceso.lstDescTotFresco = _objConfig.Value.lstDescTotFresco;
                await MedicionTiempos.MedirAsync("empaque-Costos", () => _objCostoMaterialEmpaque.ObtenerCostoMaterialEmpaqueXLiqProd(objDataProceso.lstLiqFresco), () => objDataProceso.lstLiqFresco.Count);

                List<string> lstItemCod;

                lstItemCod = MatPrimaReproceso.ObtenerLstItemHidra(objDataProceso.lstLiqRepro);
                var tareaCostPromHidra = await MedicionTiempos.MedirAsync("hidratacion-SONG", () => _objMateriaPrima.CostoUltMovXItemCod(lstItemCod, dtFechaInicio, dtFechaFin), objResultado => objResultado?.Count);

                objDataProceso.dcCostoHidraReproceso = tareaCostPromHidra.Sum(obj => obj.dcConsumoTotal);
                objDataProceso.lstLibrasParticion = new List<LibrasParticionDto>();
                objDataProceso.lstDetalleEtapa = new List<DetalleEtapaProcesoDto>(); // misma vigencia que la base física.
                // Llamada para Fresco
                // using var objTiempoMotoresProceso = MedicionTiempos.IniciarTramo("motores-proceso", () => (objDataProceso.lstLiqFresco?.Count ?? 0) + (objDataProceso.lstLiqRepro?.Count ?? 0));
                objMotorProceso.AsignarCostoProcesoFrs(objDataProceso);
                // Llamada para Reproceso
                objMotorProceso.AsignarCostoProcesoRpc(objDataProceso);
                // Llamada para Tarifario
                objMotorProceso.AsignarCostoProcesoTarifa(objDataProceso);
                //Sumrizamos libras 
                //objMotorProceso.SumarizarLibrasNoEditable(objDataProceso);
                // NUEVO: recalcular cuando valores y libras ya son definitivos
                objMotorProceso.RecalcularCostosUnitariosExactos(
                    objDataProceso.lstProcesoFrs,
                    objDataProceso.lstProcesoRpc);
                // objTiempoMotoresProceso.Dispose();
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

        private async Task ObtenerValProceso(DateOnly dtFechaInicio, DataProcesoParam objDataProceso, CierreValorizacionContexto? ctx = null)
        {
            try
            {

                int diasEnMes = DateTime.DaysInMonth(dtFechaInicio.Year, dtFechaInicio.Month);
                DateOnly dtFechaCorte = new DateOnly(dtFechaInicio.Year, dtFechaInicio.Month, diasEnMes);

                //Se obtienen las formas de congelamiento desde el catalogoDet  
                objDataProceso.lstCongTunel = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento Tunel")).Select(int.Parse).ToList();
                objDataProceso.lstCongIqf = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento Brine")).Select(int.Parse).ToList();
                objDataProceso.lstCongBrine = (await _objProcesoParametro.ConsultarCatalogoXDes("Tipo Congelamiento IQF")).Select(int.Parse).ToList();
                var tareaResultadosFrs = ctx == null ? _objProcesoParametro.ConsultarProcesosFrescoConValores(dtFechaCorte) : Task.FromResult(ctx.lstProcesoFrs);
                var tareaResultadosRpc = ctx == null ? _objProcesoParametro.ConsultarProcesosReproConValores(dtFechaCorte) : Task.FromResult(ctx.lstProcesoRpc);
                var tareaCostosParticion = ctx == null ? _objProcesoParametro.ConsultarCostoProcesoParticion(dtFechaCorte.Year, dtFechaCorte.Month) : Task.FromResult(ctx.lstCostoProcesoParticion);
                var tareaConfiguracionCostoProductivo = ctx == null ? _objProcesoParametro.ConsultarConfiguracionRuntimeCostoProductivo() : Task.FromResult(ctx.objConfiguracion ?? throw new InvalidOperationException("Falta la configuración del cierre."));
                await Task.WhenAll(tareaResultadosFrs, tareaResultadosRpc, tareaCostosParticion, tareaConfiguracionCostoProductivo);
                // Recalcular muta parametros; cada tarea recibe copias profundas propias.
                objDataProceso.lstProcesoFrs = EntradaCosteo.Ordenar(EntradaCosteo.Copiar(await tareaResultadosFrs));
                objDataProceso.lstProcesoRpc = EntradaCosteo.Ordenar(EntradaCosteo.Copiar(await tareaResultadosRpc));
                objDataProceso.lstCostoProcesoParticion = EntradaCosteo.Ordenar(EntradaCosteo.Copiar(await tareaCostosParticion));
                var configuracion = EntradaCosteo.Copiar(await tareaConfiguracionCostoProductivo);
                configuracion.lstProcesos = configuracion.lstProcesos.OrderBy(x => x.strCodigo, StringComparer.Ordinal).ThenBy(EntradaCosteo.ClaveCompleta, StringComparer.Ordinal).ToList();
                configuracion.lstAplicaciones = configuracion.lstAplicaciones.OrderBy(x => x.strPcCodigo, StringComparer.Ordinal).ThenBy(x => x.strTipoProcesoCodigo, StringComparer.Ordinal).ThenBy(EntradaCosteo.ClaveCompleta, StringComparer.Ordinal).ToList();
                configuracion.lstNiveles = configuracion.lstNiveles.OrderBy(x => x.strCodigo, StringComparer.Ordinal).ThenBy(EntradaCosteo.ClaveCompleta, StringComparer.Ordinal).ToList();
                AplicarConfiguracionCostoProductivoRuntime(objDataProceso, configuracion);
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
            // LEGADO: Warren se guarda con guardar-cierre-param-proc.
            _objLogger.LogWarning("[GuardarWarren] LEGADO: Warren se guarda con guardar-cierre-param-proc.");
            try
            {
                int anio = Convert.ToInt32(request.strAnio);
                int mes = Convert.ToInt32(request.strMes);
                DateOnly fin = new(anio, mes, DateTime.DaysInMonth(anio, mes));
                WarrenResultadoDto resultado = await CalcularWarrenPeriodo(anio, mes, request.dcObjetivoWarren);
                await _objProcesoParametro.RegistrarParametrosWarren(fin, resultado, request.strUsuario);
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

        public async Task<WarrenResultadoDto> CalcularWarrenPeriodo(
            int anio,
            int mes,
            decimal objetivoWarren)
        {
            DateOnly inicio = new(anio, mes, 1);
            DateOnly fin = new(anio, mes, DateTime.DaysInMonth(anio, mes));

            var tareaPfr = ObtenerLiquidacionValorizada(inicio, fin);
            var tareaParametros = _objProcesoParametro.ConsultarProcesosFrescoConValores(fin);
            await Task.WhenAll(tareaPfr, tareaParametros);

            return _objMotorWarren.Calcular(
                await tareaPfr,
                await tareaParametros,
                objetivoWarren);
        }

        /// <summary>
        /// Reconstruye Warren para el GET de ParamProc a partir del mismo snapshot
        /// proceso/origen que ya alimenta la pantalla (PFR + RPC), NO desde
        /// MotorWarren.Calcular (que solo conoce PFR y subestima el costo normal
        /// de Cola, provocando una transferencia Entero->Cola excesiva).
        /// </summary>
        public async Task<WarrenResultadoDto?> ConsultarWarrenPeriodo(
            int intAnio,
            int intMes,
            IEnumerable<CostoProductivoProcesoOrigenDto> lstProcesosOrigen,
            // IEnumerable<ProcesoResultadoDto> lstCatalogoPfr)
            IEnumerable<ProcesoResultadoDto> lstCatalogoPfr, ConfiguracionCostoProductivoRuntimeDto? objConfiguracion = null)
        {
            try
            {
                DateOnly dtFechaCorte = new(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));

                decimal? dcObjetivoWarren = await _objProcesoParametro.ConsultarObjetivoWarren(dtFechaCorte);
                if (!dcObjetivoWarren.HasValue || dcObjetivoWarren.Value <= 0m) return null;

                List<CostoProductivoProcesoOrigenDto> lstProcesosOrigenActual =
                    (lstProcesosOrigen ?? Array.Empty<CostoProductivoProcesoOrigenDto>()).ToList();

                List<ProcesoResultadoDto> lstCatalogoPfrActual =
                    (lstCatalogoPfr ?? Array.Empty<ProcesoResultadoDto>()).ToList();

                if (lstProcesosOrigenActual.Count == 0)
                    throw new InvalidOperationException(
                        "No existe el resumen proceso/origen del snapshot actual para reconstruir Warren.");

                if (lstCatalogoPfrActual.Count == 0)
                    throw new InvalidOperationException(
                        "No existe el catálogo PFR del snapshot actual para reconstruir Warren.");

                WarrenResultadoDto objWarren = MotorWarrenCierre.Calcular(
                    lstProcesosOrigenActual,
                    lstCatalogoPfrActual,
                    dcObjetivoWarren.Value, objConfiguracion);
                // MotorWarren.RegistrarDiagnosticoResultado(_objLogger, "ConsultaCierre", objWarren); // DIAGNÓSTICO TEMPORAL

                // Validación defensiva adicional.
                if (objWarren.dcLibrasCola > 0m)
                {
                    decimal dcCostoFinalCola =
                        (objWarren.dcCostoColaActual * objWarren.dcLibrasCola + objWarren.dcAjusteTotal)
                        / objWarren.dcLibrasCola;

                    decimal dcObjetivoEsperado = objWarren.dcAjusteTotal > 0m
                        ? objWarren.dcObjetivoWarren
                        : objWarren.dcCostoColaActual;

                    if (Math.Abs(dcCostoFinalCola - dcObjetivoEsperado) > 0.0001m)
                        throw new InvalidOperationException(
                            $"Warren consultado no cuadra. Objetivo: {dcObjetivoEsperado:N4}; Resultado: {dcCostoFinalCola:N4}.");
                }

                return objWarren;
            }
            catch (Exception objException)
            {
                ManejoLog<CalculoCostosFeature>.Error(
                    _objLogger,
                    nameof(CalculoCostosFeature),
                    nameof(ConsultarWarrenPeriodo),
                    objException);
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

        #region Configuracion Costo Productivo

        public async Task<CostoProductivoMantenimientoDto>
            ConsultarConfiguracionCostoProductivo(bool incluirCuentas = true)
        {
            try
            {
                return await _objProcesoParametro
                    .ConsultarMantenimientoCostoProductivo(incluirCuentas);
            }
            catch (Exception ex)
            {
                ManejoLog<CalculoCostosFeature>.Error(
                    _objLogger,
                    nameof(CalculoCostosFeature),
                    nameof(ConsultarConfiguracionCostoProductivo),
                    ex);
                throw;
            }
        }

        public Task<GuardarCostoProductivoMantenimientoResponse> GuardarConfiguracionCostoProductivoConResultado(
            GuardarCostoProductivoMantenimientoRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            return _objProcesoParametro.GuardarMantenimientoCostoProductivoConResultado(request);
        }

        public async Task<bool> GuardarConfiguracionCostoProductivo(
            GuardarCostoProductivoMantenimientoRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                return await _objProcesoParametro
                    .GuardarMantenimientoCostoProductivo(request);
            }
            catch (Exception ex)
            {
                ManejoLog<CalculoCostosFeature>.Error(
                    _objLogger,
                    nameof(CalculoCostosFeature),
                    nameof(GuardarConfiguracionCostoProductivo),
                    ex);
                throw;
            }
        }

        public async Task<List<CuentaCostoMantenimientoDto>> ConsultarCuentasConfiguradasCostoProductivo()
        {
            try
            {
                return await _objProcesoParametro.ConsultarCuentasMantenimientoCostoProductivo();
            }
            catch (Exception ex)
            {
                ManejoLog<CalculoCostosFeature>.Error(_objLogger, nameof(CalculoCostosFeature), nameof(ConsultarCuentasConfiguradasCostoProductivo), ex);
                throw;
            }
        }

        public async Task<CuentasDisponiblesResultadoDto> ConsultarCuentasDisponiblesCostoProductivo(
            CuentasDisponiblesFiltroDto filtro)
        {
            try
            {
                return await _objProcesoParametro.ConsultarCuentasDisponibles(filtro);
            }
            catch (Exception ex)
            {
                ManejoLog<CalculoCostosFeature>.Error(_objLogger, nameof(CalculoCostosFeature), nameof(ConsultarCuentasDisponiblesCostoProductivo), ex);
                throw;
            }
        }

        /// <summary>
        /// Aplica al snapshot de trabajo la configuración productiva leída de SGCAM.
        /// Se invoca una sola vez desde ObtenerValProceso y luego todos los motores
        /// consultan el objeto en memoria.
        ///
        /// Construye el mapa de niveles de esta ejecución para MotorGrafoTopologico,
        /// conservando el mapa histórico como fallback por clave.
        /// </summary>
        private static void AplicarConfiguracionCostoProductivoRuntime(
            DataProcesoParam objDataProceso,
            ConfiguracionCostoProductivoRuntimeDto configuracion)
        {
            configuracion ??= new ConfiguracionCostoProductivoRuntimeDto();
            objDataProceso.objConfiguracionCostoProductivo = configuracion;
            objDataProceso.dicNivelesCosteoRuntime = configuracion.lstNiveles
                .Where(x => !string.IsNullOrWhiteSpace(x.strCodigo))
                .GroupBy(x => x.strCodigo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Last().intNivel, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Diarios de Movimientos y Costo de Venta Histórico

        // private static readonly HashSet<int> _hshProdDiagnostico = new() { 5359, 3861 };

        // Transacciones de salida (trs_codigo EX, EV, EOB, EXP): su costo es el promedio ponderado GLOBAL por CodProd + Talla. Se compara con Trim y sin distinguir mayúsculas.
        private static readonly HashSet<string> _hshTrsSalidaGlobal = new(StringComparer.OrdinalIgnoreCase) { "EX", "EV", "EOB", "EXP" };

        public async Task<List<CostVentUni>> ConsultarCostoVentaUni(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<CostVentUni> lstCostVentUni = new(), lstLbsLotePiso = new();
            List<LiquidacionResultado> lstLiqFrs = new();
            List<MatPrimaReproceso> lstLiqRpc = new();
            List<InventarioVal> lstInvVal = new();
            List<DiarioCosto> lstMovInv = new();
            List<PrecioFrsXMov> lstPrecioEsXMov = new();
            try
            {
                HashSet<string> hshCodProdExcluidos = new(StringComparer.OrdinalIgnoreCase) { "1006", "3473" };
                HashSet<string> hshDescripcionesExcluidas = new(StringComparer.OrdinalIgnoreCase) { "INICIALIZA INVENTARIO TOMA ene 2026", "INICIALIZA INVENTARIO DIC" };
                HashSet<string> hshTipEgr = new() { "UNI", "CDI", "R7" };
                HashSet<int> hshTalCodEx = new() { 128, 5 };
                HashSet<string> hshMovIng = new() { "DV", "IAJ", "REPING", "CNEI" };
                HashSet<string> hshMovEsp = new() { "UNI", "R7", "CDI" };
                HashSet<string> hshMovEgr = new() { "EAJ", "EMU", "REPROE", "LB", "UNI", "DIR", "CNE", "SMT", "EOB", "EX", "EXP", "EV" };
                var trLiqFrs = ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin);
                var trLiqRpc = ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin);
                var trInvVal = _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin);
                var trMovInvIng = _objMateriaPrima.IngresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
                var trMovInvEgr = _objMateriaPrima.EgresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(trLiqFrs, trLiqRpc, trInvVal, trMovInvIng, trMovInvEgr //, trCostVentEspeciales
                    );
                lstLiqFrs = trLiqFrs.Result;
                lstLiqRpc = trLiqRpc.Result;
                lstInvVal = trInvVal.Result;
                // Este flujo también construye las fuentes del costo de salida del diario de cierre.
                // DiagnosticoPrecision.Liquidaciones(_objLogger, "FuentesCostoSalidaDiarioPFR", lstLiqFrs);
                // DiagnosticoPrecision.Liquidaciones(_objLogger, "FuentesCostoSalidaDiarioRPC", lstLiqRpc.Where(x => x.strAgrupacion == "2. PROCESADO")
                    // .Select(x => new LiquidacionResultado { strTipoLiq = "LIQ_RPC", intLote = x.intLoteUnificado, intCodProd = x.intCodProd,
                        // intLidCodTal = x.intCodTal, dcLibras = x.dbLibras, dcTotalDolSum = x.dcTotalDolSum, dcCostoTotXLibra = x.dcCostoTotXLibra }));
                // DiagnosticoPrecision.Reproceso(_objLogger, "FuentesCostoSalidaDiarioRecibido", lstLiqRpc);
                lstLbsLotePiso = await _objMateriaPrima.ConsultarLotePiso(
                        lstLiqRpc
                        .Where(x => !hshTipEgr.Contains(x.strTipCod) && x.dbCostoXSecuencial == 0)
                        .Select(b => (decimal)b.intLotNumero)
                        .Distinct()
                        .ToList()
                        );
                lstMovInv.AddRange(trMovInvIng.Result
                    .Where(obj =>
                    hshMovIng.Contains(obj.strCodTip)
                    &&
                    !(hshCodProdExcluidos.Contains(obj.strProCodcor) && hshTalCodEx.Contains(obj.stTalCodigo) && hshDescripcionesExcluidas.Contains(obj.strDescri))
                    ));
                lstMovInv.AddRange(trMovInvEgr.Result
                    .Where(obj =>
                    hshMovEgr.Contains(obj.strCodTip) &&
                    !(hshCodProdExcluidos.Contains(obj.strProCodcor) && hshTalCodEx.Contains(obj.stTalCodigo) && hshDescripcionesExcluidas.Contains(obj.strDescri))
                ));
                lstCostVentUni.AddRange(lstLiqFrs.Select(obj => new CostVentUni(obj)));
                lstCostVentUni.AddRange(
                    lstLiqRpc
                    .Where(obj => !(obj.strAgrupacion == "1. RECIBIDO" && hshMovEsp.Contains(obj.strTipCod)))
                    .Select(obj => new CostVentUni(obj))
                 );
                lstCostVentUni.AddRange(lstInvVal.Select(obj => new CostVentUni(obj)));
                var dicPrecioEsXMov = PrecioFrsXMov.GenerarDiccionarioCostoXTalla(lstPrecioEsXMov);
                var dicProcesado = MatPrimaReproceso.GenerarDicProcGlobal(lstLiqRpc, _objLogger);
                var dicLiqFrs = LiquidacionResultado.GenerarDiccionarioLbs(lstLiqFrs, _objLogger);
                var dicInvVal = InventarioVal.GenerarDiccionarioCostoXTalla(lstInvVal, _objLogger);
                var dicFuenteInv = lstInvVal.Where(x => x.objLotkey != null).GroupBy(x => x.objLotkey).ToDictionary(g => g.Key,
                    g => (dcTotal: g.Sum(x => x.dcCostoTot), dcLibras: g.Sum(x => x.dcLibras)));
                var dicFuenteFrs = lstLiqFrs.GroupBy(x => x.objLotkey).ToDictionary(g => g.Key,
                    g => (dcTotal: g.Sum(x => x.dcTotalCostoWarren ?? x.dcTotalDolSum), dcLibras: g.Sum(x => (decimal)x.dcLibras)));
                var dicFuenteProc = lstLiqRpc.Where(x => x.strAgrupacion == "2. PROCESADO" && x.dbCostoXSecuencial > 0 && x.dbLibras > 0)
                    .GroupBy(x => x.objRpcValOrKey).ToDictionary(g => g.Key,
                        g => (dcTotal: g.Sum(x => x.dcTotalDolSum), dcLibras: g.Sum(x => (decimal)x.dbLibras)));
                var lstPrecisionEgresos = new List<(string strOrigen, decimal dcAsignado, decimal dcFuente, decimal dcEsperado, bool blCopia)>();
                foreach (var objReg in lstMovInv)
                {
                    string strOrigenPrecision = "ORIGINAL";
                    decimal dcPrecioPrecision = objReg.dcCostoUnit;
                    (decimal dcTotal, decimal dcLibras) objFuentePrecision = (0m, 0m);
                    bool blCopiaPrecision = false;
                    if (objReg.dcCostoUnit == 0)
                    {
                        if (dicInvVal.TryGetValue(objReg.objLoteProdTalKey, out decimal dcPrecioInv))
                        { strOrigenPrecision = "INV"; dcPrecioPrecision = dcPrecioInv; objFuentePrecision = dicFuenteInv[objReg.objLoteProdTalKey]; }
                        else if (dicLiqFrs.TryGetValue(objReg.objLoteProdTalKey, out decimal dcPrecioFrs))
                        { strOrigenPrecision = "FRS"; dcPrecioPrecision = dcPrecioFrs; objFuentePrecision = dicFuenteFrs[objReg.objLoteProdTalKey]; }
                        else if (dicProcesado.TryGetValue(objReg.objLoteProdTalReciKey, out decimal dcPrecioProc))
                        { strOrigenPrecision = "PROC"; dcPrecioPrecision = dcPrecioProc; objFuentePrecision = dicFuenteProc[objReg.objLoteProdTalReciKey]; }
                    }
                    decimal dcEsperadoPrecision = objReg.dcCostoTot;
                    if (strOrigenPrecision != "ORIGINAL")
                    {
                        objReg.InicializarCamposCost(dcPrecioPrecision);
                        dcEsperadoPrecision = objFuentePrecision.dcLibras == 0m ? Math.Round(dcPrecioPrecision * objReg.dcLibras, 2)
                            : Math.Round(objFuentePrecision.dcTotal * objReg.dcLibras / objFuentePrecision.dcLibras, 2);
                        blCopiaPrecision = objReg.strTipo == "E" && objFuentePrecision.dcLibras > 0m
                            && Math.Abs(Math.Abs(objReg.dcLibras) - objFuentePrecision.dcLibras) <= 0.005m;
                        if (blCopiaPrecision) dcEsperadoPrecision = -objFuentePrecision.dcTotal;
                        objReg.dcCostoTot = dcEsperadoPrecision;
                    }
                    if (objReg.strTipo == "E") lstPrecisionEgresos.Add((strOrigenPrecision, objReg.dcCostoTot, objFuentePrecision.dcTotal, dcEsperadoPrecision, blCopiaPrecision));
                }
                // if (DiagnosticoPrecision.blActivo)
                // {
                    // foreach (var objGrupoPrecision in lstPrecisionEgresos.GroupBy(x => x.strOrigen))
                        // _objLogger.LogInformation("[DiagPrecision] EgresosINV Origen={Origen} Filas={Filas} CostoAsignado={Asignado} TotalFuenteReferenciado={Fuente} TotalEsperadoProporcional={Esperado} CopiasTotal={Copias} Delta={Delta}",
                            // objGrupoPrecision.Key, objGrupoPrecision.Count(), objGrupoPrecision.Sum(x => x.dcAsignado), objGrupoPrecision.Sum(x => x.dcFuente),
                            // objGrupoPrecision.Sum(x => x.dcEsperado), objGrupoPrecision.Count(x => x.blCopia), objGrupoPrecision.Sum(x => x.dcAsignado - x.dcEsperado));
                    // DiagnosticoPrecision.Diferencia(_objLogger, "EgresosINV", lstPrecisionEgresos.Sum(x => x.dcAsignado - x.dcEsperado));
                // }
                lstCostVentUni.AddRange(lstMovInv.Select(obj => new CostVentUni(obj)));
                lstCostVentUni.AddRange(lstLbsLotePiso);
                AsignarCostoGlobalExportaciones(lstCostVentUni);
                // DiagnosticoPrecision.Movimientos(_objLogger, "CostoVentaUnitario", lstCostVentUni);
                return lstCostVentUni;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error Feature : {nameof(CalculoCostosFeature)} funcion : {nameof(ConsultarCostoVentaUni)} error: " +
                    objException.Message);
                throw;
            }
        }

        // Reemplazada por el ponderado global de exportaciones (AsignarCostoGlobalExportaciones)

        private static bool EsTrsSalidaGlobal(CostVentUni objReg) => !string.IsNullOrWhiteSpace(objReg.strCodMov) && _hshTrsSalidaGlobal.Contains(objReg.strCodMov.Trim());

        /// <summary>
        /// Costo unitario global por CodProd + Talla, sin importar el lote.
        /// Orden: 1) costos ya asignados a los movimientos, 2) se excluyen las salidas (_hshTrsSalidaGlobal), 3) CU con lo demás.
        /// Universo: todos los movimientos que NO son salidas con libras (dcLibras != 0), INCLUIDO el inventario inicial y los de costo 0.
        /// Fórmula: CU = SUM(dcCostoTot) / SUM(dcLibras), valores firmados (sin Math.Abs).
        /// No se calcula si las libras netas están en (-0.10, 0.10) o si libras y dólares tienen signo opuesto (se registra si blRegistrar).
        /// </summary>
        public Dictionary<(int intCodProd, int intCodTalla), decimal> CalcularCostoGlobalProdTalla(List<CostVentUni> lstCostVentUni, bool blRegistrar = true)
        {
            const decimal dcToleranciaLibras = 0.10m;
            var dicCostoGlobal = new Dictionary<(int intCodProd, int intCodTalla), decimal>();
            foreach (var objGrupo in lstCostVentUni.Where(x => !EsTrsSalidaGlobal(x) && x.dcLibras != 0m).GroupBy(x => (x.intCodProd, x.intCodTalla)))
            {
                decimal dcLibras = objGrupo.Sum(x => x.dcLibras), dcCosto = objGrupo.Sum(x => x.dcCostoTot);
                if ((dcLibras > -dcToleranciaLibras && dcLibras < dcToleranciaLibras) || Math.Sign(dcLibras) != Math.Sign(dcCosto))
                {
                    continue;
                }
                dicCostoGlobal[objGrupo.Key] = dcCosto / dcLibras;
            }
            return dicCostoGlobal;
        }

        /// <summary>
        /// Asigna el CU global a las salidas EX, EV, EOB y EXP con libras de cada CodProd + Talla: dcCostoUnit = CU y dcCostoTot = dcLibras * CU.
        /// No modifica dcLibras, signos ni el costo de los demás movimientos. Sin CU global, la salida conserva su costo (se registra).
        /// </summary>
        private void AsignarCostoGlobalExportaciones(List<CostVentUni> lstCostVentUni)
        {
            var dicCostoGlobal = CalcularCostoGlobalProdTalla(lstCostVentUni);
            var hshSinCostoGlobal = new HashSet<(int, int)>();
            foreach (CostVentUni objReg in lstCostVentUni.Where(x => EsTrsSalidaGlobal(x) && x.dcLibras != 0m))
            {
                if (!dicCostoGlobal.TryGetValue((objReg.intCodProd, objReg.intCodTalla), out decimal dcCostoGlobal)) { hshSinCostoGlobal.Add((objReg.intCodProd, objReg.intCodTalla)); continue; }
                objReg.dcCostoUnit = dcCostoGlobal; objReg.dcCostoTot = objReg.dcLibras * dcCostoGlobal;
            }
            // foreach (var objClave in hshSinCostoGlobal) _objLogger.LogWarning("[CostoVentaUni][CostoGlobal] Salidas sin costo global Prod={Prod} Talla={Talla}", objClave.Item1, objClave.Item2); // LOG DESACTIVADO (evitar ruido en el log)
            // RegistrarDiagnosticoCostoGlobal(lstCostVentUni, dicCostoGlobal); // DIAGNÓSTICO TEMPORAL // LOG DESACTIVADO (evitar ruido en el log)
        }

        // private void RegistrarDiagnosticoCostoGlobal(List<CostVentUni> lstCostVentUni, Dictionary<(int intCodProd, int intCodTalla), decimal> dicCostoGlobal)
        // {
            // foreach (int intCodProd in _hshProdDiagnostico)
            // {
                // var lstProducto = lstCostVentUni.Where(x => x.intCodProd == intCodProd).ToList();
                // if (lstProducto.Count == 0) { _objLogger.LogWarning("[Diag][CostoGlobal] Prod={Prod} SIN FILAS en lstCostVentUni", intCodProd); continue; }
                // foreach (var objTalla in lstProducto.GroupBy(x => x.intCodTalla))
                // {
                    // var lstUniverso = objTalla.Where(x => !EsTrsSalidaGlobal(x) && x.dcLibras != 0m).ToList();
                    // var lstSalidas = objTalla.Where(EsTrsSalidaGlobal).ToList();
                    // _objLogger.LogInformation("[Diag][CostoGlobal] Prod={Prod} Talla={Talla} Universo filas={N} Libras={Lb} Costo={Cto} | Salidas filas={M} Libras={LbS} Costo={CtoS} CodMov=[{Cod}] | CU={CU}", intCodProd, objTalla.Key, lstUniverso.Count, lstUniverso.Sum(x => x.dcLibras), lstUniverso.Sum(x => x.dcCostoTot), lstSalidas.Count, lstSalidas.Sum(x => x.dcLibras), lstSalidas.Sum(x => x.dcCostoTot), string.Join(",", lstSalidas.Select(x => x.strCodMov).Distinct()), dicCostoGlobal.TryGetValue((intCodProd, objTalla.Key), out decimal dcCostoGlobal) ? (object)dcCostoGlobal : "SIN_CU");
                    // foreach (var objOrigen in lstUniverso.GroupBy(x => (x.strTipoLiq, x.strAgrupacion, x.strCodMov)).Take(30))
                        // _objLogger.LogInformation("[Diag][CostoGlobal]   Prod={Prod} Talla={Talla} Origen={TipoLiq}|{Agrup}|{CodMov} filas={N} Libras={Lb} Costo={Cto}", intCodProd, objTalla.Key, objOrigen.Key.strTipoLiq, objOrigen.Key.strAgrupacion, objOrigen.Key.strCodMov, objOrigen.Count(), objOrigen.Sum(x => x.dcLibras), objOrigen.Sum(x => x.dcCostoTot));
                // }
            // }
        // }

        public async Task<DiariosCierreDto> ConsultarDiariosCierre(int intAnio, int intMes)
        {
            List<DiarioMovimientoCuentaDto> lstConfig = await _objProcesoParametro.ConsultarConfiguracionDiarioMovimiento();
            List<DiarioMovimientoPersistenciaDto> lstGuardado = await _objProcesoParametro.ConsultarDiarioMovimientoPeriodo(intAnio, intMes);
            List<DiarioMovimientoPersistenciaDto> lstGuardadoVisible = lstGuardado
                .Where(x => x.strProcesoCodigo != DiarioMovimientoCodigos.CostoVentaSalidaHistorico).ToList();
            if (lstGuardadoVisible.Count > 0)
                return DiarioCierrePresentacionBuilder.Construir(intAnio, intMes, lstGuardadoVisible, lstConfig, guardado: true);

            DiarioMovimientoCalculoResultadoDto objCalculo = await CalcularDiariosCierreInterno(intAnio, intMes);
            return objCalculo.objVista;
        }

        public async Task<DiariosCierreDto> GuardarDiariosCierre(GuardarDiariosCierreRequest objRequest)
        {
            if (objRequest == null) throw new ArgumentNullException(nameof(objRequest));
            if (objRequest.intAnio < 2000 || objRequest.intAnio > 2100) throw new ArgumentOutOfRangeException(nameof(objRequest.intAnio));
            if (objRequest.intMes < 1 || objRequest.intMes > 12) throw new ArgumentOutOfRangeException(nameof(objRequest.intMes));
            string strUsuario = string.IsNullOrWhiteSpace(objRequest.strUsuario) ? "SISTEMA" : objRequest.strUsuario.Trim();

            // El POST recalcula todo en backend; no confía en los importes enviados por Angular.
            DiarioMovimientoCalculoResultadoDto objCalculo = await CalcularDiariosCierreInterno(objRequest.intAnio, objRequest.intMes);
            DateOnly dtFechaCorte = new(objRequest.intAnio, objRequest.intMes, DateTime.DaysInMonth(objRequest.intAnio, objRequest.intMes));
            bool blGuardado = await _objProcesoParametro.GuardarDiarioMovimientoPeriodo(
                objRequest.intAnio, objRequest.intMes, dtFechaCorte, DiarioMovimientoCodigos.ProcesosCierreAutomatico,
                objCalculo.lstPersistenciaPeriodo, strUsuario);
            if (!blGuardado) throw new InvalidOperationException("No fue posible guardar los diarios del período.");

            // DVS solo agrega registros y se guarda separado para conservar el costo histórico de salida.
            if (objCalculo.lstDvsPendiente.Count > 0)
                await _objProcesoParametro.GuardarCostoVentaSalidaHistorico(objCalculo.lstDvsPendiente, strUsuario);
            objCalculo.objVista.blGuardado = true;
            objCalculo.objVista.strEstado = "GUARDADO";
            return objCalculo.objVista;
        }

        private async Task<DiarioMovimientoCalculoResultadoDto> CalcularDiariosCierreInterno(int intAnio, int intMes)
        {
            DateOnly dtInicio = new(intAnio, intMes, 1);
            DateOnly dtFin = new(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));
            var trConfig = _objProcesoParametro.ConsultarConfiguracionDiarioMovimiento();
            var trLiquidaciones = ObtenerLiquidacionValorizada(dtInicio, dtFin);
            var trNc = _objProcesoParametro.ConsultarNotasCreditoRetornoContenedor(dtInicio, dtFin);
            var trCostoSalidaActual = ConstruirCostoVentaSalidaHistorico(dtInicio, dtFin);
            await Task.WhenAll(trConfig, trLiquidaciones, trNc, trCostoSalidaActual);

            List<DiarioMovimientoCuentaDto> lstConfig = trConfig.Result;
            List<LiquidacionResultado> lstLiquidaciones = trLiquidaciones.Result;
            List<NotaCreditoRetornoContenedorDto> lstNotas = trNc.Result.Where(x => x.blEsRetornoContenedor).ToList();
            List<CostoVentaSalidaHistoricoDto> lstCostoSalidaActual = trCostoSalidaActual.Result;
            // if (DiagnosticoPrecision.blActivo)
            // {
                // DiagnosticoPrecision.Liquidaciones(_objLogger, "DiarioCierrePFR", lstLiquidaciones);
            // }
            ValidarConservacionWarrenParaDiario(lstLiquidaciones);
            var objMotorTransferencias = new MotorDiarioCierreConfigurado(_objLogger);
            List<DiarioMovimientoPersistenciaDto> lstTransferencias = objMotorTransferencias.Construir(lstLiquidaciones, lstConfig, intAnio, intMes);
            var objMotorCostoHistorico = new MotorCostoVentaSalidaHistorico();
            List<DiarioMovimientoPersistenciaDto> lstDvsActual = objMotorCostoHistorico.APersistencia(lstCostoSalidaActual);
            var objMotorRetorno = new MotorRetornoContenedor();
            var lstFilasRetorno = new List<DiarioMovimientoPersistenciaDto>();
            var lstDvsPendiente = new List<DiarioMovimientoPersistenciaDto>();
            lstDvsPendiente.AddRange(lstDvsActual);
            var lstAdvertencias = new List<string>();
            var dicCostoOriginal = new Dictionary<(int intAnio, int intMes), List<CostoVentaSalidaHistoricoDto>>();

            foreach (var objGrupoPeriodo in lstNotas.Where(x => x.dtFechaFacturaAplicada.HasValue)
                .GroupBy(x => new { intAnio = x.dtFechaFacturaAplicada!.Value.Year, intMes = x.dtFechaFacturaAplicada!.Value.Month }))
            {
                int intAnioSalida = objGrupoPeriodo.Key.intAnio, intMesSalida = objGrupoPeriodo.Key.intMes;
                DateOnly dtInicioSalida = new(intAnioSalida, intMesSalida, 1);
                DateOnly dtFinSalida = new(intAnioSalida, intMesSalida, DateTime.DaysInMonth(intAnioSalida, intMesSalida));
                List<string> lstFacturasKey = objGrupoPeriodo.Select(x => x.strFacturaKey)
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                List<DiarioMovimientoPersistenciaDto> lstDvsGuardado = await _objProcesoParametro
                    .ConsultarCostoVentaSalidaHistorico(intAnioSalida, intMesSalida, lstFacturasKey);
                List<FacturaCostoSalidaDto> lstFacturasBase = await _objProcesoParametro.ConsultarFacturasCostoSalida(dtInicioSalida, dtFinSalida);
                List<string> lstCodigosProducto = lstFacturasBase.Where(x => x.intCodProd > 0).Select(x => x.intCodProd.ToString()).Distinct().ToList();
                List<InfoProd> lstProductos = await _objMateriaPrima.ObtenerInfoProd(lstCodigosProducto);
                List<CostoVentaSalidaHistoricoDto> lstCostosPeriodo = objMotorCostoHistorico.DesdePersistencia(lstDvsGuardado, lstFacturasBase, lstProductos);
                HashSet<string> hshEncontradas = lstCostosPeriodo.Select(x => x.strFacturaKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
                List<string> lstFaltantes = lstFacturasKey.Where(x => !hshEncontradas.Contains(x)).ToList();
                if (lstFaltantes.Count > 0)
                {
                    // Compatibilidad histórica: reconstruir con el costo de venta unitario del período original.
                    List<CostoVentaSalidaHistoricoDto> lstReconstruido = await ConstruirCostoVentaSalidaHistorico(dtInicioSalida, dtFinSalida);
                    lstCostosPeriodo.AddRange(lstReconstruido.Where(x => lstFaltantes.Contains(x.strFacturaKey, StringComparer.OrdinalIgnoreCase)));
                    // Al confirmar se congela el mes reconstruido para evitar recalcularlo en el futuro.
                    lstDvsPendiente.AddRange(objMotorCostoHistorico.APersistencia(lstReconstruido));
                }
                dicCostoOriginal[(intAnioSalida, intMesSalida)] = lstCostosPeriodo;
            }

            foreach (NotaCreditoRetornoContenedorDto objNota in lstNotas)
            {
                if (!objNota.dtFechaFacturaAplicada.HasValue)
                {
                    lstAdvertencias.Add($"NC {objNota.strNumero}: no se obtuvo fecha de la factura aplicada.");
                    continue;
                }
                var objPeriodo = (objNota.dtFechaFacturaAplicada.Value.Year, objNota.dtFechaFacturaAplicada.Value.Month);
                if (!dicCostoOriginal.TryGetValue(objPeriodo, out List<CostoVentaSalidaHistoricoDto>? lstCostosPeriodo))
                {
                    lstAdvertencias.Add($"NC {objNota.strNumero}: no se pudo reconstruir el período de salida.");
                    continue;
                }
                List<CostoVentaSalidaHistoricoDto> lstCostoFactura = lstCostosPeriodo
                    .Where(x => string.Equals(x.strFacturaKey, objNota.strFacturaKey, StringComparison.OrdinalIgnoreCase)).ToList();
                if (lstCostoFactura.Count == 0)
                {
                    lstAdvertencias.Add($"NC {objNota.strNumero}: no existe costo histórico para factura {objNota.strAplicaFactura}.");
                    continue;
                }
                lstFilasRetorno.AddRange(objMotorRetorno.ConstruirPersistencia(intAnio, intMes, objNota, lstCostoFactura, lstConfig));
            }

            // DVS se guarda separado porque solo agrega registros históricos.
            List<DiarioMovimientoPersistenciaDto> lstPersistenciaPeriodo = lstTransferencias.Concat(lstFilasRetorno).ToList();
            DiariosCierreDto objVista = DiarioCierrePresentacionBuilder.Construir(
                intAnio, intMes, lstPersistenciaPeriodo, lstConfig, lstAdvertencias, guardado: false);
            return new DiarioMovimientoCalculoResultadoDto
            {
                objVista = objVista,
                lstPersistenciaPeriodo = lstPersistenciaPeriodo,
                lstDvsPendiente = lstDvsPendiente
                    .GroupBy(x => new { x.intAnio, x.intMes, x.strAgrupacion, x.strCentroCodigo, x.strSubcentroCodigo })
                    .Select(x => x.First()).ToList()
            };
        }

        private static string NormalizarClaseDiario(string? strClase) =>
            (strClase ?? string.Empty).Trim().ToUpperInvariant() switch { "A+" => "A", "N" => "B", var x => x };

        /// <summary>
        /// Diagnóstico (solo log) de la conservación de Warren en el diario. Misma clasificación y mismo delta que MotorDiarioCierreConfigurado.Agrupar (dcTotalWarren - dcCostTotalProc).
        /// Registra el detalle por Partición / ClaseOriginal / Clase y el resumen Global / ContabilizableABC / Omitido. Emite A LO SUMO UNA advertencia por ejecución (anomalías + chequeos de conservación).
        /// </summary>
        private void ValidarConservacionWarrenParaDiario(List<LiquidacionResultado> lstLiquidaciones)
        {
            var lstLineas = lstLiquidaciones.Select(x =>
            {
                string? strParticion = ParticionCosteo.ClasificarFrs(x.strProClas01, x.strProClas05);
                string strClase = NormalizarClaseDiario(x.strProClas02);
                decimal dcNormal = x.dcCostTotalProc ?? 0m;
                decimal dcDelta = (x.dcTotalWarren ?? dcNormal) - dcNormal;
                bool blContabilizable = !string.IsNullOrWhiteSpace(strParticion) && (strClase is "A" or "B" or "C");
                return new { Liq = x, Particion = strParticion ?? string.Empty, ClaseOriginal = x.strProClas02 ?? string.Empty, Clase = strClase, Delta = dcDelta, Contabilizable = blContabilizable };
            }).ToList();

            // foreach (var objGrupo in lstLineas.Where(x => x.Delta != 0m).GroupBy(x => (x.Particion, x.ClaseOriginal, x.Clase)).OrderBy(x => x.Key.Particion).ThenBy(x => x.Key.Clase))
                // _objLogger.LogInformation("[DiagWarrenDiario] Detalle Partición={Particion} ClaseOriginal={ClaseOriginal} Clase={Clase} Líneas={Lineas} Delta={Delta}", objGrupo.Key.Particion, objGrupo.Key.ClaseOriginal, objGrupo.Key.Clase, objGrupo.Count(), objGrupo.Sum(x => x.Delta));

            decimal dcGlobal = lstLineas.Sum(x => x.Delta), dcContabilizable = lstLineas.Where(x => x.Contabilizable).Sum(x => x.Delta), dcOmitido = lstLineas.Where(x => !x.Contabilizable).Sum(x => x.Delta);
            // _objLogger.LogInformation("[DiagWarrenDiario] Resumen Global={Global} ContabilizableABC={ContabilizableABC} Omitido={Omitido}", dcGlobal, dcContabilizable, dcOmitido);

            // var lstAnomalias = lstLineas.Where(x => x.Delta != 0m && !x.Contabilizable).ToList();
            // if (lstAnomalias.Count > 0 || Math.Abs(dcGlobal) > 0.01m || Math.Abs(dcOmitido) > 0.01m)
            // {
                // string strMuestra = string.Join(" | ", lstAnomalias.Take(20).Select(x => $"Prod={x.Liq.intCodProd} Lote={x.Liq.intLote} Clas01={x.Liq.strProClas01} Clas05={x.Liq.strProClas05} ClaseOriginal={x.ClaseOriginal} Delta={x.Delta}"));
                // _objLogger.LogWarning("[DiagWarrenDiario] Warren en el diario: Global={Global} (debería ser ~0) Omitido={Omitido} líneasConDeltaSinClasificar={Lineas}. Muestra (máx. 20): {Muestra}", dcGlobal, dcOmitido, lstAnomalias.Count, strMuestra);
            // }
        }

        private async Task<List<CostoVentaSalidaHistoricoDto>> ConstruirCostoVentaSalidaHistorico(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            var trCostos = ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
            var trFacturas = _objProcesoParametro.ConsultarFacturasCostoSalida(dtFechaInicio, dtFechaFin);
            var trConfig = _objProcesoParametro.ConsultarConfiguracionDiarioMovimiento("DCE");
            var trInvVal = ObtenerInventarioValorizadoBase(dtFechaInicio, dtFechaFin);
            await Task.WhenAll(trCostos, trFacturas, trConfig, trInvVal);
            var dicCostoAplicado = MotorValoracionCostoVenta.CostoPorProdTallaDesdeDetalle(
                trCostos.Result.Select(x => new FilaValoracion(x.strTipoLiq, x.strTipo, x.intLote, x.intCodProd, x.intCodTalla,
                    x.strAgrupacion ?? "", x.strCodMov ?? "", x.dcLibras, x.dcCostoTot)),
                MotorValoracionCostoVenta.CostosUnitariosInventario(trInvVal.Result));
            List<FacturaCostoSalidaDto> lstFacturas = trFacturas.Result;
            List<string> lstCodigos = lstFacturas.Where(x => x.intCodProd > 0).Select(x => x.intCodProd.ToString()).Distinct().ToList();
            List<InfoProd> lstProductos = await _objMateriaPrima.ObtenerInfoProd(lstCodigos);
            var objMotor = new MotorCostoVentaSalidaHistorico();
            return objMotor.Construir(trCostos.Result, lstFacturas, lstProductos, trConfig.Result, dicCostoAplicado);
        }

        #endregion
    }
}
