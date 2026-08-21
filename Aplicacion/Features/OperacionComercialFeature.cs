using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagementService.Aplicacion.DTos;
using CostManagementService.Infraestructura.EF_Core.SONG;
using CostManagementService.Infraestructura.Repository.Interface;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace CostManagementService.Aplicacion.Features
{
    public class OperacionComercialFeature
    {
        private readonly ILogger<CalculoCostosFeature> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        private readonly IVentasFacturacionService _objVentasFacturacionService;
        private readonly CalculoCostosFeature _objCostoMateriaPrima;
        private readonly IMateriaPrima _objMateriaPrima;
        private readonly MotorAsignacionPrecios _objMotorAsigPrec;

        public OperacionComercialFeature(
            ILogger<CalculoCostosFeature> objLogger,
            IMateriaPrima objMateriaPrima,
            CalculoCostosFeature objCostoMateriaPrima,
            IOptions<ParametrosConfig> objConfig,
            IVentasFacturacionService objVentasFacturacionService
            )
        {
            _objLogger = objLogger;
            _objMateriaPrima = objMateriaPrima;
            _objMotorAsigPrec = new MotorAsignacionPrecios(_objLogger);
            _objCostoMateriaPrima = objCostoMateriaPrima;
            _objConfig = objConfig;
            _objVentasFacturacionService = objVentasFacturacionService;
        }

        public async Task<List<RptVentaVsFactura>> ObtenerReporteVentasVsFacturas(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<RptVentaVsFactura> lstReporteVentFact = new();
            List<DiarioCosto> lstDiarioCost = new();
            List<CostVentUni> lstCostVentUni = new();
            List<FacturaResult> lstVentaLocal = new();
            try
            {
                var lstPesosReales = await _objVentasFacturacionService.ObtenerRepFactPesoRealXRangoFecha(dtFechaInicio, dtFechaFin);
                lstCostVentUni = await ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
                lstVentaLocal = await _objVentasFacturacionService.ObtenerFacturasXRangoFecha(dtFechaInicio, dtFechaFin);
                //var lstMovimientos = await _objVentasFacturacionService.ObtenerTracamAutoXRangoFecha(dtFechaInicio, dtFechaFin);
                //var dicFactu = RepFactPesoRealResult.CrearDicFacturaPorMovimiento(lstPesosReales);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //lstDiarioCost = await _objCostoMateriaPrima.ObtenerDiarioCostoAsync(dtFechaInicio, dtFechaFin);
                //lstReporteVentFact.AddRange(RptVentaVsFactura.CrearListadoVentas(lstPesosReales));
                lstReporteVentFact.AddRange(lstPesosReales.Select(obj => new RptVentaVsFactura(obj)).ToList());
                lstReporteVentFact.AddRange(lstVentaLocal.Where(x => 
                x.strMovTipfac.Contains("FA") && 
                !x.strCliCodigo.Contains("383") &&
                x.strMovProduc.Contains("0")).Select(obj => new RptVentaVsFactura(obj)).ToList());
                //_objMotorAsigPrec.AsignarCostoDiarioVenta(lstDiarioCost, lstReporteVentFact);
                _objMotorAsigPrec.AsignarCostVentUnitDiarioVenta(lstCostVentUni, lstReporteVentFact);
                return lstReporteVentFact;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error Feature : {nameof(OperacionComercialFeature)} funcion : {nameof(ObtenerReporteVentasVsFacturas)} error: " +
                    objException.Message);
                throw;
            }
        }



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
                HashSet<string> hshMovEgr = new() { "EAJ", "EMU", "REPROE", "LB", "UNI", "DIR", "CNE", "SMT" , "EOB" };
                //var lstPesosReales = await _objVentasFacturacionService.ObtenerRepFactPesoRealXRangoFecha(dtFechaInicio, dtFechaFin);
                var trLiqFrs = _objCostoMateriaPrima.ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin);
                var trLiqRpc = _objCostoMateriaPrima.ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin);
                var trInvVal = _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin);
                var trMovInvIng = _objMateriaPrima.IngresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
                var trMovInvEgr = _objMateriaPrima.EgresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
                //var trCostVentEspeciales = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(trLiqFrs, trLiqRpc, trInvVal, trMovInvIng, trMovInvEgr //, trCostVentEspeciales
                    );
                lstLiqFrs = trLiqFrs.Result;
                lstLiqRpc = trLiqRpc.Result;
                lstInvVal = trInvVal.Result;
                lstLbsLotePiso = await _objMateriaPrima.ConsultarLotePiso(
                        lstLiqRpc
                        .Where(x =>  !hshTipEgr.Contains(x.strTipCod) &&  x.dbCostoXSecuencial == 0)
                        .Select(b =>(decimal) b.intLotNumero)
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
                //lstPrecioEsXMov = trCostVentEspeciales.Result;
                lstCostVentUni.AddRange(lstLiqFrs.Select(obj => new CostVentUni(obj)) );
                lstCostVentUni.AddRange(
                    lstLiqRpc
                    .Where(obj =>!(obj.strAgrupacion == "1. RECIBIDO" && hshMovEsp.Contains(obj.strTipCod)))
                    .Select(obj => new CostVentUni(obj))
                 );
                lstCostVentUni.AddRange(lstInvVal/*.Where(obj => obj.dcLibras > 0)*/.Select(obj => new CostVentUni(obj)));
                var dicPrecioEsXMov = PrecioFrsXMov.GenerarDiccionarioCostoXTalla(lstPrecioEsXMov);
                var dicProcesado = MatPrimaReproceso.GenerarDicProcGlobal(lstLiqRpc);
                var dicLiqFrs = LiquidacionResultado.GenerarDiccionarioLbs(lstLiqFrs);
                //var dicProcPromedio = MatPrimaReproceso.GenerarPromedioPonderadoProc(lstLiqRpc);
                var dicInvVal = InventarioVal.GenerarDiccionarioCostoXTalla(lstInvVal);
                //var dicInValProdTall = InventarioVal.GenerarDiccionarioProdXTalla(lstInvVal);
                foreach (var objReg in lstMovInv)
                {
                    if (dicInvVal.TryGetValue(objReg.objLoteProdTalKey, out decimal objPrecioInv) && objReg.dcCostoUnit == 0)
                    {
                        objReg.InicializarCamposCost(objPrecioInv);
                    }
                    else if (dicLiqFrs.TryGetValue(objReg.objLoteProdTalKey, out decimal objPrecioFrs) && objReg.dcCostoUnit == 0)
                    {
                        objReg.InicializarCamposCost(objPrecioFrs);
                    }
                    else if (dicProcesado.TryGetValue(objReg.objLoteProdTalReciKey, out decimal objPrecioProc) && objReg.dcCostoUnit == 0)
                    {
                        objReg.InicializarCamposCost(objPrecioProc);
                    }
                    //if (dicPrecioEsXMov.TryGetValue(objReg.objLoteProdTalKey, out double objPrecio))
                    //{
                    //    objReg.InicializarCamposCost(objPrecio);
                    //}
                    //else if (dicInValProdTall.TryGetValue(objReg.objProdTalKey, out decimal objPrecioInvProdTall) && objReg.dcCostoUnit == 0)
                    //{
                    //    objReg.InicializarCamposCost(objPrecioInvProdTall);
                    //}
                    //else if (dicProcPromedio.TryGetValue(objReg.objProdTalKey, out decimal objPrecioProcPromedio) && objReg.dcCostoUnit == 0)
                    //{
                    //    objReg.InicializarCamposCost(objPrecioProcPromedio);
                    //}
                    //else if (dicRecibido.TryGetValue(objReg.objLoteProdTalReciKey, out decimal objPrecioRec))
                    //{
                    //    objReg.InicializarCamposCost(objPrecioRec);
                    //}
                    //else if (dicLiqFrsPromedio.TryGetValue(objReg.objProdTalKey, out decimal objPrecioLiqFrsPromedio))
                    //{
                    //    objReg.InicializarCamposCost(objPrecioLiqFrsPromedio);
                    //}
                }
                lstCostVentUni.AddRange(lstMovInv.Select(obj => new CostVentUni(obj)) );
                lstCostVentUni.AddRange(lstLbsLotePiso);
                //var lstVentas = await _objVentasFacturacionService.ObtenerFacturasXRangoFecha(dtFechaInicio, dtFechaFin);
                //var lstMovimientos = await _objVentasFacturacionService.ObtenerTracamAutoXRangoFecha(dtFechaInicio, dtFechaFin);
                //var dicFactu = RepFactPesoRealResult.CrearDicFacturaPorMovimiento(lstPesosReales);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //lstDiarioCost = await _objCostoMateriaPrima.ObtenerDiarioCostoAsync(dtFechaInicio, dtFechaFin);
                //lstReporteVentFact.AddRange(RptVentaVsFactura.CrearListadoVentas(lstPesosReales));
                //_objMotorAsigPrec.AsignarCostoDiarioVenta(lstDiarioCost, lstReporteVentFact);
                return lstCostVentUni;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error Feature : {nameof(OperacionComercialFeature)} funcion : {nameof(ConsultarCostoVentaUni)} error: " +
                    objException.Message);
                throw;
            }
        }

    }
}
