using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.EF_Core;
using CostManagementService.Aplicación.DTos;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface IMateriaPrima
    {
        Task<List<RptGrncLibras>> ObtenerProduccionXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<List<LbsCongelamiento>> ObtenerMatPrimFrsCongeXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<List<CopackingLbs>> ObtenerCopackingLbsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<List<ResumenEstiloLbsDto>> ObtenerResumenEstiloLbsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<List<LiquidacionResultado>> ObtenerMatPrimValFrsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin, bool blValorizada = true);

        Task<List<LiquidacionResultado>> ObtenerMatPrimValRpcsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin, bool blValorizada = true);


        #region Registrar Consultar
        Task<List<LiquidacionResultado>> ObtenerLstMatPrimValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task GuardarMatPrimValorizada(List<LiquidacionResultado> lstLiquidaciones, RequestMatPrimDto objRequest);

        Task<List<MatPrimaReproceso>> ObtenerReproValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task GuardarReproValorizado(List<MatPrimaReproceso> lstMatPrimaReproceso, RequestMatPrimDto objRequest);
        #endregion

        #region Base Inventario Materia Prima Reproceso
        Task<List<TbMateriaPrimaReproValorizada>> ObtenerInfoCostoReproceso(DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task<List<MatPrimaReproceso>> ReporteReproPlanRecibProc(DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task<List<MatPrimaReproceso>> ReporteReproPlanProc(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<ILookup<(string Producto, short Talla), decimal>> ObtenerMatPrimSaldo(List<string> lstCodProd);
        Task<ILookup<LoteRpcKeyReci, decimal>> ObtenerMatPrimSaldo(List<int> lstLiqLote);
       
        Task<List<CostoMovArtDto>> CostoUltMovXItemCod(List<string> lstItemCod, DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task<List<PrecioFrsXMov>> ObtenerPrecioFrsSinTallaXMovCam(List<long> lstLiqLotes/*, bool blUniCola = false*/);
        Task<List<RptCongInd>> ObtenerTipProcXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task<List<PrecioFrsXMov>> ObtenerConsumoMovLiqOtroProc(List<long> lstLiqLote);
        #endregion



        Task<List<SaldoBodegaDto>> ReporteSaldoInventario(DateOnly dtFechaCorte);


        Task ObtenerDatosProd(List<InvValDataDto> lstInvVal, string dtFechaCorte);

        Task CrearSaldoMatPrimExcel(List<InvValDataDto> lstInvVal);
        Task CrearInvMatPrimExcel(List<InvValDataDto> lstInvVal, RequestDataDto objRequest);

        #region Costo Material de Empaque 2
        Task<List<CostoMatEmpProdXCietunDto>> ObtenerCostMatEmpFrsProdXLiq(List<decimal> lstLote);
        Task<List<CostoMatEmpProdXCietunDto>> ObtenerCostMatEmpRpcProdXLiq(List<decimal> lstLote);
        Task<ConcurrentDictionary<string, string>> ConsultarItemEtiqueta();
        Task<ConcurrentDictionary<string, string>> ConsultarItemMasterCajita();
        Task ObtenerCostoPromBoditeXFichaTecnica(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun, DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task ObtenerCostoPromMov1XFichaTecnica(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun, DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task ObtenerCostoUltConsuMov2XFichaTecnica(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun, DateOnly dtFechaInicio, DateOnly dtFechaFin);
        #endregion

        Task<List<InventarioVal>> ConsultarInvValorizado(DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task<List<DateOnly>> ConsultarFechaCorteInv();
        Task<List<DiarioCosto>> ObtenerMovimientosAsync(DateOnly dtFechaInicio,DateOnly dtFechaFin);

        Task<List<InventarioVal>> ConsultarInvValBodite(DateOnly dtFechaCorte, string strTipoInv);


        Task<List<DiarioCosto>> EgresosInvXrangoFecha(DateOnly dtFechaInicio,DateOnly dtFechaFin);

        Task<List<DiarioCosto>> IngresosInvXrangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<List<PrecioFrsXMov>> ObtenerPrecioFrsSinTallaXMovCam(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<List<InfoTalProd>> ObtenerInfoCodTal();

    }
}
