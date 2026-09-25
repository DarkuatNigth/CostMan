using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Infraestructura.EF_Core;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace CostManagement.Infraestructura.Repository.Services
{
    public sealed class CostoProductivoService : ICostoProductivoService
    {
        private readonly IDbContextFactory<CostosDbContext> _objCostosFactory;
        private readonly IDbContextFactory<SongDbContext> _objSongFactory;
        private readonly ILogger<CostoProductivoService> _objLogger;

        public CostoProductivoService(
            ILogger<CostoProductivoService> objLogger,
            IDbContextFactory<CostosDbContext> objCostosFactory,
            IDbContextFactory<SongDbContext> objSongFactory)
        {
            _objLogger = objLogger;
            _objCostosFactory = objCostosFactory;
            _objSongFactory = objSongFactory;
        }

        public async Task<CostoProductivoConfiguracionDbDto> ConsultarConfiguracionCostoProductivo()
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        DbConnection cn = ctx.Database.GetDbConnection();
                        if (cn.State != ConnectionState.Open)
                            await cn.OpenAsync();

                        await using DbCommand cmd = cn.CreateCommand();
                        cmd.CommandText = ValueObjectsCostoProductivo.spConfiguracionCostoProductivoCostos;
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 180;

                        await using DbDataReader rd = await cmd.ExecuteReaderAsync();

                        // Resultset 1: cuentas configuradas en ConnectionCostos.
                        List<CostoProductivoConfigCuentaDto> cuentas =
                            rd.MapToList<CostoProductivoConfigCuentaDto>();

                        // Resultset 2: matriz SI/X2/TAR.
                        List<ProcesoCostoAplicacionDto> aplicaciones = new();
                        if (await rd.NextResultAsync())
                            aplicaciones = rd.MapToList<ProcesoCostoAplicacionDto>();

                        // Resultset 3: metadatos de la fila derivada Descongelado.
                        CostoProductivoDerivadoConfigDto objDescongelado = new();
                        if (await rd.NextResultAsync())
                        {
                            List<CostoProductivoDerivadoConfigDto> derivados =
                                rd.MapToList<CostoProductivoDerivadoConfigDto>();

                            objDescongelado =
                                derivados.FirstOrDefault()
                                ?? new CostoProductivoDerivadoConfigDto();
                        }

                        return new CostoProductivoConfiguracionDbDto
                        {
                            lstCuentas = cuentas,
                            lstAplicaciones = aplicaciones,
                            objDescongelado = objDescongelado
                        };
                    }, 180);
            }
            catch (Exception ex)
            {
                ManejoLog<CostoProductivoService>.Error(
                    _objLogger, nameof(CostoProductivoService),
                    nameof(ConsultarConfiguracionCostoProductivo), ex);
                throw;
            }
        }


        public async Task<List<SaldoCuentaSongDto>> ConsultarSaldosCuentaSong(int intAnio)
        {
            try
            {
                return await ManejoContext<SongDbContext>.EjecutarAsync(
                    _objSongFactory,
                    async ctx =>
                    {
                        DbConnection cn = ctx.Database.GetDbConnection();
                        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

                        await using DbCommand cmd = cn.CreateCommand();
                        cmd.CommandText = ValueObjectsCostoProductivo.spSaldosCostoProductivoSong;
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 180;
                        AgregarParametro(cmd, "@anio", DbType.Int32, intAnio);

                        await using DbDataReader rd = await cmd.ExecuteReaderAsync();
                        return rd.MapToList<SaldoCuentaSongDto>();
                    }, 180);
            }
            catch (Exception ex)
            {
                ManejoLog<CostoProductivoService>.Error(
                    _objLogger, nameof(CostoProductivoService),
                    nameof(ConsultarSaldosCuentaSong), ex);
                throw;
            }
        }

        public async Task<bool> GuardarDistribucionProcesoProductivo(
            int intAnio, int intMes, DateOnly dtFechaCorte,
            List<CostoProductivoCuentaDto> lstRegistros, string strUsuario)
        {
            try
            {
                if (lstRegistros == null || lstRegistros.Count == 0)
                    throw new ArgumentException("No existen registros para guardar.");

                if (string.IsNullOrWhiteSpace(strUsuario))
                    throw new ArgumentException("El usuario es obligatorio.");

                /*
                 * Validar información necesaria para conservar el detalle:
                 * Etapa + Proceso + Cuenta + Centro + Subcentro + Agrupación + Grupo.
                 *
                 * Descongelado es derivado y puede no tener cuenta contable.
                 */
                var lstInvalidos = lstRegistros
                    .Where(x =>
                        string.IsNullOrWhiteSpace(x.strEcCodigo) ||
                        string.IsNullOrWhiteSpace(x.strPcCodigo) ||
                        (!x.blEsDerivado && string.IsNullOrWhiteSpace(x.strCuenta)) ||
                        string.IsNullOrWhiteSpace(x.strCentroCodigo) ||
                        string.IsNullOrWhiteSpace(x.strSubcentroCodigo) ||
                        string.IsNullOrWhiteSpace(x.strAgrupacionCentro) ||
                        string.IsNullOrWhiteSpace(x.strGrupoCentro))
                    .Select(x => new
                    {
                        x.strCuenta,
                        x.strEcCodigo,
                        x.strPcCodigo,
                        x.strEtapa,
                        x.strCentroCodigo,
                        x.strSubcentroCodigo,
                        x.strAgrupacionCentro,
                        x.strGrupoCentro,
                        x.blEsDerivado
                    })
                    .Take(20)
                    .ToList();

                if (lstInvalidos.Count > 0)
                {
                    string detalle = string.Join(" | ", lstInvalidos.Select(x =>
                        $"Cuenta:{x.strCuenta}, Etapa:{x.strEcCodigo}, Proceso:{x.strPcCodigo}, Centro:{x.strCentroCodigo}, Sub:{x.strSubcentroCodigo}"));

                    throw new InvalidOperationException(
                        "Existen registros incompletos para guardar costo productivo: " + detalle);
                }

                /*
                 * Grano real de persistencia:
                 *
                 * Etapa + Proceso + Cuenta + Centro + Subcentro +
                 * Agrupación + Grupo + Naturaleza.
                 *
                 * Esto permite recuperar después el valor exacto por cuenta/proceso.
                 */
                var lstAgrupados = lstRegistros
                    .GroupBy(x => new
                    {
                        Ec = (x.strEcCodigo ?? string.Empty).Trim(),
                        Pc = (x.strPcCodigo ?? string.Empty).Trim(),
                        Cuenta = (x.strCuenta ?? string.Empty).Trim(),
                        Cen = (x.strCentroCodigo ?? string.Empty).Trim(),
                        Sub = (x.strSubcentroCodigo ?? string.Empty).Trim(),
                        Agrupacion = (x.strAgrupacionCentro ?? string.Empty).Trim(),
                        Grupo = (x.strGrupoCentro ?? string.Empty).Trim(),
                        Natura = string.IsNullOrWhiteSpace(x.strNaturaleza)
                            ? "D"
                            : x.strNaturaleza.Trim().ToUpperInvariant()
                    })
                    .Select(g => new
                    {
                        g.Key.Ec,
                        g.Key.Pc,
                        g.Key.Cuenta,
                        g.Key.Cen,
                        g.Key.Sub,
                        g.Key.Agrupacion,
                        g.Key.Grupo,
                        g.Key.Natura,

                        /*
                         * Estos valores ya vienen preparados por:
                         * PrepararMontosPersistencia().
                         *
                         * También CDF/CDV/CIF/CIV llegan distribuidos
                         * proporcionalmente entre Entero / Cola / VAG.
                         */
                        MontoEntero = Math.Round(g.Sum(x => x.dcMontoEnteroPersistencia), 5),
                        MontoCola = Math.Round(g.Sum(x => x.dcMontoColaPersistencia), 5),
                        MontoVag = Math.Round(g.Sum(x => x.dcMontoVagPersistencia), 5)
                    })
                    .Where(x => x.MontoEntero != 0m || x.MontoCola != 0m || x.MontoVag != 0m)
                    .ToList();

                if (lstAgrupados.Count == 0)
                    throw new InvalidOperationException("No existen montos distintos de cero para guardar.");

                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        DateTime ahora = DateTime.Now;
                        string equipo = Environment.MachineName;

                        /*
                         * Anular snapshot activo anterior del mismo período.
                         */
                        List<TbDistribucionProcesoProductivo> lstExistentes = await objContext
                            .TbDistribucionProcesoProductivo
                            .AsTracking()
                            .Where(x => x.DpAnio == intAnio && x.DpMes == intMes && x.DpEstado == "AC")
                            .ToListAsync();

                        foreach (TbDistribucionProcesoProductivo registro in lstExistentes)
                        {
                            registro.DpEstado = "AN";
                            registro.DpUsuarioEli = strUsuario;
                            registro.DpFechaEli = ahora;
                            registro.DpEquipoEli = equipo;
                        }

                        /*
                         * Crear nuevo snapshot.
                         */
                        List<TbDistribucionProcesoProductivo> lstNuevos = lstAgrupados
                            .Select(x => new TbDistribucionProcesoProductivo
                            {
                                DpAnio = checked((short)intAnio),
                                DpMes = checked((short)intMes),
                                DpFechaCorte = dtFechaCorte,

                                DpEcCodigo = x.Ec,
                                DpPcCodigo = x.Pc,
                                DpCtaNumero = x.Cuenta,

                                DpCenCodigo = x.Cen,
                                DpSubCodigo = x.Sub,

                                DpMontoEntero = x.MontoEntero,
                                DpMontoCola = x.MontoCola,
                                DpMontoVag = x.MontoVag,

                                DpAgrupacionCentro = x.Agrupacion,
                                DpGrupoCentro = x.Grupo,
                                DpCtaNatura = x.Natura,

                                DpEstado = "AC",
                                DpUsuarioCrea = strUsuario,
                                DpFechaCrea = ahora,
                                DpEquipoCrea = equipo
                            })
                            .ToList();

                        /*
                         * Una sola llamada SaveChangesAsync dentro de la transacción
                         * alcanza para UPDATE + INSERT.
                         */
                        if (lstNuevos.Count > 0)
                            await objContext.TbDistribucionProcesoProductivo.AddRangeAsync(lstNuevos);

                        int intAfectados = await objContext.SaveChangesAsync();

                        _objLogger.LogInformation(
                            "Costo productivo guardado por EF. Año: {Anio}, Mes: {Mes}, " +
                            "Filas anuladas: {Anuladas}, Filas nuevas: {Nuevas}, SaveChanges: {Afectados}",
                            intAnio, intMes, lstExistentes.Count, lstNuevos.Count, intAfectados);

                        return lstNuevos.Count > 0;
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<CostoProductivoService>.Error(
                    _objLogger,
                    nameof(CostoProductivoService),
                    nameof(GuardarDistribucionProcesoProductivo),
                    ex);

                throw;
            }
        }

        private static void AgregarParametro(
            DbCommand cmd, string nombre, DbType tipo, object? valor)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = nombre;
            p.DbType = tipo;
            p.Value = valor ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        public async Task<List<CostoProductivoDetalleModalDto>> ConsultarDetalleCostoProductivoPeriodo(
        int intAnio,
        int intMes)
        {
            try
            {
                return await ManejoContext<CostosDbContext>
                    .EjecutarAsync(
                        _objCostosFactory,
                        async objContext =>
                        {
                            DbConnection cn =
                                objContext.Database.GetDbConnection();

                            if (cn.State != ConnectionState.Open)
                                await cn.OpenAsync();

                            await using DbCommand cmd =
                                cn.CreateCommand();

                            cmd.CommandText =
                                "costos.sp_costoProductivo_detalleModal";

                            cmd.CommandType =
                                CommandType.StoredProcedure;

                            cmd.CommandTimeout = 180;

                            AgregarParametro(
                                cmd,
                                "@anio",
                                DbType.Int16,
                                intAnio);

                            AgregarParametro(
                                cmd,
                                "@mes",
                                DbType.Int16,
                                intMes);

                            AgregarParametro(
                                cmd,
                                "@pcCodigo",
                                DbType.String,
                                DBNull.Value);

                            await using DbDataReader reader =
                                await cmd.ExecuteReaderAsync();

                            return reader
                                .MapToList<CostoProductivoDetalleModalDto>();
                        },
                        180);
            }
            catch (Exception ex)
            {
                ManejoLog<CostoProductivoService>.Error(
                    _objLogger,
                    nameof(CostoProductivoService),
                    nameof(ConsultarDetalleCostoProductivoPeriodo),
                    ex);

                throw;
            }
        }

        public async Task<bool> GuardarCierreParamProcAtomico(
           int intAnio,
           int intMes,
           DateOnly dtFechaCorte,
           List<CostoProductivoCuentaDto> lstRegistros,
           List<ProcesoResultadoDto> lstParametros,
           WarrenResultadoDto objWarren,
           string strUsuario)
        {
            if (lstRegistros == null || lstRegistros.Count == 0)
                throw new ArgumentException("No existen cuentas de costo productivo.");
            if (lstParametros == null || lstParametros.Count == 0)
                throw new ArgumentException("No existen parámetros PFR/RPC.");
            if (objWarren?.lstDetalle == null || objWarren.lstDetalle.Count == 0)
                throw new ArgumentException("No existe detalle Warren.");
            if (string.IsNullOrWhiteSpace(strUsuario))
                throw new ArgumentException("El usuario es obligatorio.");

            var invalidos = lstRegistros
                .Where(x =>
                    string.IsNullOrWhiteSpace(x.strEcCodigo) ||
                    string.IsNullOrWhiteSpace(x.strPcCodigo) ||
                    (!x.blEsDerivado && string.IsNullOrWhiteSpace(x.strCuenta)) ||
                    string.IsNullOrWhiteSpace(x.strCentroCodigo) ||
                    string.IsNullOrWhiteSpace(x.strSubcentroCodigo) ||
                    string.IsNullOrWhiteSpace(x.strAgrupacionCentro) ||
                    string.IsNullOrWhiteSpace(x.strGrupoCentro))
                .Take(10)
                .ToList();

            if (invalidos.Count > 0)
                throw new InvalidOperationException(
                    "Existen registros incompletos en el snapshot de costo productivo.");

            return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                _objCostosFactory,
                async ctx =>
                {
                    DateTime ahora = DateTime.Now;
                    string equipo = Environment.MachineName;
                    string usuario = strUsuario.Trim();

                    // 1. Snapshot contable/productivo.
                    List<TbDistribucionProcesoProductivo> anteriores = await ctx
                        .TbDistribucionProcesoProductivo
                        .AsTracking()
                        .Where(x => x.DpAnio == intAnio && x.DpMes == intMes && x.DpEstado == "AC")
                        .ToListAsync();

                    foreach (var item in anteriores)
                    {
                        item.DpEstado = "AN";
                        item.DpUsuarioEli = usuario;
                        item.DpFechaEli = ahora;
                        item.DpEquipoEli = equipo;
                    }

                    var distribucion = lstRegistros
                        .GroupBy(x => new
                        {
                            Ec = (x.strEcCodigo ?? string.Empty).Trim(),
                            Pc = (x.strPcCodigo ?? string.Empty).Trim(),
                            Cuenta = (x.strCuenta ?? string.Empty).Trim(),
                            Cen = (x.strCentroCodigo ?? string.Empty).Trim(),
                            Sub = (x.strSubcentroCodigo ?? string.Empty).Trim(),
                            Agrupacion = (x.strAgrupacionCentro ?? string.Empty).Trim(),
                            Grupo = (x.strGrupoCentro ?? string.Empty).Trim(),
                            Natura = string.IsNullOrWhiteSpace(x.strNaturaleza)
                                ? "D" : x.strNaturaleza.Trim().ToUpperInvariant()
                        })
                        .Select(g => new TbDistribucionProcesoProductivo
                        {
                            DpAnio = checked((short)intAnio),
                            DpMes = checked((short)intMes),
                            DpFechaCorte = dtFechaCorte,
                            DpEcCodigo = g.Key.Ec,
                            DpPcCodigo = g.Key.Pc,
                            DpCtaNumero = g.Key.Cuenta,
                            DpCenCodigo = g.Key.Cen,
                            DpSubCodigo = g.Key.Sub,
                            DpMontoEntero = Math.Round(g.Sum(x => x.dcMontoEnteroPersistencia), 5),
                            DpMontoCola = Math.Round(g.Sum(x => x.dcMontoColaPersistencia), 5),
                            DpMontoVag = Math.Round(g.Sum(x => x.dcMontoVagPersistencia), 5),
                            DpAgrupacionCentro = g.Key.Agrupacion,
                            DpGrupoCentro = g.Key.Grupo,
                            DpCtaNatura = g.Key.Natura,
                            DpEstado = "AC",
                            DpUsuarioCrea = usuario,
                            DpFechaCrea = ahora,
                            DpEquipoCrea = equipo
                        })
                        .Where(x => x.DpMontoEntero != 0m || x.DpMontoCola != 0m || x.DpMontoVag != 0m)
                        .ToList();

                    if (distribucion.Count == 0)
                        throw new InvalidOperationException(
                            "No existen montos productivos distintos de cero para guardar.");

                    await ctx.TbDistribucionProcesoProductivo.AddRangeAsync(distribucion);

                    // 2. Parámetros PFR/RPC y Warren. Se eliminan únicamente los IDs
                    // involucrados; las tarifas RPC de otros procesos no se tocan.
                    List<byte> idsParametros = lstParametros
                        .Where(x => x.intCodigo > 0)
                        .Select(x => checked((byte)x.intCodigo))
                        .Distinct()
                        .ToList();
                    List<byte> idsWarren = objWarren.lstDetalle
                        .Where(x => x.intCodigo > 0)
                        .Select(x => checked((byte)x.intCodigo))
                        .Distinct()
                        .ToList();

                    List<TbParametroCosteo> parametrosAnteriores = await ctx.TbParametroCosteo
                        .AsTracking()
                        .Where(x => x.PcFecha == dtFechaCorte &&
                            ((idsParametros.Contains(x.PcPrId) &&
                              (x.PcTipoLote == "PFR" || x.PcTipoLote == "RPC")) ||
                             (idsWarren.Contains(x.PcPrId) &&
                              (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH")) ||
                             x.PcTipoLote == "WOB"))
                        .ToListAsync();

                    ctx.TbParametroCosteo.RemoveRange(parametrosAnteriores);

                    List<TbParametroCosteo> nuevosParametros = lstParametros
                        .Where(x => x.intCodigo > 0)
                        .GroupBy(x => new
                        {
                            x.intCodigo,
                            Tipo = (x.strTipoLote ?? string.Empty).Trim().ToUpperInvariant()
                        })
                        .Select(g => g.Last())
                        .Select(x => new TbParametroCosteo
                        {
                            PcPrId = checked((byte)x.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = (x.strTipoLote ?? string.Empty).Trim().ToUpperInvariant(),
                            PcLibras = Math.Round(x.dcLibras, 5),
                            PcCotoUnitario = Math.Round(x.dcCostUnitario ?? 0m, 5),
                            PcMonto = Math.Round(x.dcValor, 5),
                            PcEstado = "CE",
                            PcUsuarioCrea = usuario,
                            PcFechaCrea = ahora,
                            PcEquipoCrea = equipo
                        })
                        .ToList();

                    foreach (WarrenProcesoDto d in objWarren.lstDetalle.Where(x => x.intCodigo > 0))
                    {
                        nuevosParametros.Add(new TbParametroCosteo
                        {
                            PcPrId = checked((byte)d.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = "WEN",
                            PcLibras = Math.Round(d.dcLibrasEntero, 5),
                            PcCotoUnitario = Math.Round(d.dcCostoUnitarioEnteroWarren, 5),
                            PcMonto = Math.Round(d.dcMontoEnteroWarren, 5),
                            PcEstado = "CE",
                            PcUsuarioCrea = usuario,
                            PcFechaCrea = ahora,
                            PcEquipoCrea = equipo
                        });
                        nuevosParametros.Add(new TbParametroCosteo
                        {
                            PcPrId = checked((byte)d.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = "WSH",
                            PcLibras = Math.Round(d.dcLibrasCola, 5),
                            PcCotoUnitario = Math.Round(d.dcCostoUnitarioColaWarren, 5),
                            PcMonto = Math.Round(d.dcMontoColaWarren, 5),
                            PcEstado = "CE",
                            PcUsuarioCrea = usuario,
                            PcFechaCrea = ahora,
                            PcEquipoCrea = equipo
                        });
                    }

                    WarrenProcesoDto cabecera = objWarren.lstDetalle.First(x => x.intCodigo > 0);
                    nuevosParametros.Add(new TbParametroCosteo
                    {
                        PcPrId = checked((byte)cabecera.intCodigo),
                        PcFecha = dtFechaCorte,
                        PcTipoLote = "WOB",
                        PcLibras = Math.Round(objWarren.dcLibrasCola, 5),
                        PcCotoUnitario = Math.Round(objWarren.dcCostoColaActual, 5),
                        PcMonto = Math.Round(objWarren.dcObjetivoWarren, 5),
                        PcEstado = "CE",
                        PcUsuarioCrea = usuario,
                        PcFechaCrea = ahora,
                        PcEquipoCrea = equipo
                    });

                    await ctx.TbParametroCosteo.AddRangeAsync(nuevosParametros);

                    // Un solo SaveChanges: si falla cualquier bloque se revierte todo.
                    int afectados = await ctx.SaveChangesAsync();
                    return afectados > 0;
                });
        }
    }
}
