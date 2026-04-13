using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface IProcesoParametro
    {
        Task<List<ProcesoResultadoDto>> ObtenerProcesosFrescoConValores(DateOnly fechaCorte);
        Task<List<ProcesoResultadoDto>> ObtenerProcesosReproConValores(DateOnly fechaCorte);
        Task<bool>  RegistrarParamCosteoPfr(List<ProcesoResultadoDto> detalles, DateOnly fechaCorte, string strUsuario);

        Task<List<string>> ConsultarCatalogoXCab(int intCab);
        Task<List<string>> ConsultarCatalogoXDes(string strDescp);

        Task ObtenerCostosProcesosMatPrimPFR(List<LiquidacionResultado> lstLiquidaciones, DateOnly dtFechaFin);
        Task ObtenerCostosProcesosRepro(List<MatPrimaReproceso> lstLiquidaciones, DateOnly dtFechaFin);
    }
}
