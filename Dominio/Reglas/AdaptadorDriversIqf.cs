using System;
using System.Collections.Generic;
using System.Linq;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Dominio.Reglas.Iqf;

/// <summary>
/// Adaptador para los tipos existentes en CostMan.
/// El resolver recibe strCongeProduc y devuelve IQF, BRI, TUN, NINGUNO o null.
/// Su equivalencia con los valores reales de producción debe configurarse;
/// esta clase NO infiere congelación por el nombre comercial del producto.
/// </summary>
public static class AdaptadorDriversIqf
{
    private static string Normalizar(string? x) => (x ?? string.Empty).Trim().ToUpperInvariant();

    public static List<DriverProcesoProductoDto> Construir(
        int anio,
        int mes,
        IEnumerable<MatPrimaReproceso> procesado,
        IEnumerable<ProcesoCostoAplicacionDto> aplicaciones,
        Func<string, string?> resolverCongelacion)
    {
        _ = new DateOnly(anio, mes, 1);

        ArgumentNullException.ThrowIfNull(procesado);
        ArgumentNullException.ThrowIfNull(aplicaciones);
        ArgumentNullException.ThrowIfNull(resolverCongelacion);


        // ============================================================
        // MATRIZ DE APLICACIÓN IQF
        // ============================================================

        var matriz =
            new Dictionary<string, ProcesoCostoAplicacionDto>(
                StringComparer.OrdinalIgnoreCase);


        foreach (
            var app in aplicaciones.Where(
                x => Normalizar(x.strPcCodigo) == "IQF"))
        {
            string tipo =
                Normalizar(
                    app.strTipoProcesoCodigo);

            string regla =
                Normalizar(
                    app.strConfig);


            if (
                tipo.Length == 0 ||
                !matriz.TryAdd(tipo, app))
            {
                throw new InvalidOperationException(
                    $"Tipo IQF vacío o duplicado: {tipo}.");
            }


            if (
                regla is not (
                    "SI" or
                    "PRO" or
                    "NO" or
                    "NA"))
            {
                throw new InvalidOperationException(
                    $"Regla IQF no resuelta: {tipo}/{regla}.");
            }
        }


        if (matriz.Count == 0)
        {
            throw new InvalidOperationException(
                "No existe matriz de aplicaciones IQF.");
        }


        // ============================================================
        // ACUMULACIÓN DE LIBRAS IQF
        // ============================================================

        var grupos =
            new Dictionary<
                (string Tipo, string Particion),
                decimal>();


        foreach (var item in procesado)
        {
            // ========================================================
            // SOLO PROCESADO
            // ========================================================

            if (
                Normalizar(
                    item.strAgrupacion) !=
                "2. PROCESADO")
            {
                continue;
            }


            string tipo =
                Normalizar(
                    item.strTipCod);


            if (
                !matriz.TryGetValue(
                    tipo,
                    out var app))
            {
                continue;
            }


            string regla =
                Normalizar(
                    app.strConfig);


            // ========================================================
            // NO / NA
            //
            // Nunca participan.
            // ========================================================

            if (
                regla is "NO" or "NA")
            {
                continue;
            }


            // ========================================================
            // VALIDACIONES GENERALES
            // ========================================================

            if (item.dtLotFecha == default)
            {
                throw new InvalidOperationException(
                    $"Lote {item.intLotNumero} sin fecha de proceso.");
            }


            if (
                item.dtLotFecha.Year != anio ||
                item.dtLotFecha.Month != mes)
            {
                continue;
            }


            if (
                !double.IsFinite(
                    item.dbLibras) ||
                item.dbLibras < 0d)
            {
                throw new InvalidOperationException(
                    $"Libras IQF inválidas en lote {item.intLotNumero}.");
            }


            if (item.dbLibras == 0d)
            {
                continue;
            }


            // ========================================================
            // REGLA PRO
            //
            // Aquí sí se debe consultar strCongeProduc.
            //
            // SOLO IQF entra en este driver.
            //
            // BLOCK     -> TUN -> NO entra
            // SEMI IQF  -> TUN -> NO entra
            // BRINE     -> BRI -> NO entra
            // IQF       -> IQF -> SÍ entra
            // ========================================================

            if (regla == "PRO")
            {
                string congelacion =
                    Normalizar(
                        resolverCongelacion(
                            item.strCongeProduc ??
                            string.Empty));


                if (congelacion.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"No se resolvió PRO en lote {item.intLotNumero}, " +
                        $"producto {item.intCodProd}. " +
                        $"Congelamiento Producto: '{item.strCongeProduc}'.");
                }


                if (
                    congelacion is not (
                        "IQF" or
                        "BRI" or
                        "TUN" or
                        "NINGUNO"))
                {
                    throw new InvalidOperationException(
                        $"Resolver devolvió código desconocido: {congelacion}.");
                }


                // Este adaptador construye EXCLUSIVAMENTE IQF.
                if (congelacion != "IQF")
                {
                    continue;
                }
            }


            // ========================================================
            // REGLA SI
            //
            // SI ya significa que el tipo pertenece al driver IQF.
            //
            // NO se valida strCongeProduc.
            // NO interesa si dice BLOCK, BRINE, etc.
            //
            // La matriz manda.
            // ========================================================

            // Si regla == "SI", continúa directamente.


            // ========================================================
            // PARTICIÓN
            // ========================================================

            string? particion =
                ParticionCosteo.ClasificarRpc(
                    item.strTipoProducto);


            if (particion == null)
            {
                throw new InvalidOperationException(
                    $"Partición no reconocida en lote {item.intLotNumero}.");
            }


            var key =
                (
                    Tipo: tipo,
                    Particion: particion
                );


            grupos[key] =
                grupos.GetValueOrDefault(key) +
                Convert.ToDecimal(
                    item.dbLibras);
        }


