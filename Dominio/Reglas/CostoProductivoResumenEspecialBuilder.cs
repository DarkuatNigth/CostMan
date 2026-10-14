using CostManagement.Aplicación.DTos;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Dominio.Reglas;

/// <summary>
/// Corrige los procesos que no pueden reconstruirse únicamente con
/// MotorDistribucionCostoProductivo.lstDrivers:
///
/// - IQF: debe usar los drivers IQF ya resueltos, incluidos PRO.
/// - COP: sus cuentas son contables y su origen PFR/RPC se obtiene de las
///        libras C.Copacking calculadas en ParamProcVista/Tabla3.
/// - DEC: sus cuentas son contables (etapa PR) pero no participaba del
///        resumen proceso/origen, dejando Table9 incompleta frente a Table6.
/// </summary>
public static class CostoProductivoResumenEspecialBuilder
{
    public static void Completar(
        List<CostoProductivoProcesoOrigenDto> resumen,
        IEnumerable<CostoProductivoCuentaDto>? cuentas,
        IEnumerable<DriverProcesoProductoDto>? driversIqf,
        // Anterior: se agrega el logger para diagnosticar descabezado.
        // IEnumerable<LibrasParticionDto>? librasParticion)
        IEnumerable<LibrasParticionDto>? librasParticion, ILogger? objLogger = null)
    {
        ArgumentNullException.ThrowIfNull(resumen);

        List<CostoProductivoCuentaDto> filas =
            (cuentas ?? Array.Empty<CostoProductivoCuentaDto>()).ToList();

        // Sustituir cualquier reconstrucción genérica parcial.
        resumen.RemoveAll(x =>
            Normalizar(x.strPcCodigo) is "IQF" or "COP" or "DEC" or "DES");

        AgregarIqf(resumen, filas, driversIqf);
        AgregarCopacking(resumen, filas, librasParticion);
        AgregarDecorado(resumen, filas, librasParticion);
        // Anterior: se agrega el logger para diagnosticar descabezado.
        // AgregarDescabezado(resumen, filas, librasParticion);
        AgregarDescabezado(resumen, filas, librasParticion, objLogger);

        // BLOQUE TEMPORAL DE DEBUG (DESCABEZADO CU CONSOLIDADO) — mantener comentado.
        //var lstDebugResumenDes = resumen.Where(x => x.strPcCodigo == "DES").ToList();
    }

    /// <summary>
    /// DEC no lo reconstruye MotorDistribucionCostoProductivo.lstDrivers, así que sin
    /// esto Table9 queda sin la partida (Table6 sí la tiene), dejando el resumen
    /// proceso/origen -que alimenta Warren y el snapshot- con un universo incompleto.
    /// </summary>
    private static void AgregarDecorado(
        ICollection<CostoProductivoProcesoOrigenDto> resumen,
        IReadOnlyCollection<CostoProductivoCuentaDto> cuentas,
        IEnumerable<LibrasParticionDto>? librasParticion)
    {
        List<CostoProductivoCuentaDto> lstCuentasDecorado = cuentas
            .Where(x => Normalizar(x.strPcCodigo) == "DEC")
            .ToList();

        if (lstCuentasDecorado.Count == 0)
            return;

        List<LibrasParticionDto> lstLibrasDecorado =
            (librasParticion ?? Array.Empty<LibrasParticionDto>())
            .Where(x => Normalizar(x.strProceso) == "DECORADO")
            .Where(x => Normalizar(x.strOrigen) == "RPC")
            .ToList();

        var objDecorado = new CostoProductivoProcesoOrigenDto
        {
            strPcCodigo = "DEC",
            strProceso = "Decorado",
            strOrigen = "RPC",

            dcLibrasEntero = Libras(lstLibrasDecorado, "RPC", ParticionCosteo.ENTERO),
            dcLibrasCola = Libras(lstLibrasDecorado, "RPC", ParticionCosteo.COLA),
            dcLibrasValorAgregado = LibrasVa(lstLibrasDecorado, "RPC"),

            dcDolaresEntero = lstCuentasDecorado.Sum(x => x.dcMontoEnteroPersistencia),
            dcDolaresCola = lstCuentasDecorado.Sum(x => x.dcMontoColaPersistencia),
            dcDolaresValorAgregado = lstCuentasDecorado.Sum(x => x.dcMontoVagPersistencia)
        };

        objDecorado.Recalcular();

        if (objDecorado.dcTotalDolares != 0m || objDecorado.dcTotalLibras != 0m)
            resumen.Add(objDecorado);
    }

