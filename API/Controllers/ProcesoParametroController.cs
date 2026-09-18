using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Repository.Services;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Aplicacion.DTos;
using CostManagementService.Aplicacion.Features;
using CostManagementService.Infraestructura.EF_Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.Common;
using static CostManagement.Aplicación.DTos.HaberDistribucionDTO;

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
        private readonly OperacionComercialFeature _objOperacionComercial;

        public ProcesoParametroController(
            ILogger<ProcesoParametroController> objLogger,
            CalculoCostosFeature objCostoMateriaPrima,
            IExcelExportService excelService,
            CostManagementDbContext objContext,
            OperacionComercialFeature objOperacionComercial)
        {
            _objLogger = objLogger;
            _objCostoMateriaPrima = objCostoMateriaPrima;
            _excelService = excelService;
            _objContext = objContext;
            _objOperacionComercial = objOperacionComercial;
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
                var objVista = ParamProcVistaBuilder.Construir(objData);

                lstDataFrs = objData.lstProcesoFrs;
                lstDataRpc = objData.lstProcesoRpc;
                lstDataTarifa = objData.lstProcesoTarifa;
                //var dtResult = DataTablesResultDto.FromList(lstDataProcParam, 0);
                var dtResult = new DataTablesResultDto
                {
                    Table = lstDataFrs.AListaDeDiccionarios(), // Cargamos FRS en Table
                    Table1 = lstDataRpc.AListaDeDiccionarios(), // Cargamos RPC en Table1
                    Table2 = lstDataTarifa.AListaDeDiccionarios(), // Cargamos Tarifa en Table2
                    Table3 = objData.lstLibrasParticion.AListaDeDiccionarios(),  // ← NUEVO: matriz de particiones,
                    Table4 = objVista.lstResumen.AListaDeDiccionarios(),
                    Table5 = objVista.lstDetalleEtapa.AListaDeDiccionarios()
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

        [HttpGet("consol-elec")]
        public async Task<IActionResult> ConsolElec(string strAnio)
        {
            ConsolidadoDTO objData;
            int intAnio;
            try
            {
                if (string.IsNullOrEmpty(strAnio))
                {
                    throw new ArgumentException("El parámetro 'strAnio' no puede estar vacío.");
                }
                intAnio = Convert.ToInt32(strAnio);
                objData = await _objCostoMateriaPrima.GetConsolidadoHaber(intAnio);

                //var dtResult = DataTablesResultDto.FromList(lstDataProcParam, 0);
                var dtResult = new DataTablesResultDto
                {
                    Table = DataTablesResultDto.FromObject(objData),
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

        [HttpGet("param-elec")]
        public async Task<IActionResult> ParamElec(string strAnio, string strMes)
        {
            List<DistribucionCostoDto> objData;
            try
            {
                objData = await _objCostoMateriaPrima.ObtenerDistribucionCostosElectricos(strAnio, strMes);

                //var dtResult = DataTablesResultDto.FromList(lstDataProcParam, 0);
                var dtResult = new DataTablesResultDto
                {
                    Table = objData.Where(x => x.strTipo == "ENLEC").ToList().AListaDeDiccionarios(),
                    Table1 = objData.Where(x => x.strTipo == "REBAS").ToList().AListaDeDiccionarios()
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

        [HttpPost("param-elec")]
        public async Task<IActionResult> GuardarDistribucion([FromBody] List<DistribucionCostoDto> objRegistros)
        {
            bool blEjecuto = false;
            try
            {
                // Validar entrada
                if (objRegistros == null || objRegistros.Count == 0)
                {
                    throw new Exception("La lista de registros está vacía.");
                }

                blEjecuto = await _objCostoMateriaPrima.RegistrarDistribucionCostosElectricos(objRegistros);
                return Ok(new ApiResponse<string>
                {
                    blStatus = blEjecuto,
                    strMensaje = "Datos registrados correctamente",
                    objData = "OK"
                });
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[GuardarDistribucion] Error: {ex.Message}\n{ex.StackTrace}");
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = blEjecuto,
                    strMensaje = $"Error al guardar: {ex.Message}",
                    objData = null
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

        [HttpPost("guardar-warren")]
        public async Task<IActionResult> GuardarWarren([FromBody] GuardarWarrenRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (string.IsNullOrWhiteSpace(request.strAnio) ||
                    string.IsNullOrWhiteSpace(request.strMes))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        blStatus = false,
                        strMensaje = "Año y mes son obligatorios.",
                        objData = ""
                    });
                }

                if (request.dcObjetivoWarren <= 0m)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        blStatus = false,
                        strMensaje = "El objetivo Warren debe ser mayor a cero.",
                        objData = ""
                    });
                }

                WarrenResultadoDto resultado = await _objCostoMateriaPrima.GuardarWarren(request);

                return Ok(new ApiResponse<WarrenResultadoDto>
                {
                    blStatus = true,
                    strMensaje = "Warren registrado correctamente.",
                    objData = resultado
                });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametroController>.Error(
                    _objLogger,
                    nameof(ProcesoParametroController),
                    nameof(GuardarWarren),
                    ex);

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al guardar Warren: " +
                                 (ex.InnerException?.Message ?? ex.Message),
                    objData = ""
                });
            }
        }


        [HttpGet("costo-productivo")]
        public async Task<IActionResult> CostoProductivo(
            string strAnio,
            string strMes)
        {
            try
            {
                if (!int.TryParse(strAnio, out int intAnio) ||
                    !int.TryParse(strMes, out int intMes))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        blStatus = false,
                        strMensaje = "Año y mes son obligatorios y numéricos.",
                        objData = string.Empty
                    });
                }

                CostoProductivoResultadoDto resultado = await _objOperacionComercial.ObtenerCostoProductivo(intAnio, intMes);

                DataTablesResultDto dtResult = new DataTablesResultDto
                {
                    Table = resultado.lstCuentas.AListaDeDiccionarios(),
                    Table1 = new List<CostoProductivoResumenDto>
                {
                    resultado.objResumen
                }.AListaDeDiccionarios()
                };

                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta de costo productivo ejecutada correctamente.",
                    objData = dtResult
                });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametroController>.Error(
                    _objLogger,
                    nameof(ProcesoParametroController),
                    nameof(CostoProductivo),
                    ex);

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al consultar costo productivo: " +
                        (ex.InnerException?.Message ?? ex.Message),
                    objData = string.Empty
                });
            }
        }


        [HttpPost("costo-productivo")]
        public async Task<IActionResult> GuardarCostoProductivo(
            [FromBody] GuardarCostoProductivoRequest objRequest)
        {
            try
            {
                if (objRequest == null ||
                    !int.TryParse(objRequest.strAnio, out int intAnio) ||
                    !int.TryParse(objRequest.strMes, out int intMes))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        blStatus = false,
                        strMensaje = "Año y mes son obligatorios.",
                        objData = string.Empty
                    });
                }

                CostoProductivoResultadoDto resultado =
                    await _objOperacionComercial.GuardarCostoProductivo(
                        intAnio,
                        intMes,
                        objRequest.strUsuario);

                DataTablesResultDto dtResult = new DataTablesResultDto
                {
                    Table = resultado.lstCuentas.AListaDeDiccionarios(),
                    Table1 = new List<CostoProductivoResumenDto>
                {
                    resultado.objResumen
                }.AListaDeDiccionarios()
                };

                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Costo productivo guardado correctamente.",
                    objData = dtResult
                });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametroController>.Error(
                    _objLogger,
                    nameof(ProcesoParametroController),
                    nameof(GuardarCostoProductivo),
                    ex);

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al guardar costo productivo: " +
                        (ex.InnerException?.Message ?? ex.Message),
                    objData = string.Empty
                });
            }
        }


    }
}
