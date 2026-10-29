namespace CostManagement.Dominio.Reglas;

public sealed record LineaGastosDiarioVenta(string strTipo, decimal dcLibras);
public sealed record PoolsGastosDiarioVenta(decimal dcAdministrativos, decimal dcComercializacion, decimal dcExportacion, decimal dcOtros);
public sealed record GastosLineaDiarioVenta(decimal dcAdministrativos, decimal dcComercializacion, decimal dcExportacion, decimal dcOtros);

public static class MotorGastosDiarioVenta
{
    public static IReadOnlyList<GastosLineaDiarioVenta> Calcular(
        IReadOnlyList<LineaGastosDiarioVenta> lstLineas, PoolsGastosDiarioVenta objPools)
    {
        decimal[] lstAdm = Distribuir(lstLineas, objPools.dcAdministrativos, false);
        decimal[] lstFin = Distribuir(lstLineas, objPools.dcComercializacion, false);
        decimal[] lstExp = Distribuir(lstLineas, objPools.dcExportacion, true);
        decimal[] lstOtros = Distribuir(lstLineas, objPools.dcOtros, false);
        return Enumerable.Range(0, lstLineas.Count)
            .Select(i => new GastosLineaDiarioVenta(lstAdm[i], lstFin[i], lstExp[i], lstOtros[i]))
            .ToArray();
    }

    private static decimal[] Distribuir(IReadOnlyList<LineaGastosDiarioVenta> lstLineas,
        decimal dcPool, bool blSoloExportacion)
    {
        var lstResultado = new decimal[lstLineas.Count];
        var lstIndices = Enumerable.Range(0, lstLineas.Count)
            .Where(i => lstLineas[i].dcLibras != 0m &&
                (!blSoloExportacion || EsExportacion(lstLineas[i].strTipo))).ToArray();
        decimal dcLibras = lstIndices.Sum(i => lstLineas[i].dcLibras);
        if (dcLibras == 0m) return lstResultado;

        decimal dcUnitario = dcPool / dcLibras;
        decimal dcAsignado = 0m;
        foreach (int i in lstIndices)
        {
            // Compensa solo el residuo de precision decimal en la ultima linea elegible.
            lstResultado[i] = i == lstIndices[^1] ? dcPool - dcAsignado : lstLineas[i].dcLibras * dcUnitario;
            dcAsignado += lstResultado[i];
        }
        return lstResultado;
    }

    private static bool EsExportacion(string strTipo) =>
        string.Equals(strTipo?.Trim(), "VENTA EXPORTACION", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(strTipo?.Trim(), "VENTA MARINA", StringComparison.OrdinalIgnoreCase);
}
