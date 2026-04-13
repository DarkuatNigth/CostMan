using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Aplicación.DTos;
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
    [ApiController]
    [Route("[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly CostManagementDbContext _objContext;
        private readonly ILogger<ReportingController> _objLogger;
        private readonly IExcelExportService _excelService;
        private readonly CalculoCostosFeature _objCostoMateriaPrima;
        public ReportingController(
            ILogger<ReportingController> objLogger,
            CalculoCostosFeature objCostoMateriaPrima,
            IExcelExportService excelService,
            CostManagementDbContext objContext)
        {
            _objLogger = objLogger;
            _objCostoMateriaPrima = objCostoMateriaPrima;
            _excelService = excelService;
            _objContext = objContext;
        }

        [HttpGet("materia-prima")]
        public async Task<IActionResult> GetMateriaPrimaReportAsync(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                List<LiquidacionResultado> lstTotalResultados = await _objCostoMateriaPrima.ObtenerLiquidadaValorizada(dtFechaInicio, dtFechaFin);
                List<bool> lstBool = new List<bool> { true };
                var dtResult = new DataTablesResultDto
                {
                    Table = lstTotalResultados.AListaDeDiccionarios()//,
                    //Table1 = lstBool.AListaDeDiccionarios()
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

        [HttpGet("materia-prima-Liq")]
        public async Task<IActionResult> GetMateriaPrimaLiqReportAsync(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                List<LiquidacionResultado> lstTotalResultados = await _objCostoMateriaPrima.ObtenerReporteMateriaPrimaValorizada(dtFechaInicio, dtFechaFin);
                List<bool> lstBool = new List<bool> { true };
                var dtResult = new DataTablesResultDto
                {
                    Table = lstTotalResultados.AListaDeDiccionarios()
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

        [HttpPost("materia-prima")]
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

        [HttpPost("material-empaque")]
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


        [HttpGet("material-empaque")]
        public async Task<IActionResult> ObtenerCostoMaterialEmpaque(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {

            try
            {
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


        [HttpGet("materia-prima-repro")]
        public async Task<IActionResult> ObtenerCostoMateriaPrimaRepro(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {

            try
            {
                var lstTotalResultados = await _objCostoMateriaPrima.ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin);
                List<bool> lstBool = new List<bool> { true };
                var dtResult = new DataTablesResultDto
                {
                    Table = lstTotalResultados.AListaDeDiccionarios()//,
                    //Table1 = lstBool.AListaDeDiccionarios()
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



        [HttpPost("materia-prima-repro")]
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


        [HttpGet("materia-prima-sal")]
        public async Task<IActionResult> ObtenerInventarioValorizado(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {

            try
            {
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
        public IActionResult ObtenerEstructuraInv()
        {

            try
            {
                var lstTotalResultados = new List<InvValDataDto>() { new InvValDataDto()};
                var lstOpciones = _objCostoMateriaPrima.ObtenerDataProcesoParametro();
                var lstFechaCorte = _objCostoMateriaPrima.ObtenerDataFechaCorte().Result;
                var dtResult = new DataTablesResultDto
                {
                    Table = lstTotalResultados.AListaDeDiccionarios(), 
                    Table1 = lstOpciones.AListaDeDiccionarios(), 
                    Table2 = lstFechaCorte.AListaDeDiccionarios() 
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
            List<DiarioCosto> lstDiarioCost;
            DataTable dataTable = new DataTable();
            try
            {

                // Validar que el request no sea nulo
                if (request == null)
                    throw new Exception("El request no puede ser nulo");

                fechaInicio = DateOnly.Parse(request.modelParam[1].value.ToString());
                fechaFin = DateOnly.Parse(request.modelParam[2].value.ToString());
                // Obtén tus datos desde el servicio o repositorio


                switch (request.sp)
                {
                    case "materia-prima":
                        liquidaciones = await
                            _objCostoMateriaPrima.ObtenerReporteMateriaPrimaValorizada(fechaInicio, fechaFin);
                        dataTable = liquidaciones.ADataTable();
                        break;

                    case "material-empaque":
                        materialEmpaque = await
                            _objCostoMateriaPrima.ObtenerReporteMaterialEmpaqueValorizado(fechaInicio, fechaFin);
                        dataTable = materialEmpaque.ADataTable();
                        break;

                    case "materia-prima-repro":
                        materiaPrimaRepro = await
                            _objCostoMateriaPrima.ObtenerReporteMateriaPrimaReproValorizada(fechaInicio, fechaFin);
                        dataTable = materiaPrimaRepro.ADataTable();
                        break;

                    case "materia-prima-sal":
                        var obj = await _objCostoMateriaPrima.ObtenerInventarioValorizado(fechaInicio, fechaFin);
                        //lstDiarioCost = await
                        //    _objCostoMateriaPrima.ObtenerDiarioCostoAsync(fechaInicio, fechaFin);

                        //dataTable = lstDiarioCost.ADataTable();
                        dataTable =  obj.ADataTable();
                        break;

                    default:
                        throw new ArgumentException("Tipo de reporte no válido", nameof(request.sp));
                }


                DataGeneralResult excelBytes = await _excelService.DataGeneralExcel(request, dataTable);

                return File(excelBytes.Data.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadshteetml.sheet",
                $"{request.title}.xlsx");
            }
            catch (Exception objException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al generar Excel: " + objException.Message,
                    objData = ""
                });
            }
        }

    }
    
}
