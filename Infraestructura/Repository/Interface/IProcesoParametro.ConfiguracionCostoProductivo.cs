using CostManagement.Aplicación.DTos;

namespace CostManagement.Infraestructura.Repository.Interface
{
    // CAMBIO EN ARCHIVO ORIGINAL:
    // public interface IProcesoParametro
    //      ↓
    // public partial interface IProcesoParametro
    //
    // Esta segunda parte evita crear un service nuevo: la configuración queda
    // dentro del mismo IProcesoParametro/ProcesoParametro que ya usa
    // CalculoCostosFeature.
    public partial interface IProcesoParametro
    {
        Task<CostoProductivoMantenimientoDto> ConsultarMantenimientoCostoProductivo();

        Task<ConfiguracionCostoProductivoRuntimeDto>
            ConsultarConfiguracionRuntimeCostoProductivo();

        Task<bool> GuardarMantenimientoCostoProductivo(
            GuardarCostoProductivoMantenimientoRequest request);
    }
}
