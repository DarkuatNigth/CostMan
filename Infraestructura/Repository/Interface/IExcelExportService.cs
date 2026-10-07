using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using System.Data;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface IExcelExportService
    {
        byte[] ExportarLiquidacionesAExcel(List<LiquidacionResultado> liquidaciones);
        List<InvValDataDto> LeerExcelInvVal(Stream archivoStream);
        Task<DataGeneralResult> DataGeneralExcel(DataGeneralRequest dataGeneralRequest, DataTable dataTable, CostosUnitarios objCostUni = null);
        Task<DataGeneralResult> DataGeneralExcelHojas(DataGeneralRequest dataGeneralRequest, List<(string strNombreHoja, DataTable objTabla)> lstHojas);
        Task<DataGeneralResult> ObtenerReporteExcel(DataGeneralRequest dataGeneralRequest);
        Task<DataGeneralResult> DataDiariosCierreExcel(DataGeneralRequest request,DiariosCierreDto cierre);
    }
}
