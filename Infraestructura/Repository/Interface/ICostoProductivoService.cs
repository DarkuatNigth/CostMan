using CostManagement.Aplicación.DTos;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface ICostoProductivoService
    {
        Task<CostoProductivoConfiguracionDbDto> ConsultarConfiguracionCostoProductivo();
        Task<List<SaldoCuentaSongDto>> ConsultarSaldosCuentaSong(int intAnio);
        // misma resolución de nombres de TbMaecta que el módulo de configuración.
        Task<Dictionary<string, string>> ConsultarNombresCentroAsync(IEnumerable<string> codigos);

        Task<bool> GuardarDistribucionProcesoProductivo(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            List<CostoProductivoCuentaDto> lstRegistros,
            string strUsuario);

        Task<bool> GuardarCierreParamProcAtomico(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            List<CostoProductivoCuentaDto> lstRegistros,
            List<ProcesoResultadoDto> lstParametros,
            WarrenResultadoDto objWarren,
            string strUsuario);

        Task<List<CostoProductivoDetalleModalDto>> ConsultarDetalleCostoProductivoPeriodo(
            int intAnio,
            int intMes);

        Task<AnularValorizacionesResultadoDto> AnularValorizacionesActivasAsync(string tipo, string usuario, int? anio = null, int? mes = null);

        Task PrepararConfiguracionCierreAsync(CierreValorizacionContexto ctx);

        Task<bool> GuardarCierreParamProcSnapshotAtomico(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            List<CostoProductivoDetalleModalDto> lstDetalleContable,
            List<ProcesoResultadoDto> lstParametros,
            WarrenResultadoDto objWarren,
            string strUsuario);

        // Task ValidarParametrosCierreAsync(int intAnio, int intMes); // Revisión 2: devolver identidad para detectar reemplazos.
        Task<string> ValidarParametrosCierreAsync(int intAnio, int intMes);
        // Antes: Task GuardarValorizacionCierreAtomico(int intAnio, int intMes, string strUsuario, CierreValorizacionDto objValorizacion);
        Task GuardarValorizacionCierreAtomico(int intAnio, int intMes, string strUsuario, CierreValorizacionDto objValorizacion, List<InventarioVal>? lstInventarioFinal = null);
    }
}
