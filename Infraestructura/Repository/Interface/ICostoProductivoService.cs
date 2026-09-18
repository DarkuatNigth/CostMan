using CostManagement.Aplicación.DTos;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface ICostoProductivoService
    {
        // ConnectionCostos: devuelve 2 resultsets (cuentas + aplicaciones).
        Task<CostoProductivoConfiguracionDbDto> ConsultarConfiguracionCostoProductivo();

        // ConnectionSong: maestro contable + saldos del año.
        Task<List<SaldoCuentaSongDto>> ConsultarSaldosCuentaSong(int intAnio);

        Task<bool> GuardarDistribucionProcesoProductivo(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            List<CostoProductivoCuentaDto> lstRegistros,
            string strUsuario);
    }
}
