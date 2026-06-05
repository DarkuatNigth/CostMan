using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using System.Data;

namespace CostManagement.Infraestructura.Repository.Interface
{
    public interface IExcelExportService
    {
        byte[] ExportarLiquidacionesAExcel(List<LiquidacionResultado> liquidaciones);
        List<InvValDataDto> LeerExcelInvVal(Stream archivoStream);
        Task<DataGeneralResult> DataGeneralExcel(DataGeneralRequest dataGeneralRequest, DataTable dataTable);
        Task<DataGeneralResult> ObtenerReporteExcel(DataGeneralRequest dataGeneralRequest);
    }
}
