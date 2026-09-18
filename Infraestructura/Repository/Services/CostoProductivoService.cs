using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
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
                        AgregarParametro(cmd,"@anio",DbType.Int32,intAnio);

                        await using DbDataReader rd = await cmd.ExecuteReaderAsync();
                        return rd.MapToList<SaldoCuentaSongDto>();
                    },180);
            }
            catch(Exception ex)
            {
                ManejoLog<CostoProductivoService>.Error(
                    _objLogger,nameof(CostoProductivoService),
                    nameof(ConsultarSaldosCuentaSong),ex);
                throw;
            }
        }

        public async Task<bool> GuardarDistribucionProcesoProductivo(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            List<CostoProductivoCuentaDto> lstRegistros,
            string strUsuario)
        {
            try
            {
                if (lstRegistros == null || !lstRegistros.Any())
                    throw new ArgumentException(
                        "No existen registros para guardar.");

                if (string.IsNullOrWhiteSpace(strUsuario))
                    throw new ArgumentException(
                        "El usuario es obligatorio.");

                /*
                 * ========================================================
                 * VALIDACIÓN AGRUPACIÓN / GRUPO
                 * ========================================================
                 */
                var lstInvalidos = lstRegistros
                    .Where(x =>
                        string.IsNullOrWhiteSpace(
                            x.strAgrupacionCentro)
                        ||
                        string.IsNullOrWhiteSpace(
                            x.strGrupoCentro))
                    .Select(x => new
                    {
                        x.strCuenta,
                        x.strEtapa,
                        x.strAgrupacionCentro,
                        x.strGrupoCentro
                    })
                    .Take(20)
                    .ToList();

                if (lstInvalidos.Any())
                {
                    string detalle = string.Join(
                        " | ",
                        lstInvalidos.Select(x =>
                            $"{x.strCuenta}/{x.strEtapa}"));

                    throw new InvalidOperationException(
                        "Existen registros sin AGRUPACION o GRUPO: " +
                        detalle);
                }

                /*
                 * ========================================================
                 * GRANO REAL DE LA TABLA
                 * ========================================================
                 *
                 * La tabla NO posee dp_ctaNumero ni dp_pcCodigo.
                 *
                 * Por eso consolidamos por:
                 *
                 * Etapa
                 * Centro
                 * Subcentro
                 * Agrupación
                 * Grupo
                 * Naturaleza
                 */
                var lstAgrupados = lstRegistros
                    .GroupBy(x => new
                    {
                        Ec =
                            (x.strEcCodigo ?? string.Empty)
                            .Trim(),

                        Cen =
                            (x.strCentroCodigo ?? string.Empty)
                            .Trim(),

                        Sub =
                            (x.strSubcentroCodigo ?? string.Empty)
                            .Trim(),

                        Agrupacion =
                            (x.strAgrupacionCentro ?? string.Empty)
                            .Trim(),

                        Grupo =
                            (x.strGrupoCentro ?? string.Empty)
                            .Trim(),

                        Natura =
                            string.IsNullOrWhiteSpace(
                                x.strNaturaleza)
                                ? "D"
                                : x.strNaturaleza
                                    .Trim()
                                    .ToUpperInvariant()
                    })
                    .Select(g => new
                    {
                        g.Key.Ec,
                        g.Key.Cen,
                        g.Key.Sub,
                        g.Key.Agrupacion,
                        g.Key.Grupo,
                        g.Key.Natura,

                        /*
                         * IMPORTANTE:
                         *
                         * dcMontoEnteroPersistencia contempla también
                         * CDF/CDV/CIF/CIV cuando físicamente deben
                         * almacenarse en dp_montoEntero.
                         */
                        MontoEntero =
                            Math.Round(
                                g.Sum(x =>
                                    x.dcMontoEnteroPersistencia),
                                4),

                        MontoCola =
                            Math.Round(
                                g.Sum(x =>
                                    x.dcMontoCola),
                                4),

                        MontoVag =
                            Math.Round(
                                g.Sum(x =>
                                    x.dcMontoVag),
                                4)
                    })
                    .Where(x =>
                        x.MontoEntero != 0m
                        ||
                        x.MontoCola != 0m
                        ||
                        x.MontoVag != 0m)
                    .ToList();

                if (!lstAgrupados.Any())
                {
                    throw new InvalidOperationException(
                        "No existen montos distintos de cero para guardar.");
                }

                return await ManejoContext<CostosDbContext>
                    .EjecutarEnTransaccionAsync(
                        _objCostosFactory,
                        async objContext =>
                        {
                            DateTime ahora =
                                DateTime.Now;

                            string equipo =
                                Environment.MachineName;

                            /*
                             * ====================================================
                             * 1. ANULAR CIERRE ACTIVO ANTERIOR
                             * ====================================================
                             */
                            List<TbDistribucionProcesoProductivo>
                                lstExistentes =
                                    await objContext
                                        .TbDistribucionProcesoProductivo
                                        .AsTracking()
                                        .Where(x =>
                                            x.DpAnio == intAnio
                                            &&
                                            x.DpMes == intMes
                                            &&
                                            x.DpEstado == "AC")
                                        .ToListAsync();

                            foreach (
                                TbDistribucionProcesoProductivo registro
                                in lstExistentes)
                            {
                                registro.DpEstado =
                                    "AN";

                                registro.DpUsuarioEli =
                                    strUsuario;

                                registro.DpFechaEli =
                                    ahora;

                                registro.DpEquipoEli =
                                    equipo;
                            }

                            if (lstExistentes.Any())
                            {
                                await objContext
                                    .SaveChangesAsync();
                            }

                            /*
                             * ====================================================
                             * 2. CREAR NUEVO SNAPSHOT
                             * ====================================================
                             */
                            List<TbDistribucionProcesoProductivo>
                                lstNuevos =
                                    lstAgrupados
                                        .Select(x =>
                                            new TbDistribucionProcesoProductivo
                                            {
                                                DpAnio =
                                                    checked((short)intAnio),

                                                DpMes =
                                                    checked((short)intMes),

                                                DpFechaCorte =
                                                    dtFechaCorte,

                                                DpEcCodigo =
                                                    x.Ec,

                                                DpCenCodigo =
                                                    x.Cen,

                                                DpSubCodigo =
                                                    x.Sub,

                                                DpMontoEntero =
                                                    x.MontoEntero,

                                                DpMontoCola =
                                                    x.MontoCola,

                                                DpMontoVag =
                                                    x.MontoVag,

                                                DpEstado =
                                                    "AC",

                                                /*
                                                 * AQUÍ GUARDAMOS REALMENTE
                                                 * LAS COLUMNAS DEL EXCEL.
                                                 */
                                                DpAgrupacionCentro =
                                                    x.Agrupacion,

                                                DpCtaNatura =
                                                    x.Natura,

                                                DpGrupoCentro =
                                                    x.Grupo,

                                                DpUsuarioCrea =
                                                    strUsuario,

                                                DpFechaCrea =
                                                    ahora,

                                                DpEquipoCrea =
                                                    equipo
                                            })
                                        .ToList();

                            await objContext
                                .TbDistribucionProcesoProductivo
                                .AddRangeAsync(lstNuevos);

                            int intAfectados =
                                await objContext
                                    .SaveChangesAsync();

                            _objLogger.LogInformation(
                                "Costo productivo guardado por EF. " +
                                "Año: {Anio}, Mes: {Mes}, " +
                                "Filas anuladas: {Anuladas}, " +
                                "Filas nuevas: {Nuevas}, " +
                                "SaveChanges: {Afectados}",
                                intAnio,
                                intMes,
                                lstExistentes.Count,
                                lstNuevos.Count,
                                intAfectados);

                            return intAfectados > 0;
                        });
            }
            catch (Exception ex)
            {
                ManejoLog<CostoProductivoService>.Error(
                    _objLogger,
                    nameof(CostoProductivoService),
                    nameof(
                        GuardarDistribucionProcesoProductivo),
                    ex);

                throw;
            }
        }

        private static void AgregarParametro(
            DbCommand cmd,string nombre,DbType tipo,object? valor)
        {
            DbParameter p=cmd.CreateParameter();
            p.ParameterName=nombre;
            p.DbType=tipo;
            p.Value=valor ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
