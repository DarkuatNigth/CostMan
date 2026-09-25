using CostManagement.Aplicación.DTos;
using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
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
                HashSet<string> hshMovEgr = new() { "EAJ", "EMU", "REPROE", "LB", "UNI", "DIR", "CNE", "SMT", "EOB" };
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

        #region Costo y gasto Productivo
        public async Task<CostoProductivoResultadoDto> ObtenerCostoProductivo(int intAnio, int intMes)
        {
            if (intAnio < 2000 || intAnio > 2100) throw new ArgumentOutOfRangeException(nameof(intAnio), "Año inválido.");
            if (intMes < 1 || intMes > 12) throw new ArgumentOutOfRangeException(nameof(intMes), "Mes inválido.");

            DateOnly dtFin = new DateOnly(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));
            DateTime dtFechaCorte = dtFin.ToDateTime(TimeOnly.MinValue);

            try
            {
                var trConfigCostos = _objCostoProductivoService.ConsultarConfiguracionCostoProductivo();
                var trSaldosSong = _objCostoProductivoService.ConsultarSaldosCuentaSong(intAnio);
                var trProceso = _objCostoMateriaPrima.ObtenerParametroProceso(dtFechaCorte);

                await Task.WhenAll(trConfigCostos, trSaldosSong, trProceso);

                CostoProductivoConfiguracionDbDto objConfigCostos = trConfigCostos.Result;
                List<CostoProductivoConfigCuentaDto> lstConfig = objConfigCostos.lstCuentas ?? new();
                List<ProcesoCostoAplicacionDto> lstAplicaciones = objConfigCostos.lstAplicaciones ?? new();
                List<SaldoCuentaSongDto> lstSaldos = trSaldosSong.Result ?? new();
                DataProcesoParam objDataProceso = trProceso.Result;
                List<MatPrimaReproceso> lstRpcCompleto = objDataProceso.lstLiqReproCompleto ?? new();

                if (lstConfig.Count == 0)
                    throw new InvalidOperationException("No existen cuentas activas para costo productivo.");

                Dictionary<(int Empresa, string Cuenta), SaldoCuentaSongDto> dicSaldos = lstSaldos
                    .GroupBy(x => (Empresa: x.intEmpresa, Cuenta: NormalizarCuenta(x.strCuenta)))
                    .ToDictionary(x => x.Key, x => x.First());

                Dictionary<string, List<ProcesoCostoAplicacionDto>> dicAplicaciones = lstAplicaciones
                    .GroupBy(x => (x.strPcCodigo ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

                var objMotor = new MotorDistribucionCostoProductivo(_objLogger);
                CostoProductivoDriversDto objDrivers =
                    objMotor.Construir(objDataProceso, lstRpcCompleto, lstAplicaciones);

                // Para cuentas presentes en más de un proceso (ej. 50301040104),
                // la participación se calcula por el peso real de sus drivers.
                Dictionary<(int Empresa, string Cuenta, string Pc), decimal> factoresCuentaProceso =
                    ConstruirFactoresCuentaProceso(lstConfig, objMotor, objDrivers);

                List<CostoProductivoCuentaDto> lstResultado = new(lstConfig.Count + 1);
                int secuencia = 1;

                foreach (CostoProductivoConfigCuentaDto config in lstConfig)
                {
                    string cuenta = NormalizarCuenta(config.strCuenta);
                    string pc = (config.strPcCodigo ?? string.Empty).Trim().ToUpperInvariant();

                    dicSaldos.TryGetValue((config.intEmpresa, cuenta), out SaldoCuentaSongDto? saldo);

                    decimal debeFuente = 0m;
                    decimal creditoFuente = 0m;
                    string naturaleza = "D";

                    if (saldo != null)
                    {
                        (debeFuente, creditoFuente) = saldo.ObtenerAcumulado(intMes);
                        naturaleza = string.IsNullOrWhiteSpace(saldo.strNaturaleza)
                            ? "D" : saldo.strNaturaleza.Trim().ToUpperInvariant();
                    }

                    // Solo movimiento del mes: Debe - Haber.
                    decimal montoFuente = Math.Round(debeFuente - creditoFuente, 4);

                    decimal factor = factoresCuentaProceso.GetValueOrDefault(
                        (config.intEmpresa, cuenta, pc), 1m);

                    decimal montoCuenta = Math.Round(montoFuente * factor, 4);
                    decimal debe = Math.Round(debeFuente * factor, 4);
                    decimal credito = Math.Round(creditoFuente * factor, 4);

                    var row = new CostoProductivoCuentaDto
                    {
                        intId = secuencia++,
                        strOrigen = "CONTABLE",

                        strEtapa = config.strProcesoCosto,
                        strGrupoEtapa = config.strEtapaGeneral,
                        strEcCodigo = config.strEcCodigo,
                        strPcCodigo = config.strPcCodigo,

                        strTipo = config.strTipo,
                        strTipoCosto = config.strTipoCosto,
                        strTipo3 = config.strTipo3,
                        strAgrupacionCentro = config.strAgrupacionCentro,
                        strGrupoCentro = config.strGrupoCentro,

                        strCuenta = cuenta,
                        strCentroCodigo = config.strCentroCodigo,
                        strSubcentroCodigo = config.strSubcentroCodigo,

                        // Prioridad a BASE ACTUALIZADA (2), columnas E y G.
                        strCentroCosto = !string.IsNullOrWhiteSpace(config.strCentroCosto)
                            ? config.strCentroCosto.Trim()
                            : saldo?.ObtenerDescripcionJerarquia(config.strCentroCodigo) ?? config.strCentroCodigo,

                        strSubcentroCosto = !string.IsNullOrWhiteSpace(config.strSubcentroCosto)
                            ? config.strSubcentroCosto.Trim()
                            : saldo?.ObtenerDescripcionJerarquia(config.strSubcentroCodigo) ?? config.strSubcentroCodigo,

                        strRubro = saldo?.strNivel4Descripcion?.Trim() ?? string.Empty,
                        strAuxiliar = saldo?.strCuentaDescripcion?.Trim() ?? string.Empty,
                        strNaturaleza = naturaleza,

                        dcDebe = debe,
                        dcCredito = credito,
                        dcMontoFuenteSong = montoFuente,
                        dcFactorAsignacion = factor,
                        dcMontoCuenta = montoCuenta,
                        blEncontradaSong = saldo != null
                    };

                    /*
                     * Q / pc_codigo manda.
                     * NO existe regla "si centro inicia 504 => indirecto".
                     * LOG, REC, CLA, TUN, etc. se distribuyen por su driver.
                     */
                    switch (pc)
                    {
                        case "CDF":
                            row.dcCostoDirectoFijo = montoCuenta;
                            row.strClasificacionMonto = "COSTO_DIRECTO_FIJO";
                            row.strOrigenDistribucion = "CONFIG_Q";
                            break;

                        case "CDV":
                            row.dcCostoDirectoVariable = montoCuenta;
                            row.strClasificacionMonto = "COSTO_DIRECTO_VARIABLE";
                            row.strOrigenDistribucion = "CONFIG_Q";
                            break;

                        case "CIF":
                            row.dcCostoIndirectoFijo = montoCuenta;
                            row.strClasificacionMonto = "COSTO_INDIRECTO_FIJO";
                            row.strOrigenDistribucion = "CONFIG_Q";
                            break;

                        case "CIV":
                            row.dcCostoIndirectoVariable = montoCuenta;
                            row.strClasificacionMonto = "COSTO_INDIRECTO_VARIABLE";
                            row.strOrigenDistribucion = "CONFIG_Q";
                            break;

                        default:
                            DistribucionMontoProductoDto dist =
                                objMotor.DistribuirMonto(pc, montoCuenta, objDrivers, cuenta);

                            row.dcMontoEntero = dist.dcEntero;
                            row.dcMontoCola = dist.dcCola;
                            row.dcMontoVag = dist.dcValorAgregado;
                            row.dcLibrasDriver = dist.dcLibrasDriver;
                            row.dcPesoDriver = dist.dcPesoDriver;
                            row.strOrigenDistribucion = dist.strOrigen;
                            row.strClasificacionMonto = dist.blUsoDriver
                                ? "DISTRIBUCION_DRIVER" : "FALLBACK_CUENTA";

                            // Auditoría especial LOG / REC / CLA.
                            row.dcLibrasRecibidasEntero = dist.dcLibrasRecibidasEntero;
                            row.dcLibrasRecibidasCola = dist.dcLibrasRecibidasCola;
                            row.dcLibrasProcesadasEntero = dist.dcLibrasProcesadasEntero;
                            row.dcLibrasProcesadasCola = dist.dcLibrasProcesadasCola;
                            row.dcCostoUnitarioRecibido = dist.dcCostoUnitarioRecibido;
                            row.dcCostoUnitarioEntero = dist.dcCostoUnitarioEntero;
                            row.dcCostoUnitarioCola = dist.dcCostoUnitarioCola;
                            break;
                    }

                    List<ProcesoCostoAplicacionDto> aplicaciones =
                        dicAplicaciones.GetValueOrDefault(pc, new List<ProcesoCostoAplicacionDto>());

                    // Solo configuraciones que realmente aplican.
                    List<ProcesoCostoAplicacionDto> aplicacionesActivas = aplicaciones
                        .Where(x => x.intFactor > 0 || x.blEsTarifa)
                        .ToList();

                    row.strProcesosAplicables = aplicacionesActivas.Count == 0
                        ? "SIN CONFIGURACION"
                        : string.Join(", ", aplicacionesActivas.Select(x => x.strTipoProceso));

                    row.intCantidadDestinos = aplicacionesActivas.Count;
                    row.RecalcularTotales();
                    lstResultado.Add(row);
                }

                CostoProductivoCuentaDto? rowDescongelado =
                    objMotor.AplicarReclasificacionDescongelado(
                        lstResultado, objDrivers.objDescongelado, objConfigCostos.objDescongelado);

                foreach (CostoProductivoCuentaDto fila in lstResultado)
                {
                    if (fila.blEsDerivado) continue;

                    List<ProcesoCostoAplicacionDto> aplicacionesFila = dicAplicaciones.GetValueOrDefault(
                        (fila.strPcCodigo ?? string.Empty).Trim(), new List<ProcesoCostoAplicacionDto>());

                    fila.lstDetalleProcesosAplicables = objMotor.ConstruirDetalleProcesosAplicables(
                        fila.strPcCodigo, fila.dcTotal, objDrivers, aplicacionesFila);

                    fila.intCantidadDestinos = fila.lstDetalleProcesosAplicables.Count;
                    fila.strProcesosAplicables = fila.lstDetalleProcesosAplicables.Count == 0
                        ? "SIN CONFIGURACION"
                        : string.Join(", ", fila.lstDetalleProcesosAplicables.Select(x => x.strProceso));
                }

                if (rowDescongelado != null)
                {
                    rowDescongelado.intId = secuencia++;
                    lstResultado.Add(rowDescongelado);
                }

                // El GET ya deja listos los importes exactos que posteriormente se persistirán.
                PrepararMontosPersistencia(lstResultado, objMotor, objDrivers);


                List<CostoProductivoProcesoOrigenDto> lstProcesosOrigen =
                    objMotor.ConstruirResumenProcesosOrigen(
                        lstResultado,
                        objDrivers,
                        lstAplicaciones);

                CostoProductivoResumenDto resumen =
                    CostoProductivoResumenDto.Crear(lstResultado, objDrivers.objDescongelado);

                return new CostoProductivoResultadoDto
                {
                    lstCuentas = lstResultado,
                    objResumen = resumen,
                    objDescongelado = objDrivers.objDescongelado,
                    lstProcesosOrigen = lstProcesosOrigen
                };
            }
            catch (Exception objException)
            {
                _objLogger.LogError(objException,
                    "Error Feature {Feature} funcion {Funcion}.",
                    nameof(OperacionComercialFeature), nameof(ObtenerCostoProductivo));
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

                DistribucionMontoProductoDto distribucion =
                    objMotor.DistribuirMonto(pc, fila.dcMontoCuenta, objDrivers, fila.strCuenta);

                fila.dcMontoEnteroPersistencia = Math.Round(distribucion.dcEntero, 5);
                fila.dcMontoColaPersistencia = Math.Round(distribucion.dcCola, 5);
                fila.dcMontoVagPersistencia = Math.Round(distribucion.dcValorAgregado, 5);

                decimal totalDistribuido =
                    fila.dcMontoEnteroPersistencia +
                    fila.dcMontoColaPersistencia +
                    fila.dcMontoVagPersistencia;

                decimal diferencia = Math.Round(fila.dcMontoCuenta - totalDistribuido, 4);

                if (Math.Abs(diferencia) > 0.01m)
                    throw new InvalidOperationException(
                        $"La cuenta {fila.strCuenta} del proceso {pc} no pudo distribuirse completamente. " +
                        $"Monto: {fila.dcMontoCuenta:N4}; Distribuido: {totalDistribuido:N4}; Diferencia: {diferencia:N4}.");
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
