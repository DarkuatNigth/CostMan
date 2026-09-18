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
                HashSet<string> hshMovEgr = new() { "EAJ", "EMU", "REPROE", "LB", "UNI", "DIR", "CNE", "SMT" , "EOB" };
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
                        .Where(x =>  !hshTipEgr.Contains(x.strTipCod) &&  x.dbCostoXSecuencial == 0)
                        .Select(b =>(decimal) b.intLotNumero)
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
                lstCostVentUni.AddRange(lstLiqFrs.Select(obj => new CostVentUni(obj)) );
                lstCostVentUni.AddRange(
                    lstLiqRpc
                    .Where(obj =>!(obj.strAgrupacion == "1. RECIBIDO" && hshMovEsp.Contains(obj.strTipCod)))
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
                lstCostVentUni.AddRange(lstMovInv.Select(obj => new CostVentUni(obj)) );
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


        public async Task<CostoProductivoResultadoDto> ObtenerCostoProductivo(
            int intAnio,
            int intMes)
        {
            if (intAnio < 2000 || intAnio > 2100)
                throw new ArgumentOutOfRangeException(nameof(intAnio), "Año inválido.");

            if (intMes < 1 || intMes > 12)
                throw new ArgumentOutOfRangeException(nameof(intMes), "Mes inválido.");

            DateOnly dtInicio = new DateOnly(intAnio, intMes, 1);
            DateOnly dtFin = new DateOnly(
                intAnio,
                intMes,
                DateTime.DaysInMonth(intAnio, intMes));

            DateTime dtFechaCorte = dtFin.ToDateTime(TimeOnly.MinValue);

            try
            {
                // ConnectionCostos:
                // Resultset 1 = cuentas + TIPO/Tipo costo/AGRUPACION/GRUPO.
                // Resultset 2 = matriz SI/X2/TAR.
                // Resultset 3 = metadatos derivados de Descongelado.
                var trConfigCostos =
                    _objCostoProductivoService.ConsultarConfiguracionCostoProductivo();

                // ConnectionSong: tb_maecta + tb_salcta2.
                var trSaldosSong = _objCostoProductivoService.ConsultarSaldosCuentaSong(intAnio);

                // ObtenerParametroProceso sirve para fresco y las libras ya construidas.
                var trProceso = _objCostoMateriaPrima.ObtenerParametroProceso(dtFechaCorte);

                /*
                 * ObtenerParametroProceso ya llena lstLiqReproCompleto
                 * usando ReporteReproPlanRecibProc.
                 * No hacemos una segunda consulta de reproceso.
                 */
                await Task.WhenAll(
                    trConfigCostos,
                    trSaldosSong,
                    trProceso);

                CostoProductivoConfiguracionDbDto objConfigCostos = trConfigCostos.Result;
                List<CostoProductivoConfigCuentaDto> lstConfig = objConfigCostos.lstCuentas;
                List<ProcesoCostoAplicacionDto> lstAplicaciones = objConfigCostos.lstAplicaciones;
                List<SaldoCuentaSongDto> lstSaldos = trSaldosSong.Result;
                DataProcesoParam objDataProceso = trProceso.Result;

                List<MatPrimaReproceso> lstRpcCompleto =
                    objDataProceso.lstLiqReproCompleto
                    ?? new List<MatPrimaReproceso>();

                if (!lstConfig.Any())
                    throw new InvalidOperationException(
                        "No existen cuentas activas en costos.tb_configCuentaCosto.");

                // El saldo SONG debe repartirse una sola vez por cuenta.
                // Por eso cada cuenta debe sumar factor 1.
                var factoresInvalidos = lstConfig
                    .GroupBy(x => (x.intEmpresa, NormalizarCuenta(x.strCuenta)))
                    .Select(g => new
                    {
                        g.Key,
                        Factor = g.Sum(x => x.dcFactorAsignacion)
                    })
                    .Where(x => Math.Abs(x.Factor - 1m) > 0.0000001m)
                    .ToList();

                if (factoresInvalidos.Any())
                {
                    string cuentas = string.Join(
                        ", ",
                        factoresInvalidos.Take(10)
                            .Select(x => $"{x.Key.Item2}:{x.Factor:N10}"));

                    throw new InvalidOperationException(
                        "Hay cuentas cuya suma de factores no es 1. " + cuentas);
                }

                Dictionary<(int Empresa, string Cuenta), SaldoCuentaSongDto> dicSaldos =
                    lstSaldos
                        .GroupBy(x => (x.intEmpresa, NormalizarCuenta(x.strCuenta)))
                        .ToDictionary(x => x.Key, x => x.First());

                Dictionary<string, List<ProcesoCostoAplicacionDto>> dicAplicaciones =
                    lstAplicaciones
                        .GroupBy(x => (x.strPcCodigo ?? string.Empty).Trim(),
                            StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            x => x.Key,
                            x => x.ToList(),
                            StringComparer.OrdinalIgnoreCase);

                var objMotor = new MotorDistribucionCostoProductivo(_objLogger);

                CostoProductivoDriversDto objDrivers =
                    objMotor.Construir(
                        objDataProceso,
                        lstRpcCompleto,
                        lstAplicaciones);

                List<CostoProductivoCuentaDto> lstResultado =
                    new List<CostoProductivoCuentaDto>(lstConfig.Count + 1);

                int secuencia = 1;

                foreach (CostoProductivoConfigCuentaDto config in lstConfig)
                {
                    string cuenta = NormalizarCuenta(config.strCuenta);

                    dicSaldos.TryGetValue(
                        (config.intEmpresa, cuenta),
                        out SaldoCuentaSongDto? saldo);

                    decimal debeFuente = 0m;
                    decimal creditoFuente = 0m;
                    string naturaleza = "D";

                    if (saldo != null)
                    {
                        (debeFuente, creditoFuente) =
                            saldo.ObtenerAcumulado(intMes);

                        naturaleza = string.IsNullOrWhiteSpace(saldo.strNaturaleza)
                            ? "D"
                            : saldo.strNaturaleza.Trim().ToUpperInvariant();
                    }

                    decimal montoFuente = naturaleza == "C"
                        ? creditoFuente - debeFuente
                        : debeFuente - creditoFuente;

                    montoFuente = Math.Round(montoFuente, 4);

                    decimal factor = config.dcFactorAsignacion <= 0m
                        ? 1m
                        : config.dcFactorAsignacion;

                    // Para 50301040104:
                    //   CLA = saldo SONG * 0.6312571992
                    //   CAJ = saldo SONG * 0.3687428008
                    decimal montoCuenta =
                        Math.Round(montoFuente * factor, 4);

                    decimal debe =
                        Math.Round(debeFuente * factor, 4);

                    decimal credito =
                        Math.Round(creditoFuente * factor, 4);

                    var row = new CostoProductivoCuentaDto
                    {
                        intId = secuencia++,
                        strOrigen = "CONTABLE",
                        strEtapa = config.strProcesoCosto,
                        strGrupoEtapa = config.strEtapaGeneral,
                        strEcCodigo = config.strEcCodigo,
                        strPcCodigo = config.strPcCodigo,

                        // BASE ACTUALIZADA K/L/Q/R/S.
                        strTipo = config.strTipo,
                        strTipoCosto = config.strTipoCosto,
                        strTipo3 = config.strTipo3,
                        strAgrupacionCentro = config.strAgrupacionCentro,
                        strGrupoCentro = config.strGrupoCentro,

                        strCuenta = cuenta,
                        strCentroCodigo = config.strCentroCodigo,
                        strSubcentroCodigo = config.strSubcentroCodigo,
                        strCentroCosto =
                            saldo?.ObtenerDescripcionJerarquia(config.strCentroCodigo)
                            ?? config.strCentroCodigo,
                        strSubcentroCosto =
                            saldo?.ObtenerDescripcionJerarquia(config.strSubcentroCodigo)
                            ?? config.strSubcentroCodigo,
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

                    string pc =
                        (config.strPcCodigo ?? string.Empty)
                        .Trim()
                        .ToUpperInvariant();

                    string centro =
                        (config.strCentroCodigo ?? string.Empty).Trim();

                    string tipoCosto =
                        (config.strTipoCosto ?? string.Empty)
                        .Trim()
                        .ToUpperInvariant();

                    // Q / tb_procesocosto manda los costos estructurales.
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
                            /*
                             * Regla 504:
                             * Q conserva la etapa operativa (por ejemplo Tunel),
                             * pero el monto NO se clasifica como Entero/Cola/VAG.
                             * L determina Fijo/Variable indirecto.
                             */
                            if (centro.StartsWith(
                                "504",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                if (tipoCosto == "FIJO")
                                {
                                    row.dcCostoIndirectoFijo = montoCuenta;
                                    row.strClasificacionMonto =
                                        "COSTO_INDIRECTO_FIJO";
                                }
                                else
                                {
                                    row.dcCostoIndirectoVariable = montoCuenta;
                                    row.strClasificacionMonto =
                                        "COSTO_INDIRECTO_VARIABLE";
                                }

                                row.strOrigenDistribucion =
                                    "504_TIPO_COSTO_L";
                            }
                            else
                            {
                                DistribucionMontoProductoDto dist =
                                    objMotor.DistribuirMonto(
                                        pc,
                                        montoCuenta,
                                        objDrivers,
                                        cuenta);

                                row.dcMontoEntero = dist.dcEntero;
                                row.dcMontoCola = dist.dcCola;
                                row.dcMontoVag = dist.dcValorAgregado;
                                row.dcLibrasDriver = dist.dcLibrasDriver;
                                row.dcPesoDriver = dist.dcPesoDriver;
                                row.strOrigenDistribucion = dist.strOrigen;
                                row.strClasificacionMonto =
                                    dist.blUsoDriver
                                        ? "DISTRIBUCION_DRIVER"
                                        : "FALLBACK_CUENTA";
                            }
                            break;
                    }

                    List<ProcesoCostoAplicacionDto> aplicaciones =
                        dicAplicaciones.GetValueOrDefault(
                            pc,
                            new List<ProcesoCostoAplicacionDto>());

                    row.strProcesosAplicables =
                        aplicaciones.Count == 0
                            ? "SIN CONFIGURACION SI/X2/TAR"
                            : string.Join(
                                ", ",
                                aplicaciones.Select(x =>
                                    x.strConfig == "X2"
                                        ? $"{x.strTipoProceso} (x2)"
                                        : x.strConfig == "TAR"
                                            ? $"{x.strTipoProceso} (tarifa)"
                                            : x.strTipoProceso));

                    row.intCantidadDestinos = aplicaciones.Count;
                    row.RecalcularTotales();
                    lstResultado.Add(row);
                }

                // Reclasificación: NO crea costo nuevo.
                // Se retira del VAG general y se agrega a Descongelado.
                CostoProductivoCuentaDto? rowDescongelado =
                    objMotor.AplicarReclasificacionDescongelado(
                        lstResultado,
                        objDrivers.objDescongelado,
                        objConfigCostos.objDescongelado);

                foreach (CostoProductivoCuentaDto fila in lstResultado)
                {
                    if (fila.blEsDerivado)
                        continue;

                    List<ProcesoCostoAplicacionDto> aplicacionesFila =
                        dicAplicaciones.GetValueOrDefault(
                            (fila.strPcCodigo ?? string.Empty).Trim(),
                            new List<ProcesoCostoAplicacionDto>());

                    fila.lstDetalleProcesosAplicables =
                        objMotor.ConstruirDetalleProcesosAplicables(
                            fila.strPcCodigo,
                            fila.dcTotal,
                            objDrivers,
                            aplicacionesFila);
                }


                if (rowDescongelado != null)
                {
                    rowDescongelado.intId = secuencia++;
                    lstResultado.Add(rowDescongelado);
                }

                CostoProductivoResumenDto resumen =
                    CostoProductivoResumenDto.Crear(
                        lstResultado,
                        objDrivers.objDescongelado);

                return new CostoProductivoResultadoDto
                {
                    lstCuentas = lstResultado,
                    objResumen = resumen,
                    objDescongelado = objDrivers.objDescongelado
                };
            }
            catch (Exception objException)
            {
                _objLogger.LogError(
                    $"Error Feature : {nameof(OperacionComercialFeature)} " +
                    $"funcion : {nameof(ObtenerCostoProductivo)} " +
                    $"error: {objException.Message}");
                throw;
            }
        }

        public async Task<CostoProductivoResultadoDto> GuardarCostoProductivo(
            int intAnio,
            int intMes,
            string strUsuario)
        {
            CostoProductivoResultadoDto resultado =
                await ObtenerCostoProductivo(intAnio, intMes);

            if (!resultado.objResumen.blCuadra)
            {
                throw new InvalidOperationException(
                    $"La distribución no cuadra. " +
                    $"Diferencia: {resultado.objResumen.dcDiferencia:N4}. " +
                    $"Descongelado sin asignar: " +
                    $"{resultado.objResumen.dcDescongeladoSinAsignar:N4}.");
            }

            DateOnly fechaCorte = new DateOnly(
                intAnio,
                intMes,
                DateTime.DaysInMonth(intAnio, intMes));

            bool guardado =
                await _objCostoProductivoService
                    .GuardarDistribucionProcesoProductivo(
                        intAnio,
                        intMes,
                        fechaCorte,
                        resultado.lstCuentas,
                        strUsuario);

            if (!guardado)
                throw new InvalidOperationException(
                    "No se pudo guardar el costo productivo.");

            return resultado;
        }

        private static string NormalizarCuenta(string? cuenta) =>
            (cuenta ?? string.Empty).Trim();

    }
}
