using CostManagement.Aplicación.DTos;
using System.Globalization;

namespace CostManagement.Dominio.Reglas;

// contrato compartido de valoración para el libro y los diarios.
public static class FuenteCosto
{
    public const string CuadreGeneral = "CUADRE GENERAL DETALLADO";
    public const string InventarioFinal = "INVENTARIO FINAL";
    public const string InventarioInicial = "INVENTARIO INICIAL";
    public const string IngresosPeriodo = "INGRESOS DEL PERIODO";
    public const string SinCosto = "SIN COSTO";
}

public readonly record struct CostoAplicado(decimal dcCostoUnit, string strFuente);

public readonly record struct FilaValoracion(
    string strTipoLiq, string strTipo, int intLote, int intCodProd, int intCodTalla,
    string strAgrupacion, string strCodMov, decimal dcLibras, decimal dcCostoTot);

public sealed record CostosInventario(
    IReadOnlyDictionary<(int intCodProd, int intCodTalla), decimal> dicFinal,
    IReadOnlyDictionary<(int intCodProd, int intCodTalla), decimal> dicInicial);

public sealed record CostoMovimientoValorado(decimal dcCostoUnit, string strFlag, string strFuente);

public static class MotorValoracionCostoVenta
{
    public const decimal dcToleranciaLibras = 0.10m;

    public static decimal Truncar4(decimal dcValor) => Math.Truncate(dcValor * 10000m) / 10000m;

    public static decimal CostoTotalEgreso(decimal dcLibras, decimal dcCostoUnit, bool blNegativo) =>
        Math.Round((blNegativo ? -1 : 1) * Math.Abs(dcLibras) * Truncar4(dcCostoUnit),
            2, MidpointRounding.AwayFromZero);

    public static CostosInventario CostosUnitariosInventario(IEnumerable<InventarioVal> lstInventario)
    {
        var dicFinal = new Dictionary<(int intCodProd, int intCodTalla), decimal>();
        var dicInicial = new Dictionary<(int intCodProd, int intCodTalla), decimal>();
        var lstGrupos = lstInventario
            .Where(objFila => Normalizar(objFila.strCodTip) is "FINAL" or "INICIAL")
            .Select(objFila => (
                objFila,
                blCodigoValido: int.TryParse(objFila.strProCodcor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intCodProd),
                intCodProd))
            .Where(obj => obj.blCodigoValido)
            .GroupBy(obj => (
                obj.intCodProd,
                intCodTalla: (int)obj.objFila.stTalCodigo,
                strTipo: Normalizar(obj.objFila.strCodTip)), obj => obj.objFila);

        foreach (var objGrupo in lstGrupos)
        {
            decimal dcLibras = objGrupo.Sum(objFila => objFila.dcLibras);
            decimal dcCosto = objGrupo.Sum(objFila => objFila.dcCostoTot);
            // Comparar signos evita desbordar una multiplicación innecesaria.
            if (Math.Abs(dcLibras) <= dcToleranciaLibras || dcCosto == 0m
                || Math.Sign(dcLibras) != Math.Sign(dcCosto))
                continue;

            var dicDestino = objGrupo.Key.strTipo == "FINAL" ? dicFinal : dicInicial;
            dicDestino[(objGrupo.Key.intCodProd, objGrupo.Key.intCodTalla)] = Math.Abs(dcCosto / dcLibras);
        }

        return new CostosInventario(dicFinal, dicInicial);
    }

    // solo el inventario FINAL del endpoint recibe el costo global sin redondeos.
    public static void ValorarInventarioFinal(
        IEnumerable<InventarioVal> lstInventario,
        IReadOnlyDictionary<(int intCodProd, int intCodTalla), CostoAplicado> dicCosto)
    {
        foreach (var objFila in lstInventario)
        {
            if (Normalizar(objFila.strCodTip) != "FINAL"
                || !int.TryParse(objFila.strProCodcor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intCodProd))
                continue;

            if (dicCosto.TryGetValue((intCodProd, (int)objFila.stTalCodigo), out var objCosto)
                && objCosto.strFuente != FuenteCosto.SinCosto && objCosto.dcCostoUnit > 0m)
            {
                objFila.dcCostoUnit = objCosto.dcCostoUnit;
                objFila.dcCostoTot = objFila.dcLibras * objCosto.dcCostoUnit;
                objFila.strFuenteCosto = objCosto.strFuente;
            }
            else
            {
                objFila.strFuenteCosto = FuenteCosto.SinCosto;
            }
        }
    }

