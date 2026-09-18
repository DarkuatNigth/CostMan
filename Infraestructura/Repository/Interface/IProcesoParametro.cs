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
        Task<List<ProcesoResultadoDto>> ConsultarParametrosWarren(DateOnly fechaCorte);
        Task<bool> RegistrarParametrosWarren(
            DateOnly fechaCorte,
            WarrenResultadoDto objWarren,
            string strUsuario);

        #endregion

        #region Catalogo Parametrizacion
        Task<List<string>> ConsultarCatalogoXCab(int intCab);
        Task<List<string>> ConsultarCatalogoXDes(string strDescp);
        #endregion

        #region Parametrizacion Costos Electricos

        Task<List<DistribucionCostoDto>> ConsultarDistribucion(int anio, int mes);

        Task<bool> CrearOActualizarDistribucion(List<DistribucionCostoDto> objRegistros);

        Task<List<HaberDistribucionDTO>> GetHaberesDistribucion(int anio);
        #endregion
    }
}