        // ============================================================
        // RESULTADO
        // ============================================================

        return grupos
            .OrderBy(
                x => x.Key.Tipo,
                StringComparer.Ordinal)
            .ThenBy(
                x => x.Key.Particion,
                StringComparer.Ordinal)
            .Select(x =>
                new DriverProcesoProductoDto
                {
                    strPcCodigo =
                        "IQF",

                    strTipCodigo =
                        x.Key.Tipo,

                    strParticionCodigo =
                        x.Key.Particion,

                    strParticion =
                        ParticionCosteo.Descripcion(
                            x.Key.Particion),

                    dcLibras =
                        x.Value,

                    dcPeso =
                        x.Value,

                    intFactor =
                        1
                })
            .ToList();
    }

    // Llamar para una lectura que corresponda inequívocamente a esta empresa.
    public static IqfLibras Resumir(int empresa, IEnumerable<DriverProcesoProductoDto> drivers)
    {
        var filas = drivers.Where(x => Normalizar(x.strPcCodigo) == "IQF").ToList();
        return new IqfLibras(empresa,
            filas.Where(x => x.strParticionCodigo == ParticionCosteo.ENTERO).Sum(x => x.dcLibras),
            filas.Where(x => x.strParticionCodigo == ParticionCosteo.COLA).Sum(x => x.dcLibras),
            filas.Where(x => x.strParticionCodigo is ParticionCosteo.ENTERO_VA or ParticionCosteo.COLA_VA)
                .Sum(x => x.dcLibras));
    }

    // El detalle se alimenta de los mismos drivers, incluidos PRO ya resueltos.
    // Evita volver a filtrar sólo por intFactor de la matriz original.
    public static List<CostoProductivoProcesoAplicableDetalleDto> ConstruirDetalle(
        IqfReparto distribucion,
        IEnumerable<DriverProcesoProductoDto> drivers,
        IEnumerable<ProcesoCostoAplicacionDto> aplicaciones)
    {
        var nombres = aplicaciones.Where(x => Normalizar(x.strPcCodigo) == "IQF")
            .GroupBy(x => Normalizar(x.strTipoProcesoCodigo))
            .ToDictionary(g => g.Key, g => g.First().strTipoProceso ?? g.Key);
        var porTipo = new Dictionary<string, decimal>();
        foreach (var p in new[] { IqfParticion.EN, IqfParticion.CO, IqfParticion.VA })
        {
            decimal monto = p == IqfParticion.EN ? distribucion.Entero :
                p == IqfParticion.CO ? distribucion.Cola : distribucion.ValorAgregado;
            var lista = drivers.Where(x => Normalizar(x.strPcCodigo) == "IQF")
                .Where(x => p == IqfParticion.EN ? x.strParticionCodigo == ParticionCosteo.ENTERO :
                    p == IqfParticion.CO ? x.strParticionCodigo == ParticionCosteo.COLA :
                    x.strParticionCodigo is ParticionCosteo.ENTERO_VA or ParticionCosteo.COLA_VA)
                .GroupBy(x => Normalizar(x.strTipCodigo))
                .Select(g => (Tipo: g.Key, Peso: g.Sum(x => x.dcPeso)))
                .Where(x => x.Peso > 0m).OrderBy(x => x.Tipo, StringComparer.Ordinal).ToList();
            if (monto == 0m) continue;
            decimal peso = lista.Sum(x => x.Peso);
            if (peso <= 0m) throw new InvalidOperationException("Monto IQF sin detalle de drivers.");
            var valores = lista.Select(x => MotorReclasificacionIqf.Redondear(monto * (x.Peso / peso))).ToArray();
            int receptor = lista.FindIndex(x => x.Peso == lista.Max(y => y.Peso));
            valores[receptor] += MotorReclasificacionIqf.Redondear(monto - valores.Sum());
            for (int i = 0; i < lista.Count; i++)
                porTipo[lista[i].Tipo] = porTipo.GetValueOrDefault(lista[i].Tipo) + valores[i];
        }
        return porTipo.Select(x => new CostoProductivoProcesoAplicableDetalleDto
        {
            strCodigoProceso = x.Key, strProceso = nombres.GetValueOrDefault(x.Key, x.Key),
            dcDolares = x.Value
        }).ToList();
    }
}
