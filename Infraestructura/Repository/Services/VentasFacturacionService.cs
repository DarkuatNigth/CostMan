using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Repository.Services;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Dominio.Entidades;
using CostManagementService.Infraestructura.EF_Core.SONG;
using CostManagementService.Infraestructura.Repository.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace CostManagementService.Infraestructura.Repository.Services
{
    public class VentasFacturacionService : IVentasFacturacionService
    {

        private readonly IDbContextFactory<CostManagementDbContext> _objContextFactory;
        private readonly IDbContextFactory<SongDbContext> _objSongFactory;
        private readonly IDbContextFactory<CostosDbContext> _objCostosFactory;
        private readonly IProcesoParametro _objProcesoParametro;
        private readonly ILogger<VentasFacturacionService> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        public VentasFacturacionService(
            ILogger<VentasFacturacionService> objLogger,
            IProcesoParametro objProcesoParametro,
            IOptions<ParametrosConfig> objConfig,
            IDbContextFactory<CostManagementDbContext> objContextFactory,
            IDbContextFactory<CostosDbContext> objCostosFactory,
            IDbContextFactory<SongDbContext> objSongFactory)
        {
            _objLogger = objLogger;
            _objContextFactory = objContextFactory;
            _objCostosFactory = objCostosFactory;
            _objSongFactory = objSongFactory;
            _objProcesoParametro = objProcesoParametro;
            _objConfig = objConfig;
        }

        // =====================================================================
        // rep_tracamAuto → TracamAutoResult
        // Parámetros: @fein char(10), @fefi char(10)
        // =====================================================================
        public async Task<List<TracamAutoResult>> ObtenerSpTracamAutoXRangoFecha(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                return await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        var objParamInicio = new SqlParameter("@fein", SqlDbType.Char, 10)
                        { Value = dtFechaInicio.ToString("yyyy/MM/dd") };
                        var objParamFin = new SqlParameter("@fefi", SqlDbType.Char, 10)
                        { Value = dtFechaFin.ToString("yyyy/MM/dd") };

                        using var cmd = objContext.Database.GetDbConnection().CreateCommand();
                        cmd.CommandText = "EXEC rep_tracamAuto @fein, @fefi";
                        cmd.CommandTimeout = 180;
                        cmd.Parameters.Add(objParamInicio);
                        cmd.Parameters.Add(objParamFin);

                        if (cmd.Connection.State != ConnectionState.Open)
                            await cmd.Connection.OpenAsync();

                        using var reader = await cmd.ExecuteReaderAsync();
                        return DataReaderMapper.MapToList<TracamAutoResult>(reader);
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<VentasFacturacionService>.Error(_objLogger, nameof(VentasFacturacionService), nameof(ObtenerSpTracamAutoXRangoFecha), objException);
                throw;
            }
        }

        public async Task<List<TracamAutoResult>> ObtenerTracamAutoXRangoFecha(
           DateOnly dtFechaInicio,
           DateOnly dtFechaFin)
        {
            try
            {
                return await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        DateTime dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(00, 00));
                        DateTime dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));

                        return await objContext.Database
                            .SqlQueryRaw<TracamAutoResult>(
                                new ValueObjects().strRepTracamAuto,
                                new SqlParameter("@feini", dtFeInicio),
                                new SqlParameter("@fefin", dtFeFin)
                            )
                            .AsNoTracking()
                            .ToListAsync();
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<VentasFacturacionService>.Error(_objLogger, nameof(VentasFacturacionService), nameof(ObtenerTracamAutoXRangoFecha), objException);
                throw;
            }
        }


        // =====================================================================
        // SPE_repfactpesoreal → RepFactPesoRealResult
        // Parámetros: @desde varchar(10), @hasta varchar(10), @tipo varchar(1) = 'P'
        // =====================================================================
        public async Task<List<RepFactPesoRealResult>> ObtenerRepFactPesoRealXRangoFecha(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin,
            string strTipo = "P")
        {
            try
            {
                return await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        var objParamDesde = new SqlParameter("@desde", SqlDbType.VarChar, 10)
                        { Value = dtFechaInicio.ToString("yyyy/MM/dd") };
                        var objParamHasta = new SqlParameter("@hasta", SqlDbType.VarChar, 10)
                        { Value = dtFechaFin.ToString("yyyy/MM/dd") };
                        var objParamTipo = new SqlParameter("@tipo", SqlDbType.VarChar, 1)
                        { Value = strTipo };

                        using var cmd = objContext.Database.GetDbConnection().CreateCommand();
                        cmd.CommandText = "EXEC SPE_repfactpesoreal @desde, @hasta, @tipo";
                        cmd.CommandTimeout = 180;
                        cmd.Parameters.Add(objParamDesde);
                        cmd.Parameters.Add(objParamHasta);
                        cmd.Parameters.Add(objParamTipo);

                        if (cmd.Connection.State != ConnectionState.Open)
                            await cmd.Connection.OpenAsync();

                        using var reader = await cmd.ExecuteReaderAsync();
                        return DataReaderMapper.MapToList<RepFactPesoRealResult>(reader);
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<VentasFacturacionService>.Error(_objLogger, nameof(VentasFacturacionService), nameof(ObtenerRepFactPesoRealXRangoFecha), objException);
                throw;
            }
        }


        // =====================================================================
        // sp_factura → FacturaResult
        // Parámetros: @fecini char(10), @fecfin char(10), @mov varchar(5) = NULL
        // =====================================================================
        public async Task<List<FacturaResult>> ObtenerFacturasXRangoFecha(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin,
            string? strMov = null)
        {
            try
            {
                return await ManejoContext<SongDbContext>.EjecutarAsync(
                    _objSongFactory,
                    async objContext =>
                    {
                        var objParamInicio = new SqlParameter("@fecini", SqlDbType.Char, 10)
                        { Value = dtFechaInicio.ToString("yyyy/MM/dd") };
                        var objParamFin = new SqlParameter("@fecfin", SqlDbType.Char, 10)
                        { Value = dtFechaFin.ToString("yyyy/MM/dd") };

                        // @mov es opcional — si viene null se envía DBNull para que el SP lo trate como NULL
                        var objParamMov = new SqlParameter("@mov", SqlDbType.VarChar, 5)
                        { Value = strMov is null ? DBNull.Value : strMov };

                        using var cmd = objContext.Database.GetDbConnection().CreateCommand();
                        cmd.CommandText = "EXEC sp_factura @fecini, @fecfin, @mov";
                        cmd.CommandTimeout = 180;
                        cmd.Parameters.Add(objParamInicio);
                        cmd.Parameters.Add(objParamFin);
                        cmd.Parameters.Add(objParamMov);

                        if (cmd.Connection.State != ConnectionState.Open)
                            await cmd.Connection.OpenAsync();

                        using var reader = await cmd.ExecuteReaderAsync();
                        return DataReaderMapper.MapToList<FacturaResult>(reader);
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<VentasFacturacionService>.Error(_objLogger, nameof(VentasFacturacionService), nameof(ObtenerFacturasXRangoFecha), objException);
                throw;
            }
        }
    }
}
