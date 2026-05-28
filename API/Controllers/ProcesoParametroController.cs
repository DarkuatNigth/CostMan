using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Repository.Services;
using CostManagement.Infraestructura.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.Common;

namespace CostManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProcesoParametroController : ControllerBase
    {
        private readonly CostManagementDbContext _objContext;
        private readonly ILogger<ProcesoParametroController> _objLogger;
        private readonly IExcelExportService _excelService;
        private readonly CalculoCostosFeature _objCostoMateriaPrima;
        public ProcesoParametroController(
            ILogger<ProcesoParametroController> objLogger,
            CalculoCostosFeature objCostoMateriaPrima,
            IExcelExportService excelService,
            CostManagementDbContext objContext)
        {
            _objLogger = objLogger;
            _objCostoMateriaPrima = objCostoMateriaPrima;
            _excelService = excelService;
            _objContext = objContext;
        }

        [HttpGet("param-proc-data")]
        public IActionResult ParamProcData()
        {
            try
            {
                var lstDataProcParam = _objCostoMateriaPrima.ObtenerDataProcesoParametro();


                return Ok(new ApiResponse<List<DataProcesoParamDto>>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = lstDataProcParam
                });
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[ProcesoParametroController].[Obtener] Ocurrio un error: {objException.Message}");

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al ejecutar la consulta: " + objException.Message,
                    objData = ""
                });
            }
        }

        [HttpGet("param-proc")]
        public async Task<IActionResult> ParamProc(string strAnio, string strMes)
        {
            List<ProcesoResultadoDto> lstDataTarifa, lstDataFrs, lstDataRpc;
            DataProcesoParam objData;
            try
            {
                DateTime dtFechaCorte = new DateTime(Convert.ToInt16(strAnio), Convert.ToInt16(strMes), DateTime.DaysInMonth(Convert.ToInt16(strAnio), Convert.ToInt16(strMes)));
                objData = await _objCostoMateriaPrima.ObtenerParametroProceso(dtFechaCorte);

                lstDataFrs = objData.lstProcesoFrs;
                lstDataRpc = objData.lstProcesoRpc;
                lstDataTarifa = objData.lstProcesoTarifa;
                //var dtResult = DataTablesResultDto.FromList(lstDataProcParam, 0);
                var dtResult = new DataTablesResultDto
                {
                    Table = lstDataFrs.AListaDeDiccionarios(), // Cargamos FRS en Table
                    Table1 = lstDataRpc.AListaDeDiccionarios(), // Cargamos RPC en Table1
                    Table2 = lstDataTarifa.AListaDeDiccionarios() // Cargamos Tarifa en Table2
                };
                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[ProcesoParametroController].[Obtener] Ocurrio un error: {objException.Message}");

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al ejecutar la consulta: " + objException.Message,
                    objData = ""
                });
            }
        }


        [HttpPost("guardar-param-pfr")]
        public async Task<IActionResult> GuardarParamProcPfr([FromBody] GuardarParametrosRequest objGuardarParam)
        {
            try
            {

                if (objGuardarParam.LstValores == null || !objGuardarParam.LstValores.Any())
                {
                    return BadRequest(new ApiResponse<string> { blStatus = false, strMensaje = "La lista de valores está vacía." });
                }

                // 2. Calcular fecha de corte: Último día del mes/año recibido
                int anio = Convert.ToInt32(objGuardarParam.strAnio);
                int mes = Convert.ToInt32(objGuardarParam.strMes);
                DateTime dtFechaCorte = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));
                //DateOnly fechaCorte = DateOnly.FromDateTime(dtFechaCorte);

                // 3. Llamar a la lógica de negocio (Feature o Service)
                // Nota: Asegúrate de que registrarParametros esté expuesto en tu Feature
                bool resultado = await _objCostoMateriaPrima.RegistrarParamProcPfr(dtFechaCorte, objGuardarParam);

                if (resultado)
                {
                    return Ok(new ApiResponse<string>
                    {
                        blStatus = true,
                        strMensaje = "Datos registrados correctamente",
                        objData = "OK"
                    });
                }

                return BadRequest(new ApiResponse<string> { blStatus = false, strMensaje = "No se pudo completar el registro." });
            }
            catch (Exception objException)
            {
                string strMessage = objException.InnerException?.Message ?? objException.Message;
                ManejoLog<ProcesoParametroController>.Error(_objLogger, nameof(ProcesoParametroController), nameof(GuardarParamProcPfr), objException);
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al guardar: " + strMessage
                });
            }
        }
    }
}
