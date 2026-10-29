using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Aplicacion.DTos;
using CostManagementService.Aplicación.DTos;
using CostManagementService.Aplicacion.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;

namespace CostManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly ILogger<ReportingController> _objLogger;
        private readonly IExcelExportService _excelService;
        private readonly CalculoCostosFeature _objCostoMateriaPrima;
        private readonly OperacionComercialFeature _objOperacionComercial;
        public ReportingController(
            ILogger<ReportingController> objLogger,
            CalculoCostosFeature objCostoMateriaPrima,
            OperacionComercialFeature objOperacionComercial,
            IExcelExportService excelService)
        {
            _objLogger = objLogger;
            _objCostoMateriaPrima = objCostoMateriaPrima;
            _objOperacionComercial = objOperacionComercial;
            _excelService = excelService;
        }

        [HttpPost("materia-prima")]
        public async Task<IActionResult> GetMateriaPrimaReportAsync([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;
                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);
                List<LiquidacionResultado> lstTotalResultados = await _objCostoMateriaPrima.ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin);
                CostosUnitarios objCostUni = await _objCostoMateriaPrima.ObtenerValProcesoFrs(dtFechaInicio);
                List<bool> lstBool = new List<bool> { true };
                var dtResult = new DataTablesResultDto
                {
                    Table = lstTotalResultados.AReporteMateriaPrima(),
                    Table3 = DataTablesResultDto.FromObject(objCostUni)
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
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al ejecutar la consulta: " + objException.Message,
                    objData = ""
                });
            }
        }

        [HttpPost("materia-prima-Liq")]
        public async Task<IActionResult> GetMateriaPrimaLiqReportAsync([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);

                var objLibro = await _objOperacionComercial.ConsultarCostoVentaUniLibro(dtFechaInicio, dtFechaFin);
                List<bool> lstBool = new List<bool> { true };
                var dtResult = DataTablesResultDto.FromList(objLibro.lstCostoGlobal, 0);
                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });
            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al ejecutar la consulta: " + objException.Message,
                    objData = ""
                });
            }
        }

        [HttpPost("materia-prima/grabar")]
        public async Task<IActionResult> PostMateriaPrimaReportAsync([FromBody] RequestMatPrimDto objRequest)
        {
            try
            {
                 await _objCostoMateriaPrima.RegistrarMpFrsValorizada(objRequest);

                return Ok(new ApiResponse<EmptyResult>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = Empty
                });
            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al ejecutar la consulta: " + objException.Message,
                    objData = ""
                });
            }
        }

        [HttpPost("material-empaque/grabar")]
        public async Task<IActionResult> ActualizarCostoMaterialEmpaque(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {

            try
            {
                await _objCostoMateriaPrima.ProcesarDataMaterialEmpaque(dtFechaInicio, dtFechaFin);

                return Ok(new ApiResponse<EmptyResult>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = Empty
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al actualizar los datos: " + objException.Message,
                    objData = ""
                });
            }
        }


        [HttpPost("material-empaque")]
        public async Task<IActionResult> ObtenerCostoMaterialEmpaque([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);

                var lstTotalResultados = await _objCostoMateriaPrima.ObtenerReporteMaterialEmpaqueValorizado(dtFechaInicio, dtFechaFin);

                var dtResult = DataTablesResultDto.FromList(lstTotalResultados, 0);
                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }



        [HttpPost("material-empaque-auditoria")]
        public async Task<IActionResult> AuditarCostoMaterialEmpaque([FromBody] MaterialEmpaqueAuditoriaRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);
                string? strProducto = objReq.strProducto;
                int? intLote = objReq.intLote;
                bool blSoloErrores = objReq.blSoloErrores;

                var resultado = await _objCostoMateriaPrima.AuditarMaterialEmpaque(
                    dtFechaInicio,
                    dtFechaFin,
                    strProducto,
                    intLote,
                    blSoloErrores);

                var dtResult = new DataTablesResultDto
                {
                    Table = resultado.lstResumen.AListaDeDiccionarios(),
                    Table1 = resultado.lstDetalle.AListaDeDiccionarios()
                };

                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Auditoría de material de empaque ejecutada correctamente",
                    objData = dtResult
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error en auditoría de material de empaque: " + ex.Message,
                    objData = ""
                });
            }
        }

        [HttpPost("materia-prima-repro")]
        public async Task<IActionResult> ObtenerCostoMateriaPrimaRepro([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);

                var lstTotalResultados = await _objCostoMateriaPrima.ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin);
                CostosUnitarios objCostUni = await _objCostoMateriaPrima.ObtenerValProcesoRpc(dtFechaInicio);
                List<bool> lstBool = new List<bool> { true };
                var dtResult = new DataTablesResultDto
                {
                    Table = lstTotalResultados.AReporteMateriaPrima(),
                    Table3 = DataTablesResultDto.FromObject(objCostUni)
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
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }



        [HttpPost("materia-prima-repro/grabar")]
        public async Task<IActionResult> CrearCostoMatReproAsync([FromBody] RequestMatPrimDto objRequest)
        {
            try
            {
                await _objCostoMateriaPrima.RegistrarMpRprValorizado(objRequest);

                return Ok(new ApiResponse<EmptyResult>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = Empty
                });
            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al ejecutar la consulta: " + objException.Message,
                    objData = ""
                });
            }
        }


        [HttpPost("materia-prima-sal")]
        public async Task<IActionResult> ObtenerInventarioValorizado([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);

                var lstTotalResultados = 
                //await _objCostoMateriaPrima.ObtenerDiarioCostoAsync(dtFechaInicio, dtFechaFin);
                await _objCostoMateriaPrima.ObtenerInventarioValorizado(dtFechaInicio, dtFechaFin);
                var dtResult = DataTablesResultDto.FromList(lstTotalResultados, 0);
                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }

        [HttpGet("inv-estructura")]
        public async Task<IActionResult> ObtenerEstructuraInv()
        {

            try
            {
                var lstTotalResultados = new List<InvValDataDto>() { new InvValDataDto()};
                var lstOpciones = _objCostoMateriaPrima.ObtenerDataProcesoParametro();
                var lstFechaCorte = await _objCostoMateriaPrima.ObtenerDataFechaCorte();
                var lstInfoProdTal = await _objCostoMateriaPrima.ObtenerInfoEquiValTalla();
                var dtResult = new DataTablesResultDto
                {
                    Table = lstTotalResultados.AListaDeDiccionarios(), 
                    Table1 = lstOpciones.AListaDeDiccionarios(), 
                    Table2 = lstFechaCorte.AListaDeDiccionarios(),
                    Table3 = lstInfoProdTal.AListaDeDiccionarios()
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
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }

        [HttpPost("inv-val")]
        public async Task<IActionResult> ObtenerCostoMateriaPrimaSaldo([FromBody] RequestDataDto objRequest)
        {

            try
            {

                 await _objCostoMateriaPrima.CrearRegistroInv(objRequest);

                return Ok(new ApiResponse<EmptyResult>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = Empty
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }



        [HttpPost("diario-mov")]
        public async Task<IActionResult> ObtenerDiarioMovimiento([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);


                List<DiarioCosto> lstResult = await _objOperacionComercial.ConsultarDiarioMovimientoHojas(dtFechaInicio, dtFechaFin);
                var dtResult = DataTablesResultDto.FromList(lstResult, 0);

                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }



        [HttpPost("saldo-inv")]
        public async Task<IActionResult> ObtenerSaldoInventario([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);


                List<DiarioCosto> lstResult = await _objCostoMateriaPrima.ObtenerDiarioCostoAsync(dtFechaInicio, dtFechaFin);
                var dtResult = DataTablesResultDto.FromList(lstResult, 0);

                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }


        [HttpPost("diario-costo-venta")]
        public async Task<IActionResult> ObtenerVentasVsFacturas([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);


                var lstResult = 
                    await _objOperacionComercial.ObtenerReporteVentasVsFacturas(dtFechaInicio, dtFechaFin);
                var dtResult = DataTablesResultDto.FromList(lstResult, 0);

                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }


        [HttpPost("costo-venta-uni")]
        public async Task<IActionResult> ObtenerCostoVentaUni([FromBody] RangoFechasRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);


                var lstResult =
                    await _objOperacionComercial.ConsultarCostoVentaUniDisponible(dtFechaInicio, dtFechaFin);
                var dtResult = DataTablesResultDto.FromList(lstResult.lstDisponibleLote, 0);

                return Ok(new ApiResponse<DataTablesResultDto>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = dtResult
                });

            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }


        /// <summary>
        /// CodProd 6550 / Talla 26/30 en Costo de Venta Unitario. Candidato a
        /// eliminar una vez cerrada la investigación.
        /// </summary>
        [HttpPost("diagnostico-disponible-lote")]
        public async Task<IActionResult> DiagnosticoDisponibleLote([FromBody] DiagnosticoDisponibleLoteRequest objReq)
        {
            DateOnly dtFechaInicio, dtFechaFin;
            try
            {
                var objError = ValidarRangoFechas(objReq);
                if (objError != null)
                    return objError;

                dtFechaInicio = DateOnly.FromDateTime(objReq.dtFechaInicio);
                dtFechaFin = DateOnly.FromDateTime(objReq.dtFechaFin);
                int intLote = objReq.intLote;
                int intCodProd = objReq.intCodProd;
                string strTalla = objReq.strTalla;

                object objResultado = await _objOperacionComercial.DiagnosticoDisponibleLote(
                    dtFechaInicio, dtFechaFin, intLote, intCodProd, strTalla);

                return Ok(new ApiResponse<object>
                {
                    blStatus = true,
                    strMensaje = "Consulta ejecutada correctamente",
                    objData = objResultado
                });
            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al obtener la informacion: " + objException.Message,
                    objData = ""
                });
            }
        }


        private BadRequestObjectResult? ValidarRangoFechas(RangoFechasRequest? objReq)
        {
            string? strMensaje = objReq == null
                ? "El request no puede ser nulo"
                : objReq.dtFechaInicio == default || objReq.dtFechaFin == default
                    ? "dtFechaInicio y dtFechaFin son requeridas y deben ser fechas válidas"
                    : objReq.dtFechaInicio > objReq.dtFechaFin
                        ? "dtFechaInicio no puede ser mayor que dtFechaFin"
                        : null;

            if (strMensaje == null)
                return null;

            return BadRequest(new ApiResponse<string>
            {
                blStatus = false,
                strMensaje = strMensaje,
                objData = ""
            });
        }

        [HttpPost("exportar-excel")]
        public async Task<IActionResult> ExportarLiquidacionesExcel([FromBody] DataGeneralRequest request)
        {
            // Parsear las fechas desde string a DateOnly
            DateOnly fechaInicio;
            DateOnly fechaFin;

            List<LiquidacionResultado> liquidaciones;
            List<CostoMatEmpaDto> materialEmpaque;
            List<MatPrimaReproceso> materiaPrimaRepro;
            List<InvValDataDto> invValDataDtos;
            List<RptVentaVsFactura> lstReportVent;
            List<DiarioCosto> lstDiarioCost;
            DataTable dataTable = new DataTable();
            DataGeneralResult excelBytes = null;
            CostosUnitarios objCostUni = null;
            try
            {

                // Validar que el request no sea nulo
                if (request == null)
                    throw new Exception("El request no puede ser nulo");

                // Cambiar los índices según el orden real en que los envías desde Angular
                int indexFin = -1; // Guardará la posición que nos corresponde buscar para fechaFin

                // 1. Determinar fechaInicio y establecer qué índice buscar para fechaFin
                if (request?.modelParam != null && request.modelParam.Count > 0 &&
                    DateOnly.TryParse(request.modelParam[0]?.value?.ToString(), out DateOnly f0))
                {
                    fechaInicio = f0;
                    indexFin = 1; // Si inicio estuvo en [0], la fecha fin debería ser [1]
                }
                else if (request?.modelParam != null && request.modelParam.Count > 1 &&
                         DateOnly.TryParse(request.modelParam[1]?.value?.ToString(), out DateOnly f1))
                {
                    fechaInicio = f1;
                    indexFin = 2; // Si inicio estuvo en [1], la fecha fin debería ser [2]
                }
                else
                {
                    fechaInicio = DateOnly.FromDateTime(DateTime.Today);
                    indexFin = 1;
                }

                // 2. Asignar fechaFin usando el índice calculado
                if (request?.modelParam != null && request.modelParam.Count > indexFin &&
                    DateOnly.TryParse(request.modelParam[indexFin]?.value?.ToString(), out DateOnly ff))
                {
                    fechaFin = ff;
                }
                else
                {
                    // Si no encuentra la fecha fin en esa posición, usa fechaInicio como fallback
                    fechaFin = fechaInicio;
                }
                // Obtén tus datos desde el servicio o repositorio


                switch (request.sp)
                {
                    case "materia-prima":
                        liquidaciones = await
                            _objCostoMateriaPrima.ObtenerLiquidacionValorizada(fechaInicio, fechaFin);
                        objCostUni = await _objCostoMateriaPrima.ObtenerValProcesoFrs(fechaInicio);
                        dataTable = liquidaciones.ATablaReporteMateriaPrima();
                        break;

                    case "materia-prima-Liq":
                        var objLibroLiq = await _objOperacionComercial.ConsultarCostoVentaUniLibro(fechaInicio, fechaFin);
                        dataTable = objLibroLiq.lstCostoGlobal.ADataTable();
                        break;

                    case "material-empaque":
                        materialEmpaque = await
                            _objCostoMateriaPrima.ObtenerReporteMaterialEmpaqueValorizado(fechaInicio, fechaFin);
                        dataTable = materialEmpaque.ADataTable();
                        break;

                    case "materia-prima-repro":
                        materiaPrimaRepro = await
                            _objCostoMateriaPrima.ObtenerReporteMateriaPrimaReproValorizada(fechaInicio, fechaFin);
                        objCostUni = await _objCostoMateriaPrima.ObtenerValProcesoRpc(fechaInicio);
                        dataTable = materiaPrimaRepro.ATablaReporteMateriaPrima();
                        break;

                    case "diario-mov":
                        lstDiarioCost = await _objOperacionComercial.ConsultarDiarioMovimientoHojas(fechaInicio, fechaFin);
                        dataTable = lstDiarioCost.ADataTable();
                        break;

                    case "diario-costo-venta":
                        lstReportVent = await
                            _objOperacionComercial.ObtenerReporteVentasVsFacturas(fechaInicio, fechaFin);
                        dataTable = lstReportVent.ADataTable();
                        break;

                    case "materia-prima-sal":
                        var obj = await _objCostoMateriaPrima.ObtenerInventarioValorizado(fechaInicio, fechaFin);
                        dataTable =  obj.ADataTable();
                        break;

                    case "costo-productivo":
                        {

                            CostoProductivoResultadoDto resultado =
                                await _objOperacionComercial.ObtenerCostoProductivo(
                                    fechaInicio.Year,
                                    fechaInicio.Month);

                            List<CostoProductivoExcelDto> filas =
                                resultado.lstCuentas
                                    .Select(CostoProductivoExcelDto.Crear)
                                    .ToList();

                            if (filas.Count == 0)
                            {
                                throw new InvalidOperationException(
                                    "No existen datos para exportar.");
                            }

                            dataTable = filas.ADataTable();
                            request.columnas = CostoProductivoExcelDto.Columnas;

                            break;
                        }

                    case "diarios-cierre":
                        {
                            DiariosCierreDto objCierre =
                                await _objCostoMateriaPrima
                                    .ConsultarDiariosCierre(
                                        fechaInicio.Year,
                                        fechaInicio.Month);

                            if (objCierre == null)
                            {
                                throw new InvalidOperationException(
                                    "No se obtuvo información de diarios para el período.");
                            }

                            excelBytes =
                                await _excelService
                                    .DataDiariosCierreExcel(
                                        request,
                                        objCierre);

                            break;
                        }

                    case "costo-venta-uni":
                        var objLibroCostoVenta = await _objOperacionComercial.ConsultarCostoVentaUniLibro(fechaInicio, fechaFin);
                        // cada hoja expone únicamente las columnas definidas en el diseño.
                        var objTablaCostoDetallado = objLibroCostoVenta.lstCostoDetallado.ADataTable();
                        objTablaCostoDetallado.Columns.Remove("Movimiento");
                        var objTablaEgresosVenta = objLibroCostoVenta.lstEgresosVenta.ADataTable();
                        objTablaEgresosVenta.Columns.Remove("Movimiento");
                        var objTablaOtrosCamara = objLibroCostoVenta.lstOtrosEgresosCamara.ADataTable();
                        objTablaOtrosCamara.Columns.Remove("Movimiento");
                        excelBytes = await _excelService.DataGeneralExcelHojas(request, new List<(string, DataTable)>
                        {
                            ("Costo Detallado", objTablaCostoDetallado),
                            ("Costo Global", objLibroCostoVenta.lstCostoGlobal.ADataTable()),
                            ("Egresos Venta", objTablaEgresosVenta),
                            ("Otros Egresos Camara", objTablaOtrosCamara),
                            ("Inventario Final", objLibroCostoVenta.lstInventarioFinal.ADataTable())
                        });
                        break;

                    default:
                        excelBytes = await  _excelService.ObtenerReporteExcel(request);
                        break;
                        //throw new ArgumentException("Tipo de reporte no válido", nameof(request.sp));
                }

                if (excelBytes == null)
                {
                    excelBytes = await _excelService.DataGeneralExcel(request, dataTable, objCostUni!);

                }
                return File(excelBytes.Data.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadshteetml.sheet",
                $"{request.title}.xlsx");
            }
            catch (Exception objException)
            {
                string strMessage = objException.InnerException?.Message ?? objException.Message;
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al generar Excel: " + strMessage,
                    objData = ""
                });
            }
        }

    }
    
}
