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

                        List<IqfConfiguracionDto> iqf = new();

                        if (await rd.NextResultAsync())
                            iqf = rd.MapToList<IqfConfiguracionDto>();

                        return new CostoProductivoConfiguracionDbDto
                        {
                            lstCuentas = cuentas,
                            lstAplicaciones = aplicaciones,
                            objDescongelado = objDescongelado,
                            lstIqf = iqf
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

        private static bool TieneMontoPersistible(
            CostoProductivoCuentaDto fila)
        {
            return
                Math.Round(fila.dcMontoEnteroPersistencia, 5) != 0m ||
                Math.Round(fila.dcMontoColaPersistencia, 5) != 0m ||
                Math.Round(fila.dcMontoVagPersistencia, 5) != 0m;
        }

        private static List<string> ObtenerCamposFaltantes(
            CostoProductivoCuentaDto fila)
        {
            var faltantes = new List<string>();

            if (string.IsNullOrWhiteSpace(fila.strEcCodigo))
                faltantes.Add(nameof(fila.strEcCodigo));

            if (string.IsNullOrWhiteSpace(fila.strPcCodigo))
                faltantes.Add(nameof(fila.strPcCodigo));

            if (!fila.blEsDerivado &&
                string.IsNullOrWhiteSpace(fila.strCuenta))
            {
                faltantes.Add(nameof(fila.strCuenta));
            }

            if (string.IsNullOrWhiteSpace(fila.strCentroCodigo))
                faltantes.Add(nameof(fila.strCentroCodigo));

            if (string.IsNullOrWhiteSpace(fila.strSubcentroCodigo))
                faltantes.Add(nameof(fila.strSubcentroCodigo));

            if (string.IsNullOrWhiteSpace(fila.strAgrupacionCentro))
                faltantes.Add(nameof(fila.strAgrupacionCentro));

            if (string.IsNullOrWhiteSpace(fila.strGrupoCentro))
                faltantes.Add(nameof(fila.strGrupoCentro));

            return faltantes;
        }

        private List<CostoProductivoCuentaDto>
            ValidarYObtenerRegistrosPersistibles(
                List<CostoProductivoCuentaDto> registros,
                string operacion)
        {
            ArgumentNullException.ThrowIfNull(registros);

            foreach (CostoProductivoCuentaDto fila in registros)
                fila.RecalcularTotales();

            // Detecta un error de preparación real; no lo oculta eliminando la fila.
            List<CostoProductivoCuentaDto> sinPersistencia = registros
                .Where(x => Math.Round(x.dcTotal, 5) != 0m)
                .Where(x => !TieneMontoPersistible(x))
                .Take(20)
                .ToList();

            if (sinPersistencia.Count > 0)
            {
                string detalle = string.Join(
                    " | ",
                    sinPersistencia.Select(x =>
                        $"Id={x.intId}, Cuenta={x.strCuenta}, " +
                        $"Ec={x.strEcCodigo}, Pc={x.strPcCodigo}, " +
                        $"Total={x.dcTotal:N5}"));

                _objLogger.LogError(
                    "[{Operacion}] Filas con total visual sin persistencia: {Detalle}",
                    operacion,
                    detalle);

                throw new InvalidOperationException(
                    "Existen filas con total visual distinto de cero, pero sin " +
                    "montos de persistencia. " + detalle);
            }

            List<CostoProductivoCuentaDto> persistibles = registros
                .Where(TieneMontoPersistible)
                .ToList();

            if (persistibles.Count == 0)
            {
                throw new InvalidOperationException(
                    "No existen montos productivos distintos de cero para guardar.");
            }

            var invalidos = persistibles
                .Select(x => new
                {
                    Fila = x,
                    Faltantes = ObtenerCamposFaltantes(x)
                })
                .Where(x => x.Faltantes.Count > 0)
                .Take(20)
                .ToList();

            if (invalidos.Count > 0)
            {
                string detalle = string.Join(
                    " | ",
                    invalidos.Select(x =>
                        $"Id={x.Fila.intId}, Origen={x.Fila.strOrigen}, " +
                        $"Cuenta={x.Fila.strCuenta}, Ec={x.Fila.strEcCodigo}, " +
                        $"Pc={x.Fila.strPcCodigo}, Centro={x.Fila.strCentroCodigo}, " +
                        $"Sub={x.Fila.strSubcentroCodigo}, " +
                        $"Faltan=[{string.Join(", ", x.Faltantes)}]"));

                _objLogger.LogError(
                    "[{Operacion}] Snapshot persistible incompleto: {Detalle}",
                    operacion,
                    detalle);

                throw new InvalidOperationException(
                    "Existen registros persistibles incompletos en el snapshot. " +
                    detalle);
            }

            return persistibles;
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

                List<CostoProductivoCuentaDto> registrosPersistibles =
                    ValidarYObtenerRegistrosPersistibles(
                        lstRegistros,
                        nameof(GuardarDistribucionProcesoProductivo));

                /*
                 * Grano real de persistencia:
                 *
                 * Etapa + Proceso + Cuenta + Centro + Subcentro +
                 * Agrupación + Grupo + Naturaleza.
                 *
                 * Esto permite recuperar después el valor exacto por cuenta/proceso.
                 */
                var lstAgrupados = registrosPersistibles
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

            List<CostoProductivoCuentaDto> registrosPersistibles =
                ValidarYObtenerRegistrosPersistibles(
                    lstRegistros,
                    nameof(GuardarCierreParamProcAtomico));

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

                    var distribucion = registrosPersistibles
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

        /// <summary>
        /// Persiste exactamente el snapshot contable que el GET ya construyó y que
        /// el usuario vio en pantalla. No consulta Producción/SONG; solo usa
        /// CostosDbContext dentro de una transacción.
        /// </summary>
        public async Task<bool> GuardarCierreParamProcSnapshotAtomico(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            List<CostoProductivoDetalleModalDto> lstDetalleContable,
            List<ProcesoResultadoDto> lstParametros,
            WarrenResultadoDto objWarren,
            string strUsuario)
        {
            if (lstDetalleContable == null || lstDetalleContable.Count == 0)
                throw new ArgumentException(
                    "No existe detalle contable del snapshot para guardar.");

            if (lstParametros == null || lstParametros.Count == 0)
                throw new ArgumentException(
                    "No existen parámetros PFR/RPC del snapshot.");

            if (objWarren?.lstDetalle == null || objWarren.lstDetalle.Count == 0)
                throw new ArgumentException(
                    "No existe detalle Warren para guardar.");

            if (string.IsNullOrWhiteSpace(strUsuario))
                throw new ArgumentException("El usuario es obligatorio.");

            List<CostoProductivoDetalleModalDto> lstInvalidos = lstDetalleContable
                .Where(x =>
                    string.IsNullOrWhiteSpace(x.strEtapaCodigo) ||
                    string.IsNullOrWhiteSpace(x.strProcesoCodigo) ||
                    string.IsNullOrWhiteSpace(x.strCuenta) ||
                    string.IsNullOrWhiteSpace(x.strCentroCodigo) ||
                    string.IsNullOrWhiteSpace(x.strSubcentroCodigo) ||
                    string.IsNullOrWhiteSpace(x.strAgrupacionCentro) ||
                    string.IsNullOrWhiteSpace(x.strGrupoCentro) ||
                    string.IsNullOrWhiteSpace(x.strParticionCodigo))
                .Take(20)
                .ToList();

            if (lstInvalidos.Count > 0)
            {
                string strDetalle = string.Join(
                    " | ",
                    lstInvalidos.Select(x =>
                        $"Proceso:{x.strProcesoCodigo}, Cuenta:{x.strCuenta}, Partición:{x.strParticionCodigo}"));

                throw new InvalidOperationException(
                    "El snapshot contable contiene filas incompletas: " + strDetalle);
            }

            var lstDistribucionSnapshot = lstDetalleContable
                .Where(x => Math.Abs(x.dcDolares) > 0.0000001m)
                .GroupBy(x => new
                {
                    Ec = (x.strEtapaCodigo ?? string.Empty).Trim(),
                    Pc = (x.strProcesoCodigo ?? string.Empty).Trim(),
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

                    MontoEntero = Math.Round(
                        g.Where(x => NormalizarParticionSnapshot(x.strParticionCodigo) == "EN")
                            .Sum(x => x.dcDolares),
                        5),

                    MontoCola = Math.Round(
                        g.Where(x => NormalizarParticionSnapshot(x.strParticionCodigo) == "CO")
                            .Sum(x => x.dcDolares),
                        5),

                    MontoVag = Math.Round(
                        g.Where(x => NormalizarParticionSnapshot(x.strParticionCodigo) == "VA")
                            .Sum(x => x.dcDolares),
                        5)
                })
                .Where(x =>
                    x.MontoEntero != 0m ||
                    x.MontoCola != 0m ||
                    x.MontoVag != 0m)
                .ToList();

            if (lstDistribucionSnapshot.Count == 0)
                throw new InvalidOperationException(
                    "No existen montos contables distintos de cero para guardar.");

            // Validación clave: lo persistido debe volver exactamente a la suma del modal.
            decimal dcTotalModal = Math.Round(
                lstDetalleContable.Sum(x => x.dcDolares),
                4);

            decimal dcTotalPersistencia = Math.Round(
                lstDistribucionSnapshot.Sum(x =>
                    x.MontoEntero + x.MontoCola + x.MontoVag),
                4);

            if (Math.Abs(dcTotalModal - dcTotalPersistencia) > 0.01m)
                throw new InvalidOperationException(
                    $"El snapshot contable no cuadra antes de guardar. Modal: {dcTotalModal:N4}; " +
                    $"Persistencia: {dcTotalPersistencia:N4}.");

            return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                _objCostosFactory,
                async objContext =>
                {
                    DateTime dtAhora = DateTime.Now;
                    string strEquipo = Environment.MachineName;
                    string strUsuarioLimpio = strUsuario.Trim();

                    // ============================================================
                    // 1. ANULAR DISTRIBUCIÓN ACTIVA ANTERIOR
                    // ============================================================
                    List<TbDistribucionProcesoProductivo> lstAnteriores = await objContext
                        .TbDistribucionProcesoProductivo
                        .AsTracking()
                        .Where(x =>
                            x.DpAnio == intAnio &&
                            x.DpMes == intMes &&
                            x.DpEstado == "AC")
                        .ToListAsync();

                    foreach (TbDistribucionProcesoProductivo objItem in lstAnteriores)
                    {
                        objItem.DpEstado = "AN";
                        objItem.DpUsuarioEli = strUsuarioLimpio;
                        objItem.DpFechaEli = dtAhora;
                        objItem.DpEquipoEli = strEquipo;
                    }

                    // ============================================================
                    // 2. INSERTAR EXACTAMENTE EL SNAPSHOT DEL MODAL
                    // ============================================================
                    List<TbDistribucionProcesoProductivo> lstNuevos =
                        lstDistribucionSnapshot
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
                            DpUsuarioCrea = strUsuarioLimpio,
                            DpFechaCrea = dtAhora,
                            DpEquipoCrea = strEquipo
                        })
                        .ToList();

                    await objContext.TbDistribucionProcesoProductivo.AddRangeAsync(
                        lstNuevos);

                    // ============================================================
                    // 3. REEMPLAZAR PARÁMETROS PFR/RPC + WARREN
                    // ============================================================
                    List<byte> lstIdsParametros = lstParametros
                        .Where(x => x.intCodigo > 0)
                        .Select(x => checked((byte)x.intCodigo))
                        .Distinct()
                        .ToList();

                    List<byte> lstIdsWarren = objWarren.lstDetalle
                        .Where(x => x.intCodigo > 0)
                        .Select(x => checked((byte)x.intCodigo))
                        .Distinct()
                        .ToList();

                    List<TbParametroCosteo> lstParametrosAnteriores = await objContext
                        .TbParametroCosteo
                        .AsTracking()
                        .Where(x =>
                            x.PcFecha == dtFechaCorte &&
                            ((lstIdsParametros.Contains(x.PcPrId) &&
                              (x.PcTipoLote == "PFR" || x.PcTipoLote == "RPC")) ||
                             (lstIdsWarren.Contains(x.PcPrId) &&
                              (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH")) ||
                             x.PcTipoLote == "WOB"))
                        .ToListAsync();

                    objContext.TbParametroCosteo.RemoveRange(
                        lstParametrosAnteriores);

                    List<TbParametroCosteo> lstNuevosParametros = lstParametros
                        .Where(x => x.intCodigo > 0)
                        .GroupBy(x => new
                        {
                            x.intCodigo,
                            Tipo = (x.strTipoLote ?? string.Empty)
                                .Trim()
                                .ToUpperInvariant()
                        })
                        .Select(g => g.Last())
                        .Select(x => new TbParametroCosteo
                        {
                            PcPrId = checked((byte)x.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = (x.strTipoLote ?? string.Empty)
                                .Trim()
                                .ToUpperInvariant(),
                            PcLibras = Math.Round(x.dcLibras, 5),
                            PcCotoUnitario = Math.Round(x.dcCostUnitario ?? 0m, 5),
                            PcMonto = Math.Round(x.dcValor, 5),
                            PcEstado = "CE",
                            PcUsuarioCrea = strUsuarioLimpio,
                            PcFechaCrea = dtAhora,
                            PcEquipoCrea = strEquipo
                        })
                        .ToList();

                    foreach (WarrenProcesoDto objWarrenDetalle in
                        objWarren.lstDetalle.Where(x => x.intCodigo > 0))
                    {
                        lstNuevosParametros.Add(new TbParametroCosteo
                        {
                            PcPrId = checked((byte)objWarrenDetalle.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = "WEN",
                            PcLibras = Math.Round(objWarrenDetalle.dcLibrasEntero, 5),
                            PcCotoUnitario = Math.Round(
                                objWarrenDetalle.dcCostoUnitarioEnteroWarren,
                                5),
                            PcMonto = Math.Round(objWarrenDetalle.dcMontoEnteroWarren, 5),
                            PcEstado = "CE",
                            PcUsuarioCrea = strUsuarioLimpio,
                            PcFechaCrea = dtAhora,
                            PcEquipoCrea = strEquipo
                        });

                        lstNuevosParametros.Add(new TbParametroCosteo
                        {
                            PcPrId = checked((byte)objWarrenDetalle.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = "WSH",
                            PcLibras = Math.Round(objWarrenDetalle.dcLibrasCola, 5),
                            PcCotoUnitario = Math.Round(
                                objWarrenDetalle.dcCostoUnitarioColaWarren,
                                5),
                            PcMonto = Math.Round(objWarrenDetalle.dcMontoColaWarren, 5),
                            PcEstado = "CE",
                            PcUsuarioCrea = strUsuarioLimpio,
                            PcFechaCrea = dtAhora,
                            PcEquipoCrea = strEquipo
                        });
                    }

                    WarrenProcesoDto objCabecera = objWarren.lstDetalle
                        .First(x => x.intCodigo > 0);

                    lstNuevosParametros.Add(new TbParametroCosteo
                    {
                        PcPrId = checked((byte)objCabecera.intCodigo),
                        PcFecha = dtFechaCorte,
                        PcTipoLote = "WOB",
                        PcLibras = Math.Round(objWarren.dcLibrasCola, 5),
                        PcCotoUnitario = Math.Round(objWarren.dcCostoColaActual, 5),
                        PcMonto = Math.Round(objWarren.dcObjetivoWarren, 5),
                        PcEstado = "CE",
                        PcUsuarioCrea = strUsuarioLimpio,
                        PcFechaCrea = dtAhora,
                        PcEquipoCrea = strEquipo
                    });

                    // BLOQUE TEMPORAL DE DEBUG (DESCABEZADO CU CONSOLIDADO) — mantener comentado.
                    //var lstDebugParametrosDes = lstNuevosParametros.Where(x => x.PcPrId == 4)
                    //    .Select(x => new { x.PcPrId, x.PcTipoLote, x.PcLibras, x.PcCotoUnitario, x.PcMonto }).ToList();

                    await objContext.TbParametroCosteo.AddRangeAsync(
                        lstNuevosParametros);

                    // UN SOLO SaveChanges dentro de la transacción.
                    int intAfectados = await objContext.SaveChangesAsync();

                    _objLogger.LogInformation(
                        "Cierre ParamProc guardado desde snapshot. Año={Anio}, Mes={Mes}, " +
                        "Distribución={Distribucion}, Parámetros={Parametros}, Afectados={Afectados}",
                        intAnio,
                        intMes,
                        lstNuevos.Count,
                        lstNuevosParametros.Count,
                        intAfectados);

                    return intAfectados > 0;
                });
        }

        private static string NormalizarParticionSnapshot(string? strParticion)
        {
            string strValor = (strParticion ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            return strValor switch
            {
                "EN" => "EN",
                "CO" => "CO",
                "SH" => "CO",
                "VA" => "VA",
                "EV" => "VA",
                "SV" => "VA",
                _ => string.Empty
            };
        }
    }
}
