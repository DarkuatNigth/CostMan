using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CostManagement.Dominio.Reglas.Iqf;

public enum IqfParticion
{
    EN,
    CO,
    VA
}

public readonly record struct IqfClave(int Empresa, string Cuenta);

public sealed record IqfFuenteConfiguracion(
    int Empresa,
    string CuentaFuente,
    string CuentaEntero,
    string CuentaCola,
    string CuentaValorAgregado,
    string ModoMonto,
    bool RequiereCreditoRetiro);

public sealed record IqfSaldoMes(
    int Empresa,
    string Cuenta,
    decimal Debe,
    decimal Credito);

public sealed record IqfLibras(
    int Empresa,
    decimal Entero,
    decimal Cola,
    decimal ValorAgregado)
{
    public decimal Total => Entero + Cola + ValorAgregado;
}

public sealed record IqfReparto(
    decimal Entero,
    decimal Cola,
    decimal ValorAgregado)
{
    public decimal Total => Entero + Cola + ValorAgregado;

    public decimal Obtener(IqfParticion particion) => particion switch
    {
        IqfParticion.EN => Entero,
        IqfParticion.CO => Cola,
        IqfParticion.VA => ValorAgregado,
        _ => throw new ArgumentOutOfRangeException(nameof(particion))
    };
}

public sealed record IqfBaseCuenta(
    decimal NetoOriginalSong,
    decimal RecuperadoIqf,
    decimal RetiradoIqf)
{
    public decimal MontoDistribuir =>
        MotorReclasificacionIqf.Redondear(
            NetoOriginalSong + RecuperadoIqf - RetiradoIqf);
}

public sealed record IqfAsignacionFuente(
    IqfClave Fuente,
    decimal Debe,
    decimal Credito,
    decimal NetoOriginalSong,
    decimal CostoIqf,
    decimal Recuperacion,
    IqfReparto DistribucionCosto,
    IqfReparto DistribucionRetiro);

public sealed record IqfResultado(
    IReadOnlyDictionary<IqfClave, IqfBaseCuenta> Bases,
    IReadOnlyList<IqfAsignacionFuente> Asignaciones,
    decimal TotalNetoOriginal,
    decimal TotalRecuperado,
    decimal TotalRetirado,
    decimal TotalBasesAjustadas);

