using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagementService.Aplicacion.DTos;
using CostManagementService.Infraestructura.Repository.Interface;
using Microsoft.Extensions.Options;

namespace CostManagementService.Aplicacion.Features
{
    public class OperacionComercialFeature
    {
        private readonly ILogger<CalculoCostosFeature> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        private readonly IVentasFacturacionService _objVentasFacturacionService;

        public OperacionComercialFeature(
            ILogger<CalculoCostosFeature> objLogger,
            IOptions<ParametrosConfig> objConfig,
            IVentasFacturacionService objVentasFacturacionService
            )
        {
            _objLogger = objLogger;
            _objConfig = objConfig;
            _objVentasFacturacionService = objVentasFacturacionService;
        }

        public async Task ObtenerReporteVentasVsFacturas(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                var lstVentas = await _objVentasFacturacionService.ObtenerFacturasXRangoFecha(dtFechaInicio, dtFechaFin);
                var lstMovimientos = await _objVentasFacturacionService.ObtenerTracamAutoXRangoFecha(dtFechaInicio, dtFechaFin);
                var lstPesosReales = await _objVentasFacturacionService.ObtenerRepFactPesoRealXRangoFecha(dtFechaInicio, dtFechaFin);

                List<RptVentaVsFactura> lstReporteVentFact = RptVentaVsFactura.CrearListadoVentas(lstPesosReales);


            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error Feature : {nameof(OperacionComercialFeature)} funcion : {nameof(ObtenerReporteVentasVsFacturas)} error: " +
                    objException.Message);
                throw;
            }
        }


    }
}
