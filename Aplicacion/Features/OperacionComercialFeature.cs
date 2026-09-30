using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagement.Dominio.Reglas.Iqf;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagementService.Aplicacion.DTos;
using CostManagementService.Infraestructura.EF_Core.SONG;
using CostManagementService.Infraestructura.Repository.Interface;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace CostManagementService.Aplicacion.Features
{
    public class OperacionComercialFeature
    {
        private readonly ILogger<CalculoCostosFeature> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        private readonly IVentasFacturacionService _objVentasFacturacionService;
        private readonly CalculoCostosFeature _objCostoMateriaPrima;
        private readonly IMateriaPrima _objMateriaPrima;
        private readonly MotorAsignacionPrecios _objMotorAsigPrec;
        private readonly MotorDisponibleInventario _objMotorDisponibleInventario;
        private readonly ICostoProductivoService _objCostoProductivoService;

        public OperacionComercialFeature(
            ILogger<CalculoCostosFeature> objLogger,
            IMateriaPrima objMateriaPrima,
            CalculoCostosFeature objCostoMateriaPrima,
            IOptions<ParametrosConfig> objConfig,
            IVentasFacturacionService objVentasFacturacionService,
            ICostoProductivoService objCostoProductivoService
            )
        {
            _objLogger = objLogger;
            _objMateriaPrima = objMateriaPrima;
            _objMotorAsigPrec = new MotorAsignacionPrecios(_objLogger);
            _objCostoMateriaPrima = objCostoMateriaPrima;
            _objConfig = objConfig;
            _objVentasFacturacionService = objVentasFacturacionService;
            _objCostoProductivoService = objCostoProductivoService;
            _objMotorDisponibleInventario = new MotorDisponibleInventario();
        }

        public async Task<List<RptVentaVsFactura>> ObtenerReporteVentasVsFacturas(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<RptVentaVsFactura> lstReporteVentFact = new();
            List<DiarioCosto> lstDiarioCost = new();
            List<CostVentUni> lstCostVentUni = new();
            List<FacturaResult> lstVentaLocal = new();
            try
            {
                var lstPesosReales = await _objVentasFacturacionService.ObtenerRepFactPesoRealXRangoFecha(dtFechaInicio, dtFechaFin);
                lstCostVentUni = await ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
                lstVentaLocal = await _objVentasFacturacionService.ObtenerFacturasXRangoFecha(dtFechaInicio, dtFechaFin);
                //var lstMovimientos = await _objVentasFacturacionService.ObtenerTracamAutoXRangoFecha(dtFechaInicio, dtFechaFin);
                //var dicFactu = RepFactPesoRealResult.CrearDicFacturaPorMovimiento(lstPesosReales);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //lstDiarioCost = await _objCostoMateriaPrima.ObtenerDiarioCostoAsync(dtFechaInicio, dtFechaFin);
                //lstReporteVentFact.AddRange(RptVentaVsFactura.CrearListadoVentas(lstPesosReales));
                lstReporteVentFact.AddRange(lstPesosReales.Select(obj => new RptVentaVsFactura(obj)).ToList());
                lstReporteVentFact.AddRange(lstVentaLocal.Where(x =>
                x.strMovTipfac.Contains("FA") &&
                !x.strCliCodigo.Contains("383") &&
                x.strMovProduc.Contains("0")).Select(obj => new RptVentaVsFactura(obj)).ToList());
                //_objMotorAsigPrec.AsignarCostoDiarioVenta(lstDiarioCost, lstReporteVentFact);
                _objMotorAsigPrec.AsignarCostVentUnitDiarioVenta(lstCostVentUni, lstReporteVentFact);
                return lstReporteVentFact;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error Feature : {nameof(OperacionComercialFeature)} funcion : {nameof(ObtenerReporteVentasVsFacturas)} error: " +
                    objException.Message);
                throw;
            }
        }



        public async Task<List<CostVentUni>> ConsultarCostoVentaUni(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<CostVentUni> lstCostVentUni = new(), lstLbsLotePiso = new();
            List<LiquidacionResultado> lstLiqFrs = new();
            List<MatPrimaReproceso> lstLiqRpc = new();
            List<InventarioVal> lstInvVal = new();
            List<DiarioCosto> lstMovInv = new();
            List<PrecioFrsXMov> lstPrecioEsXMov = new();
            try
            {
                HashSet<string> hshCodProdExcluidos = new(StringComparer.OrdinalIgnoreCase) { "1006", "3473" };
                HashSet<string> hshDescripcionesExcluidas = new(StringComparer.OrdinalIgnoreCase) { "INICIALIZA INVENTARIO TOMA ene 2026", "INICIALIZA INVENTARIO DIC" };
                HashSet<string> hshTipEgr = new() { "UNI", "CDI", "R7" };
                HashSet<int> hshTalCodEx = new() { 128, 5 };
                HashSet<string> hshMovIng = new() { "DV", "IAJ", "REPING", "CNEI" };
                HashSet<string> hshMovEsp = new() { "UNI", "R7", "CDI" };
                HashSet<string> hshMovEgr = new() { "EAJ", "EMU", "REPROE", "LB", "UNI", "DIR", "CNE", "SMT", "EOB", "EX", "EXP", "EV" };
                //var lstPesosReales = await _objVentasFacturacionService.ObtenerRepFactPesoRealXRangoFecha(dtFechaInicio, dtFechaFin);
                var trLiqFrs = _objCostoMateriaPrima.ObtenerLiquidacionValorizada(dtFechaInicio, dtFechaFin);
                var trLiqRpc = _objCostoMateriaPrima.ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin);
                var trInvVal = _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin);
                var trMovInvIng = _objMateriaPrima.IngresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
                var trMovInvEgr = _objMateriaPrima.EgresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
                //var trCostVentEspeciales = _objMateriaPrima.ObtenerPrecioFrsSinTallaXMovCam(dtFechaInicio, dtFechaFin);
                await Task.WhenAll(trLiqFrs, trLiqRpc, trInvVal, trMovInvIng, trMovInvEgr //, trCostVentEspeciales
                    );
                lstLiqFrs = trLiqFrs.Result;
                lstLiqRpc = trLiqRpc.Result;
                lstInvVal = trInvVal.Result;
                lstLbsLotePiso = await _objMateriaPrima.ConsultarLotePiso(
                        lstLiqRpc
                        .Where(x => !hshTipEgr.Contains(x.strTipCod) && x.dbCostoXSecuencial == 0)
                        .Select(b => (decimal)b.intLotNumero)
                        .Distinct()
                        .ToList()
                        );
                lstMovInv.AddRange(trMovInvIng.Result
                    .Where(obj =>
                    hshMovIng.Contains(obj.strCodTip)
                    &&
                    !(hshCodProdExcluidos.Contains(obj.strProCodcor) && hshTalCodEx.Contains(obj.stTalCodigo) && hshDescripcionesExcluidas.Contains(obj.strDescri))
                    ));
                lstMovInv.AddRange(trMovInvEgr.Result
                    .Where(obj =>
                    hshMovEgr.Contains(obj.strCodTip) &&
                    !(hshCodProdExcluidos.Contains(obj.strProCodcor) && hshTalCodEx.Contains(obj.stTalCodigo) && hshDescripcionesExcluidas.Contains(obj.strDescri))
                ));
                //lstPrecioEsXMov = trCostVentEspeciales.Result;
                lstCostVentUni.AddRange(lstLiqFrs.Select(obj => new CostVentUni(obj)));
                lstCostVentUni.AddRange(
                    lstLiqRpc
                    .Where(obj => !(obj.strAgrupacion == "1. RECIBIDO" && hshMovEsp.Contains(obj.strTipCod)))
                    .Select(obj => new CostVentUni(obj))
                 );
                lstCostVentUni.AddRange(lstInvVal/*.Where(obj => obj.dcLibras > 0)*/.Select(obj => new CostVentUni(obj)));
                var dicPrecioEsXMov = PrecioFrsXMov.GenerarDiccionarioCostoXTalla(lstPrecioEsXMov);
                var dicProcesado = MatPrimaReproceso.GenerarDicProcGlobal(lstLiqRpc);
                var dicLiqFrs = LiquidacionResultado.GenerarDiccionarioLbs(lstLiqFrs);
                //var dicProcPromedio = MatPrimaReproceso.GenerarPromedioPonderadoProc(lstLiqRpc);
                var dicInvVal = InventarioVal.GenerarDiccionarioCostoXTalla(lstInvVal);
                //var dicInValProdTall = InventarioVal.GenerarDiccionarioProdXTalla(lstInvVal);
                foreach (var objReg in lstMovInv)
                {
                    if (dicInvVal.TryGetValue(objReg.objLoteProdTalKey, out decimal objPrecioInv) && objReg.dcCostoUnit == 0)
                    {
                        objReg.InicializarCamposCost(objPrecioInv);
                    }
                    else if (dicLiqFrs.TryGetValue(objReg.objLoteProdTalKey, out decimal objPrecioFrs) && objReg.dcCostoUnit == 0)
                    {
                        objReg.InicializarCamposCost(objPrecioFrs);
                    }
                    else if (dicProcesado.TryGetValue(objReg.objLoteProdTalReciKey, out decimal objPrecioProc) && objReg.dcCostoUnit == 0)
                    {
                        objReg.InicializarCamposCost(objPrecioProc);
                    }
                    //if (dicPrecioEsXMov.TryGetValue(objReg.objLoteProdTalKey, out double objPrecio))
                    //{
                    //    objReg.InicializarCamposCost(objPrecio);
                    //}
                    //else if (dicInValProdTall.TryGetValue(objReg.objProdTalKey, out decimal objPrecioInvProdTall) && objReg.dcCostoUnit == 0)
                    //{
                    //    objReg.InicializarCamposCost(objPrecioInvProdTall);
                    //}
                    //else if (dicProcPromedio.TryGetValue(objReg.objProdTalKey, out decimal objPrecioProcPromedio) && objReg.dcCostoUnit == 0)
                    //{
                    //    objReg.InicializarCamposCost(objPrecioProcPromedio);
                    //}
                    //else if (dicRecibido.TryGetValue(objReg.objLoteProdTalReciKey, out decimal objPrecioRec))
                    //{
                    //    objReg.InicializarCamposCost(objPrecioRec);
                    //}
                    //else if (dicLiqFrsPromedio.TryGetValue(objReg.objProdTalKey, out decimal objPrecioLiqFrsPromedio))
                    //{
                    //    objReg.InicializarCamposCost(objPrecioLiqFrsPromedio);
                    //}
                }
                lstCostVentUni.AddRange(lstMovInv.Select(obj => new CostVentUni(obj)));
                lstCostVentUni.AddRange(lstLbsLotePiso);
                //var lstVentas = await _objVentasFacturacionService.ObtenerFacturasXRangoFecha(dtFechaInicio, dtFechaFin);
                //var lstMovimientos = await _objVentasFacturacionService.ObtenerTracamAutoXRangoFecha(dtFechaInicio, dtFechaFin);
                //var dicFactu = RepFactPesoRealResult.CrearDicFacturaPorMovimiento(lstPesosReales);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //var dicMovCam = TracamAutoResult.CrearDicMovimiento(lstMovimientos);
                //lstDiarioCost = await _objCostoMateriaPrima.ObtenerDiarioCostoAsync(dtFechaInicio, dtFechaFin);
                //lstReporteVentFact.AddRange(RptVentaVsFactura.CrearListadoVentas(lstPesosReales));
                //_objMotorAsigPrec.AsignarCostoDiarioVenta(lstDiarioCost, lstReporteVentFact);
                return lstCostVentUni;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error Feature : {nameof(OperacionComercialFeature)} funcion : {nameof(ConsultarCostoVentaUni)} error: " +
                    objException.Message);
                throw;
            }
        }

        public async Task<CostoVentaUnitarioResultadoDto> ConsultarCostoVentaUniDisponible(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                List<CostVentUni> lstDetalleActual =
                    await ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);

                CostoVentaUnitarioResultadoDto objResultado =
                    _objMotorDisponibleInventario.Calcular(lstDetalleActual);

                // El endpoint NO devuelve estas auditorías como tablas adicionales.
                // Se registran internamente para no ocultar problemas de data.
                if (objResultado.objAuditoria.intTotalRecibidosSinLoteOrigen > 0)
                {
                    _objLogger.LogWarning(
                        "[CostoVentaDisponible] RECIBIDOS sin intLoteOrigen válido: {Cantidad}. Libras: {Libras}.",
                        objResultado.objAuditoria.intTotalRecibidosSinLoteOrigen,
                        objResultado.objAuditoria.dcLibrasRecibidosSinLoteOrigen);
                }

                if (objResultado.objAuditoria.intTotalClavesNegativas > 0)
                {
                    _objLogger.LogWarning(
                        "[CostoVentaDisponible] Claves Lote+Producto+Talla negativas: {Cantidad}. Libras: {Libras}.",
                        objResultado.objAuditoria.intTotalClavesNegativas,
                        objResultado.objAuditoria.dcLibrasClavesNegativas);
                }

                if (objResultado.objAuditoria.intTotalEgresosSinIngreso > 0)
                {
                    _objLogger.LogWarning(
                        "[CostoVentaDisponible] Egresos sin ingreso en la misma clave resuelta: {Cantidad}.",
                        objResultado.objAuditoria.intTotalEgresosSinIngreso);
                }

                foreach (CostoVentaDisponibleCuadreDto objDiferencia in
                    objResultado.objAuditoria.lstDiferenciasCuadre)
                {
                    _objLogger.LogError(
                        "[CostoVentaDisponible] DIFERENCIA CUADRE Prod={Prod} Talla={Talla}. " +
                        "LbsBase={LbsBase} LbsDisponible={LbsDisponible} DifLbs={DifLbs}. " +
                        "CostoBase={CostoBase} CostoDisponible={CostoDisponible} DifCosto={DifCosto}.",
                        objDiferencia.intCodProd,
                        objDiferencia.intCodTalla,
                        objDiferencia.dcLibrasBase,
                        objDiferencia.dcLibrasDisponible,
                        objDiferencia.dcDiferenciaLibras,
                        objDiferencia.dcCostoBase,
                        objDiferencia.dcCostoDisponible,
                        objDiferencia.dcDiferenciaCosto);
                }

                return objResultado;
            }
            catch (Exception objException)
            {
                _objLogger.LogError(
                    objException,
                    "Error Feature {Feature} funcion {Funcion}.",
                    nameof(OperacionComercialFeature),
                    nameof(ConsultarCostoVentaUniDisponible));
                throw;
            }
        }

        /// <summary>
        /// DIAGNÓSTICO TEMPORAL — no forma parte del flujo de negocio.
        /// Reproduce, en una sola llamada, los puntos de inspección que se
        /// revisarían con breakpoints manuales en ConsultarCostoVentaUni /
        /// MotorDisponibleInventario para una clave Lote+CodProd+Talla puntual:
        /// inventario inicial, movimientos de inventario, reproceso (filtrado
        /// por intLoteUnificado o intLoteOrigen) y el grupo final que arma
        /// MotorDisponibleInventario tras resolver el lote efectivo del RECIBIDO.
        /// Candidato a borrar una vez cerrada la investigación del lote 187165.
        /// </summary>
        public async Task<object> DiagnosticoDisponibleLote(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin,
            int intLoteBuscado,
            int intCodProd,
            string strTalla)
        {
            string strTallaNorm = (strTalla ?? string.Empty).Trim();

            var trLiqRpc = _objCostoMateriaPrima.ObtenerReporteMateriaPrimaReproValorizada(dtFechaInicio, dtFechaFin);
            var trInvVal = _objMateriaPrima.ConsultarInvValorizado(dtFechaInicio, dtFechaFin);
            var trMovInvIng = _objMateriaPrima.IngresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
            var trMovInvEgr = _objMateriaPrima.EgresosInvXrangoFecha(dtFechaInicio, dtFechaFin);
            var trCostVentUni = ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);

            await Task.WhenAll(trLiqRpc, trInvVal, trMovInvIng, trMovInvEgr, trCostVentUni);

            List<MatPrimaReproceso> lstLiqRpcFiltrado = trLiqRpc.Result
                .Where(x => x.intCodProd == intCodProd
                         && (x.strTalDescri ?? string.Empty).Trim() == strTallaNorm
                         && (x.intLoteUnificado == intLoteBuscado || x.intLoteOrigen == intLoteBuscado))
                .ToList();

            List<InventarioVal> lstInvValFiltrado = trInvVal.Result
                .Where(x => x.intLote == intLoteBuscado
                         && (x.strProCodcor ?? string.Empty).Trim() == intCodProd.ToString()
                         && (x.strTalDescri ?? string.Empty).Trim() == strTallaNorm)
                .ToList();

            List<DiarioCosto> lstMovInvFiltrado = trMovInvIng.Result
                .Concat(trMovInvEgr.Result)
                .Where(x => x.intLote == intLoteBuscado
                         && (x.strProCodcor ?? string.Empty).Trim() == intCodProd.ToString()
                         && (x.strTalDescri ?? string.Empty).Trim() == strTallaNorm)
                .ToList();

            // Mismo criterio que MotorDisponibleInventario: RECIBIDO -> intLoteOrigen,
            // el resto -> intLote. Reconstruye lstGrupo sin volver a sumar/restar,
            // solo agrupando por la clave ya resuelta.
            var lstGrupoFinal = trCostVentUni.Result
                .Select(x => new
                {
                    Registro = x,
                    LoteResuelto = string.Equals((x.strAgrupacion ?? string.Empty).Trim(), "1. RECIBIDO", StringComparison.OrdinalIgnoreCase)
                        ? (x.intLoteOrigen.HasValue && x.intLoteOrigen.Value > 0 ? x.intLoteOrigen : null)
                        : (int?)x.intLote
                })
                .Where(x => x.LoteResuelto == intLoteBuscado
                         && x.Registro.intCodProd == intCodProd
                         && (x.Registro.strTalla ?? string.Empty).Trim() == strTallaNorm)
                .Select(x => x.Registro)
                .ToList();

            return new
            {
                InvVal = new
                {
                    Cantidad = lstInvValFiltrado.Count,
                    SumLibras = lstInvValFiltrado.Sum(x => x.dcLibras),
                    Filas = lstInvValFiltrado
                },
                MovInv = new
                {
                    Cantidad = lstMovInvFiltrado.Count,
                    SumLibras = lstMovInvFiltrado.Sum(x => x.dcLibras),
                    Filas = lstMovInvFiltrado
                },
                LiqRpc = new
                {
                    Cantidad = lstLiqRpcFiltrado.Count,
                    SumLibras = lstLiqRpcFiltrado.Sum(x => (decimal)x.dbLibras),
                    Filas = lstLiqRpcFiltrado
                },
                GrupoFinal = new
                {
                    Cantidad = lstGrupoFinal.Count,
                    SumLibras = lstGrupoFinal.Sum(x => x.dcLibras),
                    SumCosto = lstGrupoFinal.Sum(x => x.dcCostoTot),
                    Filas = lstGrupoFinal
                }
            };
        }

        #region Costo y gasto Productivo

        private static readonly Dictionary<string, string>
            _mapCongelacionProducto =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["IQF"] = "IQF",

                    ["BLOCK"] = "TUN",
                    ["SEMI IQF"] = "TUN",

                    ["BRINE"] = "BRI"
                };


        private static string ResolverCongelacionProducto(
            string? valor)
        {
            string clave =
                (valor ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (_mapCongelacionProducto.TryGetValue(
                clave,
                out string? resultado))
            {
                return resultado;
            }

            throw new InvalidOperationException(
                $"Congelación PRO no configurada: '{valor}'.");
        }
        public async Task<CostoProductivoResultadoDto> ObtenerCostoProductivo(
    int intAnio,
    int intMes,
    DataProcesoParam? objDataProcesoPrecargado = null)
        {
            if (intAnio < 2000 || intAnio > 2100)
                throw new ArgumentOutOfRangeException(
                    nameof(intAnio),
                    "Año inválido.");

            if (intMes < 1 || intMes > 12)
                throw new ArgumentOutOfRangeException(
                    nameof(intMes),
                    "Mes inválido.");

            DateOnly dtFin = new DateOnly(
                intAnio,
                intMes,
                DateTime.DaysInMonth(intAnio, intMes));

            DateTime dtFechaCorte =
                dtFin.ToDateTime(TimeOnly.MinValue);

            try
            {
                // ============================================================
                // 1. CONSULTAS BASE
                // ============================================================

                var trConfigCostos =
                    _objCostoProductivoService
                        .ConsultarConfiguracionCostoProductivo();

                var trSaldosSong =
                    _objCostoProductivoService
                        .ConsultarSaldosCuentaSong(intAnio);

                Task<DataProcesoParam> trProceso =
                            objDataProcesoPrecargado == null
                                ? _objCostoMateriaPrima.ObtenerParametroProceso(dtFechaCorte)
                                : Task.FromResult(objDataProcesoPrecargado);


                await Task.WhenAll(
                    trConfigCostos,
                    trSaldosSong,
                    trProceso);


                CostoProductivoConfiguracionDbDto objConfigCostos =
                    trConfigCostos.Result;

                List<CostoProductivoConfigCuentaDto> lstConfig =
                    objConfigCostos.lstCuentas ?? new();

                List<ProcesoCostoAplicacionDto> lstAplicaciones =
                    objConfigCostos.lstAplicaciones ?? new();

                List<SaldoCuentaSongDto> lstSaldos =
                    trSaldosSong.Result ?? new();

                DataProcesoParam objDataProceso =
                    trProceso.Result;

                List<MatPrimaReproceso> lstRpcCompleto =
                    objDataProceso.lstLiqReproCompleto ?? new();


                if (lstConfig.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No existen cuentas activas para costo productivo.");
                }


                // ============================================================
                // 2. SALDOS SONG
                // ============================================================

                Dictionary<(int Empresa, string Cuenta), SaldoCuentaSongDto>
                    dicSaldos =
                        lstSaldos
                            .GroupBy(x => (
                                Empresa: x.intEmpresa,
                                Cuenta: NormalizarCuenta(x.strCuenta)))
                            .ToDictionary(
                                x => x.Key,
                                x => x.First());


                // ============================================================
                // 3. APLICACIONES POR PROCESO
                // ============================================================

                Dictionary<string, List<ProcesoCostoAplicacionDto>>
                    dicAplicaciones =
                        lstAplicaciones
                            .GroupBy(
                                x =>
                                    (x.strPcCodigo ?? string.Empty)
                                    .Trim(),
                                StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                x => x.Key,
                                x => x.ToList(),
                                StringComparer.OrdinalIgnoreCase);


                // ============================================================
                // 4. DRIVERS GENERALES
                // ============================================================

                var objMotor =
                    new MotorDistribucionCostoProductivo(
                        _objLogger);


                CostoProductivoDriversDto objDrivers =
                    objMotor.Construir(
                        objDataProceso,
                        lstRpcCompleto,
                        lstAplicaciones);


                // ============================================================
                // 5. CONFIGURACIÓN IQF
                // ============================================================

                List<IqfFuenteConfiguracion> configIqf =
                    (objConfigCostos.lstIqf ?? new())
                        .Select(x => x.Convertir())
                        .ToList();


                // ============================================================
                // 6. DRIVERS IQF
                //
                // IMPORTANTE:
                // ResolverCongelacionProducto debe usar un diccionario explícito
                // contra los valores reales de strCongeProduc.
                //
                // No usar Contains("IQF").
                //
                // Una fila PRO no resuelta debe detener el cálculo.
                // ============================================================

                List<DriverProcesoProductoDto> driversIqf =
                    AdaptadorDriversIqf.Construir(
                        intAnio,
                        intMes,
                        lstRpcCompleto,
                        lstAplicaciones,
                        ResolverCongelacionProducto);


                IqfLibras librasIqf =
                    AdaptadorDriversIqf.Resumir(
                        1,
                        driversIqf);


                // ============================================================
                // 7. UNIVERSO DE CUENTAS IQF / SONG
                //
                // Solo cargar al motor cuentas que realmente participan
                // en la configuración.
                // ============================================================

                HashSet<(int Empresa, string Cuenta)> universo =
                    lstConfig.Select(x => (
            Empresa: (int)x.intEmpresa,
            Cuenta: NormalizarCuenta(x.strCuenta)))
        .ToHashSet();


                List<IqfSaldoMes> saldosIqf =
                    lstSaldos
                        .Where(x =>
                            universo.Contains((
                                x.intEmpresa,
                                NormalizarCuenta(
                                    x.strCuenta))))
                        .Select(x =>
                        {
                            (
                                decimal debe,
                                decimal credito
                            ) =
                                x.ObtenerAcumulado(
                                    intMes);


                            return new IqfSaldoMes(
                                x.intEmpresa,
                                NormalizarCuenta(
                                    x.strCuenta),
                                debe,
                                credito);
                        })
                        .ToList();


                // ============================================================
                // 8. MOTOR RECLASIFICACIÓN IQF
                //
                // Debe ejecutarse ANTES:
                //
                // - del foreach de configuración
                // - del factor cuenta/proceso
                // - de la construcción de filas
                // ============================================================

                IqfResultado resultadoIqf =
                    MotorReclasificacionIqf.Calcular(
                        saldosIqf,
                        configIqf,
                        new[]
                        {
                    librasIqf
                        });


                Dictionary<IqfClave, IqfAsignacionFuente>
                    asignacionesIqf =
                        resultadoIqf
                            .Asignaciones
                            .ToDictionary(
                                x => x.Fuente);


                // ============================================================
                // 9. FACTORES CUENTA / PROCESO
                //
                // IMPORTANTE:
                // recién después del motor IQF.
                // ============================================================

                Dictionary<
                    (int Empresa, string Cuenta, string Pc),
                    decimal
                > factoresCuentaProceso =
                    ConstruirFactoresCuentaProceso(
                        lstConfig,
                        objMotor,
                        objDrivers);


                // ============================================================
                // 10. RESULTADO
                // ============================================================

                List<CostoProductivoCuentaDto> lstResultado =
                    new(lstConfig.Count + 1);

                int secuencia = 1;


                // ============================================================
                // 11. CONSTRUCCIÓN DE FILAS
                // ============================================================

                foreach (
                    CostoProductivoConfigCuentaDto config
                    in lstConfig)
                {
                    string cuenta =
                        NormalizarCuenta(
                            config.strCuenta);

                    string pc =
                        (config.strPcCodigo ??
                            string.Empty)
                        .Trim()
                        .ToUpperInvariant();


                    dicSaldos.TryGetValue(
                        (
                            config.intEmpresa,
                            cuenta
                        ),
                        out SaldoCuentaSongDto? saldo);


                    decimal debeFuente = 0m;
                    decimal creditoFuente = 0m;
                    string naturaleza = "D";


                    if (saldo != null)
                    {
                        (
                            debeFuente,
                            creditoFuente
                        ) =
                            saldo.ObtenerAcumulado(
                                intMes);


                        naturaleza =
                            string.IsNullOrWhiteSpace(
                                saldo.strNaturaleza)
                                ? "D"
                                : saldo
                                    .strNaturaleza
                                    .Trim()
                                    .ToUpperInvariant();
                    }


                    // ========================================================
                    // MONTO ORIGINAL SONG
                    //
                    // Nunca modificar este valor para simular un cuadre.
                    // ========================================================

                    decimal montoFuente =
                        Math.Round(
                            debeFuente -
                            creditoFuente,
                            4);


                    // ========================================================
                    // CLAVE IQF
                    // ========================================================

                    IqfClave claveCuenta =
                        MotorReclasificacionIqf.Clave(
                            config.intEmpresa,
                            cuenta);


                    // ========================================================
                    // BASE AJUSTADA IQF
                    //
                    // Si la cuenta no fue afectada por la reclasificación,
                    // conserva montoFuente.
                    // ========================================================

                    resultadoIqf.Bases.TryGetValue(
                        claveCuenta,
                        out IqfBaseCuenta? baseIqf);


                    decimal baseAjustada =
                        baseIqf?.MontoDistribuir ??
                        montoFuente;


                    // ========================================================
                    // FACTOR CUENTA / PROCESO
                    // ========================================================

                    decimal factor =
                        factoresCuentaProceso
                            .GetValueOrDefault(
                                (
                                    config.intEmpresa,
                                    cuenta,
                                    pc
                                ),
                                1m);


                    // ========================================================
                    // MONTO FINAL ASIGNADO A LA FILA
                    // ========================================================

                    decimal montoCuenta =
                        Math.Round(
                            baseAjustada *
                            factor,
                            4);


                    // Debe / Crédito permanecen como auditoría SONG.
                    decimal debe =
                        Math.Round(
                            debeFuente *
                            factor,
                            4);

                    decimal credito =
                        Math.Round(
                            creditoFuente *
                            factor,
                            4);


                    // ========================================================
                    // FILA
                    // ========================================================

                    var row =
                        new CostoProductivoCuentaDto
                        {
                            intId =
                                secuencia++,

                            strOrigen =
                                "CONTABLE",

                            strEtapa =
                                config.strProcesoCosto,

                            strGrupoEtapa =
                                config.strEtapaGeneral,

                            strEcCodigo =
                                config.strEcCodigo,

                            strPcCodigo =
                                config.strPcCodigo,

                            strTipo =
                                config.strTipo,

                            strTipo2 =
                                config.strTipo2,

                            strTipoCosto =
                                config.strTipoCosto,

                            strTipo3 =
                                config.strTipo3,

                            strDriverCodigo =
                                config.strDriverCodigo,

                            strAgrupacionCentro =
                                config.strAgrupacionCentro,

                            strGrupoCentro =
                                config.strGrupoCentro,

                            strCuenta =
                                cuenta,

                            strCentroCodigo =
                                config.strCentroCodigo,

                            strSubcentroCodigo =
                                config.strSubcentroCodigo,

                            // ================================================
                            // BASE ACTUALIZADA / JERARQUÍA
                            // ================================================

                            strCentroCosto =
                                !string.IsNullOrWhiteSpace(
                                    config.strCentroCosto)
                                    ? config
                                        .strCentroCosto
                                        .Trim()
                                    : saldo
                                        ?.ObtenerDescripcionJerarquia(
                                            config.strCentroCodigo)
                                      ??
                                      config.strCentroCodigo,

                            strSubcentroCosto =
                                !string.IsNullOrWhiteSpace(
                                    config.strSubcentroCosto)
                                    ? config
                                        .strSubcentroCosto
                                        .Trim()
                                    : saldo
                                        ?.ObtenerDescripcionJerarquia(
                                            config.strSubcentroCodigo)
                                      ??
                                      config.strSubcentroCodigo,

                            strRubro =
                                saldo
                                    ?.strNivel4Descripcion
                                    ?.Trim()
                                ??
                                string.Empty,

                            strAuxiliar =
                                saldo
                                    ?.strCuentaDescripcion
                                    ?.Trim()
                                ??
                                string.Empty,

                            strNaturaleza =
                                naturaleza,

                            // ================================================
                            // AUDITORÍA SONG
                            // ================================================

                            dcDebe =
                                debe,

                            dcCredito =
                                credito,

                            // SIEMPRE el Debe - Crédito original SONG.
                            dcMontoFuenteSong =
                                montoFuente,

                            dcFactorAsignacion =
                                factor,

                            // Base IQF ajustada × factor.
                            dcMontoCuenta =
                                montoCuenta,

                            // ================================================
                            // AUDITORÍA IQF
                            // ================================================

                            dcMontoOriginalAsignado =
                                Math.Round(
                                    (
                                        baseIqf
                                            ?.NetoOriginalSong
                                        ??
                                        montoFuente
                                    ) *
                                    factor,
                                    4),

                            dcRecuperadoIqf =
                                Math.Round(
                                    (
                                        baseIqf
                                            ?.RecuperadoIqf
                                        ??
                                        0m
                                    ) *
                                    factor,
                                    4),

                            dcRetiradoIqf =
                                Math.Round(
                                    (
                                        baseIqf
                                            ?.RetiradoIqf
                                        ??
                                        0m
                                    ) *
                                    factor,
                                    4),

                            blEncontradaSong =
                                saldo != null
                        };


                    // ========================================================
                    // 12. CLASIFICACIÓN / DISTRIBUCIÓN
                    //
                    // Q / pc_codigo manda.
                    // ========================================================

                    switch (pc)
                    {
                        // ====================================================
                        // COSTOS DIRECTOS / INDIRECTOS
                        // ====================================================

                        case "CDF":
                            {
                                row.dcCostoDirectoFijo =
                                    montoCuenta;

                                row.strClasificacionMonto =
                                    "COSTO_DIRECTO_FIJO";

                                row.strOrigenDistribucion =
                                    "CONFIG_Q";

                                break;
                            }


                        case "CDV":
                            {
                                row.dcCostoDirectoVariable =
                                    montoCuenta;

                                row.strClasificacionMonto =
                                    "COSTO_DIRECTO_VARIABLE";

                                row.strOrigenDistribucion =
                                    "CONFIG_Q";

                                break;
                            }


                        case "CIF":
                            {
                                row.dcCostoIndirectoFijo =
                                    montoCuenta;

                                row.strClasificacionMonto =
                                    "COSTO_INDIRECTO_FIJO";

                                row.strOrigenDistribucion =
                                    "CONFIG_Q";

                                break;
                            }


                        case "CIV":
                            {
                                row.dcCostoIndirectoVariable =
                                    montoCuenta;

                                row.strClasificacionMonto =
                                    "COSTO_INDIRECTO_VARIABLE";

                                row.strOrigenDistribucion =
                                    "CONFIG_Q";

                                break;
                            }


                        // ====================================================
                        // IQF
                        //
                        // El reparto del costo usa el DEBE.
                        // El retiro usa CRÉDITO POSITIVO.
                        //
                        // No volver a distribuir por objMotor.
                        // ====================================================

                        case "IQF":
                            {
                                if (
                                    factor != 1m ||
                                    !asignacionesIqf.TryGetValue(
                                        claveCuenta,
                                        out IqfAsignacionFuente? iqf))
                                {
                                    throw new InvalidOperationException(
                                        $"Fuente IQF sin asignacion unica: {cuenta}.");
                                }


                                row.dcMontoEntero =
                                    iqf
                                        .DistribucionCosto
                                        .Entero;

                                row.dcMontoCola =
                                    iqf
                                        .DistribucionCosto
                                        .Cola;

                                row.dcMontoVag =
                                    iqf
                                        .DistribucionCosto
                                        .ValorAgregado;


                                row.dcLibrasDriver =
                                    librasIqf.Total;

                                row.dcPesoDriver =
                                    librasIqf.Total;


                                row.strClasificacionMonto =
                                    "DISTRIBUCION_IQF";


                                row.strOrigenDistribucion =
                                    iqf.Recuperacion > 0m
                                        ? "IQF_LIBRAS_CON_RETIRO"
                                        : "IQF_LIBRAS_SIN_RETIRO";


                                break;
                            }


                        // ====================================================
                        // RESTO
                        //
                        // Utiliza el driver efectivo configurado en SQL.
                        // ====================================================

                        default:
                            {
                                string driver =
                                    (
                                        config.strDriverCodigo ??
                                        string.Empty
                                    )
                                    .Trim()
                                    .ToUpperInvariant();


                                string codigoDistribucion =
                                    driver == "DIRECTO_CUENTA" ||
                                    driver.Length == 0
                                        ? pc
                                        : driver;


                                DistribucionMontoProductoDto dist =
                                    objMotor.DistribuirMonto(
                                        codigoDistribucion,
                                        montoCuenta,
                                        objDrivers,
                                        cuenta);


                                row.dcMontoEntero =
                                    dist.dcEntero;

                                row.dcMontoCola =
                                    dist.dcCola;

                                row.dcMontoVag =
                                    dist.dcValorAgregado;

                                row.dcLibrasDriver =
                                    dist.dcLibrasDriver;

                                row.dcPesoDriver =
                                    dist.dcPesoDriver;

                                row.strOrigenDistribucion =
                                    dist.strOrigen;

                                row.strClasificacionMonto =
                                    dist.blUsoDriver
                                        ? "DISTRIBUCION_DRIVER"
                                        : "FALLBACK_CUENTA";


                                // ================================================
                                // Auditoría LOG / REC / CLA
                                // ================================================

                                row.dcLibrasRecibidasEntero =
                                    dist.dcLibrasRecibidasEntero;

                                row.dcLibrasRecibidasCola =
                                    dist.dcLibrasRecibidasCola;

                                row.dcLibrasProcesadasEntero =
                                    dist.dcLibrasProcesadasEntero;

                                row.dcLibrasProcesadasCola =
                                    dist.dcLibrasProcesadasCola;

                                row.dcCostoUnitarioRecibido =
                                    dist.dcCostoUnitarioRecibido;

                                row.dcCostoUnitarioEntero =
                                    dist.dcCostoUnitarioEntero;

                                row.dcCostoUnitarioCola =
                                    dist.dcCostoUnitarioCola;


                                break;
                            }
                    }


                    // ========================================================
                    // 13. DESCRIPCIÓN INICIAL DE APLICACIONES
                    //
                    // Para IQF el detalle real se construirá más adelante
                    // usando driversIqf.
                    // ========================================================

                    string driverAplicacion =
                        (
                            config.strDriverCodigo ??
                            string.Empty
                        )
                        .Trim()
                        .ToUpperInvariant();


                    if (
                        driverAplicacion.Length == 0 ||
                        driverAplicacion == "DIRECTO_CUENTA")
                    {
                        driverAplicacion =
                            pc;
                    }


                    List<ProcesoCostoAplicacionDto> aplicaciones =
                        dicAplicaciones
                            .GetValueOrDefault(
                                driverAplicacion,
                                new List<
                                    ProcesoCostoAplicacionDto>());


                    List<ProcesoCostoAplicacionDto>
                        aplicacionesActivas =
                            aplicaciones
                                .Where(x =>
                                    x.intFactor > 0 ||
                                    x.blEsTarifa)
                                .ToList();


                    row.strProcesosAplicables =
                        aplicacionesActivas.Count == 0
                            ? "SIN CONFIGURACION"
                            : string.Join(
                                ", ",
                                aplicacionesActivas
                                    .Select(x =>
                                        x.strTipoProceso));


                    row.intCantidadDestinos =
                        aplicacionesActivas.Count;


                    row.RecalcularTotales();


                    lstResultado.Add(row);
                }


                // ============================================================
                // 14. DESCONGELADO
                //
                // Permanece como costo derivado/tarifa.
                // No aplicar aquí otra vez la reclasificación IQF.
                // ============================================================

                CostoProductivoCuentaDto? rowDescongelado =
                    objMotor.AplicarReclasificacionDescongelado(
                        lstResultado,
                        objDrivers.objDescongelado,
                        objConfigCostos.objDescongelado);


                // ============================================================
                // 15. DETALLE DE PROCESOS
                // ============================================================

                foreach (
                    CostoProductivoCuentaDto fila
                    in lstResultado)
                {
                    if (fila.blEsDerivado)
                    {
                        continue;
                    }


                    // ========================================================
                    // IQF
                    //
                    // Utiliza exactamente los mismos drivers ya resueltos.
                    // No volver a hacer retiro/reclasificación.
                    // ========================================================

                    if (
                        (fila.strPcCodigo ??
                            string.Empty)
                        .Equals(
                            "IQF",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        fila.lstDetalleProcesosAplicables =
                            AdaptadorDriversIqf
                                .ConstruirDetalle(
                                    new IqfReparto(
                                        fila.dcMontoEntero,
                                        fila.dcMontoCola,
                                        fila.dcMontoVag),
                                    driversIqf,
                                    lstAplicaciones);
                    }
                    else
                    {
                        // ====================================================
                        // RESTO DE PROCESOS
                        //
                        // Utilizar driver efectivo.
                        // ====================================================

                        string driverDetalle =
                            (
                                fila.strDriverCodigo ??
                                string.Empty
                            )
                            .Trim()
                            .ToUpperInvariant();


                        if (
                            driverDetalle.Length == 0 ||
                            driverDetalle ==
                            "DIRECTO_CUENTA")
                        {
                            driverDetalle =
                                (
                                    fila.strPcCodigo ??
                                    string.Empty
                                )
                                .Trim()
                                .ToUpperInvariant();
                        }


                        List<ProcesoCostoAplicacionDto>
                            aplicacionesFila =
                                dicAplicaciones
                                    .GetValueOrDefault(
                                        driverDetalle,
                                        new List<
                                            ProcesoCostoAplicacionDto>());


                        fila.lstDetalleProcesosAplicables =
                            objMotor
                                .ConstruirDetalleProcesosAplicables(
                                    driverDetalle,
                                    fila.dcTotal,
                                    objDrivers,
                                    aplicacionesFila);
                    }


                    fila.intCantidadDestinos =
                        fila
                            .lstDetalleProcesosAplicables
                            .Count;


                    fila.strProcesosAplicables =
                        fila
                            .lstDetalleProcesosAplicables
                            .Count == 0
                                ? "SIN CONFIGURACION"
                                : string.Join(
                                    ", ",
                                    fila
                                        .lstDetalleProcesosAplicables
                                        .Select(x =>
                                            x.strProceso));
                }


                // ============================================================
                // 16. AGREGAR DESCONGELADO
                // ============================================================

                if (rowDescongelado != null)
                {
                    rowDescongelado.intId =
                        secuencia++;

                    lstResultado.Add(
                        rowDescongelado);
                }


                // ============================================================
                // 17. PREPARAR PERSISTENCIA
                //
                // IMPORTANTE:
                //
                // IQF ya viene reclasificado.
                // No volver a retirar/reclasificar durante el guardado.
                // ============================================================

                PrepararMontosPersistencia(
                    lstResultado,
                    objMotor,
                    objDrivers);


                // ============================================================
                // 18. RESUMEN POR PROCESO / ORIGEN
                //
                // Warren consumirá posteriormente este resultado.
                // ============================================================

                List<CostoProductivoProcesoOrigenDto>
                    lstProcesosOrigen =
                        objMotor
                            .ConstruirResumenProcesosOrigen(
                                lstResultado,
                                objDrivers,
                                lstAplicaciones);

                CostoProductivoResumenEspecialBuilder.Completar(
                            lstProcesosOrigen,
                            lstResultado,
                            driversIqf,
                            objDataProceso.lstLibrasParticion ?? new List<LibrasParticionDto>());

                CostoProductivoResumenEspecialBuilder.ValidarCuadreContraCuentas(
                            lstProcesosOrigen,
                            lstResultado);


                // ============================================================
                // 19. RESUMEN GENERAL
                // ============================================================

                CostoProductivoResumenDto resumen =
                    CostoProductivoResumenDto
                        .Crear(
                            lstResultado,
                            objDrivers.objDescongelado);


                // ============================================================
                // 20. RESPUESTA
                // ============================================================

                return new CostoProductivoResultadoDto
                {
                    lstCuentas =
                        lstResultado,

                    objResumen =
                        resumen,

                    objDescongelado =
                        objDrivers.objDescongelado,

                    lstProcesosOrigen =
                        lstProcesosOrigen
                };
            }
            catch (Exception objException)
            {
                _objLogger.LogError(
                    objException,
                    "Error Feature {Feature} funcion {Funcion}.",
                    nameof(OperacionComercialFeature),
                    nameof(ObtenerCostoProductivo));

                throw;
            }
        }


        public async Task<CierreParamProcResultadoDto> GuardarCierreParamProc(
            int intAnio,
            int intMes,
            string strUsuario,
            CostoProductivoResultadoDto objCosto,
            List<ProcesoResultadoDto> lstParametros,
            WarrenResultadoDto objWarren)
        {
            ValidarSnapshotCostoProductivo(objCosto.lstCuentas, objCosto.objResumen);

            DateOnly fechaCorte = new(
                intAnio,
                intMes,
                DateTime.DaysInMonth(intAnio, intMes));

            bool guardado = await _objCostoProductivoService.GuardarCierreParamProcAtomico(
                intAnio,
                intMes,
                fechaCorte,
                objCosto.lstCuentas,
                lstParametros,
                objWarren,
                strUsuario);

            if (!guardado)
                throw new InvalidOperationException("No se pudo guardar el cierre ParamProc.");

            return new CierreParamProcResultadoDto
            {
                objCostoProductivo = objCosto,
                lstParametrosPfr = lstParametros
                    .Where(x => string.Equals(x.strTipoLote, "PFR", StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                lstParametrosRpc = lstParametros
                    .Where(x => string.Equals(x.strTipoLote, "RPC", StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                objWarren = objWarren
            };
        }

        /// <summary>
        /// Guarda exactamente el snapshot que el GET ya construyó y que el usuario
        /// vio en pantalla (Table6/Table9/Table/Table1 + base Warren de Angular).
        /// No vuelve a consultar Producción, SONG ni a recalcular el costo productivo.
        /// </summary>
        public async Task<CierreParamProcResultadoDto> GuardarCierreParamProcSnapshot(
            int intAnio,
            int intMes,
            string strUsuario,
            List<CostoProductivoDetalleModalDto> lstDetalleContable,
            List<CostoProductivoProcesoOrigenDto> lstProcesosOrigen,
            List<ProcesoResultadoDto> lstParametros,
            WarrenResultadoDto objWarren)
        {
            DateOnly dtFechaCorte = new(
                intAnio,
                intMes,
                DateTime.DaysInMonth(intAnio, intMes));

            bool blGuardado =
                await _objCostoProductivoService.GuardarCierreParamProcSnapshotAtomico(
                    intAnio,
                    intMes,
                    dtFechaCorte,
                    lstDetalleContable,
                    lstParametros,
                    objWarren,
                    strUsuario);

            if (!blGuardado)
                throw new InvalidOperationException(
                    "No se pudo guardar el cierre ParamProc desde el snapshot.");

            return new CierreParamProcResultadoDto
            {
                objCostoProductivo = new CostoProductivoResultadoDto
                {
                    lstProcesosOrigen = lstProcesosOrigen ?? new()
                },
                lstParametrosPfr = lstParametros
                    .Where(x => string.Equals(
                        x.strTipoLote,
                        "PFR",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                lstParametrosRpc = lstParametros
                    .Where(x => string.Equals(
                        x.strTipoLote,
                        "RPC",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                objWarren = objWarren
            };
        }

        public async Task<CostoProductivoResultadoDto> GuardarCostoProductivo(
            int intAnio,
            int intMes,
            string strUsuario,
            List<CostoProductivoCuentaDto>? lstRegistros = null,
            CostoProductivoResumenDto? objResumen = null)
        {
            CostoProductivoResultadoDto resultado;

            if (lstRegistros != null && lstRegistros.Count > 0 && objResumen != null)
            {
                ValidarSnapshotCostoProductivo(lstRegistros, objResumen);

                resultado = new CostoProductivoResultadoDto
                {
                    lstCuentas = lstRegistros,
                    objResumen = objResumen,
                    objDescongelado = new DescongeladoResultadoDto
                    {
                        dcMontoAsignado = objResumen.dcDescongelado,
                        dcMontoTotal = objResumen.dcDescongelado,
                        dcLibrasRecibidasPt = objResumen.dcLibrasDescongelado,
                        dcTarifa = objResumen.dcTarifaDescongelado,
                        dcMontoSinAsignar = objResumen.dcDescongeladoSinAsignar,
                        intLotesSinSalida = objResumen.intLotesDescongeladoSinSalida
                    }
                };
            }
            else
            {
                // Compatibilidad temporal con clientes anteriores.
                resultado = await ObtenerCostoProductivo(intAnio, intMes);
            }

            if (!resultado.objResumen.blCuadra)
                throw new InvalidOperationException(
                    $"La distribución no cuadra. Diferencia: {resultado.objResumen.dcDiferencia:N4}. " +
                    $"Descongelado sin asignar: {resultado.objResumen.dcDescongeladoSinAsignar:N4}.");

            DateOnly fechaCorte = new DateOnly(
                intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));

            bool guardado = await _objCostoProductivoService.GuardarDistribucionProcesoProductivo(
                intAnio, intMes, fechaCorte, resultado.lstCuentas, strUsuario);

            if (!guardado)
                throw new InvalidOperationException("No se pudo guardar el costo productivo.");

            return resultado;
        }



        private static void ValidarSnapshotCostoProductivo(
            List<CostoProductivoCuentaDto> lstRegistros,
            CostoProductivoResumenDto objResumen)
        {
            foreach (CostoProductivoCuentaDto row in lstRegistros)
                row.RecalcularTotales();

            List<CostoProductivoCuentaDto> contables =
                lstRegistros.Where(x => !x.blEsDerivado).ToList();

            if (contables.Any(x => !x.blCuadra))
            {
                string detalle = string.Join(", ",
                    contables.Where(x => !x.blCuadra).Take(10)
                        .Select(x => $"{x.strCuenta}:{x.dcDiferencia:N4}"));

                throw new InvalidOperationException(
                    "El snapshot recibido contiene cuentas descuadradas: " + detalle);
            }

            decimal montoFuente = Math.Round(contables.Sum(x => x.dcMontoCuenta), 4);
            decimal totalVisual = Math.Round(lstRegistros.Sum(x => x.dcTotal), 4);

            decimal totalPersistencia = Math.Round(lstRegistros.Sum(x =>
                x.dcMontoEnteroPersistencia +
                x.dcMontoColaPersistencia +
                x.dcMontoVagPersistencia), 4);

            if (Math.Abs(montoFuente - totalVisual) > 0.01m)
                throw new InvalidOperationException(
                    $"Snapshot visual descuadrado. Fuente: {montoFuente:N4}; Total: {totalVisual:N4}.");

            if (Math.Abs(montoFuente - totalPersistencia) > 0.01m)
                throw new InvalidOperationException(
                    $"Snapshot de persistencia descuadrado. Fuente: {montoFuente:N4}; Persistencia: {totalPersistencia:N4}.");

            if (Math.Abs(objResumen.dcMontoCuenta - montoFuente) > 0.01m)
                throw new InvalidOperationException(
                    "El resumen recibido no coincide con las cuentas enviadas.");

            if (Math.Abs(objResumen.dcDiferencia) > 0.01m || !objResumen.blCuadra)
                throw new InvalidOperationException(
                    "El resumen recibido no está cuadrado.");
        }


        public async Task<List<CostoProductivoDetalleModalDto>> ObtenerInformacionCostProd(int intAnio, int intMes)
        {
            try
            {
                return await _objCostoProductivoService.ConsultarDetalleCostoProductivoPeriodo(intAnio, intMes);
            }
            catch (Exception objException)
            {
                _objLogger.LogError(
                    objException,
                    "Error Feature {Feature} funcion {Funcion}.",
                    nameof(OperacionComercialFeature),
                    nameof(ObtenerInformacionCostProd));

                throw;
            }
        }

        private static string NormalizarCuenta(string? cuenta) =>
            (cuenta ?? string.Empty).Trim();

        private void PrepararMontosPersistencia(
            List<CostoProductivoCuentaDto> lstResultado,
            MotorDistribucionCostoProductivo objMotor,
            CostoProductivoDriversDto objDrivers)
        {
            foreach (CostoProductivoCuentaDto fila in lstResultado)
            {
                if (!fila.blEsCostoEstructural)
                {
                    fila.dcMontoEnteroPersistencia = Math.Round(fila.dcMontoEntero, 5);
                    fila.dcMontoColaPersistencia = Math.Round(fila.dcMontoCola, 5);
                    fila.dcMontoVagPersistencia = Math.Round(fila.dcMontoVag, 5);
                    continue;
                }

                string pc = (fila.strPcCodigo ?? string.Empty).Trim().ToUpperInvariant();

                // Si una parte de esta cuenta ya fue reclasificada a la fila derivada
                // DSC, esa parte no vuelve a distribuirse aquí; ya está en Descongelado.
                // El guard solo aplica a filas que realmente tuvieron reclasificación:
                // dcMontoCuenta puede ser legítimamente negativo (notas de crédito,
                // reversos) en cuentas que nunca tocó Descongelado, y eso no es un error.
                decimal dcMontoPersistir = fila.dcMontoCuenta;

                if (fila.dcReclasificadoDescongelado > 0m)
                {
                    dcMontoPersistir = Math.Round(
                        fila.dcMontoCuenta - fila.dcReclasificadoDescongelado,
                        4);

                    if (dcMontoPersistir < -0.01m)
                    {
                        throw new InvalidOperationException(
                            $"La reclasificación de Descongelado supera el monto de la cuenta {fila.strCuenta}. " +
                            $"Cuenta: {fila.dcMontoCuenta:N4}; Reclasificado: {fila.dcReclasificadoDescongelado:N4}.");
                    }

                    if (dcMontoPersistir < 0m)
                        dcMontoPersistir = 0m;
                }

                DistribucionMontoProductoDto distribucion =
                    objMotor.DistribuirMonto(
                        pc,
                        dcMontoPersistir,
                        objDrivers,
                        fila.strCuenta);

                fila.dcMontoEnteroPersistencia = Math.Round(distribucion.dcEntero, 5);
                fila.dcMontoColaPersistencia = Math.Round(distribucion.dcCola, 5);
                fila.dcMontoVagPersistencia = Math.Round(distribucion.dcValorAgregado, 5);

                decimal totalDistribuido =
                    fila.dcMontoEnteroPersistencia +
                    fila.dcMontoColaPersistencia +
                    fila.dcMontoVagPersistencia;

                decimal diferencia = Math.Round(dcMontoPersistir - totalDistribuido, 4);

                if (Math.Abs(diferencia) > 0.01m)
                    throw new InvalidOperationException(
                        $"La cuenta {fila.strCuenta} del proceso {pc} no pudo distribuirse completamente. " +
                        $"Monto a persistir: {dcMontoPersistir:N4}; Distribuido: {totalDistribuido:N4}; Diferencia: {diferencia:N4}.");
            }
        }

        private Dictionary<(int Empresa, string Cuenta, string Pc), decimal> ConstruirFactoresCuentaProceso(
            List<CostoProductivoConfigCuentaDto> lstConfig,
            MotorDistribucionCostoProductivo objMotor,
            CostoProductivoDriversDto objDrivers)
        {
            var resultado = new Dictionary<(int Empresa, string Cuenta, string Pc), decimal>();

            var grupos = lstConfig.GroupBy(x =>
                (Empresa: x.intEmpresa, Cuenta: NormalizarCuenta(x.strCuenta)));

            foreach (var grupo in grupos)
            {
                List<CostoProductivoConfigCuentaDto> aplicaciones = grupo
                    .GroupBy(x => (x.strPcCodigo ?? string.Empty).Trim().ToUpperInvariant())
                    .Select(x => x.First())
                    .ToList();

                if (aplicaciones.Count == 1)
                {
                    string pcUnico = (aplicaciones[0].strPcCodigo ?? string.Empty)
                        .Trim().ToUpperInvariant();

                    resultado[(grupo.Key.Empresa, grupo.Key.Cuenta, pcUnico)] = 1m;
                    continue;
                }

                var pesos = aplicaciones.Select(config =>
                {
                    string pc = (config.strPcCodigo ?? string.Empty)
                        .Trim().ToUpperInvariant();

                    DistribucionMontoProductoDto dist =
                        objMotor.DistribuirMonto(pc, 1m, objDrivers, grupo.Key.Cuenta);

                    decimal peso = dist.dcPesoDriver > 0m
                        ? dist.dcPesoDriver
                        : dist.dcLibrasDriver;

                    return new { Pc = pc, Peso = peso };
                }).ToList();

                decimal pesoTotal = pesos.Sum(x => x.Peso);

                if (pesoTotal <= 0m)
                    throw new InvalidOperationException(
                        $"La cuenta {grupo.Key.Cuenta} pertenece a más de un proceso, " +
                        "pero no existe peso de driver para calcular su participación.");

                foreach (var item in pesos)
                    resultado[(grupo.Key.Empresa, grupo.Key.Cuenta, item.Pc)] =
                        item.Peso / pesoTotal;
            }

            return resultado;
        }
        #endregion
    }
}
