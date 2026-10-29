using CostManagement.Infraestructura.Utils;
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
        // LB corresponde a laboratorio/muestras.
        private static readonly HashSet<string> _hshEgresosVenta = new(StringComparer.OrdinalIgnoreCase)
            { "EX", "EV", "EOB", "EXP", "LB" };
        private static readonly HashSet<string> _hshAjustes = new(StringComparer.OrdinalIgnoreCase) { "EAJ", "IAJ" };

        // flujo exclusivo del Excel, sin modificar el detalle original ni el disponible del front.
        public async Task<(List<CostoVentaMovimientoHojaDto> lstCostoDetallado,
            List<CostoVentaGlobalProdTallaDto> lstCostoGlobal,
            List<CostoVentaMovimientoHojaDto> lstEgresosVenta,
            List<CostoVentaMovimientoHojaDto> lstOtrosEgresosCamara,
            List<CostoVentaMovimientoHojaDto> lstInventarioFinal)> ConsultarCostoVentaUniLibro(
                DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            var trDetalle = ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
            var trInvVal = _objCostoMateriaPrima.ObtenerInventarioValorizadoBase(dtFechaInicio, dtFechaFin);
            await Task.WhenAll(trDetalle, trInvVal);
            var lstDetalle = trDetalle.Result;
            var objCostosInv = MotorValoracionCostoVenta.CostosUnitariosInventario(trInvVal.Result);

            static string Normalizar(string? strValor) => (strValor ?? string.Empty).Trim().ToUpperInvariant();
            static bool TieneFlag(string strFlags, string strFlag) => strFlags.Split(" | ").Contains(strFlag);
            // static CostoVentaMovimientoHojaDto CrearFila(CostVentUni objDetalle) => new()
            CostoVentaMovimientoHojaDto CrearFila(CostVentUni objDetalle) => new()
            {
                strTipoLiq = Normalizar(objDetalle.strTipoLiq),
                strTipo = Normalizar(objDetalle.strTipo),
                intLoteUni = objDetalle.intLoteUni,
                intLote = objDetalle.intLote,
                intCodProd = objDetalle.intCodProd,
                intCodTalla = objDetalle.intCodTalla,
                strDescripcion = objDetalle.strDescripcion ?? string.Empty,
                strTalla = objDetalle.strTalla ?? string.Empty,
                strProcProd = objDetalle.strProcProd ?? string.Empty,
                strAgrupacion = objDetalle.strAgrupacion ?? string.Empty,
                strTipoProd = objDetalle.strTipoProd ?? string.Empty,
                strCongelamiento = objDetalle.strCongelamiento ?? string.Empty,
                strClase = objDetalle.strClase ?? string.Empty,
                dcLibras = objDetalle.dcLibras,
                dcCostoUnit = Math.Abs(objDetalle.dcCostoUnit),
                dcCostoTot = objDetalle.dcCostoTot,
                strCodMov = Normalizar(objDetalle.strCodMov),
                dtFecha = objDetalle.dtFechaMov, dcMasters = objDetalle.dcMasters, dtFechaCorte = dtFechaFin,
                strDescriMov = objDetalle.strDescriMov ?? string.Empty
            };

            var lstCostoDetallado = new List<CostoVentaMovimientoHojaDto>();
            foreach (var objDetalle in lstDetalle.Where(x => string.IsNullOrWhiteSpace(x.strCodMov)))
            {
                var objFila = CrearFila(objDetalle);
                string strAgrupacion = Normalizar(objFila.strAgrupacion);
                objFila.strOrigen = objFila.strTipoLiq == "INVENTARIO" ? "INVENTARIO INICIAL"
                    : objFila.strTipoLiq == "LIQ_PFR" ? "LIQUIDACION FRESCO"
                    : strAgrupacion == "1. RECIBIDO" ? "REPROCESO RECIBIDO"
                    : strAgrupacion == "2. PROCESADO" ? "REPROCESO PROCESADO" : "REPROCESO";
                var lstFlags = new List<string>();
                decimal dcCostoConSigno = Math.Sign(objFila.dcLibras) * Math.Abs(objFila.dcCostoTot);
                if (dcCostoConSigno != objFila.dcCostoTot) lstFlags.Add("SIGNO_AJUSTADO");
                objFila.dcCostoTot = dcCostoConSigno;
                if (objFila.dcCostoTot == 0m) lstFlags.Add("COSTO_0");
                objFila.strRevisar = string.Join(" | ", lstFlags);
                lstCostoDetallado.Add(objFila);
            }

            _objLogger.LogInformation("desactivado: se conserva el costo del PROCESADO del motor.");

            // costos ponderados exclusivamente de ingresos base, antes de agregar movimientos.
            int intFilasBase = lstCostoDetallado.Count;
            var objValorador = new ValoradorMovimientos(lstCostoDetallado.Select(x => new FilaValoracion(x.strTipoLiq, x.strTipo, x.intLote, x.intCodProd, x.intCodTalla,
                    x.strAgrupacion ?? "", x.strCodMov ?? "", x.dcLibras, x.dcCostoTot)), objCostosInv);
            foreach (var objDetalle in lstDetalle.Where(x => !string.IsNullOrWhiteSpace(x.strCodMov)
                && !_hshEgresosVenta.Contains(Normalizar(x.strCodMov)) && !_hshAjustes.Contains(Normalizar(x.strCodMov))))
            {
                var objFila = CrearFila(objDetalle);
                var objVal = objValorador.CostoMovimiento(objFila.intLote, objFila.intCodProd, objFila.intCodTalla);
                objFila.strOrigen = "MOVIMIENTO CAMARA";
                objFila.dcCostoUnit = objVal.dcCostoUnit;
                objFila.dcCostoTot = objFila.dcLibras * objVal.dcCostoUnit;
                objFila.strRevisar = objVal.strFlag;
                objFila.strFuenteCosto = objVal.strFuente;
                lstCostoDetallado.Add(objFila);
            }

            var dicCosto = MotorValoracionCostoVenta.CostoPorProdTalla(
                lstCostoDetallado.Select(x => new FilaValoracion(x.strTipoLiq, x.strTipo, x.intLote, x.intCodProd, x.intCodTalla,
                    x.strAgrupacion ?? "", x.strCodMov ?? "", x.dcLibras, x.dcCostoTot)), objCostosInv);

            string[] arrFlagsGlobal = { "LIBRAS<=0", "COSTO_TOTAL<=0", "COSTO>7", "COSTO>8",
                "INV_LIBRAS_NEGATIVAS", "INV_COSTO_0", "SIGNO_AJUSTADO", "SIN_COSTO_GLOBAL", "COSTO_SOLO_INGRESOS" };
            var dicCantidadPorFlag = arrFlagsGlobal.ToDictionary(strFlag => strFlag, _ => 0);
            var lstCostoGlobal = new List<CostoVentaGlobalProdTallaDto>();
            foreach (var objGrupo in lstCostoDetallado.GroupBy(x => (x.intCodProd, x.intCodTalla)))
            {
                var objPrimero = objGrupo.First();
                var objGlobal = new CostoVentaGlobalProdTallaDto
                {
                    intCodProd = objGrupo.Key.intCodProd,
                    intCodTalla = objGrupo.Key.intCodTalla,
                    strTalla = objPrimero.strTalla,
                    strDescripcion = objPrimero.strDescripcion,
                    strTipoProd = objPrimero.strTipoProd,
                    strCongelamiento = objPrimero.strCongelamiento,
                    strClase = objPrimero.strClase,
                    dcLibrasTotales = objGrupo.Sum(x => x.dcLibras),
                    dcCostoTot = objGrupo.Sum(x => x.dcCostoTot),
                    dcLibrasIngreso = objGrupo.Where(x => x.strTipo == "I").Sum(x => x.dcLibras),
                    dcCostoIngreso = objGrupo.Where(x => x.strTipo == "I").Sum(x => x.dcCostoTot),
                    dcLibrasEgreso = objGrupo.Where(x => x.strTipo == "E").Sum(x => x.dcLibras),
                    dcCostoEgreso = objGrupo.Where(x => x.strTipo == "E").Sum(x => x.dcCostoTot),
                    intCantidadMovimientos = objGrupo.Count()
                };
                var objCostoAplicado = dicCosto.TryGetValue(objGrupo.Key, out var objCostoEncontrado)
                    ? objCostoEncontrado : new CostoAplicado(0m, FuenteCosto.SinCosto);
                objGlobal.dcCostoGlobal = objCostoAplicado.strFuente != FuenteCosto.SinCosto
                    ? objCostoAplicado.dcCostoUnit : null;
                objGlobal.strFuenteCosto = objCostoAplicado.strFuente;
                var lstFlags = new List<string>();
                if (objGlobal.dcLibrasTotales <= 0m) lstFlags.Add("LIBRAS<=0");
                if (objGlobal.dcCostoTot <= 0m) lstFlags.Add("COSTO_TOTAL<=0");
                if (objGlobal.dcCostoGlobal > dcUmbralAtencionCostoGlobal) lstFlags.Add("COSTO>7");
                if (objGlobal.dcCostoGlobal > dcUmbralRevisionCostoGlobal) lstFlags.Add("COSTO>8");
                if (objGrupo.Any(x => x.strTipoLiq == "INVENTARIO" && x.dcLibras < 0m))
                    lstFlags.Add("INV_LIBRAS_NEGATIVAS");
                if (objGrupo.Any(x => x.strTipoLiq == "INVENTARIO" && x.dcCostoTot == 0m))
                    lstFlags.Add("INV_COSTO_0");
                if (objGrupo.Any(x => TieneFlag(x.strRevisar, "SIGNO_AJUSTADO"))) lstFlags.Add("SIGNO_AJUSTADO");
                if (objCostoAplicado.strFuente == FuenteCosto.SinCosto) lstFlags.Add("SIN_COSTO_GLOBAL");
                if (objCostoAplicado.strFuente == FuenteCosto.IngresosPeriodo) lstFlags.Add("COSTO_SOLO_INGRESOS");
                objGlobal.strRevisar = string.Join(" | ", lstFlags);
                foreach (string strFlag in lstFlags) dicCantidadPorFlag[strFlag]++;
                lstCostoGlobal.Add(objGlobal);
            }
            lstCostoGlobal = lstCostoGlobal.OrderByDescending(x => x.strRevisar.Length > 0)
                .ThenBy(x => x.intCodProd).ThenBy(x => x.strTalla).ToList();
            // dicCosto comparte costo y fuente entre el global y los egresos.

            CostoVentaMovimientoHojaDto ValorarCamara(CostVentUni objDetalle, bool blVenta)
            {
                var objFila = CrearFila(objDetalle);
                objFila.strOrigen = blVenta ? "EGRESO VENTA" : "OTRO CAMARA";
                bool blExisteCosto = dicCosto.TryGetValue((objFila.intCodProd, objFila.intCodTalla), out var objCostoAplicado);
                bool blTieneCosto = blExisteCosto && objCostoAplicado.strFuente != FuenteCosto.SinCosto
                    && objCostoAplicado.dcCostoUnit > 0m;
                objFila.dcCostoUnit = blTieneCosto ? MotorValoracionCostoVenta.Truncar4(objCostoAplicado.dcCostoUnit) : 0m;
                objFila.dcCostoTot = MotorValoracionCostoVenta.CostoTotalEgreso(
                    objFila.dcLibras, objFila.dcCostoUnit, blVenta || objFila.strTipo == "E");
                objFila.strFuenteCosto = blExisteCosto ? objCostoAplicado.strFuente : FuenteCosto.SinCosto;
                if (!blTieneCosto) objFila.strRevisar = "SIN_COSTO_GLOBAL";
                return objFila;
            }

            var lstMovimientosCamara = lstDetalle.Where(x => !string.IsNullOrWhiteSpace(x.strCodMov))
                .Select(x => (objDetalle: x, strCod: Normalizar(x.strCodMov))).ToList();
            var lstEgresosVenta = lstMovimientosCamara.Where(x => _hshEgresosVenta.Contains(x.strCod))
                .Select(x => ValorarCamara(x.objDetalle, true)).ToList();
            var lstOtrosEgresosCamara = lstMovimientosCamara.Where(x => _hshAjustes.Contains(x.strCod))
                .Select(x => ValorarCamara(x.objDetalle, false)).ToList();
            var lstInventarioFinal = lstCostoDetallado.Concat(lstEgresosVenta).Concat(lstOtrosEgresosCamara)
                .Select(x =>
                {
                    var objCopia = x.CopiarParaInventarioFinal();
                    if (x.strOrigen == "MOVIMIENTO CAMARA") objCopia.strOrigen = "COSTO DETALLADO";
                    return objCopia;
                })
                .OrderBy(x => x.intCodProd).ThenBy(x => x.strTalla).ThenBy(x => x.intLote).ToList();

            // conteos de filas sobre la unión final, sin duplicarlas por aparecer en dos hojas.
            _objLogger.LogInformation(
                "Costo Detallado={Detalle}; Costo Global={Global}; Egresos Venta={Ventas}; Otros Egresos Camara={Camara}; Inventario Final={Final}; grupos por flag: {Flags}; filas SIN_COSTO_GLOBAL={SinCosto}; filas SIGNO_AJUSTADO={SignoAjustado}; bases={Bases}; MOVIMIENTO CAMARA={MovimientosCamara}; COSTO_LOTE={CostoLote}; COSTO_PRODTALLA={CostoProdTalla}; SIN_COSTO_LOTE={SinCostoLote}; COSTO_INV_LOTE={CostoInvLote}; COSTO_INGRESOS_PRODTALLA={CostoIngresosProdTalla}",
                lstCostoDetallado.Count, lstCostoGlobal.Count, lstEgresosVenta.Count, lstOtrosEgresosCamara.Count,
                lstInventarioFinal.Count, string.Join(" | ", arrFlagsGlobal.Select(x => $"{x}: {dicCantidadPorFlag[x]}")),
                lstInventarioFinal.Count(x => TieneFlag(x.strRevisar, "SIN_COSTO_GLOBAL")),
                lstInventarioFinal.Count(x => TieneFlag(x.strRevisar, "SIGNO_AJUSTADO")),
                intFilasBase, lstCostoDetallado.Count - intFilasBase,
                lstCostoDetallado.Count(x => TieneFlag(x.strRevisar, "COSTO_LOTE")),
                lstCostoDetallado.Count(x => TieneFlag(x.strRevisar, "COSTO_PRODTALLA")),
                lstCostoDetallado.Count(x => TieneFlag(x.strRevisar, "SIN_COSTO_LOTE")),
                lstCostoDetallado.Count(x => TieneFlag(x.strRevisar, "COSTO_INV_LOTE")),
                lstCostoDetallado.Count(x => TieneFlag(x.strRevisar, "COSTO_INGRESOS_PRODTALLA")));
            return (lstCostoDetallado, lstCostoGlobal, lstEgresosVenta, lstOtrosEgresosCamara, lstInventarioFinal);
        }
        // diario desde Costo Detallado y Otros Egresos Camara al CU global absoluto.
        public async Task<List<DiarioCosto>> ConsultarDiarioMovimientoHojas(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            var objLibro = await ConsultarCostoVentaUniLibro(dtFechaInicio, dtFechaFin);
            var dicCu = objLibro.lstCostoGlobal.Where(x => x.dcCostoGlobal.HasValue && x.dcCostoGlobal.Value != 0m)
                .ToDictionary(x => (x.intCodProd, x.intCodTalla), x => MotorValoracionCostoVenta.Truncar4(Math.Abs(x.dcCostoGlobal!.Value)));
            var lstDiario = new List<DiarioCosto>();
            int intSinCosto = 0;
            foreach (var objFila in objLibro.lstCostoDetallado.Concat(objLibro.lstOtrosEgresosCamara))
            {
                bool blCu = dicCu.TryGetValue((objFila.intCodProd, objFila.intCodTalla), out decimal dcCu);
                if (!blCu) intSinCosto++;
                lstDiario.Add(new DiarioCosto
                {
                    strTipo = objFila.strTipo, dtFechaMov = objFila.dtFecha ?? dtFechaFin.ToDateTime(TimeOnly.MinValue),
                    strDescripcion = string.IsNullOrWhiteSpace(objFila.strDescriMov) ? objFila.strOrigen : objFila.strDescriMov,
                    strProdDescri = objFila.strProcProd, intLote = objFila.intLote, strProClas01 = objFila.strClase,
                    strProClas05 = objFila.strTipoProd, strTalDescri = objFila.strTalla, strProCodcor = objFila.intCodProd.ToString(),
                    strProDesesp = objFila.strDescripcion, dcLibras = objFila.dcLibras, dcMasters = objFila.dcMasters ?? 0m,
                    dcCostoUnit = blCu ? dcCu : 0m,
                    dcCostoTot = Math.Round(objFila.dcLibras * dcCu, 2, MidpointRounding.AwayFromZero),
                    strProCod = objFila.strCodMov, strDescri = objFila.strDescriMov
                });
            }
            if (intSinCosto > 0) _objLogger.LogWarning("diario-mov: {SinCosto} movimientos sin CU global (costo 0).", intSinCosto);
            return lstDiario;
        }

        // umbrales exclusivos de la hoja Costo Global.
        private const decimal dcToleranciaLibrasCostoGlobal = 0.10m;
        private const decimal dcUmbralAtencionCostoGlobal = 7m;
        private const decimal dcUmbralRevisionCostoGlobal = 8m;
        private const decimal dcUmbralRevisionCostoIngresos = 8m;

        // conjunto compartido por las hojas Disponible MP y Costo Global.
        private static readonly HashSet<string> _hshSalidasVenta = new(StringComparer.OrdinalIgnoreCase) { "EX", "EV", "EOB", "EXP" };

        private readonly ILogger<CalculoCostosFeature> _objLogger;
        private static readonly HashSet<int> _hshProdDiagnostico = new() { 5359, 3861 };
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
            List<MuestrasObsequiosDto> lstObsequiosFacturas = new();
            try
            {
                // lectura de inventario en paralelo con las consultas del reporte.
                var trInvVal = _objCostoMateriaPrima.ObtenerInventarioValorizadoBase(dtFechaInicio, dtFechaFin);
                var lstPesosReales = await _objVentasFacturacionService.ObtenerRepFactPesoRealXRangoFecha(dtFechaInicio, dtFechaFin);
                lstCostVentUni = await ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
                lstVentaLocal = await _objVentasFacturacionService.ObtenerFacturasXRangoFecha(dtFechaInicio, dtFechaFin);
                lstObsequiosFacturas = await _objMateriaPrima.ObtenerMuestrasObsequios(dtFechaInicio, dtFechaFin);
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
                // ASIGNACIÓN ANTERIOR (promedio Σ dólares / Σ libras sobre todas las filas), reemplazada por el costo global sin exportaciones.
                //_objMotorAsigPrec.AsignarCostVentUnitDiarioVenta(lstCostVentUni, lstReporteVentFact);
                var objCostosInv = MotorValoracionCostoVenta.CostosUnitariosInventario(await trInvVal);
                Dictionary<(int, int), decimal> dicCostoGlobal = MotorValoracionCostoVenta.CostoPorProdTallaDesdeDetalle(
                    lstCostVentUni.Select(x => new FilaValoracion(x.strTipoLiq, x.strTipo, x.intLote, x.intCodProd, x.intCodTalla,
                    x.strAgrupacion ?? "", x.strCodMov ?? "", x.dcLibras, x.dcCostoTot)), objCostosInv)
                    .Where(x => x.Value.strFuente != FuenteCosto.SinCosto)
                    .ToDictionary(x => x.Key, x => x.Value.dcCostoUnit);
                _objMotorAsigPrec.AsignarCostVentUnitGlobalDiarioVenta(lstCostVentUni, dicCostoGlobal, lstReporteVentFact, _hshProdDiagnostico);
                var lstMuestrasObsequios = lstObsequiosFacturas.Select(x => new RptVentaVsFactura(x)).ToList();
                _objMotorAsigPrec.AsignarCostVentUnitGlobalDiarioVenta(lstCostVentUni, dicCostoGlobal, lstMuestrasObsequios);
                lstReporteVentFact.AddRange(lstMuestrasObsequios);

                // SONG devuelve los doce meses: una sola consulta por cada anio cubierto.
                var dicSaldosPorAnio = new Dictionary<int, List<SaldoCuentaSongDto>>();
                for (int intAnio = dtFechaInicio.Year; intAnio <= dtFechaFin.Year; intAnio++)
                    dicSaldosPorAnio[intAnio] = await _objCostoProductivoService.ConsultarSaldosCuentaSong(intAnio);

                var objResultadoPools = PoolsGastosSong.Calcular(dicSaldosPorAnio, dtFechaInicio, dtFechaFin);
                foreach (var objPeriodo in objResultadoPools.Desconocidos.GroupBy(x => new { x.Anio, x.Mes }))
                    _objLogger.LogWarning(
                        "Gastos SONG sin clasificacion ADM/FIN/EXP/OTROS. Periodo {Anio}-{Mes}, cantidad {Cantidad}, monto total {MontoTotal}. Primeros 10 detalles: {Detalles}",
                        objPeriodo.Key.Anio, objPeriodo.Key.Mes, objPeriodo.Count(), objPeriodo.Sum(x => x.Monto),
                        string.Join("; ", objPeriodo.Take(10).Select(x => $"Cuenta {x.Cuenta}, monto {x.Monto}")));
                var objPools = objResultadoPools.Pools;
                var lstGastos = MotorGastosDiarioVenta.Calcular(lstReporteVentFact
                    .Select(x => new LineaGastosDiarioVenta(x.strTipo, x.dcLibras)).ToArray(), objPools);
                for (int i = 0; i < lstReporteVentFact.Count; i++)
                {
                    var objLinea = lstReporteVentFact[i];
                    objLinea.dcGastosAdministrativos = lstGastos[i].dcAdministrativos;
                    objLinea.dcGastosComercializacion = lstGastos[i].dcComercializacion;
                    objLinea.dcGastosExportacion = lstGastos[i].dcExportacion;
                    objLinea.dcGastosOtros = lstGastos[i].dcOtros;
                    objLinea.CalculosReporte();
                }
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
            return await _objCostoMateriaPrima.ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
        }

        /// <summary>
        /// Disponible a costo global: cada fila del disponible (por lote y consolidado) toma el costo ponderado global de su CodProd + Talla (CalcularCostoGlobalProdTalla, sin exportaciones).
        /// dcCostoXLibra = CU; costo de ingreso, de egreso y disponible = libras x CU. Sin CU global para el CodProd + Talla, la fila conserva el cálculo del motor.
        /// </summary>
        private void AplicarCostoGlobalDisponible(CostoVentaUnitarioResultadoDto objResultado, List<CostVentUni> lstDetalle)
        {
            var dicCostoGlobal = _objCostoMateriaPrima.CalcularCostoGlobalProdTalla(lstDetalle, false);
            foreach (CostoVentaDisponibleLoteDto objLote in objResultado.lstDisponibleLote)
            {
                if (!dicCostoGlobal.TryGetValue((objLote.intCodProd, objLote.intCodTalla), out decimal dcCostoGlobal)) continue;
                objLote.dcCostoXLibra = dcCostoGlobal; 
                objLote.dcCostoIngreso = objLote.dcLibrasIngreso * dcCostoGlobal; 
                objLote.dcCostoEgreso = -Math.Abs(objLote.dcLibrasEgreso) * dcCostoGlobal;
                objLote.dcCostoTotalDisponible = objLote.dcLibrasDisponible * dcCostoGlobal;
            }
            foreach (CostoVentaUnitarioDto objProd in objResultado.lstCostoVentaUnitario)
            {
                if (!dicCostoGlobal.TryGetValue((objProd.intCodProd, objProd.intCodTalla), out decimal dcCostoGlobal)) continue;
                objProd.dcCostoVentaUnitario = dcCostoGlobal; objProd.dcCostoTotalDisponible = objProd.dcLibrasDisponible * dcCostoGlobal;
            }
        }

        private static void AplicarSignoPorTipo(List<CostoVentaDisponibleMpDto> lstDisponible, List<CostVentUni> lstDetalle)
        {
            foreach (CostoVentaDisponibleMpDto objLote in lstDisponible) { objLote.dcCostoIngreso = Math.Abs(objLote.dcCostoIngreso); objLote.dcCostoEgreso = -Math.Abs(objLote.dcCostoEgreso); }
            foreach (CostVentUni objReg in lstDetalle)
            {
                if (objReg.strTipo == "E") objReg.dcCostoTot = -Math.Abs(objReg.dcCostoTot);
                else if (objReg.strTipo == "I") objReg.dcCostoTot = Math.Abs(objReg.dcCostoTot);
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
                AplicarCostoGlobalDisponible(objResultado, lstDetalleActual);

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

        // costo MP por la clave del motor, sin modificar los movimientos del detalle.
        private static List<CostoVentaDisponibleMpDto> AplicarCostoRealMateriaPrimaLote(
            CostoVentaUnitarioResultadoDto objResultado, List<CostVentUni> lstSinVentas)
        {
            var dicCostoMp = lstSinVentas.Where(x => x.strTipo == "I" && x.dcLibras > 0m)
                .GroupBy(x => ((int?)x.intLote, x.intCodProd, x.intCodTalla))
                .ToDictionary(x => x.Key, x => x.Sum(y => Math.Abs(y.dcCostoTot)) / x.Sum(y => y.dcLibras));
            var hshInventarioNegativo = lstSinVentas.Where(x => x.strTipo == "I" && x.dcLibras < 0m &&
                    string.Equals(x.strTipoLiq?.Trim(), "INVENTARIO", StringComparison.OrdinalIgnoreCase))
                .Select(x => ((int?)x.intLote, x.intCodProd, x.intCodTalla)).ToHashSet();
            var lstDisponible = new List<CostoVentaDisponibleMpDto>();
            foreach (var objLote in objResultado.lstDisponibleLote)
            {
                var objClave = (objLote.intLote, objLote.intCodProd, objLote.intCodTalla);
                bool blTieneCostoMp = dicCostoMp.TryGetValue(objClave, out decimal dcCostoMp);
                var lstFlags = new List<string>();
                if (dcCostoMp > dcUmbralAtencionCostoGlobal) lstFlags.Add("COSTO>7");
                if (dcCostoMp > dcUmbralRevisionCostoGlobal) lstFlags.Add("COSTO>8");
                if (!blTieneCostoMp) lstFlags.Add("SIN_COSTO_MP");
                if (objLote.dcLibrasDisponible < 0m) lstFlags.Add("SALDO_NEGATIVO");
                if (objLote.strEstadoSaldo == "EGRESO_SIN_INGRESO") lstFlags.Add("EGRESO_SIN_INGRESO");
                if (hshInventarioNegativo.Contains(objClave)) lstFlags.Add("INV_LIBRAS_NEGATIVAS");
                lstDisponible.Add(new CostoVentaDisponibleMpDto
                {
                    strTipoProceso = objLote.strTipoProceso,
                    intLote = objLote.intLote,
                    intCodProd = objLote.intCodProd,
                    intCodTalla = objLote.intCodTalla,
                    strTalla = objLote.strTalla,
                    strDescripcion = objLote.strDescripcion,
                    strTipoProd = objLote.strTipoProd,
                    strCongelamiento = objLote.strCongelamiento,
                    strClase = objLote.strClase,
                    dcLibrasIngreso = objLote.dcLibrasIngreso,
                    dcLibrasEgreso = objLote.dcLibrasEgreso,
                    dcLibrasDisponible = objLote.dcLibrasDisponible,
                    dcCostoXLibra = dcCostoMp,
                    dcCostoIngreso = objLote.dcLibrasIngreso * dcCostoMp,
                    dcCostoEgreso = -Math.Abs(objLote.dcLibrasEgreso) * dcCostoMp,
                    dcCostoTotalDisponible = objLote.dcLibrasDisponible * dcCostoMp,
                    intCantidadMovimientos = objLote.intCantidadMovimientos,
                    strEstadoSaldo = objLote.strEstadoSaldo,
                    strRevisar = string.Join(" | ", lstFlags)
                });
            }
            return lstDisponible;
        }

        // cálculo independiente; no modifica los movimientos ni sus signos.
        private List<CostoVentaGlobalHojaDto> ConstruirHojaCostoGlobal(List<CostVentUni> lstDetalle)
        {
            string[] arrFlags = { "LIBRAS_NETAS<=0", "DOLARES_NETOS<=0", "COSTO>7", "COSTO>8", "INV_LIBRAS_NEGATIVAS", "INV_COSTO_0", "ING>8" };
            var dicCantidadPorFlag = arrFlags.ToDictionary(strFlag => strFlag, _ => 0);
            var lstCostoGlobal = new List<CostoVentaGlobalHojaDto>();
            var lstGrupos = lstDetalle
                .Where(x => !(x.strTipo == "E" && _hshSalidasVenta.Contains(x.strCodMov?.Trim() ?? "")))
                .GroupBy(x => (x.intCodProd, x.intCodTalla));

            foreach (var objGrupo in lstGrupos)
            {
                var objPrimero = objGrupo.First();
                var lstIngresos = objGrupo.Where(x => x.strTipo == "I").ToList();
                var lstEgresos = objGrupo.Where(x => x.strTipo == "E").ToList();
                var lstInventario = lstIngresos.Where(x =>
                    string.Equals(x.strTipoLiq?.Trim(), "INVENTARIO", StringComparison.OrdinalIgnoreCase)).ToList();
                var objFila = new CostoVentaGlobalHojaDto
                {
                    intCodProd = objGrupo.Key.intCodProd,
                    intCodTalla = objGrupo.Key.intCodTalla,
                    strTalla = objPrimero.strTalla,
                    strDescripcion = objPrimero.strDescripcion,
                    strTipoProd = objPrimero.strTipoProd,
                    strCongelamiento = objPrimero.strCongelamiento,
                    strClase = objPrimero.strClase,
                    dcLibrasIngreso = lstIngresos.Sum(x => x.dcLibras),
                    dcCostoIngreso = lstIngresos.Sum(x => x.dcCostoTot),
                    dcLibrasEgreso = lstEgresos.Sum(x => x.dcLibras),
                    dcCostoEgreso = lstEgresos.Sum(x => x.dcCostoTot),
                    dcLibrasNetas = objGrupo.Sum(x => x.dcLibras),
                    dcDolaresNetos = objGrupo.Sum(x => x.dcCostoTot),
                    intCantidadMovimientos = objGrupo.Count()
                };
                objFila.dcCostoPonderadoGlobal =
                    objFila.dcLibrasNetas > dcToleranciaLibrasCostoGlobal && objFila.dcDolaresNetos > 0m
                        ? objFila.dcDolaresNetos / objFila.dcLibrasNetas : null;
                objFila.dcCostoSoloIngresos = objFila.dcLibrasIngreso > 0m
                    ? objFila.dcCostoIngreso / objFila.dcLibrasIngreso : null;

                var lstFlags = new List<string>();
                if (objFila.dcLibrasNetas <= 0m) lstFlags.Add("LIBRAS_NETAS<=0");
                if (objFila.dcLibrasNetas > 0m && objFila.dcDolaresNetos <= 0m) lstFlags.Add("DOLARES_NETOS<=0");
                if (objFila.dcCostoPonderadoGlobal > dcUmbralAtencionCostoGlobal) lstFlags.Add("COSTO>7");
                if (objFila.dcCostoPonderadoGlobal > dcUmbralRevisionCostoGlobal) lstFlags.Add("COSTO>8");
                if (lstInventario.Any(x => x.dcLibras < 0m)) lstFlags.Add("INV_LIBRAS_NEGATIVAS");
                if (lstInventario.Any(x => x.dcCostoTot == 0m && x.dcLibras != 0m)) lstFlags.Add("INV_COSTO_0");
                if (objFila.dcCostoSoloIngresos > dcUmbralRevisionCostoIngresos) lstFlags.Add("ING>8");
                objFila.strRevisar = string.Join(" | ", lstFlags);
                foreach (string strFlag in lstFlags) dicCantidadPorFlag[strFlag]++;
                lstCostoGlobal.Add(objFila);
            }

            _objLogger.LogInformation(
                "[CostoGlobal] Grupos={CantidadGrupos}; con flags={CantidadConFlags}; por flag: {ConteosFlags}",
                lstCostoGlobal.Count, lstCostoGlobal.Count(x => x.strRevisar.Length > 0),
                string.Join(" | ", arrFlags.Select(strFlag => $"{strFlag}: {dicCantidadPorFlag[strFlag]}")));

            return lstCostoGlobal.OrderByDescending(x => x.strRevisar.Length > 0)
                .ThenBy(x => x.intCodProd).ThenBy(x => x.strTalla).ToList();
        }

        public async Task<(List<CostoVentaDisponibleMpDto> lstDisponible, List<CostVentUni> lstDetalle, List<CostoVentaGlobalHojaDto> lstCostoGlobal)> ConsultarCostoVentaUniHojas(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                List<CostVentUni> lstDetalle = await ConsultarCostoVentaUni(dtFechaInicio, dtFechaFin);
                var lstSinVentas = lstDetalle.Where(x => !(x.strTipo == "E" &&
                    _hshSalidasVenta.Contains(x.strCodMov?.Trim() ?? ""))).ToList();
                CostoVentaUnitarioResultadoDto objResultado = _objMotorDisponibleInventario.Calcular(lstSinVentas);
                var lstDisponible = AplicarCostoRealMateriaPrimaLote(objResultado, lstSinVentas);
                string[] arrFlagsDisponible = { "COSTO>7", "COSTO>8", "SIN_COSTO_MP", "SALDO_NEGATIVO", "EGRESO_SIN_INGRESO", "INV_LIBRAS_NEGATIVAS" };
                _objLogger.LogInformation(
                    "[DisponibleMP] Lotes={CantidadLotes}; ventas excluidas={CantidadVentasExcluidas}; por flag: {ConteosFlags}",
                    lstDisponible.Count, lstDetalle.Count - lstSinVentas.Count,
                    string.Join(" | ", arrFlagsDisponible.Select(strFlag =>
                        $"{strFlag}: {lstDisponible.Count(x => x.strRevisar.Split(" | ").Contains(strFlag))}")));
                // materializar la hoja 3 antes del ajuste de signos de presentación.
                var lstCostoGlobal = ConstruirHojaCostoGlobal(lstDetalle);
                AplicarSignoPorTipo(lstDisponible, lstDetalle);
                return (lstDisponible, lstDetalle.OrderBy(x => x.intCodProd).ThenBy(x => x.intCodTalla).ThenBy(x => x.intLote).ToList(), lstCostoGlobal);
            }
            catch (Exception objException)
            {
                _objLogger.LogError(objException, "Error Feature {Feature} funcion {Funcion}.", nameof(OperacionComercialFeature), nameof(ConsultarCostoVentaUniHojas));
                throw;
            }
        }

        /// <summary>
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
                                // config.strAgrupacionCentro,
                                ClasificacionCentroCostoProductivo.ObtenerAgrupacion(config),

                            strGrupoCentro =
                                // config.strGrupoCentro,
                                ClasificacionCentroCostoProductivo.ObtenerGrupo(config),

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
                objMotor.CompletarAuditoriaDrivers(lstResultado, objDrivers, objDataProceso.lstLibrasParticion ?? new List<LibrasParticionDto>(), objDataProceso);


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
                            objDataProceso.lstLibrasParticion ?? new List<LibrasParticionDto>(), _objLogger);

                // participación de Table9 después de completar todos los procesos.
                CostoProductivoResumenEspecialBuilder.CalcularParticipacion(lstProcesosOrigen);

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

                // enriquecer solo nombres después de calcular todos los importes.
                var dicNombresCentro = await _objCostoProductivoService.ConsultarNombresCentroAsync(
                    lstResultado.Select(x => x.strCentroCodigo).Concat(lstResultado.Select(x => x.strSubcentroCodigo)));
                foreach (var objFila in lstResultado)
                {
                    objFila.strCentroNombre = dicNombresCentro.GetValueOrDefault((objFila.strCentroCodigo ?? string.Empty).Trim(), string.Empty);
                    objFila.strSubcentroNombre = dicNombresCentro.GetValueOrDefault((objFila.strSubcentroCodigo ?? string.Empty).Trim(), string.Empty);
                }

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
        /// Antes: No vuelve a consultar Producción, SONG ni a recalcular el costo productivo.
        /// valoriza la producción con parámetros del snapshot antes de abrir la transacción. (Anterior.)
        /// paso 1 guarda solo parámetros, Warren y distribución.
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
                objWarren = objWarren,
                objResumen = new CierreValorizacionResumenDto { strEstado = "PARAMETROS_GUARDADOS", blWarrenGuardado = true }
            };
        }

        public async Task<CierreValorizacionResultadoDto> GuardarCierreValorizacion(int intAnio, int intMes, string strUsuario)
        {
            #region Medicion paso 2
            // resumen raiz de todos los tramos inclusivos del paso 2.
            // using var objTiempoPaso2 = MedicionTiempos.IniciarMetodo(_objLogger, "GuardarCierreValorizacion", () => null);
            // using var objTiempoPreparacion = MedicionTiempos.IniciarTramo("cierre-contexto-configuracion-version");
            #endregion
            // await _objCostoProductivoService.ValidarParametrosCierreAsync(intAnio, intMes); // Antes: solo existencia.
            string strVersionParametros = await _objCostoProductivoService.ValidarParametrosCierreAsync(intAnio, intMes);
            CierreValorizacionContexto ctx = await _objCostoMateriaPrima.ObtenerContextoValorizacionCierreAsync(intAnio, intMes);
            await _objCostoProductivoService.PrepararConfiguracionCierreAsync(ctx);
            if (!string.Equals(strVersionParametros, await _objCostoProductivoService.ValidarParametrosCierreAsync(intAnio, intMes), StringComparison.Ordinal))
                throw new InvalidOperationException("Los parámetros del cierre cambiaron durante la lectura. Reintente la valorización.");
            // objTiempoPreparacion.Dispose();
            CierreValorizacionDto objValorizacion = await _objCostoMateriaPrima.ValorizarCierreAsync(intAnio, intMes, ctx);
            objValorizacion.strVersionParametros = strVersionParametros;
            // Inventario final del mes (misma lógica de materia-prima-sal; si no hay snapshot del último día, el último disponible del mes), calculado ANTES de abrir la transacción.
            DateOnly dtInicioMes = new(intAnio, intMes, 1), dtFinMes = new(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));
            List<InventarioVal> lstInventarioFinal = (await _objCostoMateriaPrima.ObtenerInventarioValorizado(dtInicioMes, dtFinMes, true)).Where(x => x.strTipo == "F").ToList();
            if (lstInventarioFinal.Count == 0) throw new InvalidOperationException($"No existe inventario de bodega (tb_boditesaldMes) en {intAnio}-{intMes:00} para guardar en el cierre.");
            // Antes: await _objCostoProductivoService.GuardarValorizacionCierreAtomico(intAnio, intMes, strUsuario, objValorizacion);
            await _objCostoProductivoService.GuardarValorizacionCierreAtomico(intAnio, intMes, strUsuario, objValorizacion, lstInventarioFinal);
            return new CierreValorizacionResultadoDto { objResumen = objValorizacion.objResumen };
        }

        public Task<AnularValorizacionesResultadoDto> AnularValorizacionesActivasAsync(string tipo, string usuario, int? anio = null, int? mes = null)
            => _objCostoProductivoService.AnularValorizacionesActivasAsync(tipo, usuario, anio, mes);

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
