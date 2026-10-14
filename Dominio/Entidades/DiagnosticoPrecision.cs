using CostManagement.Aplicación.DTos;
using Microsoft.Extensions.Logging;

namespace CostManagement.Dominio.Entidades;

// Diagnóstico temporal: desactivar desde esta constante al cerrar el seguimiento.
public static class DiagnosticoPrecision
{
    public const bool blActivo = true;
    private static readonly HashSet<(int intLote, int intProd, int intTalla)> _hshSeguimientoPrecision = new()
        { (189571, 1006, 1045), (798728, 1006, 1045) };
    public static bool EsSeguimiento(int intLote, int intProd, int intTalla) => _hshSeguimientoPrecision.Contains((intLote, intProd, intTalla));
    public static bool EsUnitarioDosDecimales(decimal dcUnitario) => dcUnitario == Math.Round(dcUnitario, 2);
    public static void Diccionario(ILogger? objLogger, string strPunto, int intClaves, int intDosDecimales)
    {
        if (blActivo) objLogger?.LogInformation("[DiagPrecision] {Punto} Claves={Claves} ClavesUnitarioHasta2DecAntes={DosDecimales}", strPunto, intClaves, intDosDecimales);
    }
    public static void Diferencia(ILogger? objLogger, string strPunto, decimal dcDelta)
    {
        if (blActivo && Math.Abs(dcDelta) > 0.01m) objLogger?.LogWarning("[DiagPrecision] {Punto} Delta={Delta}", strPunto, dcDelta);
    }
    public static void Liquidaciones(ILogger objLogger, string strPunto, IEnumerable<LiquidacionResultado> lstLiquidaciones)
    {
        if (!blActivo) return;
        foreach (var objGrupo in lstLiquidaciones.GroupBy(x => x.strTipoLiq == "LIQ_PFR" ? "PFR" : "RPC"))
        {
            var lstFilas = objGrupo.GroupBy(x => (x.intLote, x.intCodProd, x.intLidCodTal)).Select(g => new
            {
                intLote = g.Key.intLote, intProd = g.Key.intCodProd, intTalla = g.Key.intLidCodTal,
                dcLibras = g.Sum(x => (decimal)x.dcLibras), dcTotal = g.Sum(x => x.dcTotalDolSum),
                dcReconstruido = g.Sum(x => (x.dcCostoTotXLibra ?? 0m) * (decimal)x.dcLibras)
            }).ToList();
            decimal dcTotal = lstFilas.Sum(x => x.dcTotal), dcReconstruido = lstFilas.Sum(x => x.dcReconstruido);
            string strTop = string.Join(" | ", lstFilas.OrderByDescending(x => Math.Abs(x.dcTotal - x.dcReconstruido)).Take(10)
                .Select(x => $"Lote={x.intLote} CodProd={x.intProd} Talla={x.intTalla} Libras={x.dcLibras} Total={x.dcTotal} Unit={(x.dcLibras == 0m ? 0m : x.dcReconstruido / x.dcLibras)} Delta={x.dcTotal - x.dcReconstruido}"));
            objLogger.LogInformation("[DiagPrecision] {Punto} Origen={Origen} Total={Total} UnitarioPorLibras={Reconstruido} Delta={Delta} Top10={Top}", strPunto, objGrupo.Key, dcTotal, dcReconstruido, dcTotal - dcReconstruido, strTop);
        }
        Diferencia(objLogger, strPunto, lstLiquidaciones.Sum(x => x.dcTotalDolSum - (x.dcCostoTotXLibra ?? 0m) * (decimal)x.dcLibras));
    }
    public static void Reproceso(ILogger objLogger, string strPunto, IEnumerable<MatPrimaReproceso> lstReproceso)
    {
        if (!blActivo) return;
        var lstRecibido = lstReproceso.Where(x => x.strAgrupacion == "1. RECIBIDO").ToList();
        decimal dcTotal = lstRecibido.Sum(x => x.dbCostoTotal), dcReconstruido = lstRecibido.Sum(x => x.dbCostoXSecuencial * (decimal)x.dbLibras);
        objLogger.LogInformation("[DiagPrecision] {Punto} RECIBIDO Filas={Filas} Total={Total} UnitarioPorLibras={Reconstruido} Delta={Delta}", strPunto, lstRecibido.Count, dcTotal, dcReconstruido, dcTotal - dcReconstruido);
        Diferencia(objLogger, strPunto, dcTotal - dcReconstruido);
    }
    public static void Movimientos(ILogger objLogger, string strPunto, IEnumerable<CostVentUni> lstMovimientos)
    {
        if (!blActivo) return;
        var lstSeguimiento = lstMovimientos.Where(x => EsSeguimiento(x.intLote, x.intCodProd, x.intCodTalla)
            || EsSeguimiento(x.intLoteUni ?? 0, x.intCodProd, x.intCodTalla) || EsSeguimiento(x.intLoteOrigen ?? 0, x.intCodProd, x.intCodTalla)).ToList();
        foreach (var objFila in lstSeguimiento)
            objLogger.LogInformation("[DiagPrecision] {Punto} Seguimiento Lote={Lote} LoteSecuencial={Secuencial} Origen={Origen} Prod={Prod} Talla={Talla} Tipo={Tipo} TipoLiq={TipoLiq} Libras={Libras} CostoUnit={CostoUnit} CostoTot={CostoTot}", strPunto, objFila.intLote, objFila.intLoteUni, objFila.intLoteOrigen, objFila.intCodProd, objFila.intCodTalla, objFila.strTipo, objFila.strTipoLiq, objFila.dcLibras, objFila.dcCostoUnit, objFila.dcCostoTot);
        decimal dcIngreso = lstSeguimiento.Where(x => x.strTipo == "I").Sum(x => x.dcCostoTot), dcEgreso = lstSeguimiento.Where(x => x.strTipo == "E").Sum(x => x.dcCostoTot);
        objLogger.LogInformation("[DiagPrecision] {Punto} Seguimiento Ingreso={Ingreso} Egreso={Egreso} Delta={Delta}", strPunto, dcIngreso, dcEgreso, dcIngreso + dcEgreso);
        Diferencia(objLogger, strPunto, dcIngreso + dcEgreso);
    }
}
