using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface ICostoMaterialEmpaque
    {
        Task CrearRegistroCab(
            LiquidacionResultado liquidacion,
            CostoMatEmpaDto empaque,
            string usuario,
            string equipo);
        Task CrearRegistroDet(
            int cabeceraId,
            List<CostoMatEmpaDto> detalles,
            string usuario,
            string equipo);

        Task CrearCostoEmpaqueCompleto( List<CostoMatEmpProdXCietunDto> lstCostosEmpaque, string usuario, string equipo);


        Task ObtenerCostoMaterialEmpaqueXLiqProd(List<LiquidacionResultado> lstResultado);

        Task ObtenerCostoMaterialEmpaqueXLiqProd(List<MatPrimaReproceso> lstMatPrimaReproceso);

        Task<List<CostoMatEmpaDto>> ObtenerCostoEmpaqueXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin);
        Task<List<CostoMatEmpaDto>> ObtenerCostoEmpaqueXLote(List<int> lstLotesFrsRpc);
    }
}