/// <summary>
/// Reclasifica el costo de las cuentas fuente IQF sin modificar la lectura
/// original de SONG.
///
/// Regla aprobada:
///   1. El costo IQF de la fuente es el Debe del mes.
///   2. Si el Credito de ESA fuente es mayor que cero:
///        - la fuente recupera exactamente ese Credito;
///        - se retira exactamente ese Credito de sus cuentas receptoras,
///          repartido con los mismos porcentajes de libras IQF EN/CO/VA.
///   3. Si el Credito es cero, no se retira nada de las receptoras.
///   4. Debe distribuirse el costo IQF por libras, exista o no retiro.
///   5. Recuperaciones y retiros deben conservar el total global.
/// </summary>
public static class MotorReclasificacionIqf
{
    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, 4, MidpointRounding.ToEven);

    public static IqfClave Clave(int empresa, string? cuenta)
    {
        if (empresa <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresa));

        string normalizada = (cuenta ?? string.Empty).Trim();
        if (normalizada.Length == 0)
            throw new ArgumentException("La cuenta es obligatoria.", nameof(cuenta));

        return new IqfClave(empresa, normalizada);
    }

    public static IqfResultado Calcular(
        IEnumerable<IqfSaldoMes> saldos,
        IEnumerable<IqfFuenteConfiguracion> configuraciones,
        IEnumerable<IqfLibras> librasPorEmpresa)
    {
        ArgumentNullException.ThrowIfNull(saldos);
        ArgumentNullException.ThrowIfNull(configuraciones);
        ArgumentNullException.ThrowIfNull(librasPorEmpresa);

        Dictionary<IqfClave, IqfSaldoMes> dicSaldos =
            CrearDiccionarioUnico(
                saldos.Select(x => x with
                {
                    Cuenta = (x.Cuenta ?? string.Empty).Trim(),
                    Debe = Redondear(x.Debe),
                    Credito = Redondear(x.Credito)
                }),
                x => Clave(x.Empresa, x.Cuenta),
                "saldo SONG");

        Dictionary<int, IqfLibras> dicLibras =
            CrearDiccionarioUnico(
                librasPorEmpresa.Select(x => x with
                {
                    Entero = Redondear(x.Entero),
                    Cola = Redondear(x.Cola),
                    ValorAgregado = Redondear(x.ValorAgregado)
                }),
                x => x.Empresa,
                "driver IQF por empresa");

        List<IqfFuenteConfiguracion> fuentes = configuraciones
            .Select(Normalizar)
            .ToList();

        Dictionary<IqfClave, IqfFuenteConfiguracion> dicFuentes =
            CrearDiccionarioUnico(
                fuentes,
                x => Clave(x.Empresa, x.CuentaFuente),
                "configuracion fuente IQF");

        Dictionary<IqfClave, IqfBaseCuenta> bases = dicSaldos.ToDictionary(
            x => x.Key,
            x => new IqfBaseCuenta(
                Redondear(x.Value.Debe - x.Value.Credito),
                0m,
                0m));

        decimal totalNetoOriginal = Redondear(
            bases.Values.Sum(x => x.NetoOriginalSong));

        var asignaciones = new List<IqfAsignacionFuente>(fuentes.Count);

        foreach (IqfFuenteConfiguracion config in dicFuentes.Values
                     .OrderBy(x => x.Empresa)
                     .ThenBy(x => x.CuentaFuente, StringComparer.Ordinal))
        {
            IqfClave claveFuente = Clave(config.Empresa, config.CuentaFuente);

            if (!dicSaldos.TryGetValue(claveFuente, out IqfSaldoMes? saldoFuente))
                throw new InvalidOperationException(
                    $"La fuente IQF {claveFuente} no fue encontrada en SONG.");

            if (saldoFuente.Debe < 0m)
                throw new InvalidOperationException(
                    $"La fuente IQF {claveFuente} tiene Debe negativo.");

            if (saldoFuente.Credito < 0m)
                throw new InvalidOperationException(
                    $"La fuente IQF {claveFuente} tiene Credito negativo.");

            if (!dicLibras.TryGetValue(config.Empresa, out IqfLibras? libras))
                throw new InvalidOperationException(
                    $"No existen libras IQF para la empresa {config.Empresa}.");

            decimal costoIqf = ResolverMontoFuente(config, saldoFuente);
            IqfReparto distribucionCosto = Repartir(costoIqf, libras);

            bool permiteRetiro =
                config.RequiereCreditoRetiro && saldoFuente.Credito > 0m;

            decimal recuperacion = permiteRetiro
                ? saldoFuente.Credito
                : 0m;

            IqfReparto distribucionRetiro = permiteRetiro
                ? Repartir(recuperacion, libras)
                : new IqfReparto(0m, 0m, 0m);

            IqfBaseCuenta baseFuente = bases[claveFuente];
            bases[claveFuente] = baseFuente with
            {
                RecuperadoIqf = Redondear(
                    baseFuente.RecuperadoIqf + recuperacion)
            };

            if (permiteRetiro)
            {
                AplicarRetiro(
                    bases,
                    Clave(config.Empresa, config.CuentaEntero),
                    distribucionRetiro.Entero,
                    claveFuente,
                    IqfParticion.EN);

                AplicarRetiro(
                    bases,
                    Clave(config.Empresa, config.CuentaCola),
                    distribucionRetiro.Cola,
                    claveFuente,
                    IqfParticion.CO);

                AplicarRetiro(
                    bases,
                    Clave(config.Empresa, config.CuentaValorAgregado),
                    distribucionRetiro.ValorAgregado,
                    claveFuente,
                    IqfParticion.VA);
            }

            decimal netoFuente = Redondear(
                saldoFuente.Debe - saldoFuente.Credito);

            decimal esperadoFuente = Redondear(
                netoFuente + recuperacion);

            decimal montoFuenteAjustado =
                bases[claveFuente].MontoDistribuir;

            if (esperadoFuente != montoFuenteAjustado)
                throw new InvalidOperationException(
                    $"La fuente IQF {claveFuente} no concilia. " +
                    $"Esperado {esperadoFuente:N4}; " +
                    $"ajustado {montoFuenteAjustado:N4}.");

            if (montoFuenteAjustado != costoIqf)
                throw new InvalidOperationException(
                    $"La fuente IQF {claveFuente} no recupero el Debe del mes. " +
                    $"Debe {costoIqf:N4}; base {montoFuenteAjustado:N4}.");

            asignaciones.Add(new IqfAsignacionFuente(
                claveFuente,
                saldoFuente.Debe,
                saldoFuente.Credito,
                netoFuente,
                costoIqf,
                recuperacion,
                distribucionCosto,
                distribucionRetiro));
        }

        decimal totalRecuperado = Redondear(
            bases.Values.Sum(x => x.RecuperadoIqf));

        decimal totalRetirado = Redondear(
            bases.Values.Sum(x => x.RetiradoIqf));

        decimal totalBasesAjustadas = Redondear(
            bases.Values.Sum(x => x.MontoDistribuir));

        if (Math.Abs(totalRecuperado - totalRetirado) >= 0.01m)
            throw new InvalidOperationException(
                $"IQF no conserva entradas y salidas. " +
                $"Recuperado {totalRecuperado:N4}; " +
                $"retirado {totalRetirado:N4}.");

        if (Math.Abs(totalNetoOriginal - totalBasesAjustadas) >= 0.01m)
            throw new InvalidOperationException(
                $"IQF altero el costo total. " +
                $"Original {totalNetoOriginal:N4}; " +
                $"ajustado {totalBasesAjustadas:N4}.");

        return new IqfResultado(
            new ReadOnlyDictionary<IqfClave, IqfBaseCuenta>(bases),
            asignaciones.AsReadOnly(),
            totalNetoOriginal,
            totalRecuperado,
            totalRetirado,
            totalBasesAjustadas);
    }

    public static IqfReparto Repartir(
        decimal monto,
        IqfLibras libras)
    {
        monto = Redondear(monto);

        if (monto == 0m)
            return new IqfReparto(0m, 0m, 0m);

        if (libras.Empresa <= 0)
            throw new ArgumentOutOfRangeException(nameof(libras.Empresa));

        if (libras.Entero < 0m ||
            libras.Cola < 0m ||
            libras.ValorAgregado < 0m)
            throw new InvalidOperationException(
                "Las libras IQF no pueden ser negativas.");

        decimal totalLibras = libras.Total;
        if (totalLibras <= 0m)
            throw new InvalidOperationException(
                "No existen libras IQF elegibles para distribuir el monto.");

        decimal entero = Redondear(
            monto * libras.Entero / totalLibras);

        decimal cola = Redondear(
            monto * libras.Cola / totalLibras);

        decimal valorAgregado = Redondear(
            monto * libras.ValorAgregado / totalLibras);

        decimal residuo = Redondear(
            monto - entero - cola - valorAgregado);

        if (residuo != 0m)
        {
            if (libras.ValorAgregado >= libras.Entero &&
                libras.ValorAgregado >= libras.Cola)
                valorAgregado = Redondear(valorAgregado + residuo);
            else if (libras.Entero >= libras.Cola)
                entero = Redondear(entero + residuo);
            else
                cola = Redondear(cola + residuo);
        }

        var resultado = new IqfReparto(
            entero,
            cola,
            valorAgregado);

        if (resultado.Total != monto)
            throw new InvalidOperationException(
                $"El reparto IQF no conserva el monto {monto:N4}.");

        return resultado;
    }

    private static decimal ResolverMontoFuente(
        IqfFuenteConfiguracion config,
        IqfSaldoMes saldo)
    {
        string modo = (config.ModoMonto ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        return modo switch
        {
            "DEBE_MES" => Redondear(saldo.Debe),
            _ => throw new InvalidOperationException(
                $"Modo monetario IQF no soportado: " +
                $"{config.CuentaFuente}/{config.ModoMonto}.")
        };
    }

    private static void AplicarRetiro(
        IDictionary<IqfClave, IqfBaseCuenta> bases,
        IqfClave destino,
        decimal monto,
        IqfClave fuente,
        IqfParticion particion)
    {
        monto = Redondear(monto);
        if (monto == 0m)
            return;

        if (!bases.TryGetValue(destino, out IqfBaseCuenta? baseDestino))
            throw new InvalidOperationException(
                $"La fuente IQF {fuente} tiene Credito positivo, " +
                $"pero no se encontro en SONG la receptora " +
                $"{destino} para {particion}.");

        bases[destino] = baseDestino with
        {
            RetiradoIqf = Redondear(
                baseDestino.RetiradoIqf + monto)
        };
    }

    private static IqfFuenteConfiguracion Normalizar(
        IqfFuenteConfiguracion x)
    {
        if (x.Empresa <= 0)
            throw new InvalidOperationException(
                "Empresa IQF invalida.");

        string fuente = NormalizarCuenta(x.CuentaFuente);
        string entero = NormalizarCuenta(x.CuentaEntero);
        string cola = NormalizarCuenta(x.CuentaCola);
        string vag = NormalizarCuenta(x.CuentaValorAgregado);

        if (new[] { fuente, entero, cola, vag }
            .Any(c => c.Length == 0))
            throw new InvalidOperationException(
                "El mapa IQF contiene una cuenta vacia.");

        if (fuente == entero ||
            fuente == cola ||
            fuente == vag)
            throw new InvalidOperationException(
                $"La fuente IQF {fuente} no puede ser su propia receptora.");

        return x with
        {
            CuentaFuente = fuente,
            CuentaEntero = entero,
            CuentaCola = cola,
            CuentaValorAgregado = vag,
            ModoMonto = (x.ModoMonto ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
        };
    }

    private static string NormalizarCuenta(string? cuenta) =>
        (cuenta ?? string.Empty).Trim();

    private static Dictionary<TKey, TValue> CrearDiccionarioUnico<TValue, TKey>(
        IEnumerable<TValue> items,
        Func<TValue, TKey> keySelector,
        string descripcion)
        where TKey : notnull
    {
        var resultado = new Dictionary<TKey, TValue>();

        foreach (TValue item in items)
        {
            TKey key = keySelector(item);
            if (!resultado.TryAdd(key, item))
                throw new InvalidOperationException(
                    $"Existe {descripcion} duplicado: {key}.");
        }

        return resultado;
    }
}
