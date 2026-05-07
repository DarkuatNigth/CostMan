using CostManagementService.Infraestructura.EF_Core.SONG;

namespace CostManagementService.Infraestructura.Repository.Interface
{
    public interface IVentasFacturacionService
    {

        Task<List<TracamAutoResult>> ObtenerSpTracamAutoXRangoFecha(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin);
        Task<List<RepFactPesoRealResult>> ObtenerRepFactPesoRealXRangoFecha(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin,
            string strTipo = "P");
        Task<List<FacturaResult>> ObtenerFacturasXRangoFecha(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin,
            string? strMov = null);
        Task<List<TracamAutoResult>> ObtenerTracamAutoXRangoFecha(
          DateOnly dtFechaInicio,
          DateOnly dtFechaFin);
    }
}