    /// <summary>
    /// DES no lo reconstruye MotorDistribucionCostoProductivo.lstDrivers con las libras
    /// reales de ambos orígenes (solo trae RPC vía driver ponderado), dejando Table9
    /// desalineada contra lstLibrasParticion (Table3). Reconstruye PFR y RPC desde Table3
    /// para libras y reparte los dólares de Table6 por partición según las libras PFR/RPC.
    /// </summary>
    private static void AgregarDescabezado(
        ICollection<CostoProductivoProcesoOrigenDto> resumen,
        IReadOnlyCollection<CostoProductivoCuentaDto> cuentas,
        // Anterior: se agrega el logger para diagnosticar descabezado.
        // IEnumerable<LibrasParticionDto>? librasParticion)
        IEnumerable<LibrasParticionDto>? librasParticion, ILogger? objLogger = null)
    {
        List<CostoProductivoCuentaDto> lstCuentasDes = cuentas.Where(x => Normalizar(x.strPcCodigo) == "DES").ToList();

        // Anterior: se agrega el logger para diagnosticar descabezado.
        // if (lstCuentasDes.Count == 0)
        //     return;
        // Se registra también la ausencia de cuentas antes de retornar.

        List<LibrasParticionDto> lstLibrasDes = (librasParticion ?? Array.Empty<LibrasParticionDto>())
            .Where(x => Normalizar(x.strProceso) == "DESCABEZADO").ToList();

        var objDesPfr = new CostoProductivoProcesoOrigenDto
        {
            strPcCodigo = "DES",
            strProceso = "Descabezado",
            strOrigen = "PFR",
            dcLibrasEntero = Libras(lstLibrasDes, "PFR", ParticionCosteo.ENTERO),
            dcLibrasCola = Libras(lstLibrasDes, "PFR", ParticionCosteo.COLA),
            dcLibrasValorAgregado = LibrasVa(lstLibrasDes, "PFR")
        };
        // Reemplazado: recalcular y agregar PFR después de asignar sus dólares.
        //objDesPfr.Recalcular();
        //if (objDesPfr.dcTotalLibras != 0m)
        //    resumen.Add(objDesPfr);

        var objDesRpc = new CostoProductivoProcesoOrigenDto
        {
            strPcCodigo = "DES",
            strProceso = "Descabezado",
            strOrigen = "RPC",
            dcLibrasEntero = Libras(lstLibrasDes, "RPC", ParticionCosteo.ENTERO),
            dcLibrasCola = Libras(lstLibrasDes, "RPC", ParticionCosteo.COLA),
            dcLibrasValorAgregado = LibrasVa(lstLibrasDes, "RPC"),
            // Reemplazado: los dólares se reparten entre PFR y RPC por partición.
            //dcDolaresEntero = lstCuentasDes.Sum(x => x.dcMontoEnteroPersistencia),
            //dcDolaresCola = lstCuentasDes.Sum(x => x.dcMontoColaPersistencia),
            //dcDolaresValorAgregado = lstCuentasDes.Sum(x => x.dcMontoVagPersistencia)
        };
        decimal dcTotalEntero = lstCuentasDes.Sum(x => x.dcMontoEnteroPersistencia);
        decimal dcTotalCola = lstCuentasDes.Sum(x => x.dcMontoColaPersistencia);
        decimal dcTotalVag = lstCuentasDes.Sum(x => x.dcMontoVagPersistencia);
        decimal dcLibrasEntero = objDesPfr.dcLibrasEntero + objDesRpc.dcLibrasEntero;
        decimal dcLibrasCola = objDesPfr.dcLibrasCola + objDesRpc.dcLibrasCola;
        decimal dcLibrasVag = objDesPfr.dcLibrasValorAgregado + objDesRpc.dcLibrasValorAgregado;

        objLogger?.LogInformation("[DiagDes] Cuentas={Cuentas} TotalDES Entero={MontoEntero} Cola={MontoCola} VAG={MontoVag} LibrasPFR Entero={PfrEntero} Cola={PfrCola} VAG={PfrVag} LibrasRPC Entero={RpcEntero} Cola={RpcCola} VAG={RpcVag}",
            lstCuentasDes.Count, dcTotalEntero, dcTotalCola, dcTotalVag,
            objDesPfr.dcLibrasEntero, objDesPfr.dcLibrasCola, objDesPfr.dcLibrasValorAgregado,
            objDesRpc.dcLibrasEntero, objDesRpc.dcLibrasCola, objDesRpc.dcLibrasValorAgregado);
        if (lstCuentasDes.Count == 0) return;
        // Mismo costo unitario por partición; RPC conserva el residuo o todo si no hay libras.
        objDesPfr.dcDolaresEntero = dcLibrasEntero > 0m ? Math.Round(dcTotalEntero * objDesPfr.dcLibrasEntero / dcLibrasEntero, 5) : 0m;
        objDesPfr.dcDolaresCola = dcLibrasCola > 0m ? Math.Round(dcTotalCola * objDesPfr.dcLibrasCola / dcLibrasCola, 5) : 0m;
        objDesPfr.dcDolaresValorAgregado = dcLibrasVag > 0m ? Math.Round(dcTotalVag * objDesPfr.dcLibrasValorAgregado / dcLibrasVag, 5) : 0m;
        objDesRpc.dcDolaresEntero = dcTotalEntero - objDesPfr.dcDolaresEntero;
        objDesRpc.dcDolaresCola = dcTotalCola - objDesPfr.dcDolaresCola;
        objDesRpc.dcDolaresValorAgregado = dcTotalVag - objDesPfr.dcDolaresValorAgregado;
        objDesPfr.Recalcular();
        if (objDesPfr.dcTotalDolares != 0m || objDesPfr.dcTotalLibras != 0m)
            resumen.Add(objDesPfr);
        objDesRpc.Recalcular();
        if (objDesRpc.dcTotalDolares != 0m || objDesRpc.dcTotalLibras != 0m)
            resumen.Add(objDesRpc);
    }