    public static Dictionary<(int intCodProd, int intCodTalla), CostoAplicado> CostoPorProdTalla(
        IEnumerable<FilaValoracion> lstFilasValoradas, CostosInventario objInv)
    {
        var dicCostos = new Dictionary<(int intCodProd, int intCodTalla), CostoAplicado>();
        foreach (var objGrupo in lstFilasValoradas.GroupBy(objFila => (objFila.intCodProd, objFila.intCodTalla)))
        {
            decimal dcLibras = objGrupo.Sum(objFila => objFila.dcLibras);
            decimal dcCosto = objGrupo.Sum(objFila => objFila.dcCostoTot);
            CostoAplicado objCosto;
            if (dcLibras > dcToleranciaLibras && dcCosto > 0m)
                objCosto = new CostoAplicado(Math.Abs(dcCosto / dcLibras), FuenteCosto.CuadreGeneral);
            else if (objInv.dicFinal.TryGetValue(objGrupo.Key, out decimal dcFinal) && Math.Abs(dcFinal) > 0m)
                objCosto = new CostoAplicado(Math.Abs(dcFinal), FuenteCosto.InventarioFinal);
            else if (objInv.dicInicial.TryGetValue(objGrupo.Key, out decimal dcInicial) && Math.Abs(dcInicial) > 0m)
                objCosto = new CostoAplicado(Math.Abs(dcInicial), FuenteCosto.InventarioInicial);
            else
            {
                var lstIngresos = objGrupo.Where(objFila => EsIngreso(objFila)).ToArray();
                decimal dcLibrasIngreso = lstIngresos.Sum(objFila => objFila.dcLibras);
                decimal dcCostoIngreso = lstIngresos.Sum(objFila => objFila.dcCostoTot);
                objCosto = dcLibrasIngreso > dcToleranciaLibras && dcCostoIngreso > 0m
                    ? new CostoAplicado(Math.Abs(dcCostoIngreso / dcLibrasIngreso), FuenteCosto.IngresosPeriodo)
                    : new CostoAplicado(0m, FuenteCosto.SinCosto);
            }

            dicCostos.Add(objGrupo.Key, objCosto);
        }

        return dicCostos;
    }

    public static Dictionary<(int intCodProd, int intCodTalla), CostoAplicado> CostoPorProdTallaDesdeDetalle(
        IEnumerable<FilaValoracion> lstDetalle, CostosInventario objInv)
    {
        var lstFilas = lstDetalle.ToArray();
        var lstBase = lstFilas.Where(objFila => string.IsNullOrWhiteSpace(objFila.strCodMov))
            .Select(objFila => objFila with { dcCostoTot = Math.Sign(objFila.dcLibras) * Math.Abs(objFila.dcCostoTot) })
            .ToArray();
        var objValorador = new ValoradorMovimientos(lstBase, objInv);
        var lstMovimientos = lstFilas
            .Where(objFila => !string.IsNullOrWhiteSpace(objFila.strCodMov)
                && Normalizar(objFila.strCodMov) is not ("EX" or "EV" or "EOB" or "EXP" or "LB" or "EAJ" or "IAJ"))
            .Select(objFila => objFila with
            {
                dcCostoTot = objFila.dcLibras * objValorador.CostoMovimiento(
                    objFila.intLote, objFila.intCodProd, objFila.intCodTalla).dcCostoUnit
            });

        return CostoPorProdTalla(lstBase.Concat(lstMovimientos), objInv);
    }

    internal static string Normalizar(string? strValor) => (strValor ?? string.Empty).Trim().ToUpperInvariant();

    internal static bool EsIngreso(FilaValoracion objFila) =>
        Normalizar(objFila.strTipo) == "I" && objFila.dcLibras > 0m;
}

