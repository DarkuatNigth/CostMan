using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Reglas;
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
            try
            {
                int intAnio = Convert.ToInt16(strAnio);
                int intMes = Convert.ToInt16(strMes);

                DateTime dtFechaCorte = new DateTime(
                    intAnio,
                    intMes,
                    DateTime.DaysInMonth(intAnio, intMes));

                // ================================================================
                // 1. CARGAR UNA SOLA VEZ EL UNIVERSO PRODUCTIVO DEL PERÍODO
                // ================================================================
                DataProcesoParam objData =
                    await _objCostoMateriaPrima.ObtenerParametroProceso(dtFechaCorte);

                // ObtenerCostoProductivo recibe objData ya cargado y NO vuelve a ejecutar
                // ObtenerParametroProceso. Solo consulta configuración Costos + saldos SONG.
                CostoProductivoResultadoDto objCostoActual =
                    await _objOperacionComercial.ObtenerCostoProductivo(
                        intAnio,
                        intMes,
                        objData);

                // BLOQUE TEMPORAL DE DEBUG (PARTICION FISICA UNICA) — mantener comentado.
                //var lstDebugProcesos = objCostoActual.lstProcesosOrigen
                //    .Where(x => x.strPcCodigo == "DES" || x.strPcCodigo == "COP")
                //    .Select(x => new { x.strPcCodigo, x.strProceso, x.strOrigen, x.dcLibrasEntero, x.dcLibrasCola, x.dcLibrasValorAgregado, x.dcDolaresEntero, x.dcDolaresCola, x.dcDolaresValorAgregado })
                //    .ToList();
                //var lstDebugCuentas = objCostoActual.lstCuentas
                //    .Where(x => x.strPcCodigo == "DES" || x.strPcCodigo == "COP")
                //    .Select(x => new { x.strPcCodigo, x.strCuenta, x.strOrigenDistribucion, x.strClasificacionMonto, x.dcMontoEntero, x.dcMontoCola, x.dcMontoVag })
                //    .ToList();

                // Warren depende de objCostoActual.lstProcesosOrigen (costo normal Cola =
                // PFR + RPC; absorción exclusiva PFR Entero), por lo que ya no puede
                // calcularse en paralelo con ObtenerCostoProductivo ni con MotorWarren
                // legacy (que es PFR-only y subestima el costo normal de Cola).
                WarrenResultadoDto? objWarrenGuardado =
                    await _objCostoMateriaPrima.ConsultarWarrenPeriodo(
                        intAnio,
                        intMes,
                        objCostoActual.lstProcesosOrigen ?? new List<CostoProductivoProcesoOrigenDto>(),
                        objData.lstProcesoFrs ?? new List<ProcesoResultadoDto>());

                // ================================================================
                // 2. HIDRATACIÓN = MANO DE OBRA CONTABLE + QUÍMICOS
                // Comentado a pedido de negocio: el monto de químicos no tiene
                // fila propia en detalleContable (Table6), lo que generaba
                // descuadre modal/fila vs. lo persistido. Se retoma cuando se
                // incorpore el agrupado de materiales/químicos a Table6.
                // ================================================================
                //CostoProductivoSnapshotBuilder.SincronizarHidratacion(
                //    objData,
                //    objCostoActual.lstProcesosOrigen);

                // Table5 físico y resumen se construyen después de sincronizar Table1.
                ParamProcVistaResultadoDto objVista =
                    ParamProcVistaBuilder.Construir(objData);

                // ================================================================
                // 3. TABLE6 SALE DEL MISMO SNAPSHOT ACTUAL, NO DEL CIERRE ANTERIOR
                // ================================================================
                List<CostoProductivoDetalleModalDto> lstCostProd =
                    CostoProductivoSnapshotBuilder.ConstruirDetalleActual(
                        objCostoActual.lstCuentas);

                var dtResult = new DataTablesResultDto
                {
                    Table = (objData.lstProcesoFrs ?? new()).AListaDeDiccionarios(),
                    Table1 = (objData.lstProcesoRpc ?? new()).AListaDeDiccionarios(),
                    Table2 = (objData.lstProcesoTarifa ?? new()).AListaDeDiccionarios(),
                    Table3 = (objData.lstLibrasParticion ?? new()).AListaDeDiccionarios(),
                    Table4 = objVista.lstResumen.AListaDeDiccionarios(),
                    Table5 = objVista.lstDetalleEtapa.AListaDeDiccionarios(),

                    // Modal contable actual, exactamente con los importes que se guardarán.
                    Table6 = lstCostProd.AListaDeDiccionarios(),

                    // Warren guardado. Después del primer cierre con esta versión ya fue
                    // calculado sobre el mismo snapshot enviado por el front.
                    Table7 = new List<WarrenPeriodoResumenDto>
                    {
                        WarrenPeriodoResumenDto.Desde(objWarrenGuardado)
                    }.AListaDeDiccionarios(),

                    Table8 = (objWarrenGuardado?.lstDetalle ?? new List<WarrenProcesoDto>())
                        .AListaDeDiccionarios(),

                    // Snapshot por proceso/origen usado por el POST sin reconsultar ETL.
                    Table9 = (objCostoActual.lstProcesosOrigen ?? new())
                        .AListaDeDiccionarios()
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
                        objRequest.strUsuario,
                        objRequest.lstRegistros,
                        objRequest.objResumen);

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




        [HttpPost("guardar-cierre-param-proc")]
        public async Task<IActionResult> GuardarCierreParamProc(
            [FromBody] GuardarCierreParamProcRequest request)
        {
            try
            {
                if (request == null ||
                    !int.TryParse(request.strAnio, out int intAnio) ||
                    !int.TryParse(request.strMes, out int intMes) ||
                    request.dcObjetivoWarren <= 0m)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        blStatus = false,
                        strMensaje = "Año, mes y objetivo Warren son obligatorios.",
                        objData = string.Empty
                    });
                }

                if (request.lstDetalleContable == null || request.lstDetalleContable.Count == 0)
                    throw new InvalidOperationException(
                        "No se recibió el snapshot contable Table6.");

                if (request.lstProcesosOrigen == null || request.lstProcesosOrigen.Count == 0)
                    throw new InvalidOperationException(
                        "No se recibió el snapshot de procesos/origen Table9.");

                if (request.lstParametrosPfr == null || request.lstParametrosPfr.Count == 0)
                    throw new InvalidOperationException(
                        "No se recibió Table PFR.");

                if (request.lstParametrosRpc == null || request.lstParametrosRpc.Count == 0)
                    throw new InvalidOperationException(
                        "No se recibió Table1 RPC.");

                if (request.lstWarrenBase == null || request.lstWarrenBase.Count == 0)
                    throw new InvalidOperationException(
                        "No se recibió la base Warren del snapshot visible.");

                // ================================================================
                // IMPORTANTE: DESDE AQUÍ NO SE CONSULTA PRODUCCIÓN NI SONG.
                // ================================================================

                var objDataSnapshot = new DataProcesoParam
                {
                    lstProcesoFrs = request.lstParametrosPfr,
                    lstProcesoRpc = request.lstParametrosRpc
                };

                List<ProcesoResultadoDto> lstParametros =
                    CierreParamProcBuilder.ConstruirParametros(
                        objDataSnapshot,
                        request.lstProcesosOrigen);

                WarrenResultadoDto objWarren =
                    MotorWarrenSnapshot.Calcular(
                        request.lstWarrenBase,
                        request.dcObjetivoWarren);
                MotorWarren.RegistrarDiagnosticoResultado(_objLogger, "GuardarSnapshot", objWarren); // DIAGNÓSTICO TEMPORAL

                CierreParamProcResultadoDto objResultado =
                    await _objOperacionComercial.GuardarCierreParamProcSnapshot(
                        intAnio,
                        intMes,
                        request.strUsuario,
                        request.lstDetalleContable,
                        request.lstProcesosOrigen,
                        lstParametros,
                        objWarren);

                return Ok(new ApiResponse<CierreParamProcResultadoDto>
                {
                    blStatus = true,
                    strMensaje = "Cierre ParamProc guardado correctamente desde el snapshot consultado.",
                    objData = objResultado
                });
            }
            catch (Exception objException)
            {
                ManejoLog<ProcesoParametroController>.Error(
                    _objLogger,
                    nameof(ProcesoParametroController),
                    nameof(GuardarCierreParamProc),
                    objException);

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al guardar cierre ParamProc: " +
                        (objException.InnerException?.Message ?? objException.Message),
                    objData = string.Empty
                });
            }
        }

        [HttpGet("configuracion-costo-productivo")]
        public async Task<IActionResult> ConfiguracionCostoProductivo()
        {
            try
            {
                CostoProductivoMantenimientoDto resultado =
                    await _objCostoMateriaPrima
                        .ConsultarConfiguracionCostoProductivo();

                return Ok(new ApiResponse<CostoProductivoMantenimientoDto>
                {
                    blStatus = true,
                    strMensaje = "Configuración consultada correctamente.",
                    objData = resultado
                });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametroController>.Error(
                    _objLogger,
                    nameof(ProcesoParametroController),
                    nameof(ConfiguracionCostoProductivo),
                    ex);

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al consultar configuración: " +
                                 (ex.InnerException?.Message ?? ex.Message),
                    objData = string.Empty
                });
            }
        }

        [HttpPost("configuracion-costo-productivo")]
        public async Task<IActionResult> GuardarConfiguracionCostoProductivo(
            [FromBody] GuardarCostoProductivoMantenimientoRequest request)
        {
            try
            {
                bool resultado = await _objCostoMateriaPrima
                    .GuardarConfiguracionCostoProductivo(request);

                return Ok(new ApiResponse<string>
                {
                    blStatus = resultado,
                    strMensaje = "Configuración guardada correctamente.",
                    objData = "OK"
                });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametroController>.Error(
                    _objLogger,
                    nameof(ProcesoParametroController),
                    nameof(GuardarConfiguracionCostoProductivo),
                    ex);

                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al guardar configuración: " +
                                 (ex.InnerException?.Message ?? ex.Message),
                    objData = string.Empty
                });
            }
        }

        #region Diarios de Movimientos

        [HttpGet("diarios-cierre")]
        public async Task<IActionResult> ConsultarDiariosCierre(string strAnio, string strMes)
        {
            try
            {
                if (!int.TryParse(strAnio, out int intAnio) || !int.TryParse(strMes, out int intMes))
                    return BadRequest(new ApiResponse<string>
                    {
                        blStatus = false,
                        strMensaje = "Año y mes son obligatorios.",
                        objData = string.Empty
                    });

                DiariosCierreDto objResultado = await _objCostoMateriaPrima.ConsultarDiariosCierre(intAnio, intMes);
                return Ok(new ApiResponse<DiariosCierreDto>
                {
                    blStatus = true,
                    strMensaje = "Diarios consultados correctamente.",
                    objData = objResultado
                });
            }
            catch (Exception objException)
            {
                ManejoLog<ProcesoParametroController>.Error(_objLogger, nameof(ProcesoParametroController), nameof(ConsultarDiariosCierre), objException);
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al consultar los diarios: " + (objException.InnerException?.Message ?? objException.Message),
                    objData = string.Empty
                });
            }
        }

        [HttpPost("diarios-cierre/confirmar")]
        public async Task<IActionResult> GuardarDiariosCierre([FromBody] GuardarDiariosCierreRequest objRequest)
        {
            try
            {
                if (objRequest == null)
                    return BadRequest(new ApiResponse<string>
                    {
                        blStatus = false,
                        strMensaje = "La solicitud está vacía.",
                        objData = string.Empty
                    });

                DiariosCierreDto objResultado = await _objCostoMateriaPrima.GuardarDiariosCierre(objRequest);
                return Ok(new ApiResponse<DiariosCierreDto>
                {
                    blStatus = true,
                    strMensaje = "Diarios guardados correctamente.",
                    objData = objResultado
                });
            }
            catch (Exception objException)
            {
                ManejoLog<ProcesoParametroController>.Error(_objLogger, nameof(ProcesoParametroController), nameof(GuardarDiariosCierre), objException);
                return BadRequest(new ApiResponse<string>
                {
                    blStatus = false,
                    strMensaje = "Error al guardar los diarios: " + (objException.InnerException?.Message ?? objException.Message),
                    objData = string.Empty
                });
            }
        }

        #endregion
    }
}
