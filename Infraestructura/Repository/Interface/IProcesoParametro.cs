using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public partial interface IProcesoParametro
    {
        #region Parametros Costeo
        Task<List<ProcesoResultadoDto>> ConsultarProcesosFrescoConValores(DateOnly fechaCorte);
        Task<List<ProcesoResultadoDto>> ConsultarProcesosReproConValores(DateOnly fechaCorte);
        Task<List<ProcesoResultadoDto>> ConsultarProcesoTarifa(DateOnly fechaCorte);
        Task<bool> RegistrarParamCosteoPfr(DateOnly fechaCorte, GuardarParametrosRequest objParam);
        Task<List<ProcesoResultadoDto>> ConsultarParametrosWarren(DateOnly fechaCorte);
        Task<decimal?> ConsultarObjetivoWarren(DateOnly fechaCorte);
        Task<List<CostoProcesoParticionDto>> ConsultarCostoProcesoParticion(int anio, int mes);
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

        #region Diarios de Movimientos

        Task<List<DiarioMovimientoCuentaDto>> ConsultarConfiguracionDiarioMovimiento(string? strPcCodigo = null);

        Task<List<DiarioMovimientoPersistenciaDto>> ConsultarDiarioMovimientoPeriodo(int intAnio, int intMes, string? strPcCodigo = null);

        Task<List<DiarioMovimientoPersistenciaDto>> ConsultarCostoVentaSalidaHistorico(int intAnio, int intMes, List<string> lstFacturaKey);

        Task<bool> GuardarDiarioMovimientoPeriodo(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            IEnumerable<string> lstProcesosReemplazar,
            List<DiarioMovimientoPersistenciaDto> lstFilas,
            string strUsuario);

        Task<bool> GuardarCostoVentaSalidaHistorico(
            List<DiarioMovimientoPersistenciaDto> lstFilasDvs,
            string strUsuario);

        Task<List<NotaCreditoRetornoContenedorDto>> ConsultarNotasCreditoRetornoContenedor(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        Task<List<FacturaCostoSalidaDto>> ConsultarFacturasCostoSalida(DateOnly dtFechaInicio, DateOnly dtFechaFin);

        #endregion
    }
}