public sealed class ValoradorMovimientos
{
    private readonly Dictionary<(int intLote, int intCodProd, int intCodTalla), decimal> dicProduccionLote;
    private readonly Dictionary<(int intLote, int intCodProd, int intCodTalla), decimal> dicInventarioLote;
    private readonly Dictionary<(int intCodProd, int intCodTalla), decimal> dicProduccionProdTalla;
    private readonly Dictionary<(int intCodProd, int intCodTalla), decimal> dicIngresosProdTalla;

    public ValoradorMovimientos(IEnumerable<FilaValoracion> lstBase, CostosInventario objInv)
    {
        // objInv forma parte del contrato; los movimientos solo usan ingresos base.
        var lstIngresos = lstBase.Where(objFila => string.IsNullOrWhiteSpace(objFila.strCodMov)
            && MotorValoracionCostoVenta.EsIngreso(objFila)).ToArray();
        var lstProduccion = lstIngresos.Where(objFila =>
            MotorValoracionCostoVenta.Normalizar(objFila.strTipoLiq) == "LIQ_PFR"
            || MotorValoracionCostoVenta.Normalizar(objFila.strAgrupacion) == "2. PROCESADO").ToArray();

        dicProduccionLote = lstProduccion.GroupBy(objFila => (objFila.intLote, objFila.intCodProd, objFila.intCodTalla))
            .ToDictionary(objGrupo => objGrupo.Key, objGrupo => CostoPonderado(objGrupo));
        dicInventarioLote = lstIngresos
            .Where(objFila => MotorValoracionCostoVenta.Normalizar(objFila.strTipoLiq) == "INVENTARIO")
            .GroupBy(objFila => (objFila.intLote, objFila.intCodProd, objFila.intCodTalla))
            .ToDictionary(objGrupo => objGrupo.Key, objGrupo => CostoPonderado(objGrupo));
        dicProduccionProdTalla = lstProduccion.GroupBy(objFila => (objFila.intCodProd, objFila.intCodTalla))
            .ToDictionary(objGrupo => objGrupo.Key, objGrupo => CostoPonderado(objGrupo));
        dicIngresosProdTalla = lstIngresos.GroupBy(objFila => (objFila.intCodProd, objFila.intCodTalla))
            .ToDictionary(objGrupo => objGrupo.Key, objGrupo => CostoPonderado(objGrupo));
    }

    public CostoMovimientoValorado CostoMovimiento(int intLote, int intCodProd, int intCodTalla)
    {
        var objClaveLote = (intLote, intCodProd, intCodTalla);
        var objClaveProdTalla = (intCodProd, intCodTalla);
        if (dicProduccionLote.TryGetValue(objClaveLote, out decimal dcProduccionLote) && dcProduccionLote > 0m)
            return new CostoMovimientoValorado(dcProduccionLote, "COSTO_LOTE", "PRODUCCION DEL LOTE");
        if (dicInventarioLote.TryGetValue(objClaveLote, out decimal dcInventarioLote) && dcInventarioLote > 0m)
            return new CostoMovimientoValorado(dcInventarioLote, "COSTO_INV_LOTE", "INVENTARIO INICIAL DEL LOTE");
        if (dicProduccionProdTalla.TryGetValue(objClaveProdTalla, out decimal dcProduccion) && dcProduccion > 0m)
            return new CostoMovimientoValorado(dcProduccion, "COSTO_PRODTALLA", "PRODUCCION PRODUCTO+TALLA");
        if (dicIngresosProdTalla.TryGetValue(objClaveProdTalla, out decimal dcIngresos) && dcIngresos > 0m)
            return new CostoMovimientoValorado(dcIngresos, "COSTO_INGRESOS_PRODTALLA", "INGRESOS PRODUCTO+TALLA");

        return new CostoMovimientoValorado(0m, "SIN_COSTO_LOTE", FuenteCosto.SinCosto);
    }

    private static decimal CostoPonderado(IEnumerable<FilaValoracion> lstFilas)
    {
        decimal dcLibras = 0m;
        decimal dcCosto = 0m;
        foreach (var objFila in lstFilas)
        {
            dcLibras += objFila.dcLibras;
            dcCosto += objFila.dcCostoTot;
        }

        return dcLibras > MotorValoracionCostoVenta.dcToleranciaLibras ? Math.Abs(dcCosto / dcLibras) : 0m;
    }
}