    /// <summary>
    /// Table6 (importes persistibles por cuenta) y Table9 (resumen proceso/origen que
    /// alimenta Warren y el snapshot) deben representar el mismo universo monetario por
    /// proceso. MPE se excluye porque Material de Empaque queda fuera de "Total Procesos"
    /// y de Warren.
    /// </summary>
    public static void ValidarCuadreContraCuentas(
        IEnumerable<CostoProductivoProcesoOrigenDto> lstResumen,
        IEnumerable<CostoProductivoCuentaDto> lstCuentas)
    {
        const decimal dcTolerancia = 0.01m;

        var dicCuentas = (lstCuentas ?? Array.Empty<CostoProductivoCuentaDto>())
            .Where(x => !string.IsNullOrWhiteSpace(x.strPcCodigo))
            .Where(x => Normalizar(x.strPcCodigo) != "MPE")
            .GroupBy(x => Normalizar(x.strPcCodigo))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Entero = Math.Round(g.Sum(x => x.dcMontoEnteroPersistencia), 5),
                    Cola = Math.Round(g.Sum(x => x.dcMontoColaPersistencia), 5),
                    Vag = Math.Round(g.Sum(x => x.dcMontoVagPersistencia), 5)
                },
                StringComparer.OrdinalIgnoreCase);

        var dicResumen = (lstResumen ?? Array.Empty<CostoProductivoProcesoOrigenDto>())
            .Where(x => !string.IsNullOrWhiteSpace(x.strPcCodigo))
            .GroupBy(x => Normalizar(x.strPcCodigo))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Entero = Math.Round(g.Sum(x => x.dcDolaresEntero), 5),
                    Cola = Math.Round(g.Sum(x => x.dcDolaresCola), 5),
                    Vag = Math.Round(g.Sum(x => x.dcDolaresValorAgregado), 5)
                },
                StringComparer.OrdinalIgnoreCase);

        List<string> lstErrores = new();

        foreach (var objCuenta in dicCuentas)
        {
            dicResumen.TryGetValue(objCuenta.Key, out var objResumen);

            decimal dcDifEntero = objCuenta.Value.Entero - (objResumen?.Entero ?? 0m);
            decimal dcDifCola = objCuenta.Value.Cola - (objResumen?.Cola ?? 0m);
            decimal dcDifVag = objCuenta.Value.Vag - (objResumen?.Vag ?? 0m);

            if (Math.Abs(dcDifEntero) > dcTolerancia ||
                Math.Abs(dcDifCola) > dcTolerancia ||
                Math.Abs(dcDifVag) > dcTolerancia)
            {
                lstErrores.Add(
                    $"{objCuenta.Key}: EN dif={dcDifEntero:N5}, CO dif={dcDifCola:N5}, VA dif={dcDifVag:N5}");
            }
        }

        if (lstErrores.Count > 0)
        {
            throw new InvalidOperationException(
                "El resumen proceso/origen no cuadra contra los importes persistibles: " +
                string.Join(" | ", lstErrores));
        }
    }

    private static void AgregarIqf(
        ICollection<CostoProductivoProcesoOrigenDto> resumen,
        IReadOnlyCollection<CostoProductivoCuentaDto> cuentas,
        IEnumerable<DriverProcesoProductoDto>? driversIqf)
    {
        List<CostoProductivoCuentaDto> filas = cuentas
            .Where(x => Normalizar(x.strPcCodigo) == "IQF")
            .ToList();

        List<DriverProcesoProductoDto> drivers =
            (driversIqf ?? Array.Empty<DriverProcesoProductoDto>())
            .Where(x => Normalizar(x.strPcCodigo) == "IQF")
            .ToList();

        var dto = new CostoProductivoProcesoOrigenDto
        {
            strPcCodigo = "IQF",
            strProceso = "IQF",
            strOrigen = "RPC",

            dcLibrasEntero = drivers
                .Where(x => x.strParticionCodigo == ParticionCosteo.ENTERO)
                .Sum(x => x.dcLibras),

            dcLibrasCola = drivers
                .Where(x => x.strParticionCodigo == ParticionCosteo.COLA)
                .Sum(x => x.dcLibras),

            dcLibrasValorAgregado = drivers
                .Where(x => x.strParticionCodigo is
                    ParticionCosteo.ENTERO_VA or ParticionCosteo.COLA_VA)
                .Sum(x => x.dcLibras),

            dcDolaresEntero = filas.Sum(x => x.dcMontoEnteroPersistencia),
            dcDolaresCola = filas.Sum(x => x.dcMontoColaPersistencia),
            dcDolaresValorAgregado = filas.Sum(x => x.dcMontoVagPersistencia)
        };

        dto.Recalcular();

        if (dto.dcTotalDolares != 0m || dto.dcTotalLibras != 0m)
            resumen.Add(dto);
    }

    private static void AgregarCopacking(
        ICollection<CostoProductivoProcesoOrigenDto> resumen,
        IReadOnlyCollection<CostoProductivoCuentaDto> cuentas,
        IEnumerable<LibrasParticionDto>? librasParticion)
    {
        List<CostoProductivoCuentaDto> filas = cuentas
            .Where(x => Normalizar(x.strPcCodigo) == "COP")
            .ToList();

        List<LibrasParticionDto> libras =
            (librasParticion ?? Array.Empty<LibrasParticionDto>())
            .Where(x => Normalizar(x.strProceso) == "C.COPACKING")
            .ToList();

        decimal dolaresEn = filas.Sum(x => x.dcMontoEnteroPersistencia);
        decimal dolaresCo = filas.Sum(x => x.dcMontoColaPersistencia);
        decimal dolaresVa = filas.Sum(x => x.dcMontoVagPersistencia);

        decimal pfrEn = Libras(libras, "PFR", ParticionCosteo.ENTERO);
        decimal pfrCo = Libras(libras, "PFR", ParticionCosteo.COLA);
        decimal pfrVa = LibrasVa(libras, "PFR");

        decimal rpcEn = Libras(libras, "RPC", ParticionCosteo.ENTERO);
        decimal rpcCo = Libras(libras, "RPC", ParticionCosteo.COLA);
        decimal rpcVa = LibrasVa(libras, "RPC");

        decimal totalPfr = pfrEn + pfrCo + pfrVa;
        decimal totalRpc = rpcEn + rpcCo + rpcVa;

        (decimal pfrDolEn, decimal rpcDolEn) = DividirOrigen(
            dolaresEn, pfrEn, rpcEn, totalPfr, totalRpc, "EN");

        (decimal pfrDolCo, decimal rpcDolCo) = DividirOrigen(
            dolaresCo, pfrCo, rpcCo, totalPfr, totalRpc, "CO");

        (decimal pfrDolVa, decimal rpcDolVa) = DividirOrigen(
            dolaresVa, pfrVa, rpcVa, totalPfr, totalRpc, "VA");

        AgregarOrigenCopacking(
            resumen,
            "PFR",
            pfrEn,
            pfrCo,
            pfrVa,
            pfrDolEn,
            pfrDolCo,
            pfrDolVa);

        AgregarOrigenCopacking(
            resumen,
            "RPC",
            rpcEn,
            rpcCo,
            rpcVa,
            rpcDolEn,
            rpcDolCo,
            rpcDolVa);
    }

    private static void AgregarOrigenCopacking(
        ICollection<CostoProductivoProcesoOrigenDto> resumen,
        string origen,
        decimal lbsEn,
        decimal lbsCo,
        decimal lbsVa,
        decimal dolEn,
        decimal dolCo,
        decimal dolVa)
    {
        var dto = new CostoProductivoProcesoOrigenDto
        {
            strPcCodigo = "COP",
            strProceso = "C.Copacking",
            strOrigen = origen,
            dcLibrasEntero = lbsEn,
            dcLibrasCola = lbsCo,
            dcLibrasValorAgregado = lbsVa,
            dcDolaresEntero = dolEn,
            dcDolaresCola = dolCo,
            dcDolaresValorAgregado = dolVa
        };

        dto.Recalcular();

        if (dto.dcTotalDolares != 0m || dto.dcTotalLibras != 0m)
            resumen.Add(dto);
    }

    private static (decimal Pfr, decimal Rpc) DividirOrigen(
        decimal monto,
        decimal lbsPfrParticion,
        decimal lbsRpcParticion,
        decimal lbsPfrGlobal,
        decimal lbsRpcGlobal,
        string particion)
    {
        monto = Math.Round(monto, 5);

        if (monto == 0m)
            return (0m, 0m);

        decimal basePfr = lbsPfrParticion;
        decimal baseRpc = lbsRpcParticion;
        decimal total = basePfr + baseRpc;

        // Algunas cuentas contables COP pueden estar en un centro sin una base
        // física separada para esa partición. En ese caso se conserva el origen
        // usando la participación global de libras Copacking.
        if (total <= 0m)
        {
            basePfr = lbsPfrGlobal;
            baseRpc = lbsRpcGlobal;
            total = basePfr + baseRpc;
        }

        if (total <= 0m)
        {
            throw new InvalidOperationException(
                $"Copacking tiene dólares en {particion}, pero no existen " +
                "libras PFR/RPC para separar el origen.");
        }

        decimal pfr = Math.Round(monto * basePfr / total, 5);
        decimal rpc = Math.Round(monto - pfr, 5);
        return (pfr, rpc);
    }

    private static decimal Libras(
        IEnumerable<LibrasParticionDto> filas,
        string origen,
        string particion) =>
        filas
            .Where(x => Normalizar(x.strOrigen) == origen)
            .Where(x => x.strParticionCod == particion)
            .Sum(x => x.dcLibras);

    private static decimal LibrasVa(
        IEnumerable<LibrasParticionDto> filas,
        string origen) =>
        filas
            .Where(x => Normalizar(x.strOrigen) == origen)
            .Where(x => x.strParticionCod is
                ParticionCosteo.ENTERO_VA or ParticionCosteo.COLA_VA)
            .Sum(x => x.dcLibras);

    private static string Normalizar(string? valor) =>
        (valor ?? string.Empty)
        .Trim()
        .ToUpperInvariant()
        .Replace("Á", "A")
        .Replace("É", "E")
        .Replace("Í", "I")
        .Replace("Ó", "O")
        .Replace("Ú", "U");
}
