using CostManagement.Aplicación.DTos;

namespace CostManagement.Dominio.Reglas;

public sealed record GastoSongDesconocido(string Cuenta, decimal Monto, int Anio, int Mes);
public sealed record ResultadoPoolsGastosSong(PoolsGastosDiarioVenta Pools, IReadOnlyList<GastoSongDesconocido> Desconocidos);

public static class PoolsGastosSong
{
    public static ResultadoPoolsGastosSong Calcular(
        IReadOnlyDictionary<int, List<SaldoCuentaSongDto>> saldosPorAnio, DateOnly inicio, DateOnly fin)
    {
        decimal adm = 0m, finanzas = 0m, exp = 0m, otros = 0m;
        var desconocidos = new List<GastoSongDesconocido>();
        for (int anio = inicio.Year; anio <= fin.Year; anio++)
        {
            int mesInicio = anio == inicio.Year ? inicio.Month : 1;
            int mesFin = anio == fin.Year ? fin.Month : 12;
            foreach (var saldo in saldosPorAnio[anio])
            {
                // Misma normalizacion que OperacionComercialFeature.NormalizarCuenta.
                string cuenta = (saldo.strCuenta ?? string.Empty).Trim();
                if (!cuenta.StartsWith("505", StringComparison.Ordinal) &&
                    !cuenta.StartsWith("506", StringComparison.Ordinal) &&
                    !cuenta.StartsWith("507", StringComparison.Ordinal)) continue;

                MapaGastoVentaTipo3.Cuentas.TryGetValue(cuenta, out var tipo);
                // Los meses parcialmente cubiertos se toman completos, sin prorrateo diario.
                for (int mes = mesInicio; mes <= mesFin; mes++)
                {
                    var (debe, credito) = saldo.ObtenerAcumulado(mes);
                    decimal monto = debe - credito;
                    switch (tipo)
                    {
                        case "ADM": adm += monto; break;
                        case "FIN": finanzas += monto; break;
                        case "EXP": exp += monto; break;
                        case "OTROS": otros += monto; break;
                        default: desconocidos.Add(new(cuenta, monto, anio, mes)); break;
                    }
                }
            }
        }
        return new(new(adm, finanzas, exp, otros), desconocidos);
    }
}
