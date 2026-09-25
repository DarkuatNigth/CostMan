using CostManagement.Aplicación.DTos;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface ICostoProductivoService
    {
        Task<CostoProductivoConfiguracionDbDto> ConsultarConfiguracionCostoProductivo();
        Task<List<SaldoCuentaSongDto>> ConsultarSaldosCuentaSong(int intAnio);

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
    }
}
