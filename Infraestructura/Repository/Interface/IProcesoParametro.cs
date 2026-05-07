using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface IProcesoParametro
    {
        #region Parametros Costeo
        Task<List<ProcesoResultadoDto>> ConsultarProcesosFrescoConValores(DateOnly fechaCorte);
        Task<List<ProcesoResultadoDto>> ConsultarProcesosReproConValores(DateOnly fechaCorte);
        Task<List<ProcesoResultadoDto>> ConsultarProcesoTarifa(DateOnly fechaCorte);
        Task<bool>  RegistrarParamCosteoPfr( DateOnly fechaCorte, GuardarParametrosRequest objParam);
        #endregion

        #region Catalogo Parametrizacion
        Task<List<string>> ConsultarCatalogoXCab(int intCab);
        Task<List<string>> ConsultarCatalogoXDes(string strDescp);
        #endregion
    }
}
