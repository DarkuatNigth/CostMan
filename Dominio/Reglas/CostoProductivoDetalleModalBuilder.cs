using CostManagement.Aplicación.DTos;

namespace CostManagement.Dominio.Reglas;

/// <summary>
/// Proyecta el snapshot recién calculado a la misma estructura que consume
/// Table6 en ParamProc. De esta forma la consulta no depende del último cierre
/// persistido para mostrar MPE, IQF, COP, LOG, REC, CLA y DSC.
/// </summary>
public static class CostoProductivoDetalleModalBuilder
{
    public static List<CostoProductivoDetalleModalDto> Construir(
        IEnumerable<CostoProductivoCuentaDto>? cuentas)
    {
        var resultado = new List<CostoProductivoDetalleModalDto>();

        foreach (CostoProductivoCuentaDto fila in
                 cuentas ?? Array.Empty<CostoProductivoCuentaDto>())
        {
            Agregar(resultado, fila, "EN", "Entero",
                fila.dcMontoEnteroPersistencia);

            Agregar(resultado, fila, "CO", "Cola",
                fila.dcMontoColaPersistencia);

            Agregar(resultado, fila, "VA", "Valor Agregado",
                fila.dcMontoVagPersistencia);
        }

        return resultado
            .OrderBy(x => x.strEtapaCodigo, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.strProcesoCodigo, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.strCuenta, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => OrdenParticion(x.strParticionCodigo))
            .ToList();
    }

    private static void Agregar(
        ICollection<CostoProductivoDetalleModalDto> destino,
        CostoProductivoCuentaDto fila,
        string particionCodigo,
        string particion,
        decimal monto)
    {
        monto = Math.Round(monto, 5);

        if (monto == 0m)
            return;

        destino.Add(new CostoProductivoDetalleModalDto
        {
            strEtapaCodigo = (fila.strEcCodigo ?? string.Empty).Trim(),
            strEtapa = (fila.strGrupoEtapa ?? string.Empty).Trim(),
            strProcesoCodigo = (fila.strPcCodigo ?? string.Empty).Trim(),
            strProceso = (fila.strEtapa ?? string.Empty).Trim(),
            strCuenta = (fila.strCuenta ?? string.Empty).Trim(),
            strDescripcionCuenta = (fila.strAuxiliar ?? string.Empty).Trim(),
            strCentroCodigo = (fila.strCentroCodigo ?? string.Empty).Trim(),
            strSubcentroCodigo = (fila.strSubcentroCodigo ?? string.Empty).Trim(),
            strAgrupacionCentro =
                (fila.strAgrupacionCentro ?? string.Empty).Trim(),
            strGrupoCentro = (fila.strGrupoCentro ?? string.Empty).Trim(),
            strParticionCodigo = particionCodigo,
            strParticion = particion,
            dcDolares = monto
        });
    }

    private static int OrdenParticion(string? codigo) =>
        (codigo ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "EN" => 1,
            "CO" or "SH" => 2,
            "VA" => 3,
            _ => 99
        };
}
