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
using Microsoft.EntityFrameworkCore.Storage;

namespace CostManagement.Infraestructura.Repository.Services
{
    public sealed class CostoProductivoService : ICostoProductivoService
    {
        private readonly IDbContextFactory<CostosDbContext> _objCostosFactory;
        private readonly IDbContextFactory<SongDbContext> _objSongFactory;
        private readonly ILogger<CostoProductivoService> _objLogger;

        #region Configuracion SP JSON
        private readonly IConfiguration? _objConfiguracion;
        #endregion

        public CostoProductivoService(
            ILogger<CostoProductivoService> objLogger,
            IDbContextFactory<CostosDbContext> objCostosFactory,
            IDbContextFactory<SongDbContext> objSongFactory,
            IConfiguration? objConfiguracion = null)
        {
            _objConfiguracion = objConfiguracion;
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


        // Consulta por lotes en SONG; no modifica cuentas, factores ni importes.
        public async Task<Dictionary<string, string>> ConsultarNombresCentroAsync(IEnumerable<string> codigos)
        {
            List<string> lstCodigos = codigos.Select(x => (x ?? string.Empty).Trim()).Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (lstCodigos.Count == 0) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<TbMaecta> lstMaecta = await ManejoContext<SongDbContext>.EjecutarAsync(_objSongFactory,
                async ctx => await ctx.TbMaecta.AsNoTracking().Select(x => new TbMaecta
                {
                    CtaNumero = x.CtaNumero, CtaDeslar = x.CtaDeslar, CtaDescor = x.CtaDescor
                }).WhereInBatchAsync(x => x.CtaNumero, lstCodigos));
            return lstMaecta.GroupBy(x => x.CtaNumero.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key,
                g => (!string.IsNullOrWhiteSpace(g.First().CtaDeslar) ? g.First().CtaDeslar : (g.First().CtaDescor ?? string.Empty)).Trim(),
                StringComparer.OrdinalIgnoreCase);
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
                            .Where(x => x.DpAnio == intAnio && x.DpMes == intMes && x.DpEstado == "AC" && x.DpEcCodigo != "DM")
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
                // Anterior: return await ManejoContext<CostosDbContext>
                var lstDetalle = await ManejoContext<CostosDbContext>
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
                // completar nombres también en los modales consultados desde el cierre.
                var dicNombres = await ConsultarNombresCentroAsync(lstDetalle.Select(x => x.strCentroCodigo).Concat(lstDetalle.Select(x => x.strSubcentroCodigo)));
                foreach (var objFila in lstDetalle)
                {
                    objFila.strCentroNombre = dicNombres.GetValueOrDefault((objFila.strCentroCodigo ?? string.Empty).Trim(), string.Empty);
                    objFila.strSubcentroNombre = dicNombres.GetValueOrDefault((objFila.strSubcentroCodigo ?? string.Empty).Trim(), string.Empty);
                }
                return lstDetalle;
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
                        .Where(x => x.DpAnio == intAnio && x.DpMes == intMes && x.DpEstado == "AC" && x.DpEcCodigo != "DM")
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
                              // Antes: (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH"); ahora también se reemplaza el monto WAD.
                              (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH" || x.PcTipoLote == "WAD")) ||
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
                        // WAD conserva el monto firmado; libras y unitario son informativos.
                        nuevosParametros.Add(new TbParametroCosteo
                        {
                            PcPrId = checked((byte)d.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = "WAD",
                            PcLibras = Math.Round(d.dcLibrasCola, 5),
                            PcCotoUnitario = Math.Round(d.dcUnitarioAgregadoCola, 5),
                            PcMonto = Math.Round(d.dcAjusteWarren, 5),
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
        // Lectura única de la matriz completa; validar antes del filtro runtime.
        public async Task PrepararConfiguracionCierreAsync(CierreValorizacionContexto ctx)
        {
            await ManejoContext<CostosDbContext>.EjecutarAsync(_objCostosFactory, async objContext =>
            {
                var lstProcesos = await objContext.TbProcesocosto.AsNoTracking()
                    .OrderBy(x => x.PcOrden).Select(x => new { x.PcCodigo, x.PcNombre, x.PcEcCodigo, x.PcOrden, x.PcEstado }).ToListAsync();
                var lstTipos = await objContext.TbTipoOtrosProcesos.AsNoTracking().Where(x => x.ToEstado == "AC")
                    .Select(x => new { x.ToCodigo, x.ToNombre, x.ToNivel }).ToListAsync();
                var lstMatriz = await objContext.TbConfigTipoLiquidacionProcesoCosto.AsNoTracking().Where(x => x.CgEstado == "AC")
                    .Select(x => new { x.CgPcCodigo, x.CgToCodigo, x.CgConfig }).ToListAsync();
                string Normalizar(string? strValor) => (strValor ?? string.Empty).Trim().ToUpperInvariant();
                var lstGrupos = lstMatriz.GroupBy(x => (Pc: Normalizar(x.CgPcCodigo), To: Normalizar(x.CgToCodigo))).ToList();
                var lstConflictos = lstGrupos.Where(g => g.Select(x => Normalizar(x.CgConfig)).Distinct().Count() > 1)
                    .Select(g => $"({g.Key.Pc},{g.Key.To})").ToList();
                if (lstConflictos.Count > 0)
                    throw new InvalidOperationException("Matriz con cg_config distinto en parejas duplicadas: " + string.Join(", ", lstConflictos));
                foreach (var objGrupo in lstGrupos.Where(g => g.Count() > 1))
                    ctx.lstAdvertencias.Add($"Matriz duplicada idéntica ({objGrupo.Key.Pc},{objGrupo.Key.To}): se usa Last.");
                ctx.dicEtapas = lstProcesos.ToDictionary(x => Normalizar(x.PcCodigo), x => Normalizar(x.PcEcCodigo), StringComparer.OrdinalIgnoreCase);
                var dicProcesos = lstProcesos.Where(x => x.PcEstado == "AC").ToDictionary(x => Normalizar(x.PcCodigo));
                var dicTipos = lstTipos.ToDictionary(x => Normalizar(x.ToCodigo));
                ctx.objConfiguracion = new ConfiguracionCostoProductivoRuntimeDto
                {
                    lstProcesos = dicProcesos.Values.Select(x => new ProcesoCostoMantenimientoDto
                    {
                        strCodigo = Normalizar(x.PcCodigo), strNombre = (x.PcNombre ?? string.Empty).Trim(),
                        strEtapaCodigo = Normalizar(x.PcEcCodigo), intOrden = x.PcOrden, blActivo = true,
                        blEnMatriz = lstGrupos.Any(g => g.Key.Pc == Normalizar(x.PcCodigo))
                    }).ToList(),
                    lstNiveles = lstTipos.Select(x => new TipoProcesoNivelRuntimeDto { strCodigo = Normalizar(x.ToCodigo), intNivel = x.ToNivel }).ToList(),
                    lstAplicaciones = lstGrupos.Select(g => g.Last())
                        .Where(x => dicProcesos.ContainsKey(Normalizar(x.CgPcCodigo)) && dicTipos.ContainsKey(Normalizar(x.CgToCodigo)))
                        .Select(x => new ProcesoCostoAplicacionDto
                        {
                            strPcCodigo = Normalizar(x.CgPcCodigo), strPcNombre = (dicProcesos[Normalizar(x.CgPcCodigo)].PcNombre ?? string.Empty).Trim(),
                            strTipoProcesoCodigo = Normalizar(x.CgToCodigo), strTipoProceso = (dicTipos[Normalizar(x.CgToCodigo)].ToNombre ?? string.Empty).Trim(),
                            strConfig = Normalizar(x.CgConfig), dcTarifa = null
                        }).ToList()
                };
                return true;
            });
        }

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
                    // Adquirir el lock antes de leer cualquier fila previa del período.
                    string strRecurso = $"COSTOS_CIERRE_{intAnio}{intMes:00}";
                    await objContext.Database.ExecuteSqlRawAsync(
                        "DECLARE @intResultado int; EXEC @intResultado = sys.sp_getapplock @Resource={0}, " +
                        "@LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; " +
                        "IF @intResultado < 0 BEGIN RAISERROR(N'Ya hay un cierre en curso para el período', 16, 1); RETURN; END;",
                        strRecurso);
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
                            x.DpEstado == "AC" && x.DpEcCodigo != "DM")
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
                              // Antes: (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH"); ahora también se reemplaza el monto WAD.
                              (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH" || x.PcTipoLote == "WAD")) ||
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
                        // WAD conserva el monto firmado; libras y unitario son informativos.
                        lstNuevosParametros.Add(new TbParametroCosteo
                        {
                            PcPrId = checked((byte)objWarrenDetalle.intCodigo),
                            PcFecha = dtFechaCorte,
                            PcTipoLote = "WAD",
                            PcLibras = Math.Round(objWarrenDetalle.dcLibrasCola, 5),
                            PcCotoUnitario = Math.Round(
                                objWarrenDetalle.dcUnitarioAgregadoCola,
                                5),
                            PcMonto = Math.Round(objWarrenDetalle.dcAjusteWarren, 5),
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

        // Precondición del paso 2, también comprobada bajo el lock de escritura.
        public async Task<string> ValidarParametrosCierreAsync(int intAnio, int intMes)
        {
            return await ManejoContext<CostosDbContext>.EjecutarAsync(_objCostosFactory,
                objContext => ValidarParametrosCierreAsync(objContext, intAnio, intMes));
        }

        private static async Task<string> ValidarParametrosCierreAsync(CostosDbContext objContext, int intAnio, int intMes)
        {
            DateOnly dtCorte = new(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));
            var lstParametros = await objContext.TbParametroCosteo.AsNoTracking().Where(x => x.PcFecha == dtCorte && x.PcEstado == "CE")
                .OrderBy(x => x.PcId).Select(x => x.PcId).ToListAsync();
            var lstDistribucion = await objContext.TbDistribucionProcesoProductivo.AsNoTracking()
                .Where(x => x.DpAnio == intAnio && x.DpMes == intMes && x.DpEstado == "AC").OrderBy(x => x.DpId).Select(x => x.DpId).ToListAsync();
            if (lstParametros.Count == 0 || lstDistribucion.Count == 0)
                throw new InvalidOperationException("Primero debe guardarse el cierre de parámetros");
            // El paso 1 reemplaza parámetros e inserta distribución: sus IDs identifican esa generación sin cambiar el esquema.
            return string.Join(",", lstParametros) + "|" + string.Join(",", lstDistribucion);
        }

        #region Guardado y medicion del cierre
        // todas las cabeceras AC; cada mes confirma o revierte independientemente.
        public async Task<AnularValorizacionesResultadoDto> AnularValorizacionesActivasAsync(string tipo, string usuario, int? anio = null, int? mes = null)
        {
            if (tipo != "FRESCO" && tipo != "REPRO") throw new ArgumentException("El tipo debe ser FRESCO o REPRO.");
            if (string.IsNullOrWhiteSpace(usuario)) throw new ArgumentException("El usuario es obligatorio.");
            usuario = usuario.Trim();
            if (usuario.Length > 60 || Environment.MachineName.Length > 60)
                throw new ArgumentException("Usuario y equipo del cierre admiten hasta 60 caracteres.");
            var objOpciones = new CierreJsonOpciones(_objConfiguracion);
            if (objOpciones.intBloque <= 0 || objOpciones.intTimeout <= 0)
                throw new InvalidOperationException("Cierre: bloque y CommandTimeout deben ser positivos.");
            var resultado = new AnularValorizacionesResultadoDto { strTipo = tipo };
            var cabeceras = await ManejoContext<CostosDbContext>.EjecutarAsync(_objCostosFactory, async ctx =>
                tipo == "FRESCO"
                    ? await ctx.TbMateriaPrimaFrescoValorizada.AsNoTracking().Where(x => x.MfEstado == "AC")
                        .Select(x => new { Id = x.MfId, Fecha = x.MfFecha }).ToListAsync()
                    : await ctx.TbMateriaPrimaReproValorizada.AsNoTracking().Where(x => x.MrEstado == "AC")
                        .Select(x => new { Id = x.MrId, Fecha = x.MrFecha }).ToListAsync());
            // Filtrar las cabeceras leidas antes de agrupar por periodo.
            if (anio.HasValue && mes.HasValue)
                cabeceras = cabeceras.Where(x => x.Fecha.Year == anio.Value && x.Fecha.Month == mes.Value).ToList();
            DateTime dtAhora = DateTime.Now;
            foreach (var grupo in cabeceras.GroupBy(x => new { Anio = x.Fecha.Year, Mes = x.Fecha.Month })
                .OrderBy(x => x.Key.Anio).ThenBy(x => x.Key.Mes))
            {
                try
                {
                    var resumen = await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(_objCostosFactory, async objContext =>
                    {
                        // Mismo recurso y protocolo de applock que GuardarValorizacionCierreAtomico.
                        string strRecurso = $"COSTOS_CIERRE_{grupo.Key.Anio}{grupo.Key.Mes:00}";
                        await objContext.Database.ExecuteSqlRawAsync(
                            "DECLARE @intResultado int; EXEC @intResultado = sys.sp_getapplock @Resource={0}, " +
                            "@LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; " +
                            "IF @intResultado < 0 BEGIN RAISERROR(N'Ya hay un cierre en curso para el período', 16, 1); RETURN; END;",
                            strRecurso);
                        var filas = grupo.OrderBy(x => x.Id).Select((x, i) => new CierreJsonFila
                        {
                            idFilaCliente = i + 1, id = x.Id, anular = true
                            // CierreJsonFila serializa cabecera={} y detalles=[] para las bajas.
                        }).ToList();
                        return await CierrePersistenciaJson.EjecutarBloquesAsync(filas, objOpciones.intBloque, async lstBloque =>
                        {
                            await using var objComando = objContext.Database.GetDbConnection().CreateCommand();
                            objComando.Transaction = objContext.Database.CurrentTransaction?.GetDbTransaction()
                                ?? throw new InvalidOperationException("SP JSON requiere la transacción del cierre.");
                            objComando.CommandType = CommandType.StoredProcedure;
                            objComando.CommandText = "costos.sp_guardarValorizacionCierre";
                            objComando.CommandTimeout = objOpciones.intTimeout;
                            Agregar("@tipo", DbType.AnsiString, tipo, 10);
                            Agregar("@json", DbType.String, CierrePersistenciaJson.Serializar(lstBloque), -1);
                            Agregar("@usuario", DbType.AnsiString, usuario, 60);
                            Agregar("@equipo", DbType.AnsiString, Environment.MachineName, 60);
                            Agregar("@anio", DbType.Int32, grupo.Key.Anio);
                            Agregar("@mes", DbType.Int32, grupo.Key.Mes);
                            Agregar("@ahora", DbType.DateTime, dtAhora);
                            try
                            {
                                await using var objLector = await objComando.ExecuteReaderAsync();
                                if (!await objLector.ReadAsync()) throw new InvalidOperationException("SP JSON no devolvió resumen.");
                                var objResumen = new CierreJsonResumen(objLector.GetInt32(0), objLector.GetInt32(1), objLector.GetInt32(2), objLector.GetInt32(3), objLector.GetInt32(4));
                                if (await objLector.ReadAsync() || !await objLector.NextResultAsync())
                                    throw new InvalidOperationException("Contrato de resumen SP JSON inválido.");
                                var lstMapa = new List<CierreJsonMapa>();
                                while (await objLector.ReadAsync())
                                    lstMapa.Add(new(objLector.GetInt32(0), objLector.GetInt32(1), objLector.IsDBNull(2) ? null : objLector.GetDecimal(2), objLector.GetBoolean(3)));
                                return (objResumen, lstMapa);
                            }
                            catch (Microsoft.Data.SqlClient.SqlException objError) when (objError.Number == 2812)
                            { throw CierrePersistenciaJson.ErrorSql(objError.Number, objError); }
        
                            void Agregar(string strNombre, DbType enmTipo, object objValor, int? intTamano = null)
                            {
                                var objParametro = objComando.CreateParameter();
                                objParametro.ParameterName = strNombre; objParametro.DbType = enmTipo; objParametro.Value = objValor;
                                if (intTamano.HasValue) objParametro.Size = intTamano.Value;
                                objComando.Parameters.Add(objParametro);
                            }
                        });
                    }, intTimeout: objOpciones.intTimeout);
                    resultado.lstMeses.Add(new AnularValorizacionesMesDto
                    {
                        intAnio = grupo.Key.Anio, intMes = grupo.Key.Mes,
                        intCabeceras = resumen.intAnulados, intDetalles = resumen.intDetallesAnulados
                    });
                    _objLogger.LogInformation("Anulación confirmada Tipo={Tipo} Anio={Anio} Mes={Mes} Cabeceras={Cabeceras} Detalles={Detalles} Usuario={Usuario}",
                        tipo, grupo.Key.Anio, grupo.Key.Mes, resumen.intAnulados, resumen.intDetallesAnulados, usuario);
                }
                catch (Exception ex)
                {
                    _objLogger.LogError(ex, "Anulación fallida Tipo={Tipo} Anio={Anio} Mes={Mes} CabecerasSolicitadas={Cabeceras} Usuario={Usuario}",
                        tipo, grupo.Key.Anio, grupo.Key.Mes, grupo.Count(), usuario);
                    throw new InvalidOperationException($"Error al anular valorizaciones del mes {grupo.Key.Anio}-{grupo.Key.Mes:00}: {ex.Message}", ex);
                }
            }
            return resultado;
        }

        public async Task GuardarValorizacionCierreAtomico(int intAnio, int intMes, string strUsuario, CierreValorizacionDto objValorizacion)
        {
            // incluye apertura, commit y liberacion de la transaccion.
            // using var objTiempoTotal = MedicionTiempos.IniciarMetodo(_objLogger, "GuardarValorizacionCierreAtomico", () => objValorizacion.lstFresco.Count + objValorizacion.lstRepro.Count);
            ArgumentNullException.ThrowIfNull(objValorizacion);
            if (string.IsNullOrWhiteSpace(strUsuario)) throw new ArgumentException("El usuario es obligatorio.");
            DateOnly dtInicio = new(intAnio, intMes, 1);
            DateOnly dtFin = new(intAnio, intMes, DateTime.DaysInMonth(intAnio, intMes));
            if (objValorizacion.lstFresco.Any(x => x.objCabecera.MfFecha < dtInicio || x.objCabecera.MfFecha > dtFin || !int.TryParse(x.objCabecera.MfProCodcor, out _)) ||
                objValorizacion.lstRepro.Any(x => x.objCabecera.MrFecha < dtInicio || x.objCabecera.MrFecha > dtFin || !int.TryParse(x.objCabecera.MrProCodcor, out _)))
                throw new InvalidOperationException("La valorización contiene fechas ajenas al cierre o productos no numéricos.");
            await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(_objCostosFactory, async objContext =>
            {
                using var objTiempoLock = MedicionTiempos.IniciarTramo("applock");
                string strRecurso = $"COSTOS_CIERRE_{intAnio}{intMes:00}";
                await objContext.Database.ExecuteSqlRawAsync(
                    "DECLARE @intResultado int; EXEC @intResultado = sys.sp_getapplock @Resource={0}, " +
                    "@LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; " +
                    "IF @intResultado < 0 BEGIN RAISERROR(N'Ya hay un cierre en curso para el período', 16, 1); RETURN; END;",
                    strRecurso);
                objTiempoLock.Dispose();
                using var objTiempoVersion = MedicionTiempos.IniciarTramo("revalidar-version-parametros");
                // await ValidarParametrosCierreAsync(objContext, intAnio, intMes); // Antes: solo existencia.
                string strVersionActual = await ValidarParametrosCierreAsync(objContext, intAnio, intMes);
                if (!string.Equals(strVersionActual, objValorizacion.strVersionParametros, StringComparison.Ordinal))
                    throw new InvalidOperationException("Los parámetros del cierre cambiaron durante la valorización. Reintente la valorización.");
                objTiempoVersion.Dispose();
                #region Seleccion persistencia
                // respaldo EF conservado; no hay fallback automatico ante errores.
                int intAfectados;
                if (new CierreJsonOpciones(_objConfiguracion).blUsarSp)
                    intAfectados = await GuardarValorizacionJsonAsync(objContext, objValorizacion, dtInicio, dtFin, strUsuario.Trim());
                else
                {
                    await PrepararValorizacionCierreAsync(objContext, objValorizacion, dtInicio, dtFin, strUsuario.Trim(), DateTime.Now, Environment.MachineName);
                    using var objTiempoTracker = MedicionTiempos.IniciarTramo("DetectChanges-conteos-tablas");
                    objContext.ChangeTracker.DetectChanges();
                    var lstEntradas = objContext.ChangeTracker.Entries()
                        .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified).ToList();
                    int intEsperados = lstEntradas.Count;
                    // Un recorrido para contar Added/Modified por tabla antes de guardar.
                    var lstCambios = lstEntradas.GroupBy(x => new { Tabla = x.Metadata.GetTableName(), x.State })
                        .Select(g => $"{g.Key.Tabla}:{g.Key.State}={g.Count()}").ToList();
                    string strFilas = string.Join(",", lstCambios);
                    objTiempoTracker.Dispose();
                    // MedicionTiempos.RegistrarDetalle($"SaveChanges previstos por tabla=[{strFilas}]");
                    using var objTiempoSave = MedicionTiempos.IniciarTramo("SaveChanges", () => intEsperados);
                    intAfectados = await objContext.SaveChangesAsync();
                    objTiempoSave.Dispose();
                    // MedicionTiempos.RegistrarDetalle($"SaveChanges tablas=[{strFilas}] esperados={intEsperados} afectados={intAfectados}");
                    _objLogger.LogInformation("[TIEMPOS] Tramo=SaveChanges-filas Tablas={Tablas} Esperados={Esperados} Afectados={Afectados}", strFilas, intEsperados, intAfectados);
                    if (intAfectados != intEsperados)
                        throw new InvalidOperationException($"El cierre esperaba {intEsperados} cambios y guardó {intAfectados}.");
                }
                #endregion
                await VerificarValorizacionCierreAsync(objContext, objValorizacion, dtInicio, dtFin);
                objValorizacion.objResumen.strEstado = "AC";
                objValorizacion.objResumen.blWarrenGuardado = true;
                var lstAdvertencias = objValorizacion.lstAdvertencias.Distinct().ToList();
                objValorizacion.objResumen.lstAdvertencias = lstAdvertencias.Take(20).ToList();
                if (lstAdvertencias.Count > 20)
                    objValorizacion.objResumen.lstAdvertencias.Add($"… y {lstAdvertencias.Count - 20} advertencias más");
                _objLogger.LogInformation("Valorización de cierre guardada. Año={Anio}, Mes={Mes}, Fresco={Fresco}, Repro={Repro}, Afectados={Afectados}",
                    intAnio, intMes, objValorizacion.objResumen.intFrescoGuardados, objValorizacion.objResumen.intReproGuardados, intAfectados);
            });
        }

        #region Persistencia SP JSON
        // misma conexión/transacción y applock del cierre; sin SaveChanges.
        private async Task<int> GuardarValorizacionJsonAsync(CostosDbContext objContext, CierreValorizacionDto objValorizacion,
            DateOnly dtInicio, DateOnly dtFin, string strUsuario)
        {
            // using var objTiempoTotal = MedicionTiempos.IniciarTramo("SP-JSON-total");
            var objOpciones = new CierreJsonOpciones(_objConfiguracion);
            objContext.Database.SetCommandTimeout(objOpciones.intTimeout);
            if (strUsuario.Length > 60 || Environment.MachineName.Length > 60)
                throw new InvalidOperationException("Usuario y equipo del cierre admiten hasta 60 caracteres.");
            var lstTarifas = objValorizacion.lstRepro.SelectMany(x => x.lstDetalles)
                .Where(x => CierreDetalleDto.EsTarifa(x.strPcCodigo, x.strToCodigo)).ToList();
            if (lstTarifas.Count > 0)
            {
                var lstProcesos = await objContext.TbProcesocosto.AsNoTracking().SelectManyBatchAsync(x => x.PcCodigo,
                    lstTarifas.Select(x => x.strPcCodigo).Distinct().ToList(), objQuery => objQuery);
                ValidarCatalogoTarifasCierre(lstTarifas, lstProcesos);
            }
            using var objTiempoLectura = MedicionTiempos.IniciarTramo("SP-JSON-cabeceras-conteos");
            var lstFresco = await objContext.TbMateriaPrimaFrescoValorizada.AsNoTracking()
                .Where(x => x.MfFecha >= dtInicio && x.MfFecha <= dtFin && x.MfEstado != "AN").OrderBy(x => x.MfId).ToListAsync();
            var lstRepro = await objContext.TbMateriaPrimaReproValorizada.AsNoTracking()
                .Where(x => x.MrFecha >= dtInicio && x.MrFecha <= dtFin && x.MrEstado != "AN").OrderBy(x => x.MrId).ToListAsync();
            // Solo escalares: nunca materializar detalles ni motivos en el cliente.
            int intDetallesFresco = await objContext.TbMateriaPrimaFrescoValorizadaDetalle.CountAsync(x => x.MfdEstado == "AC" &&
                x.MfdMf.MfEstado != "AN" && x.MfdMf.MfFecha >= dtInicio && x.MfdMf.MfFecha <= dtFin);
            int intDetallesRepro = await objContext.TbMateriaPrimaReproValorizadaDetalle.CountAsync(x => x.MrdEstado == "AC" &&
                x.MrdMr.MrEstado != "AN" && x.MrdMr.MrFecha >= dtInicio && x.MrdMr.MrFecha <= dtFin);
            objTiempoLectura.Dispose();
            var lstFilasFresco = CierrePersistenciaJson.Emparejar(objValorizacion.lstFresco, lstFresco, x => x.objCabecera,
                x => (x.MfRloNumero, x.MfProCodcor, x.MfTalCodigo), x => x.MfId, x => x.lstDetalles,
                (objLinea, intId, dcUnitario, blManual) =>
                {
                    objLinea.objCabecera.MfId = intId;
                    if (blManual)
                    {
                        objLinea.objCabecera.MfCostoUnitario = dcUnitario!.Value;
                        objValorizacion.lstAdvertencias.Add($"Fresco cabecera {intId}: se conserva el costo unitario manual {dcUnitario.Value:N5}; no participa en conciliación de unitario.");
                    }
                });
            var lstFilasRepro = CierrePersistenciaJson.Emparejar(objValorizacion.lstRepro, lstRepro, x => x.objCabecera,
                x => (x.MrLotNumero, x.MrRloNumeroUnificado, x.MrProCodcor, x.MrTalCodigo), x => x.MrId, x => x.lstDetalles,
                (objLinea, intId, dcUnitario, blManual) => objLinea.objCabecera.MrId = intId);
            DateTime dtAhora = DateTime.Now;
            var objFresco = await EjecutarTipoAsync("FRESCO", lstFilasFresco);
            var objRepro = await EjecutarTipoAsync("REPRO", lstFilasRepro);
            if (objFresco.intDetallesAnulados != intDetallesFresco || objRepro.intDetallesAnulados != intDetallesRepro)
                throw new InvalidOperationException("No concilia el total de detalles anulados SP JSON con la lectura inicial.");
            objValorizacion.objResumen = new CierreValorizacionResumenDto
            {
                intFrescoGuardados = objValorizacion.lstFresco.Count, intFrescoNuevos = objFresco.intInsertados,
                intFrescoActualizados = objFresco.intActualizados, intFrescoAnulados = objFresco.intAnulados,
                intFrescoDetalles = objFresco.intDetallesInsertados,
                intReproGuardados = objValorizacion.lstRepro.Count, intReproNuevos = objRepro.intInsertados,
                intReproActualizados = objRepro.intActualizados, intReproAnulados = objRepro.intAnulados,
                intReproDetalles = objRepro.intDetallesInsertados,
                intDetallesAnulados = objFresco.intDetallesAnulados + objRepro.intDetallesAnulados
            };
            return objFresco.intInsertados + objFresco.intActualizados + objFresco.intAnulados + objFresco.intDetallesInsertados +
                objRepro.intInsertados + objRepro.intActualizados + objRepro.intAnulados + objRepro.intDetallesInsertados +
                objValorizacion.objResumen.intDetallesAnulados;

            async Task<CierreJsonResumen> EjecutarTipoAsync(string strTipo, List<CierreJsonFila> lstFilas)
            {
                int intBloque = 0;
                return await CierrePersistenciaJson.EjecutarBloquesAsync(lstFilas, objOpciones.intBloque, async lstBloque =>
                {
                    // using var objTiempoBloque = MedicionTiempos.IniciarTramo($"SP-JSON-{strTipo}-bloque-{++intBloque}", () => lstBloque.Length);
                    await using var objComando = objContext.Database.GetDbConnection().CreateCommand();
                    objComando.Transaction = objContext.Database.CurrentTransaction?.GetDbTransaction()
                        ?? throw new InvalidOperationException("SP JSON requiere la transacción del cierre.");
                    objComando.CommandType = CommandType.StoredProcedure;
                    objComando.CommandText = "costos.sp_guardarValorizacionCierre";
                    objComando.CommandTimeout = objOpciones.intTimeout;
                    Agregar("@tipo", DbType.AnsiString, strTipo, 10);
                    Agregar("@json", DbType.String, CierrePersistenciaJson.Serializar(lstBloque), -1);
                    Agregar("@usuario", DbType.AnsiString, strUsuario, 60);
                    Agregar("@equipo", DbType.AnsiString, Environment.MachineName, 60);
                    Agregar("@anio", DbType.Int32, dtInicio.Year);
                    Agregar("@mes", DbType.Int32, dtInicio.Month);
                    Agregar("@ahora", DbType.DateTime, dtAhora);
                    try
                    {
                        await using var objLector = await objComando.ExecuteReaderAsync();
                        if (!await objLector.ReadAsync()) throw new InvalidOperationException("SP JSON no devolvió resumen.");
                        var objResumen = new CierreJsonResumen(objLector.GetInt32(0), objLector.GetInt32(1), objLector.GetInt32(2), objLector.GetInt32(3), objLector.GetInt32(4));
                        if (await objLector.ReadAsync() || !await objLector.NextResultAsync())
                            throw new InvalidOperationException("Contrato de resumen SP JSON inválido.");
                        var lstMapa = new List<CierreJsonMapa>();
                        while (await objLector.ReadAsync())
                            lstMapa.Add(new(objLector.GetInt32(0), objLector.GetInt32(1), objLector.IsDBNull(2) ? null : objLector.GetDecimal(2), objLector.GetBoolean(3)));
                        // MedicionTiempos.RegistrarDetalle($"SP JSON {strTipo} bloque={intBloque} cabeceras={lstBloque.Length} detalles={objResumen.intDetallesInsertados} anulados={objResumen.intDetallesAnulados}");
                        return (objResumen, lstMapa);
                    }
                    catch (Microsoft.Data.SqlClient.SqlException objError) when (objError.Number == 2812)
                    { throw CierrePersistenciaJson.ErrorSql(objError.Number, objError); }

                    void Agregar(string strNombre, DbType enmTipo, object objValor, int? intTamano = null)
                    {
                        var objParametro = objComando.CreateParameter();
                        objParametro.ParameterName = strNombre; objParametro.DbType = enmTipo; objParametro.Value = objValor;
                        if (intTamano.HasValue) objParametro.Size = intTamano.Value;
                        objComando.Parameters.Add(objParametro);
                    }
                });
            }
        }
        #endregion

        private static async Task PrepararValorizacionCierreAsync(CostosDbContext objContext, CierreValorizacionDto objValorizacion,
            DateOnly dtInicio, DateOnly dtFin, string strUsuario, DateTime dtAhora, string strEquipo)
        {
            // using var objTiempoPreparar = MedicionTiempos.IniciarTramo("PrepararValorizacionCierreAsync");
            using var objTiempoCatalogo = MedicionTiempos.IniciarTramo("lectura-validacion-catalogo-tarifas");
            // Revalidar dentro de la transacción antes de anular o agregar cualquier fila.
            var lstTarifas = objValorizacion.lstRepro.SelectMany(x => x.lstDetalles)
                .Where(x => CierreDetalleDto.EsTarifa(x.strPcCodigo, x.strToCodigo)).ToList();
            if (lstTarifas.Count > 0)
            {
                var lstCodigos = lstTarifas.Select(x => x.strPcCodigo).Distinct().ToList();
                var lstProcesos = await objContext.TbProcesocosto.AsNoTracking()
                    .SelectManyBatchAsync(x => x.PcCodigo, lstCodigos, objQuery => objQuery);
                ValidarCatalogoTarifasCierre(lstTarifas, lstProcesos);
            }
            objTiempoCatalogo.Dispose();
            var objResumen = new CierreValorizacionResumenDto();
            objValorizacion.objResumen = objResumen;
            using var tiempolstFresco = MedicionTiempos.IniciarTramo("lectura-cabeceras-fresco");
            var lstFresco = await objContext.TbMateriaPrimaFrescoValorizada.AsTracking()
                .Where(x => x.MfFecha >= dtInicio && x.MfFecha <= dtFin && x.MfEstado != "AN").OrderBy(x => x.MfId).ToListAsync();
            tiempolstFresco.intFilas = lstFresco.Count;
            tiempolstFresco.Dispose();
            using var tiempolstRepro = MedicionTiempos.IniciarTramo("lectura-cabeceras-repro");
            var lstRepro = await objContext.TbMateriaPrimaReproValorizada.AsTracking()
                .Where(x => x.MrFecha >= dtInicio && x.MrFecha <= dtFin && x.MrEstado != "AN").OrderBy(x => x.MrId).ToListAsync();
            tiempolstRepro.intFilas = lstRepro.Count;
            tiempolstRepro.Dispose();
            var lstIdsFresco = lstFresco.Select(x => x.MfId).ToList();
            var lstIdsRepro = lstRepro.Select(x => x.MrId).ToList();
            using var tiempolstDetallesFresco = MedicionTiempos.IniciarTramo("lectura-detalles-fresco");
            var lstDetallesFresco = await objContext.TbMateriaPrimaFrescoValorizadaDetalle.AsTracking()
                .Where(x => x.MfdEstado == "AC").SelectManyBatchAsync(x => x.MfdMfId, lstIdsFresco, objQuery => objQuery);
            tiempolstDetallesFresco.intFilas = lstDetallesFresco.Count;
            tiempolstDetallesFresco.Dispose();
            using var tiempolstDetallesRepro = MedicionTiempos.IniciarTramo("lectura-detalles-repro");
            var lstDetallesRepro = await objContext.TbMateriaPrimaReproValorizadaDetalle.AsTracking()
                .Where(x => x.MrdEstado == "AC").SelectManyBatchAsync(x => x.MrdMrId, lstIdsRepro, objQuery => objQuery);
            tiempolstDetallesRepro.intFilas = lstDetallesRepro.Count;
            tiempolstDetallesRepro.Dispose();
            using var tiempolstManuales = MedicionTiempos.IniciarTramo("lectura-motivos-manuales");
            var lstManuales = await objContext.TbMateriaPrimaFrescoValorizadaMotivoCambios.AsNoTracking()
                .Where(x => x.McEstado == "AC").SelectManyBatchAsync(x => x.McMfId, lstIdsFresco, objQuery => objQuery.Select(x => x.McMfId));
            tiempolstManuales.intFilas = lstManuales.Count;
            tiempolstManuales.Dispose();
            var hshManuales = lstManuales.ToHashSet();
            using var objTiempoAnular = MedicionTiempos.IniciarTramo("anular-detalles");
            // Anular también los detalles de las cabeceras que ya no aparecen en el snapshot.
            foreach (var objDetalle in lstDetallesFresco)
            {
                objDetalle.MfdEstado = "AN"; objDetalle.MfdUsuarioMod = strUsuario;
                objDetalle.MfdFechaMod = dtAhora; objDetalle.MfdEquipoMod = strEquipo;
            }
            foreach (var objDetalle in lstDetallesRepro)
            {
                objDetalle.MrdEstado = "AN"; objDetalle.MrdUsuarioMod = strUsuario;
                objDetalle.MrdFechaMod = dtAhora; objDetalle.MrdEquipoMod = strEquipo;
            }
            objResumen.intDetallesAnulados = lstDetallesFresco.Count + lstDetallesRepro.Count;
            objTiempoAnular.Dispose();
            using var objTiempoIndices = MedicionTiempos.IniciarTramo("indices-clave-ordinal");
            var dicFresco = lstFresco.GroupBy(x => (x.MfRloNumero, x.MfProCodcor, x.MfTalCodigo))
                .ToDictionary(g => g.Key, g => new Queue<TbMateriaPrimaFrescoValorizada>(g.OrderBy(x => x.MfId)));
            var dicRepro = lstRepro.GroupBy(x => (x.MrLotNumero, x.MrRloNumeroUnificado, x.MrProCodcor, x.MrTalCodigo))
                .ToDictionary(g => g.Key, g => new Queue<TbMateriaPrimaReproValorizada>(g.OrderBy(x => x.MrId)));
            objTiempoIndices.Dispose();
            // using var objTiempoEntidades = MedicionTiempos.IniciarTramo("crear-actualizar-entidades-cabeceras-detalles");
            foreach (var objLinea in objValorizacion.lstFresco)
            {
                using var objTiempoPareja = MedicionTiempos.IniciarAcumulado("emparejamiento-clave-ordinal");
                var objNueva = objLinea.objCabecera;
                var objClave = (objNueva.MfRloNumero, objNueva.MfProCodcor, objNueva.MfTalCodigo);
                bool blExistente = dicFresco.TryGetValue(objClave, out var lstCoincidentes) && lstCoincidentes.Count > 0;
                var objCabecera = blExistente ? lstCoincidentes.Dequeue() : objNueva;
                objTiempoPareja.Dispose();
                if (blExistente)
                {
                    if (hshManuales.Contains(objCabecera.MfId))
                        objValorizacion.lstAdvertencias.Add($"Fresco cabecera {objCabecera.MfId}: se conserva el costo unitario manual {objCabecera.MfCostoUnitario:N5}; no participa en conciliación de unitario.");
                    else objCabecera.MfCostoUnitario = objNueva.MfCostoUnitario;
                    objCabecera.MfFecha = objNueva.MfFecha; objCabecera.MfBodCodigo = objNueva.MfBodCodigo;
                    objCabecera.MfMedCodigo = objNueva.MfMedCodigo; objCabecera.MfEmbCodigo = objNueva.MfEmbCodigo;
                    objCabecera.MfMasters = objNueva.MfMasters; objCabecera.MfLibras = objNueva.MfLibras;
                    objCabecera.MfCostoTotal = objNueva.MfCostoTotal;
                    objCabecera.MfCostoUnitarioWarren = objNueva.MfCostoUnitarioWarren; objCabecera.MfCostoTotalWarren = objNueva.MfCostoTotalWarren;
                    objCabecera.MfEstado = "AC"; objCabecera.MfUsuarioMod = strUsuario;
                    objCabecera.MfFechaMod = dtAhora; objCabecera.MfEquipoMod = strEquipo;
                    objResumen.intFrescoActualizados++;
                }
                else
                {
                    objCabecera.MfEstado = "AC"; objCabecera.MfUsuarioCrea = strUsuario;
                    objCabecera.MfFechaCrea = dtAhora; objCabecera.MfEquipoCrea = strEquipo;
                    objContext.TbMateriaPrimaFrescoValorizada.Add(objCabecera);
                    objResumen.intFrescoNuevos++;
                }
                foreach (var objDetalle in objLinea.lstDetalles)
                {
                    var objNuevo = new TbMateriaPrimaFrescoValorizadaDetalle
                    {
                        MfdEtapaCodigo = objDetalle.strEtapaCodigo, MfdParticion = objDetalle.strParticion,
                        MfdPcCodigo = objDetalle.strPcCodigo, MfdToCodigo = objDetalle.strToCodigo, MfdLibras = objDetalle.dcLibras,
                        MfdCostoUnitario = objDetalle.dcCostoUnitario, MfdCostoTotal = objDetalle.dcCostoTotal,
                        MfdEstado = "AC", MfdUsuarioCrea = strUsuario, MfdFechaCrea = dtAhora, MfdEquipoCrea = strEquipo
                    };
                    if (blExistente)
                    {
                        objNuevo.MfdMfId = objCabecera.MfId;
                        objContext.TbMateriaPrimaFrescoValorizadaDetalle.Add(objNuevo);
                    }
                    else objCabecera.TbMateriaPrimaFrescoValorizadaDetalle.Add(objNuevo);
                    objResumen.intFrescoDetalles++;
                }
                objLinea.objCabecera = objCabecera;
                objResumen.intFrescoGuardados++;
            }
            foreach (var objCabecera in dicFresco.Values.SelectMany(x => x).Where(x => x.MfEstado == "AC"))
            {
                objCabecera.MfEstado = "AN"; objCabecera.MfUsuarioEli = strUsuario;
                objCabecera.MfFechaEli = dtAhora; objCabecera.MfEquipoEli = strEquipo;
                objResumen.intFrescoAnulados++;
            }
            foreach (var objLinea in objValorizacion.lstRepro)
            {
                using var objTiempoPareja = MedicionTiempos.IniciarAcumulado("emparejamiento-clave-ordinal");
                var objNueva = objLinea.objCabecera;
                var objClave = (objNueva.MrLotNumero, objNueva.MrRloNumeroUnificado, objNueva.MrProCodcor, objNueva.MrTalCodigo);
                bool blExistente = dicRepro.TryGetValue(objClave, out var lstCoincidentes) && lstCoincidentes.Count > 0;
                var objCabecera = blExistente ? lstCoincidentes.Dequeue() : objNueva;
                objTiempoPareja.Dispose();
                if (blExistente)
                {
                    objCabecera.MrFecha = objNueva.MrFecha; objCabecera.MrBodCodigo = objNueva.MrBodCodigo;
                    objCabecera.MrMedCodigo = objNueva.MrMedCodigo; objCabecera.MrEmbCodigo = objNueva.MrEmbCodigo;
                    objCabecera.MrMasters = objNueva.MrMasters; objCabecera.MrLibras = objNueva.MrLibras;
                    objCabecera.MrCostoUnitario = objNueva.MrCostoUnitario; objCabecera.MrCostoTotal = objNueva.MrCostoTotal;
                    objCabecera.MrEstado = "AC"; objCabecera.MrUsuarioMod = strUsuario;
                    objCabecera.MrFechaMod = dtAhora; objCabecera.MrEquipoMod = strEquipo;
                    objResumen.intReproActualizados++;
                }
                else
                {
                    objCabecera.MrEstado = "AC"; objCabecera.MrUsuarioCrea = strUsuario;
                    objCabecera.MrFechaCrea = dtAhora; objCabecera.MrEquipoCrea = strEquipo;
                    objContext.TbMateriaPrimaReproValorizada.Add(objCabecera);
                    objResumen.intReproNuevos++;
                }
                foreach (var objDetalle in objLinea.lstDetalles)
                {
                    var objNuevo = new TbMateriaPrimaReproValorizadaDetalle
                    {
                        MrdEtapaCodigo = objDetalle.strEtapaCodigo, MrdParticion = objDetalle.strParticion,
                        MrdPcCodigo = objDetalle.strPcCodigo, MrdToCodigo = objDetalle.strToCodigo, MrdLibras = objDetalle.dcLibras,
                        MrdCostoUnitario = objDetalle.dcCostoUnitario, MrdCostoTotal = objDetalle.dcCostoTotal,
                        MrdEstado = "AC", MrdUsuarioCrea = strUsuario, MrdFechaCrea = dtAhora, MrdEquipoCrea = strEquipo
                    };
                    if (blExistente)
                    {
                        objNuevo.MrdMrId = objCabecera.MrId;
                        objContext.TbMateriaPrimaReproValorizadaDetalle.Add(objNuevo);
                    }
                    else objCabecera.TbMateriaPrimaReproValorizadaDetalle.Add(objNuevo);
                    objResumen.intReproDetalles++;
                }
                objLinea.objCabecera = objCabecera;
                objResumen.intReproGuardados++;
            }
            foreach (var objCabecera in dicRepro.Values.SelectMany(x => x).Where(x => x.MrEstado == "AC"))
            {
                objCabecera.MrEstado = "AN"; objCabecera.MrUsuarioEli = strUsuario;
                objCabecera.MrFechaEli = dtAhora; objCabecera.MrEquipoEli = strEquipo;
                objResumen.intReproAnulados++;
            }
        }

        #region Validación de tarifa de cierre
        // importe adicional con código propio o TAR; no exige pareja en la matriz.
        // Revalidar todos los destinos antes de preparar cambios dentro de la transacción.
        internal static void ValidarCatalogoTarifasCierre(List<CierreDetalleDto> lstTarifas, List<TbProcesocosto> lstProcesos)
        {
            var dicProcesos = lstProcesos.ToDictionary(x => CierreDetalleDto.NormalizarCodigo(x.PcCodigo));
            var lstFaltantes = lstTarifas.Select(x => CierreDetalleDto.NormalizarCodigo(x.strPcCodigo))
                .Distinct().Where(x => !dicProcesos.ContainsKey(x)).OrderBy(x => x).ToList();
            if (lstFaltantes.Count > 0)
                throw new InvalidOperationException($"Códigos faltantes en costos.tb_procesocosto: {string.Join(", ", lstFaltantes)}. " +
                    "Registre esos códigos en costos.tb_procesocosto o ejecute 05_insert_proceso_tarifa.sql y reintente el cierre.");
            foreach (var objGrupo in lstTarifas.GroupBy(x => CierreDetalleDto.NormalizarCodigo(x.strPcCodigo)))
                ValidarTarifaDetalleCierre(objGrupo.ToList(), dicProcesos[objGrupo.Key]);
        }

        internal static void ValidarTarifaDetalleCierre(List<CierreDetalleDto> lstTarifas, TbProcesocosto? objProceso)
        {
            if (lstTarifas.Count == 0) return;
            if (objProceso == null)
                throw new InvalidOperationException($"Códigos faltantes en costos.tb_procesocosto: {string.Join(", ", lstTarifas.Select(x => x.strPcCodigo).Distinct())}. " +
                    "Registre esos códigos en costos.tb_procesocosto o ejecute 05_insert_proceso_tarifa.sql.");
            if (string.IsNullOrWhiteSpace(objProceso.PcEcCodigo))
                throw new InvalidOperationException($"El proceso {objProceso.PcCodigo} no tiene pc_ecCodigo en el catálogo.");
            if (lstTarifas.Any(x => x.strEtapaCodigo != objProceso.PcEcCodigo.Trim().ToUpperInvariant()))
                throw new InvalidOperationException($"La etapa pc_ecCodigo de {objProceso.PcCodigo} cambió durante la valorización. Reintente el cierre.");
        }
        #endregion

        private static async Task VerificarValorizacionCierreAsync(CostosDbContext objContext, CierreValorizacionDto objValorizacion, DateOnly dtInicio, DateOnly dtFin)
        {
            // Verificacion posterior al SaveChanges, antes del commit.
            // using var objTiempoVerificar = MedicionTiempos.IniciarTramo("VerificarValorizacionCierreAsync");
            using var tiempointFresco = MedicionTiempos.IniciarTramo("verificar-cabeceras-fresco");
            int intFresco = await objContext.TbMateriaPrimaFrescoValorizada.AsNoTracking()
                .CountAsync(x => x.MfFecha >= dtInicio && x.MfFecha <= dtFin && x.MfEstado == "AC");
            tiempointFresco.intFilas = intFresco;
            tiempointFresco.Dispose();
            using var tiempointRepro = MedicionTiempos.IniciarTramo("verificar-cabeceras-repro");
            int intRepro = await objContext.TbMateriaPrimaReproValorizada.AsNoTracking()
                .CountAsync(x => x.MrFecha >= dtInicio && x.MrFecha <= dtFin && x.MrEstado == "AC");
            tiempointRepro.intFilas = intRepro;
            tiempointRepro.Dispose();
            using var tiempointDetallesFresco = MedicionTiempos.IniciarTramo("verificar-detalles-fresco");
            int intDetallesFresco = await objContext.TbMateriaPrimaFrescoValorizadaDetalle.AsNoTracking()
                .CountAsync(x => x.MfdMf.MfFecha >= dtInicio && x.MfdMf.MfFecha <= dtFin && x.MfdEstado == "AC");
            tiempointDetallesFresco.intFilas = intDetallesFresco;
            tiempointDetallesFresco.Dispose();
            using var tiempoDetallesRepro = MedicionTiempos.IniciarTramo("verificar-detalles-repro");
            int intDetallesRepro = await objContext.TbMateriaPrimaReproValorizadaDetalle.AsNoTracking()
                .CountAsync(x => x.MrdMr.MrFecha >= dtInicio && x.MrdMr.MrFecha <= dtFin && x.MrdEstado == "AC");
            tiempoDetallesRepro.intFilas = intDetallesRepro;
            tiempoDetallesRepro.Dispose();
            if (intFresco != objValorizacion.lstFresco.Count || intRepro != objValorizacion.lstRepro.Count ||
                intDetallesFresco != objValorizacion.lstFresco.Sum(x => x.lstDetalles.Count) ||
                intDetallesRepro != objValorizacion.lstRepro.Sum(x => x.lstDetalles.Count) ||
                objValorizacion.lstFresco.Any(x => x.objCabecera.MfId <= 0) || objValorizacion.lstRepro.Any(x => x.objCabecera.MrId <= 0))
                throw new InvalidOperationException("Los conteos persistidos de cabeceras y detalles no coinciden con la valorización del cierre.");
        }

        #endregion

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
