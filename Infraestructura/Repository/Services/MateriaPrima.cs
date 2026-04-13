using CostManagement.API.Controllers;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Aplicación.DTos;
using CostManagementService.Infraestructura.EF_Core;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace CostManagement.Infraestructura.Repository.Services
{
    public class MateriaPrima : IMateriaPrima
    {
        private readonly CostManagementDbContext _objContext;
        private readonly IDbContextFactory<CostManagementDbContext> _objContextFactory;
        private readonly SongDbContext _objSongContext;
        private readonly IDbContextFactory<SongDbContext> _objSongFactory;
        private readonly CostosDbContext _objCostosContext;
        private readonly IDbContextFactory<CostosDbContext> _objCostosFactory;
        private readonly IProcesoParametro _objProcesoParametro;
        private readonly ILogger<MateriaPrima> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        public MateriaPrima(
            ILogger<MateriaPrima> objLogger,
            CostManagementDbContext objContext,
            IProcesoParametro objProcesoParametro,
            CostosDbContext objCostosContext,
            SongDbContext objSongContext,
            IOptions<ParametrosConfig> objConfig,
            IDbContextFactory<CostManagementDbContext> objContextFactory,
            IDbContextFactory<CostosDbContext> objCostosFactory,
            IDbContextFactory<SongDbContext> objSongFactory)
        {
            _objLogger = objLogger;
            _objContext = objContext;
            _objCostosContext = objCostosContext;
            _objSongContext = objSongContext;
            _objContextFactory = objContextFactory;
            _objCostosFactory = objCostosFactory;
            _objSongFactory = objSongFactory;
            _objProcesoParametro = objProcesoParametro;
            _objConfig = objConfig;
        }

        #region Busqueda MateriaPrima Valorizada
        public async Task<List<RptGrncLibras>> ObtenerProduccionXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<RptGrncLibras> lstLibrasProduccion;
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                DateTime dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0)); // 00:00:00
                DateTime dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));     // 23:59:59
                lstLibrasProduccion = await (
                    from rlo in objContext.TbReglot.AsNoTracking()
                    join gtr in objContext.TbGuitra.AsNoTracking() on rlo.RloGuitra equals gtr.GtrNumero
                    join vee in objContext.TbProvee.AsNoTracking() on gtr.GtrCodpro equals vee.ClpCodigo
                    join cla in objContext.TbClacli.AsNoTracking() on vee.ClpLispre equals cla.ClaCodigo
                    join rec in objContext.TbProcesosRecep.AsNoTracking() on rlo.RloProcesodest equals rec.PrrCodigo
                    where rlo.RloEstado != "AN" 
                    && new[] { "PTE", "PFR", "PMC" }.Contains(rlo.RloTipolote)
                    && rlo.RloFecha >= dtFeInicio
                    && rlo.RloFecha <= dtFeFin

                    group new {rlo, gtr} by new
                    {
                        rlo.RloProcesodest,
                        rlo.RloTipolote,
                        rec.PrrDescripcion
                    } into g
                    select new RptGrncLibras
                    {
                        dcRloEnviad= (decimal)g.Sum(obj => obj.rlo.RloEnviad),
                        dcRloNetas = (decimal)g.Sum(obj => obj.rlo.RloNetas),
                        dcRloProCab = g.Sum(obj => obj.rlo.RloProcab),
                        dcRloRomane = (decimal)g.Sum(obj => obj.rlo.RloRomane),
                        intRloProcesodest = (int)g.Key.RloProcesodest,
                        dcRloProCol = g.Sum(obj => obj.rlo.RloProcol),
                        strRloTipolote = g.Key.RloTipolote,
                        dcRloRecibi = (decimal)g.Sum(obj => obj.rlo.RloRecibi),
                        dcRloXProco = g.Sum(obj => obj.rlo.RloXproco),
                        strPrrDescripcion = g.Key.PrrDescripcion,
                    }
                    ).ToListAsync();
                return lstLibrasProduccion;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerProduccionXRangoFecha: {ObjException.Message}");
                throw;
            }
        }

        public async Task<List<LbsCongelamiento>> ObtenerMatPrimFrsCongeXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<LbsCongelamiento> lstFrsConge;
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                DateTime dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0)); // 00:00:00
                DateTime dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));     // 23:59:59
                lstFrsConge = await (
                    from rlo in objContext.TbReglot.AsNoTracking()
                    join liq in objContext.TbLiqtun.AsNoTracking() on new
                    { A = rlo.RloNumero, B = "AC" } equals new
                    { A = liq.LiqLote, B = liq.LiqEstado }
                    join lid in objContext.TbLitund.AsNoTracking() on new
                    { A = liq.LiqNumero, B = liq.LiqLote } equals new
                    { A = (decimal)lid.LidNumero, B = (decimal)lid.LidLote }
                    join pro in objContext.TbProduc.AsNoTracking() on lid.LidProduc equals pro.ProCodcor
                    join pp in objContext.TbProces.AsNoTracking() on pro.ProClas06 equals pp.ProCodigo
                    join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                    join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                    join bod in objContext.TbBodega.AsNoTracking() on liq.LiqTunpla equals bod.BodCodigo
                    join dpr in objContext.TbDetproces.AsNoTracking() on pp.ProCongel equals dpr.DprCodigo

                    where rlo.RloFecha >= dtFeInicio
                       && rlo.RloFecha <= dtFeFin
                       && dpr.DprGruDetProces == 1
                       && rlo.RloEstado == "CE" 
                       && !new[] { "PTE", "REP" }.Contains(rlo.RloTipolote)
                    group new { lid, emb, med } by new
                    {
                        pro.ProClas03,
                        pro.ProClas06,
                        liq.LiqTunpla,
                        bod.BodDescri,
                        bod.BodTipo,
                        bod.BodEsBrine,
                        bod.BodCateg,
                        bod.BodTipo2,
                        pp.ProCongel,
                        dpr.DprDescri
                    }into g
                    select new LbsCongelamiento
                    {
                        strProClas03 = g.Key.ProClas03,
                        strProClas06 = g.Key.ProClas06,
                        strLiqTunpla = g.Key.LiqTunpla,
                        strBodDescri = g.Key.BodDescri,
                        strBodTipo = g.Key.BodTipo,
                        blBodEsBrine = (bool)(g.Key.BodEsBrine != null ? g.Key.BodEsBrine : false),
                        strBodCateg = g.Key.BodCateg != null ? g.Key.BodCateg : string.Empty,
                        strBodTipo2 = g.Key.BodTipo2,
                        intProCongel = (int)(g.Key.ProCongel != null ? g.Key.ProCongel : 0),
                        strDprDescri = g.Key.DprDescri,
                        dcLibras = (decimal)g.Sum(x => ((double)x.lid.LidCanenv * x.emb.EmbPeso * x.med.MedFactor))
                    }
                ).ToListAsync();
                return lstFrsConge;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerMatPrimFrsCongeXRangoFecha: {ObjException.Message}");
                throw;
            }
        }

        public async Task<List<CopackingLbs>> ObtenerCopackingLbsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                var objParamInicio = new SqlParameter("@fecini", SqlDbType.Char, 10) { Value = dtFechaInicio.ToString("yyyy/MM/dd") };
                var objParamFin = new SqlParameter("@fecfin", SqlDbType.Char, 10) { Value = dtFechaFin.ToString("yyyy/MM/dd") };
                //List<CopackingLbsDto> lstCopackingLbs = await _objContext
                //                        .Set<CopackingLbsDto>()
                //                        .FromSqlRaw("EXEC rep_repgen32 @fecini, @fecfin", objParamInicio, objParamFin)
                //                        .AsNoTracking()
                //                        .ToListAsync();
                List<CopackingLbs> lstCopackingLbs = new List<CopackingLbs>();

                using var cmd = objContext.Database.GetDbConnection().CreateCommand();

                cmd.CommandText = "EXEC rep_repgen32 @fecini, @fecfin";
                cmd.CommandTimeout = 180;

                cmd.Parameters.Add(objParamInicio);
                cmd.Parameters.Add(objParamFin);

                if (cmd.Connection.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                lstCopackingLbs = DataReaderMapper.MapToList<CopackingLbs>(reader);
                return lstCopackingLbs;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerLbsServicioCopacking: {ObjException.Message}");
                throw;
            }
        }

        public async Task<List<ResumenEstiloLbsDto>> ObtenerResumenEstiloLbsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                var objParamInicio = new SqlParameter("@fein", SqlDbType.Char, 10) { Value = dtFechaInicio.ToString("yyyy/MM/dd") };
                var objParamFin = new SqlParameter("@fefi", SqlDbType.Char, 10) { Value = dtFechaFin.ToString("yyyy/MM/dd") };
                List<ResumenEstiloLbsDto> lstResumenEstiloLbs = await objContext
                                        .Set<ResumenEstiloLbsDto>()
                                        .FromSqlRaw("EXEC spr_resumenLibrasDecoradas @fein, @fefi", objParamInicio, objParamFin)
                                        .AsNoTracking()
                                        .ToListAsync();
                return lstResumenEstiloLbs;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerResumenEstiloLbsXRangoFecha: {ObjException.Message}");
                throw;
            }
        }

        public async Task<List<RptCongInd>> ObtenerTipProcXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<RptCongInd> lstResumenEstiloLbs = new List<RptCongInd>();
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                var objParamInicio = new SqlParameter("@fein", SqlDbType.Char, 10) { Value = dtFechaInicio.ToString("yyyy/MM/dd") };
                var objParamFin = new SqlParameter("@fefi", SqlDbType.Char, 10) { Value = dtFechaFin.ToString("yyyy/MM/dd") };
                //List<RptCongIndDto> lstResumenEstiloLbs = await _objContext
                //                        .Set<RptCongIndDto>()
                //                        .FromSqlRaw("EXEC rep_lotopconCongInd @fein, @fefi", objParamInicio, objParamFin)
                //                        .AsNoTracking()
                //                        .ToListAsync();


                using var cmd = objContext.Database.GetDbConnection().CreateCommand();

                cmd.CommandText = "EXEC rep_lotopconCongInd @fein, @fefi";
                cmd.CommandTimeout = 180;

                cmd.Parameters.Add(objParamInicio);
                cmd.Parameters.Add(objParamFin);

                if (cmd.Connection.State != ConnectionState.Open)
                    await cmd.Connection.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                lstResumenEstiloLbs = DataReaderMapper.MapToList<RptCongInd>(reader);
                return lstResumenEstiloLbs;

            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerTipProc: {ObjException.Message}");
                throw;
            }
        }


        public async Task<List<PrecioFrsXMov>> ObtenerConsumoMovOtroProc(List<long> lstLiqLote)
        {
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                var lstConsumoOtroProceso = await objContext.TbRelode.AsNoTracking()
                    .SelectManyBatchAsync(
                    keySelector: rlo => rlo.RldLote,
                    values: lstLiqLote,
                    selector: filtered =>
                            from rld in filtered
                            join otr in objContext.TbLototr.AsNoTracking() on new
                                { A= rld.RldNumero, B=rld.RldTipo } equals new
                                { A=otr.LotNumero, B=otr.LotTipo }
                            join lid in objContext.TbLiqvad.AsNoTracking() on new
                                { A = rld.RldNumero, B = rld.RldCodtal } equals new
                                { A = lid.LidNoliqu, B = (int)lid.LidCodtal }
                            join pro in objContext.TbProduc.AsNoTracking() on lid.LidCodigo equals pro.ProCodcor
                            join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                            join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                            join tal in objContext.TbTallas.AsNoTracking() on lid.LidCodtal equals tal.TalCodigo
                            where otr.LotEstado != "AN"
                            group new { rld,lid, med, emb } by new
                            {
                                otr.LotTipo,
                                rld.RldLote,
                                pro.ProClasePago,
                                pro.ProClas01,
                                pro.ProClas05,
                                tal.TalDescri,
                                pro.ProCodcor,
                                pro.ProDesesp,
                                rld.RldCodtal
                            } into g
                            select new PrecioFrsXMov
                            (
                                g.Key.RldLote,
                                g.Key.LotTipo,
                                g.Key.ProClasePago,
                                g.Key.ProClas01,
                                g.Key.ProClas05,
                                g.Key.TalDescri,
                                g.Key.ProCodcor,
                                g.Key.ProDesesp,
                                g.Key.RldCodtal,
                                g.Average(x => (double)x.lid.LidPrecio),
                                g.Sum(x => (double)x.rld.RldCantid * x.emb.EmbPeso * x.med.MedFactor)

                            )
                    );
                return lstConsumoOtroProceso;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerConsumoOtroProceso: {ObjException.Message}");
                throw;
            }
        }

        public async Task<List<PrecioFrsXMov>> ObtenerConsumoMovLiqOtroProc(List<long> lstLiqLote)
        {
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(360);
                var lstConsumoOtroProceso = await objContext.TbLitvad.AsNoTracking()// objContext.TbLiqvag.AsNoTracking()
                    .SelectManyBatchAsync(
                    keySelector: lit => lit.LidLote,
                    values: lstLiqLote,
                    selector: filtered =>
                            from lit in filtered
                            join liq in objContext.TbLiqvag.AsNoTracking() on lit.LidNumero equals liq.LiqNumero 
                            join lid in objContext.TbLiqvad.AsNoTracking() on new
                            { A = lit.LidLote, B = lit.LidCodtal } equals new
                            { A = (long)lid.LidNoliqu, B = (int)lid.LidCodtal }
                            join otr in objContext.TbLototr.AsNoTracking() on new
                            { A = liq.LiqLote, B = liq.LiqTipo } equals new
                            { A = otr.LotNumero, B = otr.LotTipo }
                            join pro in objContext.TbProduc.AsNoTracking() on lit.LidProduc equals pro.ProCodcor
                            join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                            join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                            join tal in objContext.TbTallas.AsNoTracking() on lid.LidCodtal equals tal.TalCodigo
                            where otr.LotEstado != "AN" && liq.LiqEstado != "AN"
                            group new { lit, lid, med, emb } by new
                            {
                                otr.LotTipo,
                                lit.LidLote,
                                pro.ProClasePago,
                                pro.ProClas01,
                                pro.ProClas05,
                                tal.TalDescri,
                                pro.ProCodcor,
                                pro.ProDesesp,
                                lid.LidCodtal
                            } into g
                            select new PrecioFrsXMov
                            (
                                g.Key.LidLote,
                                g.Key.LotTipo,
                                g.Key.ProClasePago,
                                g.Key.ProClas01,
                                g.Key.ProClas05,
                                g.Key.TalDescri,
                                g.Key.ProCodcor,
                                g.Key.ProDesesp,
                                g.Key.LidCodtal,
                                g.Average(x => (double)x.lid.LidPrecio),
                                g.Sum(x => (double)x.lit.LidCanenv * x.emb.EmbPeso * x.med.MedFactor)

                            )
                    );
                return lstConsumoOtroProceso;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerConsumoOtroProceso: {ObjException.Message}");
                throw;
            }
        }

        public async Task<List<LiquidacionResultado>> ObtenerMatPrimValFrsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin, bool blValorizada = true)
        {
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                using var transaction = await objContext.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadUncommitted
                    );

                DateTime dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0)); // 00:00:00
                DateTime dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));     // 23:59:59
                List<LiquidacionResultado> lstTotalResultados = await (
                     from rlo in objContext.TbReglot.AsNoTracking()
                     join liq in objContext.TbLiqtun.AsNoTracking()
                         on new
                         { A = rlo.RloNumero, B = "AC" } equals new
                         { A = liq.LiqLote, B = liq.LiqEstado }
                     join lid in objContext.TbLitund.AsNoTracking()
                         on new
                         { A = liq.LiqNumero, B = liq.LiqLote } equals new
                         { A = (decimal)lid.LidNumero, B = (decimal)lid.LidLote }
                     join pro in objContext.TbProduc.AsNoTracking() on lid.LidProduc equals pro.ProCodcor
                     join pai in objContext.TbPais.AsNoTracking() on pro.ProDestino equals pai.PaiCodigo
                     join col in objContext.TbColor on pro.ProClas05 equals col.ColCodigo
                     join med in objContext.TbMedida on pro.ProUnimed equals med.MedCodigo
                     join emb in objContext.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                     join b1 in objContext.TbBodega on liq.LiqTunpla equals b1.BodCodigo
                     join b2 in objContext.TbBodega on liq.LiqProprc equals b2.BodCodigo
                     join pl1 in objContext.TbPlantaProcOe on b1.BodPlantaProcesoOe equals pl1.PaCodigo
                     join pl2 in objContext.TbPlantaProcOe on b2.BodPlantaProcesoOe equals pl2.PaCodigo
                     join gtr in objContext.TbGuitra on rlo.RloGuitra equals gtr.GtrNumero
                     join prv in objContext.TbProvee on gtr.GtrCodpro equals prv.ClpCodigo
                     join gprv in objContext.TbGrupo on prv.ClpGrupo equals gprv.GrpCodigo
                     join sgprv in objContext.TbSubgrupo on prv.ClpGrupoProc equals sgprv.SgrCodigo
                     join tal in objContext.TbTallas on lid.LidCodtal equals tal.TalCodigo
                     join clas in objContext.TbClasificadora on lid.LidClasificadora equals clas.ClaAbrev into clasGroup
                     from clas in clasGroup.DefaultIfEmpty()
                     join pcab in objContext.TbLiqvalPremiosCab.AsNoTracking() on lid.LidLote equals pcab.LipNoliqu into pcabGroup
                     from pcab in pcabGroup.DefaultIfEmpty()
                     join pdet in objContext.TbLiqvalPremiosDet.AsNoTracking() on new
                     { A = pcab.LipId, B = lid.LidLote, C= lid.LidProduc, D = lid.LidCodtal } equals new 
                     { A = pdet.LidCabId, B = (long)pdet.LidNoliqu, C= pdet.LidProducto, D = pdet.LidTalla } into pdetGroup
                     from pdet in pdetGroup.DefaultIfEmpty()
                     join cert in objContext.VwCatalogo.AsNoTracking() on new
                     { A = "PREMIO",B = pcab.LipTipoPremio.ToString() } equals new { A = cert.CatCodigo, B = cert.DetCodigo } into certGroup
                     from cert in certGroup.DefaultIfEmpty()
                     join liva in objContext.TbLiqvad.AsNoTracking() on new
                     { A = rlo.RloNumero , B = (decimal)lid.LidCodtal, C = pro.ProClas02, D = lid.LidProduc } equals new
                     { A = liva.LidNoliqu, B = liva.LidCodtal, C = liva.LidClase, D = liva.LidCodigo } into livaGroup
                     from liva in livaGroup.DefaultIfEmpty()
                     join rec in objContext.TbProcesosRecep.AsNoTracking() on rlo.RloProcesodest equals rec.PrrCodigo into recGroup
                     from rec in recGroup.DefaultIfEmpty()
                     join tid in objContext.TbTipdec.AsNoTracking() on new
                     { A = pro.ProDecora, B = "AC" } equals new { A = tid.TidCodigo, B = tid.TidEstado }
                     join rta in objContext.TbRetalm.AsNoTracking() on new
                     { A = pro.ProRetrac, B = "AC" } equals new { A = rta.RtaCodigo, B = rta.RtaEstado }
                     //join dret in (
                     //from dret in objContext.TbDetalleRetractilado.AsNoTracking()
                     //join pro in objContext.TbProduc.AsNoTracking() on dret.CodProd equals pro.ProCodcor
                     //join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                     //join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                     //           where dret.Cajas > 0
                     //           group new { med, emb, dret } by new
                     //           {
                     //               dret.CodProd,
                     //               dret.Lote,
                     //               dret.CodTal,
                     //               dret.CajasRetra
                     //           } into d
                     //           select new
                     //           {
                     //               d.Key.CodProd,
                     //               d.Key.Lote,
                     //               d.Key.CodTal,
                     //               LbsCajasRetra = d.Sum(x => (decimal?)((double)(x.dret.CajasRetra ?? 0m) * x.emb.EmbPeso * x.med.MedFactor)) ?? 0m
                     //           }
                     //                   )
                     //                   on new { A = lid.LidLote.ToString(), B = lid.LidProduc, C = lid.LidCodtal }
                     //                   equals new { A = dret.Lote, B = dret.CodProd, C = (int)dret.CodTal } into dretGroup
                     //from dret in dretGroup.DefaultIfEmpty()
                     //join lot in objContext.TbLototr.AsNoTracking() on new
                     //{ A = rlo.RloNumero, B = rlo.RloTipolote } equals new
                     //{ A = lot.LotNumero, B = lot.LotTipo } into lotGroup
                     //from lot in lotGroup.DefaultIfEmpty()
                     where rlo.RloFecha >= dtFeInicio
                        && rlo.RloFecha <= dtFeFin
                        && rlo.RloEstado == "CE"
                        && !new[] { "PTE", "REP" }.Contains(rlo.RloTipolote)
                     group new { lid, emb, med, liq } by new
                     {
                         rlo.RloTipolote,
                         rlo.RloNumero,
                         rlo.RloFecha,
                         rlo.RloPiscin,
                         PlantaDesc = liq.LiqPppt == "PT" ? pl1.PaDescri : pl2.PaDescri,
                         Proveedor = prv.ClpNomcom,
                         TipPro = col.ColDescri,
                         FechaLiq = liq.LiqFecha.Date,
                         HoraLiq = liq.LiqFecha.Hour,
                         CodProd = lid.LidProduc,
                         Descripcion = pro.ProDesesp,
                         Talla = tal.TalDescri,
                         //Certificado = (Convert.ToInt32(pro.ProCertif) == 2 ||
                         //               Convert.ToInt32(pro.ProCertif) == 5) ? "ASC" :
                         //              (Convert.ToInt32(pro.ProCertif) > 2 ? "OTRO" : ""),
                         Certificado = pdet.LidPrecio != null ?
                            cert.DetDescripcion : "",
                         pdet.LidPrecio,
                         BodegaDesc = b1.BodDescri,
                         prv.ClpGrupo,
                         pai.PaiDescri,
                         pro.ProClas02,
                         pro.ProClasePago,
                         lid.LidClasificadora,
                         lid.LidCodtal,
                         clas.ClaDescripcion,
                         gprv.GrpDescri,
                         sgprv.SgrDescri,
                         pro.ProClas01,
                         pro.ProClas05,
                         PaDescri = "",//pl3.PaDescri,
                         pro.ProClas03,
                         pro.ProClas06,
                         pro.ProCongela,
                         b1.BodEsBrine,
                         b1.BodCodigo,
                         rta.RtaCodigo,
                         Decorado =  pro.ProClas03 == "PT" ? true : false,
                         tid.TidCodigo,
                         //dret.LbsCajasRetra,
                         PrecioComp = liva.LidPrecio,
                         rec.PrrIniciales,
                         med.MedCodigo,
                         emb.EmbCodigo
                     } into g
                     select new LiquidacionResultado(
                             "LIQ_" + g.Key.RloTipolote, // tipoLiq
                             g.Key.RloNumero,            // lote (se pasa como object)
                             g.Key.RloFecha!.Value.Month,// mes
                             g.Key.RloFecha,             // fechaLote (DateTime?)
                             g.Key.PlantaDesc,           // planta
                             g.Key.Proveedor,            // proveedor
                             g.Key.RloPiscin,            // piscina
                             g.Sum(x => (double)x.lid.LidCanenv / x.emb.EmbCantid), // masters
                             g.Sum(x => (double)x.lid.LidCanenv * x.emb.EmbPeso * x.med.MedFactor), // libras
                             g.Key.TipPro,               // tipPro
                             g.Key.FechaLiq,             // fechaLiq
                             g.Key.HoraLiq,             // horaLiq
                             g.Key.CodProd,              // codProd
                             g.Key.Descripcion,          // descripcion
                             g.Key.Talla,                // talla
                             g.Key.Certificado,          // certificado
                             g.Key.BodegaDesc,           // bodDescri
                             g.Key.ClpGrupo,             // clpGrupo
                             g.Key.PaiDescri,            // paiDescri
                             g.Key.ProClas02,            // proClas02
                             g.Key.ProClasePago,         // proClasePago
                             g.Key.LidClasificadora,     // clasificadora
                             g.Min(x => x.liq.LiqFecini),// inicioLiq
                             g.Max(x => x.liq.LiqFecfin), // finLiq
                             g.Key.LidCodtal,           // CodTal
                             g.Key.ClaDescripcion,
                             g.Key.GrpDescri,           // Grupo Descripcion
                             g.Key.SgrDescri,           // SubGrupo Descripcion
                             g.Key.ProClas01,           // Tipo producto
                             g.Key.ProClas05,           // Tipo
                             g.Key.PaDescri,            // Planta 2 
                             g.Key.ProClas03,
                             g.Key.ProClas06,
                             g.Key.ProCongela,
                             g.Key.BodEsBrine,
                             g.Key.LidPrecio,
                             g.Key.PrecioComp,
                             g.Key.PrrIniciales,
                             g.Key.RtaCodigo,
                             g.Key.TidCodigo,
                             //g.Key.LbsCajasRetra,
                             g.Key.BodCodigo,
                             g.Key.MedCodigo,
                             g.Key.EmbCodigo
                         )
                 ).ToListAsync();
                var lstLotes = lstTotalResultados.Select(res => res.intLote.ToString()).Distinct().ToList();
                var lstInfoRetracti = await objContext.TbDetalleRetractilado.AsNoTracking()
                    .SelectManyBatchAsync(
                    keySelector: dret => dret.Lote,
                    values: lstLotes,
                    selector: filtered =>
                        from dret in filtered
                        join pro in objContext.TbProduc.AsNoTracking() on dret.CodProd equals pro.ProCodcor
                        join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                        join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                        where dret.Cajas > 0
                        group new { med, emb, dret } by new
                        {
                            dret.CodProd,
                            dret.Lote,
                            dret.CodTal,
                            dret.CajasRetra,
                            emb.EmbPeso,
                            med.MedFactor
                        } into d
                        select new
                        {
                            d.Key.CodProd,
                            d.Key.Lote,
                            d.Key.CodTal,
                            d.Key.CajasRetra,
                            d.Key.EmbPeso,
                            d.Key.MedFactor
                        }
                    );
                // 1. Generamos el diccionario agrupado por la clave (Lote, Producto, Talla)
                var dictRetra = LiquidacionResultado.GenerarDiccionarioLbsRetra(lstTotalResultados);

                // 2. Iteramos sobre los datos obtenidos de la base de datos (Retractilado)
                foreach (var info in lstInfoRetracti.OrderByDescending(obj => obj.CajasRetra))
                {
                    int intLote = Convert.ToInt32(info.Lote);
                    int intCodProd = Convert.ToInt32(info.CodProd);
                    int intCodTal = Convert.ToInt32(info.CodTal);
                    var key = new LoteFrsKey(intLote, intCodProd, intCodTal);
                    // 3. Si la clave existe en nuestro diccionario de resultados
                    if (dictRetra.TryGetValue(key, out var listaADatosActualizar))
                    {
                        // Calculamos las libras de este registro de retractilado
                        // Fórmula: (CajasRetra * PesoEmbalaje) * FactorMedida
                        decimal lbsCalculadas = (decimal)((double)info.CajasRetra * info.EmbPeso  * info.MedFactor );

                        // 4. Llenamos las libras en TODOS los objetos que coincidan con esa clave
                        foreach (var item in listaADatosActualizar)
                        {
                            if ((item.dcLibrasRetractilado ?? 0) == 0)
                            {
                                item.dcLibrasRetractilado = lbsCalculadas;

                                //info.CajasRetra = 0;

                                break;
                            }
                        }
                    }


                }
                await transaction.CommitAsync();
                return lstTotalResultados;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerMateriaPrimaFresco: {ObjException.Message}");
                throw;
            }

        }

        public async Task<List<LiquidacionResultado>> ObtenerMatPrimValRpcsXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin, bool blValorizada = true)
        {
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                using var transaction = await objContext.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadUncommitted
                    );
                DateTime dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0)); // 00:00
                DateTime dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));     // 23:59

                List<LiquidacionResultado> lstTotalResultados = await (
                    from lotr in objContext.TbLototr.AsNoTracking()
                    join lqva in objContext.TbLiqvag.AsNoTracking() on new
                    { A = lotr.LotNumero, B = lotr.LotTipo, C = "AC" } equals new
                    { A = lqva.LiqLote, B = lqva.LiqTipo, C = lqva.LiqEstado }
                    join ltva in objContext.TbLitvad on lqva.LiqNumero equals ltva.LidNumero
                    join relo in objContext.TbReglot on ltva.LidLote equals relo.RloNumero
                    join guta in objContext.TbGuitra on relo.RloGuitra equals guta.GtrNumero
                    join pove in objContext.TbProvee on guta.GtrCodpro equals pove.ClpCodigo
                    join gprv in objContext.TbGrupo on pove.ClpGrupo equals gprv.GrpCodigo
                    join sgprv in objContext.TbSubgrupo on pove.ClpGrupoProc equals sgprv.SgrCodigo
                    join prod in objContext.TbProduc//.Where(prod => prod.ProClas03 == "PT")
                     on ltva.LidProduc equals prod.ProCodcor
                    join pais in objContext.TbPais on prod.ProDestino equals pais.PaiCodigo
                    join colo in objContext.TbColor on prod.ProClas05 equals colo.ColCodigo
                    join medi in objContext.TbMedida on prod.ProUnimed equals medi.MedCodigo
                    join emba in objContext.TbEmbala on prod.ProEmbala equals emba.EmbCodigo
                    join bode in objContext.TbBodega on lqva.LiqTunpla equals bode.BodCodigo
                    join pla1 in objContext.TbPlantaProcOe on bode.BodPlantaProcesoOe equals pla1.PaCodigo
                    join pla2 in objContext.TbPlantaProcOe on lqva.LiqPlanta.ToString() equals pla2.PaCodigo
                    join tlla in objContext.TbTallas on ltva.LidCodtal equals tlla.TalCodigo
                    join tipl in objContext.TbTiplot on new
                    { A = lotr.LotTiplot, B = lotr.LotTipo } equals new
                    { A = tipl.TipCodigo, B = tipl.TipLote }
                    join clas in objContext.TbClasificadora on ltva.LidClasificadora equals clas.ClaAbrev into clasGroup
                    from clas in clasGroup.DefaultIfEmpty()
                    where lotr.LotFecha >= dtFeInicio
                       && lotr.LotFecha <= dtFeFin
                       && lotr.LotEstado.Trim() != "an"
                       && new[] { "va", "iq", "br", "RE" }.Contains(lotr.LotTipo.Trim())
                       && prod.ProClas03 == "PT"
                    group new
                    {
                        ltva,
                        lqva,
                        emba,
                        medi
                    } by new
                    {
                        tipl.TipDescri,
                        bode.BodDescri,
                        ltva.LidLote,
                        pla1.PaDescri,
                        colo.ColDescri,
                        LiqFecha = lqva.LiqFecha.Date,
                        lotr.LotFecha,
                        ltva.LidProduc,
                        prod.ProDesesp,
                        tlla.TalDescri,
                        Certificado = (Convert.ToInt32(prod.ProCertif) == 2 ||
                                       Convert.ToInt32(prod.ProCertif) == 5) ? "ASC" :
                                      (Convert.ToInt32(prod.ProCertif) > 2 ? "OTRO" : ""),
                        pove.ClpNomcom,
                        pove.ClpGrupo,
                        pais.PaiDescri,
                        prod.ProClas02,
                        prod.ProClasePago,
                        HoraLiq = lqva.LiqFecha.Hour,
                        ltva.LidClasificadora,
                        ltva.LidCodtal,
                        clas.ClaDescripcion,
                        gprv.GrpDescri,
                        sgprv.SgrDescri,
                        prod.ProClas01,
                        prod.ProClas05,
                        PaDescri2 = pla2.PaDescri,
                        prod.ProClas03,
                        prod.ProClas06,
                        prod.ProCongela,
                        bode.BodEsBrine,
                    } into g
                    select new LiquidacionResultado(
                            g.Key.TipDescri, // tipoLiq
                            g.Key.LidLote,            // lote (se pasa como object)
                            g.Key.LotFecha!.Value.Month,// mes
                            g.Key.LotFecha.Value,             // fechaLote (DateTime?)
                            g.Key.PaDescri,           // planta
                            g.Key.ClpNomcom,            // proveedor
                            "",            // piscina
                            g.Sum(x => (double)x.ltva.LidCanenv / x.emba.EmbCantid), // masters
                            g.Sum(x => ((double)x.ltva.LidCanenv * x.emba.EmbPeso * x.medi.MedFactor)), // libras
                            g.Key.ColDescri,               // tipPro
                            g.Key.LiqFecha,             // fechaLiq
                            g.Key.HoraLiq,             // horaLiq
                            g.Key.LidProduc,              // codProd
                            g.Key.ProDesesp,          // descripcion
                            g.Key.TalDescri,                // talla
                            g.Key.Certificado,          // certificado
                            g.Key.BodDescri,           // bodDescri
                            g.Key.ClpGrupo,             // clpGrupo
                            g.Key.PaiDescri,            // paiDescri
                            g.Key.ProClas02,            // proClas02
                            g.Key.ProClasePago,         // proClasePago
                            g.Key.LidClasificadora,     // clasificadora
                            g.Min(x => x.lqva.LiqFecini),// inicioLiq
                            g.Max(x => x.lqva.LiqFecfin), // finLiq
                            g.Key.LidCodtal,          // CodTal
                            g.Key.ClaDescripcion,
                            g.Key.GrpDescri,           // Grupo Descripcion
                            g.Key.SgrDescri,           // SubGrupo Descripcion
                            g.Key.ProClas01,           // Tipo producto
                            g.Key.ProClas05,           // Tipo
                            g.Key.PaDescri2,           // Planta 2 
                            g.Key.ProClas03,
                            g.Key.ProClas06,
                            g.Key.ProCongela,
                            g.Key.BodEsBrine
                        )
                    ).ToListAsync();
                if (blValorizada == true)
                {
                    //await ObtenerVlrsLiqMateriaPrima(lstTotalResultados);
                    await ObtenerTipoXCola(lstTotalResultados);
                }
                await transaction.CommitAsync();

                return lstTotalResultados;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en ObtenerMateriaPrimaReproceso: {objException.Message}");
                throw;
            }
        }

        public async Task ObtenerTipoXCola(List<LiquidacionResultado> lstLiquidaciones)
        {
            try
            {

                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                // Crear lista de claves compuestas
                var lstNumeroLote = lstLiquidaciones
                    .Select(l => (decimal)l.intLote)
                    .ToList();

                var lstInfoProdTipCola = await objContext.TbReglot.AsNoTracking()
                    .SelectManyBatchAsync
                    (
                    keySelector: rlo => rlo.RloNumero,
                    values: lstNumeroLote,
                    selector: filtered =>
                            from rlo in filtered
                            join rec in objContext.TbProcesosRecep.AsNoTracking()
                            on rlo.RloProcesodest equals rec.PrrCodigo
                            select new
                            {
                                NumLote = rlo.RloNumero,
                                TipCola = rec.PrrIniciales
                            }
                    );
                var lookup = lstInfoProdTipCola.ToLookup(x => x.NumLote);
                foreach (var objLiquidacion in lstLiquidaciones)
                {
                    var valorizada = lookup[objLiquidacion.intLote].FirstOrDefault();
                    if (valorizada != null)
                    {
                        objLiquidacion.strTipCola = valorizada.TipCola;
                    }
                }
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en ObtenerTipoXCola: {objException.Message}");
                throw;
            }
        }


        

        #endregion

        #region Guardar Materia Prima Valorizada
        public async Task<List<LiquidacionResultado>> ObtenerLstMatPrimValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<LiquidacionResultado> lstMatPrimaFresco;
            try
            {
                using var objContext = await _objCostosFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstMatPrimaFresco = await
                    (from mtfrs in objContext.TbMateriaPrimaFrescoValorizada.AsNoTracking()
                     where mtfrs.MfEstado != "AN" && mtfrs.MfFecha >= dtFechaInicio && mtfrs.MfFecha <= dtFechaFin
                     select new LiquidacionResultado
                     {
                         intLote = (int)mtfrs.MfRloNumero,
                            intLidCodTal = (int)mtfrs.MfTalCodigo,
                            intCodProd = int.Parse(mtfrs.MfProCodcor),
                            dcMasters = (double)mtfrs.MfMasters,
                            dcLibras = (double)mtfrs.MfLibras,
                            dcCostoTotXLibra = mtfrs.MfCostoUnitario,
                            dcTotalDol = (double)mtfrs.MfCostoTotal,
                     }
                     ).ToListAsync();
                return lstMatPrimaFresco;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerLstMatPrimValorizadaFrs: {ObjException.Message}");
                throw;
            }
        }



        public async Task GuardarMatPrimValorizada(List<LiquidacionResultado> lstLiquidaciones, RequestMatPrimDto ObjRequest)
        {
            try
            {
                using var objContext = await _objCostosFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);// Listas para manejar los registros
                var registrosParaInsertar = new List<TbMateriaPrimaFrescoValorizada>();
                var registrosTotalesParaReferencia = new List<TbMateriaPrimaFrescoValorizada>();

                // --- 1. VALIDACIÓN Y ACTUALIZACIÓN PREVIA (ANTES DE INSERTAR) ---
                foreach (var liq in lstLiquidaciones)
                {
                    // Buscamos si ya existe el registro por criterios únicos
                    var registroExistente = await objContext.TbMateriaPrimaFrescoValorizada.FirstOrDefaultAsync(x =>
                        x.MfRloNumero == liq.intLote &&
                        x.MfProCodcor == liq.intCodProd.ToString() &&
                        x.MfTalCodigo == (short)liq.intLidCodTal &&
                        x.MfLibras == (decimal)liq.dcLibras);

                    if (registroExistente != null)
                    {
                        // Actualización del registro existente
                        registroExistente.MfCostoUnitario = (decimal)liq.dcCostoTotXLibra;
                        registroExistente.MfCostoTotal = (decimal)liq.dcTotalDol;
                        registroExistente.MfUsuarioMod = ObjRequest.strUsuarioCrea;
                        registroExistente.MfFechaMod = DateTime.Now;

                        objContext.TbMateriaPrimaFrescoValorizada.Update(registroExistente);

                        // Agregamos a la referencia para los motivos de cambio
                        registrosTotalesParaReferencia.Add(registroExistente);
                    }
                    else
                    {
                        // Mapeo para nuevo registro
                        var nuevoRegistro = new TbMateriaPrimaFrescoValorizada
                        {
                            MfEmpCodigo = 1,
                            MfPaCodigo = 1,
                            MfFecha = (DateOnly)liq.dtFechaLote,
                            MfRloNumero = liq.intLote,
                            MfProCodcor = liq.intCodProd.ToString(),
                            MfTalCodigo = (short)liq.intLidCodTal,
                            MfBodCodigo = liq.strBodCod,
                            MfMedCodigo = (byte)liq.dcMedCodigo,
                            MfEmbCodigo = liq.strEmbCodigo,
                            MfMasters = (decimal)liq.dcMasters,
                            MfLibras = (decimal)liq.dcLibras,
                            MfCostoUnitario = (decimal)liq.dcCostoTotXLibra,
                            MfCostoTotal = (decimal)liq.dcTotalDol,
                            MfEstado = "AC",
                            MfUsuarioCrea = ObjRequest.strUsuarioCrea,
                            MfEquipoCrea = Environment.MachineName,
                            MfFechaCrea = DateTime.Now
                        };

                        registrosParaInsertar.Add(nuevoRegistro);
                        registrosTotalesParaReferencia.Add(nuevoRegistro);
                    }
                }

                // 2. Insertar registros nuevos
                if (registrosParaInsertar.Any())
                {
                    await objContext.TbMateriaPrimaFrescoValorizada.AddRangeAsync(registrosParaInsertar);
                }

                // Guardamos para procesar Updates y obtener IDs de Inserts
                await objContext.SaveChangesAsync();

                // --- 3. PROCESAMIENTO DE MOTIVOS DE CAMBIO ---
                if (ObjRequest.lstMatPrim != null && ObjRequest.lstMatPrim.Any())
                {
                    var lstMotivos = new List<TbMateriaPrimaFrescoValorizadaMotivoCambios>();

                    foreach (var itemModificado in ObjRequest.lstMatPrim)
                    {
                        if (!string.IsNullOrWhiteSpace(itemModificado.strMotivo))
                        {
                            // Buscamos en la lista unificada (existentes + nuevos)
                            var registroReferencia = registrosTotalesParaReferencia.FirstOrDefault(x =>
                                x.MfRloNumero == itemModificado.intLote &&
                                x.MfProCodcor == itemModificado.strCodProd);

                            if (registroReferencia != null)
                            {
                                lstMotivos.Add(new TbMateriaPrimaFrescoValorizadaMotivoCambios
                                {
                                    McMfId = registroReferencia.MfId, // ID recuperado de la BD o generado en el paso anterior
                                    McMotivo = itemModificado.strMotivo,
                                    McEstado = "AC",
                                    McUsuarioCrea = ObjRequest.strUsuarioCrea,
                                    McEquipoCrea = Environment.MachineName,
                                    McFechaCrea = DateTime.Now
                                });

                                // Actualizar el costo unitario si hubo edición manual
                                registroReferencia.MfCostoUnitario = itemModificado.dcValorNuevo;
                            }
                        }
                    }

                    if (lstMotivos.Any())
                    {
                        await objContext.TbMateriaPrimaFrescoValorizadaMotivoCambios.AddRangeAsync(lstMotivos);
                        await objContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en GuardarMatPrimValorizada: {ObjException.Message}");
                throw;
            }
        }

        public async Task<List<MatPrimaReproceso>> ObtenerReproValorizada(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<MatPrimaReproceso> lstMatPrimaReproceso;
            try
            {
                using var objContext = await _objCostosFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstMatPrimaReproceso = await
                    (from mrpc in objContext.TbMateriaPrimaReproValorizada.AsNoTracking()
                     where mrpc.MrEstado != "AN" && mrpc.MrFecha >= dtFechaInicio && mrpc.MrFecha <= dtFechaFin
                     select new MatPrimaReproceso
                     {
                         intLotNumero = (int)mrpc.MrLotNumero,
                         intLoteUnificado = (int)mrpc.MrRloNumeroUnificado,
                         intCodTal = (int)mrpc.MrTalCodigo,
                         intProdCod = int.Parse(mrpc.MrProCodcor),
                         dbMasters = (double)mrpc.MrMasters,
                         dbLibras = (double)mrpc.MrLibras,
                         //dbCostoXSecuencial = mrpc.MrCostoUnitario,
                         dcCostoTotXLibra = mrpc.MrCostoUnitario,
                         dbCostoTotal = mrpc.MrCostoTotal,
                     }
                     ).ToListAsync();
                return lstMatPrimaReproceso;
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en ObtenerLstMatPrimValorizadaFrs: {ObjException.Message}");
                throw;
            }
        }


        public async Task GuardarReproValorizado(List<MatPrimaReproceso> lstMatPrimaReproceso, RequestMatPrimDto objRequest)
        {
            try
            {
                using var objContext = await _objCostosFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);// --- 1. VALIDACIÓN Y ACTUALIZACIÓN PREVIA (ANTES DE INSERTAR) ---

                // Listas para manejar los registros finales
                var registrosParaInsertar = new List<TbMateriaPrimaReproValorizada>();
                var registrosTotalesParaReferencia = new List<TbMateriaPrimaReproValorizada>();

                // --- 1. VALIDACIÓN, ACTUALIZACIÓN E IDENTIFICACIÓN DE NUEVOS ---
                foreach (var item in lstMatPrimaReproceso)
                {
                    var liquidacionExistente = await objContext.TbMateriaPrimaReproValorizada.FirstOrDefaultAsync(x =>
                        x.MrLotNumero == item.intLotNumero &&
                        x.MrRloNumeroUnificado == item.intLoteUnificado &&
                        x.MrProCodcor == item.intProdCod.ToString() &&
                        x.MrTalCodigo == (short)item.intCodTal &&
                        x.MrLibras == (decimal)item.dbLibras);

                    if (liquidacionExistente != null)
                    {
                        // Actualización
                        liquidacionExistente.MrCostoUnitario = (decimal)item.dcCostoTotXLibra;
                        liquidacionExistente.MrUsuarioMod = objRequest.strUsuarioCrea;
                        liquidacionExistente.MrFechaMod = DateTime.Now;

                        objContext.TbMateriaPrimaReproValorizada.Update(liquidacionExistente);

                        // Lo añadimos a la lista de referencia para los motivos
                        registrosTotalesParaReferencia.Add(liquidacionExistente);
                    }
                    else
                    {
                        // Mapeo para Inserción
                        var nuevoRegistro = new TbMateriaPrimaReproValorizada
                        {
                            MrEmpCodigo = 1,
                            MrPaCodigo = 1,
                            MrFecha = DateOnly.FromDateTime(item.dtLotFecha),
                            MrLotNumero = item.intLotNumero,
                            MrRloNumeroUnificado = item.intLoteUnificado,
                            MrProCodcor = item.intProdCod.ToString(),
                            MrTalCodigo = (short)item.intCodTal,
                            MrBodCodigo = item.strBodCod ?? "0",
                            MrMedCodigo = (byte)item.intMedCodigo,
                            MrEmbCodigo = item.strEmbCodigo ?? "",
                            MrMasters = (decimal)item.dbMasters,
                            MrLibras = (decimal)item.dbLibras,
                            MrCostoUnitario = (decimal)item.dcCostoTotXLibra,
                            MrCostoTotal = (decimal)item.dbCostoTotal,
                            MrEstado = "AC",
                            MrUsuarioCrea = objRequest.strUsuarioCrea,
                            MrEquipoCrea = Environment.MachineName,
                            MrFechaCrea = DateTime.Now
                        };
                        registrosParaInsertar.Add(nuevoRegistro);
                        registrosTotalesParaReferencia.Add(nuevoRegistro);
                    }
                }

                // 2. Ejecutar inserciones si existen
                if (registrosParaInsertar.Any())
                {
                    await objContext.TbMateriaPrimaReproValorizada.AddRangeAsync(registrosParaInsertar);
                }

                // Guardamos para obtener los IDs de los nuevos registros
                await objContext.SaveChangesAsync();

                // --- 3. PROCESAMIENTO DE MOTIVOS DE CAMBIO ---
                if (objRequest.lstMatPrim != null && objRequest.lstMatPrim.Any())
                {
                    var lstMotivos = new List<TbMateriaPrimaFrescoValorizadaMotivoCambios>();

                    foreach (var itemModificado in objRequest.lstMatPrim)
                    {
                        if (!string.IsNullOrWhiteSpace(itemModificado.strMotivo))
                        {
                            // Buscamos en la lista que unifica existentes y nuevos
                            var registroReferencia = registrosTotalesParaReferencia.FirstOrDefault(x =>
                                x.MrLotNumero == itemModificado.intLote &&
                                x.MrProCodcor == itemModificado.strCodProd);

                            if (registroReferencia != null)
                            {
                                lstMotivos.Add(new TbMateriaPrimaFrescoValorizadaMotivoCambios
                                {
                                    McMfId = registroReferencia.MrId, // Ya tiene ID sea nuevo o viejo
                                    McMotivo = itemModificado.strMotivo,
                                    McEstado = "AC",
                                    McUsuarioCrea = objRequest.strUsuarioCrea,
                                    McEquipoCrea = Environment.MachineName,
                                    McFechaCrea = DateTime.Now
                                });

                                // Si vino un valor nuevo desde el grid, actualizamos el costo unitario
                                registroReferencia.MrCostoUnitario = itemModificado.dcValorNuevo;
                            }
                        }
                    }

                    if (lstMotivos.Any())
                    {
                        await objContext.TbMateriaPrimaFrescoValorizadaMotivoCambios.AddRangeAsync(lstMotivos);
                        await objContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"Error en GuardarReproValorizado: {ObjException.Message}");
                throw;
            }
        }
        #endregion

        #region Costo Por Ficha tecnica 2
        public async Task<List<CostoMatEmpProdXCietunDto>> ObtenerCostMatEmpFrsProdXLiq(List<decimal> lstLote)
        {
            List<CostoMatEmpProdXCietunDto> lstCostosEmpaque;
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstCostosEmpaque = await objContext.TbLiqtun.AsNoTracking()
                    .SelectManyBatchAsync(
                        keySelector: liq => liq.LiqLote,
                        values: lstLote,
                        selector: filtered =>
                            from liq in filtered
                                // Join con Detalle de Liquidación para obtener productos
                            join lid in objContext.TbLitund.AsNoTracking()
                                on new { A = liq.LiqNumero, B = liq.LiqLote } equals new { A = (decimal)lid.LidNumero, B = (decimal)lid.LidLote }
                            join ctu in objContext.TbCietun.AsNoTracking() on liq.LiqCietun equals ctu.CtuNumero
                            join pro in (
                                        from pro in objContext.TbProduc.AsNoTracking()
                                        join ftc in objContext.TbEmbalaFichaTecnica.AsNoTracking() on pro.ProCodcor equals ftc.EftId.ToString()
                                        join col in objContext.TbColor.AsNoTracking() on pro.ProClas05 equals col.ColCodigo
                                        join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                                        join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                                        where pro.ProClas03 == "PT" && ftc.EftCantidad != 0 && ftc.EftItem != 0
                                        group new { med, emb } by new
                                        {
                                            pro.ProCodcor,
                                            ftc.EftItem,
                                            ftc.EftGrupo,
                                            ftc.EftCantidad,
                                            emb.EmbPeso,
                                            pro.ProCostoDesperdicioBobinaKg,
                                            med.MedCodigo,
                                            emb.EmbCodigo
                                        } into d
                                        select new
                                        {
                                            d.Key.ProCodcor,
                                            d.Key.EftItem,
                                            d.Key.EftGrupo,
                                            d.Key.EftCantidad,
                                            d.Key.EmbPeso,
                                            d.Key.ProCostoDesperdicioBobinaKg,
                                            d.Key.MedCodigo,
                                            d.Key.EmbCodigo,
                                            LibrasXMasters = (float)d.Sum(x => x.emb.EmbCantid * x.med.MedFactor * x.emb.EmbPeso)
                                        }
                                                ) on lid.LidProduc equals pro.ProCodcor
                            group new { } by new
                            {
                                liq.LiqLote,
                                ctu.CtuNumero,
                                lid.LidProduc,
                                pro.EftItem,
                                pro.EftGrupo,
                                pro.EftCantidad,
                                pro.EmbPeso,
                                pro.ProCostoDesperdicioBobinaKg,
                                pro.LibrasXMasters,
                                pro.MedCodigo,
                                pro.EmbCodigo
                            } into g
                            select new CostoMatEmpProdXCietunDto
                            ( 
                                g.Key.LiqLote,
                                g.Key.CtuNumero,
                                g.Key.LidProduc,
                                g.Key.EftItem,
                                g.Key.EftGrupo,
                                g.Key.EftCantidad,
                                (float)g.Key.LibrasXMasters,
                                (decimal)g.Key.ProCostoDesperdicioBobinaKg,
                                g.Key.EmbPeso,
                                g.Key.MedCodigo,
                                g.Key.EmbCodigo.Trim()
                            )
                    );
                await ObtenerPrecioEgresoXCieTun(lstCostosEmpaque);

                lstCostosEmpaque = (from i in lstCostosEmpaque
                                    group i by new {i.intLiqLote, i.strProCodCor, i.intEftItem, i.dbEftCantidad } into g
                                    select new CostoMatEmpProdXCietunDto
                                    {
                                        // Mantenemos los IDs del primer registro encontrado
                                        intLiqLote = g.Key.intLiqLote,
                                        intCtuNumero = g.First().intCtuNumero,
                                        strProCodCor = g.Key.strProCodCor,
                                        intEftItem = g.Key.intEftItem,
                                        strEstadoFicha = g.Where(x => x.dbPrecioUnit != 0).First().strEstadoFicha,
                                        dtFechaEgreso = g.First().dtFechaEgreso,

                                        // Datos fijos de la Ficha Técnica
                                        strEftGrupo = g.First().strEftGrupo,
                                        dbEftCantidad = g.Key.dbEftCantidad,

                                        // --- EL CORAZÓN DE LA SOLUCIÓN ---
                                        // Promediamos los precios de las 6 tallas para tener un solo valor
                                        dbPrecioUnit = g.Average(x => x.dbPrecioUnit),
                                        dbPrecioUltConsumo = g.Average(x => x.dbPrecioUltConsumo),

                                        dcLibrasXMasters = g.First().dcLibrasXMasters,
                                        dcCostoDesperdicioBobina = g.First().dcCostoDesperdicioBobina,
                                        dbEmbPeso = g.First().dbEmbPeso,
                                        dcMedCodigo = g.First().dcMedCodigo,
                                        strEmbCodigo = g.First().strEmbCodigo
                                    }).ToList();
                return lstCostosEmpaque;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerCostMatEmpProdXLiq] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<List<CostoMatEmpProdXCietunDto>> ObtenerCostMatEmpRpcProdXLiq(List<decimal> lstLote)
        {
            List<CostoMatEmpProdXCietunDto> lstCostosEmpaque;
            try
            {

                //from lotr in objContext.TbLototr.AsNoTracking()
                //join lqva in objContext.TbLiqvag.AsNoTracking() on new
                //{ A = lotr.LotNumero, B = lotr.LotTipo, C = "AC" } equals new
                //{ A = lqva.LiqLote, B = lqva.LiqTipo, C = lqva.LiqEstado }
                //join ltva in objContext.TbLitvad on lqva.LiqNumero equals ltva.LidNumero
                //join relo in objContext.TbReglot on ltva.LidLote equals relo.RloNumero
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstCostosEmpaque = await objContext.TbLototr.AsNoTracking()
                    .SelectManyBatchAsync(
                        keySelector: lot => (decimal)lot.LotRloNumero,
                        values: lstLote,
                        selector: filtered =>
                            from lot in filtered
                            join liq in objContext.TbLiqvag.AsNoTracking() on new 
                            { A = lot.LotNumero, B = lot.LotTipo, C = "AC" } equals new
                            { A = liq.LiqLote, B = liq.LiqTipo, C = liq.LiqEstado }
                            join lid in objContext.TbLitvad.AsNoTracking() on liq.LiqNumero equals lid.LidNumero
                            join rel in objContext.TbReglot.AsNoTracking() on lid.LidLote equals rel.RloNumero
                            // new { A = liq.LiqNumero, B = liq.LiqLote } equals new { A = (decimal)lid.LidNumero, B = (decimal)lid.LidLote }
                            join ctu in objContext.TbCietun.AsNoTracking() on liq.LiqCietun equals ctu.CtuNumero 
                            join pro in (
                                        from pro in objContext.TbProduc.AsNoTracking()
                                        join ftc in objContext.TbEmbalaFichaTecnica.AsNoTracking() on pro.ProCodcor equals ftc.EftId.ToString()
                                        join col in objContext.TbColor.AsNoTracking() on pro.ProClas05 equals col.ColCodigo
                                        join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                                        join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                                        where pro.ProClas03 == "PT" && ftc.EftCantidad != 0 && ftc.EftItem != 0
                                        group new { med, emb } by new
                                        {
                                            pro.ProCodcor,
                                            ftc.EftItem,
                                            ftc.EftGrupo,
                                            ftc.EftCantidad,
                                            emb.EmbPeso,
                                            pro.ProCostoDesperdicioBobinaKg,
                                            med.MedCodigo,
                                            emb.EmbCodigo
                                        } into d
                                        select new
                                        {
                                            d.Key.ProCodcor,
                                            d.Key.EftItem,
                                            d.Key.EftGrupo,
                                            d.Key.EftCantidad,
                                            d.Key.EmbPeso,
                                            d.Key.ProCostoDesperdicioBobinaKg,
                                            d.Key.MedCodigo,
                                            d.Key.EmbCodigo,
                                            LibrasXMasters = (float)d.Sum(x => x.emb.EmbCantid * x.med.MedFactor * x.emb.EmbPeso)
                                        }
                                                ) on lid.LidProduc equals pro.ProCodcor
                            group new { } by new
                            {
                                liq.LiqLote,
                                ctu.CtuNumero,
                                lid.LidProduc,
                                pro.EftItem,
                                pro.EftGrupo,
                                pro.EftCantidad,
                                pro.EmbPeso,
                                pro.ProCostoDesperdicioBobinaKg,
                                pro.LibrasXMasters,
                                pro.MedCodigo,
                                pro.EmbCodigo
                            } into g
                            select new CostoMatEmpProdXCietunDto
                            (
                                g.Key.LiqLote,
                                g.Key.CtuNumero,
                                g.Key.LidProduc,
                                g.Key.EftItem,
                                g.Key.EftGrupo,
                                g.Key.EftCantidad,
                                (float)g.Key.LibrasXMasters,
                                (decimal)g.Key.ProCostoDesperdicioBobinaKg,
                                g.Key.EmbPeso,
                                g.Key.MedCodigo,
                                g.Key.EmbCodigo.Trim()
                            )
                    );

                
                await ObtenerPrecioEgresoXCieTun(lstCostosEmpaque);

                lstCostosEmpaque = (from i in lstCostosEmpaque
                                    group i by new { i.intLiqLote, i.strProCodCor, i.intEftItem, i.dbEftCantidad } into g
                                    select new CostoMatEmpProdXCietunDto
                                    {
                                        // Mantenemos los IDs del primer registro encontrado
                                        intLiqLote = g.Key.intLiqLote,
                                        intCtuNumero = g.First().intCtuNumero,
                                        strProCodCor = g.Key.strProCodCor,
                                        intEftItem = g.Key.intEftItem,
                                        strEstadoFicha = g.Where(x => x.dbPrecioUnit != 0).First().strEstadoFicha,
                                        dtFechaEgreso = g.First().dtFechaEgreso,

                                        // Datos fijos de la Ficha Técnica
                                        strEftGrupo = g.First().strEftGrupo,
                                        dbEftCantidad = g.Key.dbEftCantidad,

                                        // --- EL CORAZÓN DE LA SOLUCIÓN ---
                                        // Promediamos los precios de las 6 tallas para tener un solo valor
                                        dbPrecioUnit = g.Average(x => x.dbPrecioUnit),
                                        dbPrecioUltConsumo = g.Average(x => x.dbPrecioUltConsumo),

                                        dcLibrasXMasters = g.First().dcLibrasXMasters,
                                        dcCostoDesperdicioBobina = g.First().dcCostoDesperdicioBobina,
                                        dbEmbPeso = g.First().dbEmbPeso,
                                        dcMedCodigo = g.First().dcMedCodigo,
                                        strEmbCodigo = g.First().strEmbCodigo
                                    }).ToList();
                return lstCostosEmpaque;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerCostMatEmpProdXLiq] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task ObtenerPrecioEgresoXCieTun(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun)
        {
            try
            {
                var lstEgresosLiqXCieTun = lstInfoCierreTun
                    .Select(x => (decimal?)x.intCtuNumero)
                    .Distinct()
                    .ToList();
                List<MovimientoPrecioDto> lstPrecioEgreso = await _objSongContext.TbCabmov2
                    .SelectManyBatchAsync(
                        keySelector: cab => cab.MovCietun,
                        values: lstEgresosLiqXCieTun,
                        selector: filtered =>
                            from cab2 in filtered
                            join mov2 in _objSongContext.TbDetmov2.AsNoTracking()
                                on cab2.MovNummov equals mov2.DetCabece
                            select new MovimientoPrecioDto(cab2.MovNumdoc, cab2.MovCietun, mov2.DetPreuni, mov2.DetCodart, cab2.MovFecha)
                    );
                var preciosLookup = lstPrecioEgreso
                         .ToLookup(x => (x.intCietun,x.intEftItem));
                foreach (var cieTun in lstInfoCierreTun)
                {
                    var objValor = preciosLookup[(cieTun.intCtuNumero, cieTun.intEftItem)].FirstOrDefault();
                    if (objValor != null)
                    {
                        cieTun.dbPrecioUnit = objValor.dbDetPreuni;
                        cieTun.strEstadoFicha = "E";
                        cieTun.dtFechaEgreso = objValor.dtFechaEgreso;
                    }
                }
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerPrecioEgresoXCieTun] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task ObtenerCostoPromMov2XFichaTecnica(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun, DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                var lstCodigosFichas = lstInfoCierreTun
                    .Select(pr => pr.intEftItem.ToString());
                var lstCostoPromedio = await (
                            from cab in _objSongContext.TbCabmov2.AsNoTracking()
                            join det in _objSongContext.TbDetmov2.AsNoTracking() on cab.MovNummov equals det.DetCabece
                            join bod in _objSongContext.TbBodega.AsNoTracking() on det.DetBodega equals bod.BodCodigo
                            where cab.MovTipo == "EGR"
                               && (bod.BodConsiderarCostoMaterial ?? "") == "S"
                               //&& cab.MovFecha.CompareTo(dtFechaInicio.ToString("yyyy/MM/dd")) >= 0
                               //&& cab.MovFecha.CompareTo(dtFechaFin.ToString("yyyy/MM/dd")) <= 0
                            select new { det.DetCodart, cab.MovFecha, det.DetPreuni }
                        ).SelectManyBatchAsync(
                            keySelector: x => x.DetCodart,
                            values: lstCodigosFichas,
                            selector: filtered => filtered
                                .GroupBy(x => x.DetCodart)
                                .Select(g => g.OrderByDescending(x => x.MovFecha).First())
                                .Select(res => new
                                {
                                    IteCodigo = res.DetCodart.Trim(),
                                    Fecha = res.MovFecha,
                                    CostoProm = res.DetPreuni
                                })
                        );
                var costosLookup = lstCostoPromedio
                    .ToLookup(x =>  x.IteCodigo);
                // Itero la lista original para actualizarla
                foreach (var producto in lstInfoCierreTun)
                {
                    // Busco usando la misma estructura :  Ítem
                    var llave = producto.intEftItem.ToString();
                    //(producto.strProCodCor, producto.intEftItem.ToString());

                    var costo = costosLookup[llave].FirstOrDefault();

                    if (costo != null)
                    {
                        producto.dbPrecioUnit = costo.CostoProm;
                        producto.strEstadoFicha = "M";
                    }
                }
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerCostoPromXFichaTecnica] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task ObtenerCostoPromBoditeXFichaTecnica(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun, DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<string> lstNotInGrupoItem;
            try
            {
                using var objContext = await _objSongFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstNotInGrupoItem = _objConfig.Value.lstNotInGrupoItem;
                var lstCodigosFichas = lstInfoCierreTun
                    .Select(pr => pr.intEftItem.ToString())
                    .Distinct()
                    .ToList();
                var lstCostoPromedio = await objContext.TbBodite.AsNoTracking()
                   .SelectManyBatchAsync(
                       keySelector: cab => cab.IteCodigo,
                       values: lstCodigosFichas,
                       selector: filtered =>
                            from b in filtered
                            //where
                            //      b.BodFecUltConsumo.CompareTo(dtFechaInicio.ToString("yyyy/MM/dd")) >= 0 &&
                            //      b.BodFecUltConsumo.CompareTo(dtFechaFin.ToString("yyyy/MM/dd")) <= 0
                            group b by b.IteCodigo into g
                            where g.Sum(x => x.BodStock ?? 0) > 0  // HAVING SUM(bod_stock) > 0

                            select new
                            {
                                IteCodigo = g.Key.Trim(),
                                CostoProm = g.Sum(x => (x.BodStock ?? 0) * (double)(x.BodCospro ?? 0)) /
                                            g.Sum(x => x.BodStock ?? 0),
                                Fecha = g.Max(x => x.BodFecUltConsumo) // Tomamos la fecha más reciente dentro del rango
                            }
                   );

                var costosLookup = lstCostoPromedio.Where(obj => obj.CostoProm != 0)
                    .ToLookup(x => int.Parse(x.IteCodigo));

                foreach (var producto in lstInfoCierreTun.Where(prod => (prod.dbPrecioUnit ?? 0.0) == 0.0 /*&& !lstNotInGrupoItem.Contains(prod.strEftGrupo)*/))
                {
                    var costo = costosLookup[producto.intEftItem].FirstOrDefault();

                    if (costo != null)
                    {
                        producto.dbPrecioUnit = costo.CostoProm;
                        producto.strEstadoFicha = "B";
                        if (!string.IsNullOrEmpty(costo.Fecha))
                        {
                            if (DateTime.TryParse(costo.Fecha, out var fechaDateTime))
                            {
                                producto.dtFechaEgreso = DateOnly.FromDateTime(fechaDateTime);
                            }
                        }
                    }
                }
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerCostoPromXFichaTecnica] Ocurrio un error: {objException.Message}");
                throw;
            }
        }


        public async Task ObtenerCostoPromMov1XFichaTecnica(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun, DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {

            List<string> lstNotInGrupoItem;
            try
            {
                using var objContext = await _objSongFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstNotInGrupoItem = _objConfig.Value.lstNotInGrupoItem;
                var lstCodigosFichas = lstInfoCierreTun
                    .Select(pr => pr.intEftItem.ToString())
                    .Distinct()
                    .ToList();

                var lstCostoPromedio = await (
                    from cab in objContext.TbCabmov1.AsNoTracking()
                    join det in objContext.TbDetmov1.AsNoTracking()
                         on new { A = cab.MovNummov, B = cab.MovTipo } equals new { A = det.DetCabece, B = "CO" }
                    where cab.MovEstado != "AN"
                    select new
                    {
                        DetCodart = det.DetCodart,
                        MovFecha = cab.MovFecha,
                        DetPreuni = det.DetPreuni,
                        MovPordes = cab.MovPordes
                    }
                ).SelectManyBatchAsync(
                    keySelector: x => x.DetCodart,
                    values: lstCodigosFichas,
                    batchSelector: filtered => filtered.Select(x => new
                    {
                        x.DetCodart,
                        x.MovFecha,
                        x.DetPreuni,
                        x.MovPordes
                    }),
                    selector: items => items
                        .GroupBy(x => x.DetCodart)
                        .Select(g => g.OrderByDescending(x => x.MovFecha).First())
                        .Select(res => new
                        {
                            IteCodigo = res.DetCodart.Trim(),
                            Fecha = res.MovFecha,
                            CostoProm = res.MovPordes != null && res.MovPordes != 0
                                ? res.DetPreuni - (res.DetPreuni * res.MovPordes.Value) / 100.0
                                : res.DetPreuni
                        })
                );

                var costosLookup = lstCostoPromedio
                     .ToLookup(x =>  x.IteCodigo);
                foreach (var producto in lstInfoCierreTun.Where(prod => (prod.dbPrecioUnit ?? 0.0) == 0.0 /*&& !lstNotInGrupoItem.Contains(prod.strEftGrupo)*/))
                {
                    // Busco usando la misma estructura:  Ítem
                    var llave = producto.intEftItem.ToString();//(producto.strProCodCor, producto.intEftItem.ToString());

                    var costo = costosLookup[llave].FirstOrDefault();

                    if (costo != null)
                    {
                        producto.dbPrecioUnit = costo.CostoProm;
                        producto.strEstadoFicha = "M"; 
                        if (!string.IsNullOrEmpty(costo.Fecha))
                        {
                            if (DateTime.TryParse(costo.Fecha, out var fechaDateTime))
                            {
                                producto.dtFechaEgreso = DateOnly.FromDateTime(fechaDateTime);
                            }
                        }
                    }
                }
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerCostoPromXFichaTecnica] Ocurrio un error: {objException.Message}");
                throw;
            }
        }


        public async Task ObtenerCostoUltConsuMov2XFichaTecnica(List<CostoMatEmpProdXCietunDto> lstInfoCierreTun, DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                using var objContext = await _objSongFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                var lstCodigosFichas = lstInfoCierreTun
                    .Select(pr => pr.intEftItem.ToString())
                    .Distinct()
                    .ToList();

                var datosBrutos = await objContext.TbDetmov2.AsNoTracking()
                    .SelectManyBatchAsync(
                        keySelector: det => det.DetCodart, // Filtramos directamente por código de artículo
                        values: lstCodigosFichas,
                        selector: filteredDet =>
                            from det in filteredDet
                            join cab in objContext.TbCabmov2.AsNoTracking()
                                on det.DetCabece equals cab.MovNummov
                            join bod in objContext.TbBodega.AsNoTracking()
                                on det.DetBodega equals bod.BodCodigo
                            where cab.MovTipo == "EGR"
                               && (bod.BodConsiderarCostoMaterial ?? "") == "S"
                            // && cab.MovFecha.CompareTo(dtFechaInicio.ToString("yyyy/MM/dd")) >= 0
                            // && cab.MovFecha.CompareTo(dtFechaFin.ToString("yyyy/MM/dd")) <= 0
                            select new
                            {
                                DetCodart = det.DetCodart,
                                MovFecha = cab.MovFecha,
                                DetPreuni = det.DetPreuni
                            }
                    );

                // 2. Procesamos en MEMORIA para obtener el costo más reciente por cada ítem
                var lstCostoPromedio = datosBrutos
                    .GroupBy(x => x.DetCodart.Trim()) // Agrupamos por código limpio
                    .Select(g =>
                    {
                        // Ordenamos por fecha y tomamos el primero (el más reciente)
                        var res = g.OrderByDescending(x => x.MovFecha).First();

                        return new
                        {
                            IteCodigo = res.DetCodart.Trim(),
                            Fecha = res.MovFecha,
                            CostoProm = (double)res.DetPreuni
                        };
                    })
                    .ToList();

                var costosLookup = lstCostoPromedio
                    .ToLookup(x => /*(x.ProdCor, x.IteCodigo)*/ x.IteCodigo);
                // Itero la lista original para actualizarla
                foreach (var producto in lstInfoCierreTun)
                {
                    // Busco usando la misma estructura :  Ítem
                    var llave = producto.intEftItem.ToString();
                    //(producto.strProCodCor, producto.intEftItem.ToString());

                    var costo = costosLookup[llave].FirstOrDefault();

                    if (costo != null)
                    {
                        producto.dbPrecioUltConsumo = costo.CostoProm;
                    }
                }
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerCostoPromXFichaTecnica] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        #endregion

        #region Base Inventario Materia Prima Reproceso

        public async Task<List<MatPrimaReproceso>> ReporteReproPlanRecibProc(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {

                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                DateTime dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(00, 00)); // 00:00:00
                DateTime dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));      // 23:59:59
                List<int> lstProdCodNotIn = new List<int>() { 5909, 5908, 5907, 5911, 5912 };
                var tmpTipoCopacking = new[]
                {
                    new TipoCopacking("0", "NO COPACKING"),
                    new TipoCopacking("1", "COPACKING EN SONGA"),
                    new TipoCopacking("2", "COPACKING EN OTRAS CIAS.")
                }.AsQueryable();
                var dictTipoCopacking = tmpTipoCopacking
                    .ToDictionary(x => x.Codigo, x => x.Descripcion);

                var lstLibrasProcesadas = await (
                    from lot in objContext.TbLototr.AsNoTracking()
                    join liq in objContext.TbLiqvag.AsNoTracking()
                                           on new { A = lot.LotNumero, B = lot.LotTipo }
                                           equals new { A = liq.LiqLote, B = liq.LiqTipo }
                                           //join tpc in tmpTipoCopacking on lot.LotCopack equals tpc.Codigo
                    join lid in objContext.TbLitvad.AsNoTracking() on liq.LiqNumero equals lid.LidNumero
                    join tot in objContext.TbTiplot.AsNoTracking() on lot.LotTiplot equals tot.TipCodigo
                    join pro in objContext.TbProduc.AsNoTracking() on lid.LidProduc equals pro.ProCodcor
                    join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                    join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                    join tal in objContext.TbTallas.AsNoTracking() on lid.LidCodtal equals tal.TalCodigo
                    join bod in objContext.TbBodega.AsNoTracking() on liq.LiqTunpla equals bod.BodCodigo
                    join proc in objContext.TbPlantaProcOe.AsNoTracking() on bod.BodPlantaProcesoOe equals proc.PaCodigo
                    join pres in objContext.TbProces.AsNoTracking() on pro.ProClas06 equals pres.ProCodigo
                    join detPres in objContext.TbDetproces.AsNoTracking() on pres.ProCongel equals detPres.DprCodigo
                    join hidra in (
                        from ctrh in objContext.TblCtrltrathid.AsNoTracking()
                        join rece in objContext.TblRecetahidrat.AsNoTracking()
                                            on new { A = ctrh.CthHidratante, B = ctrh.CthCrucocido }
                                           equals new { A = rece.RecTipohid, B = rece.RecTipo }
                        join ring in objContext.TbRelIngredientesTrataItem.AsNoTracking()
                                            on new { A = rece.RecCodrelingr, B = "AC" }
                                            equals new { A = ring.RtCodrel, B = ring.RtEstado }
                        group new { rece, ctrh, ring } by new
                        {
                            ctrh.CthLote,
                            ctrh.CthSeclote,
                            ctrh.CthTalla,
                            rece.RecNombre,
                            rece.RecTipo,
                            ring.RtCodItem,
                            rece.RecPorchid,
                            rece.RecPorcsal
                        } into g
                        select new
                        {
                            g.Key.CthLote,
                            CthHidlbs = g.Sum(x => x.ctrh.CthHidlbs),
                            CthSallbs = g.Sum(x => x.ctrh.CthSallbs),
                            RecNombre = g.Key.RecNombre,
                            RecTipo = g.Key.RecTipo,
                            RtCodItem = g.Key.RtCodItem,
                            CthSecLote = g.Key.CthSeclote,
                            g.Key.CthTalla,
                            recSal = g.Key.RecPorcsal,
                            recHid = g.Key.RecPorchid
                        }
                    )
                        on new { A = lot.LotRloNumero, B = lot.LotNumero.ToString(), C = lid.LidCodtal.ToString() } 
                        equals new { A = (decimal?)hidra.CthLote, B = hidra.CthSecLote, C = hidra.CthTalla } 
                    into hidraGroup
                    from hidra in hidraGroup.DefaultIfEmpty()
                    join pso in 
                    (
                        from ctran in objContext.TbCabtrans.AsNoTracking()
                        join dtran in objContext.TbDetrans.AsNoTracking() 
                                            on new { A = ctran.Nsecuencial, B = ctran.Id780 }
                                           equals new { A = dtran.Nsecuencial, B = dtran.Id780 }
                        where ctran.TroCodigo =="15" && dtran.ProCodcor != "3473"
                        group new { ctran, dtran } by new
                        {
                            ctran.Lote,
                            ctran.TraSecuencial,
                            //dtran.ProCodcor,
                            //dtran.TalCodigo
                        } into g select new
                        {
                            g.Key.Lote,
                            g.Key.TraSecuencial,
                            //g.Key.ProCodcor,
                            //g.Key.TalCodigo,
                            Peso = g.Sum(x => x.dtran.Peso)
                        }
                    ) on new { A = lot.LotRloNumero.ToString(), B = lot.LotNumero/*, C = lid.LidCodtal.ToString()*/ } 
                    equals new { A = pso.Lote, B = (decimal)pso.TraSecuencial/*, C = pso.TalCodigo*/ } into psoGroup
                    from pso in psoGroup.DefaultIfEmpty()

                    join dret in (
                                from dret in objContext.TbDetalleRetractilado.AsNoTracking()
                                join pro in objContext.TbProduc.AsNoTracking() on dret.CodProd equals pro.ProCodcor
                                join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                                join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                                where dret.Cajas > 0
                                group new { med, emb, dret } by new
                                {
                                    dret.CodProd,
                                    dret.Lote,
                                    dret.CodTal
                                } into d
                                select new
                                {
                                    d.Key.CodProd,
                                    d.Key.Lote,
                                    d.Key.CodTal,
                                    LbsCajasRetra = d.Sum(x => (decimal?)((double)(x.dret.CajasRetra ?? 0m) * x.emb.EmbPeso  * x.med.MedFactor )) ?? 0m
                                }
                                        ) 
                                        on new { A = lot.LotRloNumero.ToString(), B = lid.LidProduc, C = lid.LidCodtal } 
                                        equals new { A = dret.Lote, B = dret.CodProd, C = (int)dret.CodTal } into dretGroup
                    from dret in dretGroup.DefaultIfEmpty()
                        //Revisar
                    join pcab in objContext.TbLiqvalPremiosCab.AsNoTracking() on lid.LidLote equals pcab.LipNoliqu into pcabGroup
                    from pcab in pcabGroup.DefaultIfEmpty()
                    join pdet in objContext.TbLiqvalPremiosDet.AsNoTracking() on new
                    { A = pcab.LipId, B = lid.LidLote, C = lid.LidProduc, D = lid.LidCodtal } equals new
                    { A = pdet.LidCabId, B = (long)pdet.LidNoliqu, C = pdet.LidProducto, D = pdet.LidTalla } into pdetGroup
                    from pdet in pdetGroup.DefaultIfEmpty()
                    join cert in objContext.VwCatalogo.AsNoTracking() on new
                    { A = "PREMIO", B = pcab.LipTipoPremio.ToString() } equals new { A = cert.CatCodigo, B = cert.DetCodigo } into certGroup
                    from cert in certGroup.DefaultIfEmpty()
                    join tid in objContext.TbTipdec.AsNoTracking() on new
                    { A = pro.ProDecora, B = "AC" } equals new { A = tid.TidCodigo, B = tid.TidEstado }
                    join rta in objContext.TbRetalm.AsNoTracking() on new
                    { A = pro.ProRetrac, B = "AC" } equals new { A = rta.RtaCodigo, B = rta.RtaEstado }


                    where lot.LotFecha >= dtFeInicio
                       && lot.LotFecha <= dtFeFin
                       && lot.LotEstado != "AN"
                       && liq.LiqEstado == "AC"
                    group new { lid, emb, med } by new
                    {
                        tot.TipCodigo,
                        tot.TipDescri,
                        lot.LotTipo,
                        lot.LotCopack,
                        //tpc.Descripcion,
                        lot.LotNumero,
                        lot.LotRloNumero,
                        lot.LotRecibi,
                        lot.LotProces,
                        lot.LotValagr,
                        lot.LotFecha,
                        lid.LidCodtal,
                        lid.LidProduc,
                        pro.ProDesesp,
                        pro.ProClas02,
                        tal.TalDescri,
                        bod.BodCodigo,
                        bod.BodEsBrine,
                        proc.PaDescri,
                        TipoProducto =
                            pro.ProClas01 == "CC" && pro.ProClas05 == "EN" ? "ENTERO" :
                            pro.ProClas01 == "SC" && pro.ProClas05 == "SH" ? "COLA" :
                            pro.ProClas01 == "CC" && pro.ProClas05 == "VA" ? "ENTERO VALOR AGREGADO" :
                            pro.ProClas01 == "SC" && pro.ProClas05 == "VA" ? "COLA VALOR AGREGADO" : "",
                        pro.ProClas03,
                        detPres.DprDescri,
                        hidra.RecNombre,
                        hidra.RecTipo,
                        hidra.CthSallbs,
                        hidra.CthHidlbs,
                        hidra.RtCodItem,
                        hidra.recSal,
                        hidra.recHid,
                        Peso = new List<string> { "5909", "5908", "5907", "5911", "5912" }.Contains(lid.LidProduc) ? 0 :
                pso.Peso,
                        LbsCajasRetra = dret.LbsCajasRetra != null ? dret.LbsCajasRetra : 0m,
                        Pelado = pso.Peso != null ? true : false,
                        Decorado = lot.LotTiplot == "RLL" && pro.ProClas03 == "PT" ? true : false,
                        Retractilado = dret.LbsCajasRetra != null ? true : false,
                        Certificado = pdet.LidPrecio != null ?
                            cert.DetDescripcion : "",
                        LidPremio = pdet.LidPrecio,
                        emb.EmbCodigo,
                        med.MedCodigo,
                        tid.TidCodigo,
                        rta.RtaCodigo,
                        blDescabezado = pro.ProClas01 == "SC" && pro.ProClas05 != "VA" ? true : false,
                        pro.ProCongela

                    } into g
                    select new
                    {
                        g.Key.TipCodigo,
                        g.Key.TipDescri,
                        g.Key.LotTipo,
                        g.Key.LotCopack,
                        //TipoCopacking = g.Key.Descripcion,
                        g.Key.LotNumero,
                        ClaseProd = g.Key.ProClas02,
                        g.Key.ProClas03,
                        LoteUnificado = g.Key.LotRloNumero,
                        PlantaProceso = g.Key.PaDescri,
                        g.Key.TipoProducto,
                        CongelamientoProducto = g.Key.DprDescri,
                        g.Key.LotRecibi,
                        g.Key.LotProces,
                        g.Key.LotValagr,
                        g.Key.Peso,
                        g.Key.LbsCajasRetra,
                        g.Key.LotFecha,
                        g.Key.LidProduc,
                        g.Key.ProDesesp,
                        g.Key.TalDescri,
                        g.Key.LidCodtal,
                        Procesado = Math.Round(g.Sum(x => ((double)x.lid.LidCanenv * x.emb.EmbPeso * x.med.MedFactor)), 2),
                        g.Key.RecNombre,
                        g.Key.RecTipo,
                        g.Key.CthSallbs,
                        g.Key.CthHidlbs,
                        g.Key.recSal,
                        g.Key.recHid,
                        g.Key.Pelado,
                        g.Key.Decorado,
                        g.Key.Retractilado,
                        g.Key.Certificado,
                        g.Key.LidPremio,
                        g.Key.RtCodItem,
                        g.Key.BodCodigo,
                        g.Key.EmbCodigo,
                        g.Key.MedCodigo,
                        CantCajas = Math.Round(g.Sum(x => (double)x.lid.LidCanenv / x.emb.EmbCantid),2),
                        g.Key.BodEsBrine,
                        g.Key.TidCodigo,
                        g.Key.RtaCodigo,
                        g.Key.blDescabezado,
                        g.Key.ProCongela

                    }).ToListAsync();



                var lstLibrasProcesadasUni =
                    (from lstUni in lstLibrasProcesadas
                     group new { lstUni } by new
                     {
                         lstUni.TipDescri,
                         lstUni.LotNumero,
                         lstUni.LotTipo,
                         TipoCopacking = dictTipoCopacking.GetValueOrDefault(lstUni.LotCopack, ""),
                         lstUni.LoteUnificado,
                         lstUni.LotProces,
                         lstUni.LotFecha
                     } into g
                     select new
                     {
                         g.Key.TipDescri,
                         g.Key.TipoCopacking,
                         g.Key.LotNumero,
                         g.Key.LotTipo,
                         g.Key.LoteUnificado,
                         g.Key.LotProces,
                         g.Key.LotFecha,
                         Procesado = g.Sum(x => x.lstUni.Procesado),
                         recibi = g.Min(x => x.lstUni.LotRecibi)
                     }
                     ).ToList();

                _objLogger.LogInformation($"\nCantidad inicial de lineas lstLibrasProcesadasTemp: {lstLibrasProcesadas.Count}" +
                    $"\nCantidad de datos agrupados lstLibrasProcesadas: {lstLibrasProcesadas.Count}"
                    );

                List<decimal> lstLotNumero = lstLibrasProcesadasUni
                    .Select(x =>x.LotNumero)
                    .Distinct()
                    .ToList();
                var lstLibrasRecibidas =
                        await objContext.TbLototr.AsNoTracking()
                        .SelectManyBatchAsync(
                            keySelector: lot => lot.LotNumero,
                            values: lstLotNumero,
                            selector: filtLot =>
                                from lot in filtLot
                                join rld in objContext.TbRelode.AsNoTracking()
                                   on new { A = lot.LotNumero, B = lot.LotTipo }
                                   equals new { A = rld.RldNumero, B = rld.RldTipo }
                                join rlo in objContext.TbReglot.AsNoTracking()
                                   on rld.RldLote equals rlo.RloNumero
                                   into rloGroup
                                from rlo in rloGroup.DefaultIfEmpty()
                                join pro in objContext.TbProduc.AsNoTracking() on rld.RldProcod equals pro.ProCodcor
                                join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                                join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                                join tal in objContext.TbTallas.AsNoTracking() on rld.RldCodtal equals tal.TalCodigo
                                group new { rld, emb, med, rlo } by new
                                {
                                    lot.LotTiplot,
                                    pro.ProClas02,
                                    TipDescri = "", //lp.TipDescri,
                                    lot.LotTipo, //lp.LotTipo,
                                    lot.LotCopack,
                                    lot.LotNumero,
                                    LoteUnificado = lot.LotRloNumero, //lp.LoteUnificado,
                                    lot.LotProces, //lp.LotProces,
                                    lot.LotFecha, //lp.LotFecha,
                                    rld.RldLote,
                                    FechaLote = rlo.RloFecha.Value.Date,
                                    rld.RldProcod,
                                    pro.ProDesesp,
                                    TalCodigo = rld.RldCodtal,
                                    tal.TalDescri
                                } into g
                                select new
                                {
                                    TipCodigo = g.Key.LotTiplot,
                                    TipDescri = g.Key.TipDescri,
                                    LotTipo = g.Key.LotTipo,
                                    TipoCopacking = "",
                                    CodCopacking = g.Key.LotCopack,
                                    LotNumero = g.Key.LotNumero,
                                    LoteUnificado = g.Key.LoteUnificado,
                                    ClaseProd = g.Key.ProClas02,
                                    PlantaProceso = "",
                                    TipoProducto = "",
                                    CongelamientoProducto = "",
                                    LoteOrigen = g.Key.RldLote,
                                    RloFecha = g.Key.FechaLote,
                                    Recibido = 0.0m,
                                    LotProces = g.Key.LotProces,
                                    LotFecha = g.Key.LotFecha,
                                    Produc = g.Key.RldProcod,
                                    DescriProduc = g.Key.ProDesesp,
                                    TalDescri = g.Key.TalDescri,
                                    Libras = Math.Truncate(g.Sum(x => (double)x.rld.RldCantid * x.emb.EmbPeso * x.med.MedFactor) * 100) / 100,
                                    Agrupacion = "1. RECIBIDO",
                                    g.Key.TalCodigo
                                }
                        );

                var dictTipDescri = lstLibrasProcesadasUni
                    .GroupBy(x => x.LotNumero)
                    .ToDictionary(g => g.Key, g => g.First().TipDescri);


                var dictMinRecibi = lstLibrasProcesadasUni
                    .GroupBy(x => x.LotNumero)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Min(x => x.recibi!.Value)
                    );
                List<MatPrimaReproceso> lstTotalLibrasRecProc = (
                    from lstRec in lstLibrasRecibidas
                    select new MatPrimaReproceso
                                      (
                                        lstRec.TipCodigo,
                                        dictTipDescri.GetValueOrDefault(lstRec.LotNumero, ""),
                        //lstLibrasProcesadasUni.Select(tpc => new { tpc.LotNumero, tpc.TipDescri })
                        //                .First(tpc => tpc.LotNumero == lstRec.LotNumero).TipDescri,
                                        lstRec.ClaseProd,
                                        lstRec.LotTipo,
                                        lstRec.CodCopacking,
                                         dictTipoCopacking.GetValueOrDefault(lstRec.CodCopacking, ""),
                                        //tmpTipoCopacking.Select(tpc => new { tpc.Codigo, tpc.Descripcion })
                                        //.First(tpc => tpc.Codigo == lstRec.CodCopacking).Descripcion,
                                          lstRec.LotNumero,
                                         lstRec.LoteUnificado,
                                          "",
                                          "",
                                          "",
                                          lstRec.LoteOrigen,
                                          lstRec.RloFecha,
                                          dictMinRecibi.GetValueOrDefault(lstRec.LotNumero, 0),
                                          //(decimal)lstLibrasProcesadasUni.Where(tpc => tpc.LotNumero == lstRec.LotNumero)
                                          //.Select(tpc => tpc.recibi).Min(obj => obj!.Value),
                                          lstRec.LotProces,
                                          lstRec.LotFecha,
                                          lstRec.Produc,
                                          lstRec.DescriProduc,
                                          lstRec.TalDescri,
                                          lstRec.Libras,
                                          lstRec.Agrupacion,
                                          lstRec.TalCodigo

                                      )
                      ).ToList().Concat(
                    from lstProc in lstLibrasProcesadas
                    select new MatPrimaReproceso
                    (
                        lstProc.TipCodigo,
                        lstProc.TipDescri,
                        lstProc.ClaseProd,
                        lstProc.LotTipo,
                        lstProc.LotCopack,
                        dictTipoCopacking.GetValueOrDefault(lstProc.LotCopack, ""),
                        //tmpTipoCopacking.Select(tpc => new { tpc.Codigo, tpc.Descripcion })
                        //    .First(tpc => tpc.Codigo == lstProc.LotCopack).Descripcion,
                        lstProc.LotNumero,
                        lstProc.LoteUnificado,
                        lstProc.PlantaProceso,
                        lstProc.TipoProducto,
                        lstProc.CongelamientoProducto,
                        0,
                        null,
                        0,
                        0,
                        lstProc.LotFecha,
                        lstProc.LidProduc,
                        lstProc.ProDesesp,
                        lstProc.TalDescri,
                        lstProc.Procesado,
                        "2. PROCESADO",
                        lstProc.LidCodtal,
                        lstProc.RecTipo,
                        lstProc.RecNombre,
                        lstProc.Pelado,
                        lstProc.Decorado,
                        lstProc.Retractilado,
                        lstProc.ProClas03,
                        lstProc.Certificado,
                        lstProc.LidPremio,
                        lstProc.CthHidlbs,
                        lstProc.CthSallbs,
                        lstProc.RtCodItem,
                        lstProc.LbsCajasRetra,
                        lstProc.Peso,
                        lstProc.BodCodigo,
                        lstProc.EmbCodigo,
                        lstProc.MedCodigo,
                        lstProc.CantCajas,
                        lstProc.BodEsBrine, 
                        lstProc.RtaCodigo, 
                        lstProc.TidCodigo, 
                        lstProc.blDescabezado, 
                        lstProc.ProCongela,
                        lstProc.recSal,
                        lstProc.recHid
                    )
                    ).ToList();
                _objLogger.LogInformation($"\nCantidad final de lineas lstTotalLibrasRecProc: {lstTotalLibrasRecProc.Count}" +
                    $"\nCantidad de libras recibidas lstLibrasRecibidas: {lstLibrasRecibidas.Count}"
                    );
                return lstTotalLibrasRecProc;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en ReporteReproPlanRecibProc: {objException.Message}");
                throw;
            }

        }


        public async Task<ILookup<(string Producto, short Talla), decimal>> ObtenerMatPrimSaldo(List<string> lstCodProd)
        {
            try
            {
                var lstPreciosInv = await _objCostosContext.TbMateriaPrimaSaldo.AsNoTracking()
                                                .Where(mps => lstCodProd.Contains(mps.MpsProCodcor)
                                                           && mps.MpsTipo == "I")
                                                .GroupBy(mps => new { mps.MpsProCodcor, mps.MpsTalCodigo })
                                                .Select(g => new
                                                {
                                                    g.Key.MpsProCodcor,
                                                    g.Key.MpsTalCodigo,
                                                    TotalLibras = g.Sum(x => Math.Abs(x.MpsLibras)),
                                                    TotalCosto = g.Sum(x => Math.Abs(x.MpsCostoTotal))
                                                })
                                                .Where(x => x.TotalLibras > 0)
                                                .Select(x => new
                                                {
                                                    Producto = x.MpsProCodcor,
                                                    Talla = x.MpsTalCodigo,
                                                    PrecioPromedio = x.TotalCosto / x.TotalLibras
                                                })
                                                .ToListAsync();
               var lookupPrecios = lstPreciosInv.ToLookup(
                    key => (key.Producto, (short)key.Talla),
                    val => val.PrecioPromedio
                );
                return lookupPrecios;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en ObtenerMatPrimSaldo: {objException.Message}");
                throw;
            }
        }

        public async Task<ILookup<(int LiqLote,string Producto, short Talla), decimal>> ObtenerMatPrimSaldo(List<int> lstLiqLote)
        {
            try
            {
                var lstPreciosInv = await _objCostosContext.TbMateriaPrimaSaldo.AsNoTracking()
                                                .SelectManyBatchAsync
                                                (
                                                 keySelector: mps => mps.MpsRloNumero,
                                                        values: lstLiqLote,
                                                        selector: filtLot =>
                                                            from lot in filtLot
                                                            where lot.MpsTipo == "I"
                                                            select new
                                                            {
                                                                Lote = lot.MpsRloNumero,
                                                                Producto = lot.MpsProCodcor,
                                                                Talla = lot.MpsTalCodigo,
                                                                TotalLibras = lot.MpsLibras,
                                                                TotalCosto = lot.MpsCostoTotal,
                                                                CostUni = lot.MpsCostoUnitario
                                                            }
                                                );
                var lookupPrecios = lstPreciosInv
                                                .GroupBy(mps => new { mps.Lote, mps.Producto, mps.Talla })
                                                .Select(g => new
                                                {
                                                    g.Key.Lote,
                                                    g.Key.Producto,
                                                    g.Key.Talla,
                                                    CostUni = g.Average(x => x.CostUni),
                                                    TotalLibras = g.Sum(x => x.TotalLibras),
                                                    TotalCosto = g.Sum(x => x.TotalCosto)
                                                })
                                                .Where(x => x.TotalLibras > 0)
                    .ToLookup(
                    key => (key.Lote,key.Producto, (short)key.Talla),
                    val => val.CostUni///val.TotalCosto / val.TotalLibras 
                );
                return lookupPrecios;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en ObtenerMatPrimSaldo: {objException.Message}");
                throw;
            }
        }


        
        public async Task<List<CostoMovArtDto>> CostoUltMovXItemCod(List<string> lstItemCod, DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {

                using var objSongContext = await _objSongFactory.CreateDbContextAsync();
                objSongContext.Database.SetCommandTimeout(180);
                var lstItemCodCosto = await objSongContext.TbDetmov2.AsNoTracking()
                                              .SelectManyBatchAsync(
                                                  keySelector: det => det.DetCodart, // Filtramos directamente por código de artículo
                                                  values: lstItemCod,
                                                  selector: filteredDet =>
                                                      from det in filteredDet
                                                      join cab in objSongContext.TbCabmov2.AsNoTracking()
                                                          on det.DetCabece equals cab.MovNummov
                                                      join bod in objSongContext.TbBodega.AsNoTracking()
                                                          on det.DetBodega equals bod.BodCodigo
                                                      where  new List<string>(){ "EGR", "RI"}.Contains(cab.MovTipo) // == "EGR"
                                                       //&& (bod.BodConsiderarCostoMaterial ?? "") == "S"
                                                       && cab.MovSubcen == "030003"
                                                       && cab.MovFecha.CompareTo(dtFechaInicio.ToString("yyyy/MM/dd")) >= 0
                                                       && cab.MovFecha.CompareTo(dtFechaFin.ToString("yyyy/MM/dd")) <= 0
                                                       group new 
                                                       { det, cab } 
                                                       by new
                                                       {
                                                           det.DetCodart,
                                                           cab.MovFecha,
                                                           det.DetNomart,
                                                           det.DetPreuni,
                                                           cab.MovTipo
                                                       }
                                                        into g
                                                      select new CostoMovArtDto
                                                      (
                                                          g.Key.DetCodart,
                                                          g.Key.DetNomart,
                                                          g.Key.MovFecha,
                                                          g.Sum(x => x.det.DetCanti * (g.Key.MovTipo == "RI" ? -1 : 1)),
                                                          g.Sum(x => x.det.DetSubtot * (g.Key.MovTipo == "RI" ? -1 : 1))
                                                          
                                                      )
                                              );
                List<CostoMovArtDto> lstCostoPromedio = (
                                from x in lstItemCodCosto
                                group x by x.intItemCod into g
                                let totalCantidad = (decimal)g.Sum(i => i.dcCantidad)
                                let totalSubtotal = (decimal)g.Sum(i => i.dcConsumoTotal)
                                select new CostoMovArtDto
                                (
                                    g.Key,                                     // intItemCod
                                    g.Max(i => i.strItemDescripcion.Trim()),          // 
                                    g.Max(i => i.strMovFecha),                 // Fecha más reciente
                                    totalCantidad,
                                    totalSubtotal,                         // Cantidad Neta
                                    totalCantidad != 0
                                        ? Math.Round(totalSubtotal / totalCantidad, 4)
                                        : 0                                    // Costo Promedio
                                )
                            ).ToList();
                return lstCostoPromedio;

            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en CostoUltMovXItemCod: {objException.Message}");
                throw;
            }
        }

        public async Task<List<PrecioFrsXMov>> ObtenerPrecioFrsSinTallaXMovCam(List<long> lstLiqLotes/*, bool blUniCola = false*/)
        {
            List<int> lstCodProdFrsSinTalla = new List<int>() { 320, 321 };
            //List<string> lstTipoTransacNotIn = new List<string>() { "EX","TB","01", "CAL","EXA","DES","DSR","LB","TM","CNE","CNEI","CC","ECP","EMU","EOB","EPR","MBSI","SEL","EXP","VAG","LB","NA","PD","DB","ETM","DD"};
            try
            {

                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                using var transaction = await objContext.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadUncommitted
                    );
                List<PrecioFrsXMov> lstPrecioFrsMovCam = await
                        //from trc in  TbTracamAuto
                        objContext.TbTracadAuto.AsNoTracking().SelectManyBatchAsync
                        (
                             keySelector: lot => lot.TcdLote,
                            values: lstLiqLotes,
                            selector: filtLot =>
                            from tcd in filtLot
                            join trc in objContext.TbTracamAuto.AsNoTracking() on tcd.TcdNumero equals trc.TrcNumsec
                            join lid in objContext.TbLiqvad.AsNoTracking() on new
                            { A = tcd.TcdLote, B = tcd.TcdCodtal } equals new
                            { A = (long)lid.LidNoliqu, B = lid.LidCodtal }
                            join pro in objContext.TbProduc.AsNoTracking() on lid.LidCodigo equals pro.ProCodcor
                            join emb in objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                            join med in objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                            join tal in objContext.TbTallas.AsNoTracking() on lid.LidCodtal equals tal.TalCodigo
                            join trs in objContext.TbTransa.AsNoTracking() on new
                            { A = trc.TrcTipo, B = trc.TrcIngegr } equals new
                            { A = trs.TrsCodigo, B = trs.TrsTipo }
                            where //lstLiqLotes.Contains(tcd.TcdLote) && 
                            trc.TrcEstado == "ac" && trc.TrcIngegr == "E" 
                            //&& trc.TrcTipo == "UNI" 
                            && !new List<string> { "EX","TB","01", "CAL","EXA","DES","DSR","LB","TM","CNE","CNEI","CC",
                                "ECP","EMU","EOB","EPR","MBSI","SEL","EXP","VAG","LB","NA","PD","ETM","DD" }
                            .Contains(trc.TrcTipo)
                            //trc.TrcTipo != "EX"// && tcd.TcdCodtal == intCodtal
                            group new { lid, tcd, med, emb } by new
                            {
                                tcd.TcdLote,
                                trc.TrcTipo,
                                pro.ProClasePago,
                                pro.ProClas01,
                                pro.ProClas05,
                                tal.TalDescri,
                                pro.ProCodcor,
                                pro.ProDesesp,
                                tcd.TcdCodtal,


                            } into g
                            select new PrecioFrsXMov(
                                g.Key.TcdLote,
                                g.Key.TrcTipo,
                                g.Key.ProClasePago,
                                g.Key.ProClas01,
                                g.Key.ProClas05,
                                g.Key.TalDescri,
                                g.Key.ProCodcor,
                                g.Key.ProDesesp,
                                g.Key.TcdCodtal,
                                g.Average(x => (double)x.lid.LidPrecio),
                                g.Sum(x => (double)x.tcd.TcdCantid * x.emb.EmbPeso * x.med.MedFactor)
                            )
                            ) ;
                return /*blUniCola ? lstPrecioFrsMovCam.Where(obj => obj.strTrcTipo == "UNI").ToList() :*/ lstPrecioFrsMovCam;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en ObtenerPrecioFrsSinTallaXMovCam: {objException.Message}");
                throw;
            }
        }

        public async Task<List<SaldoBodegaDto>>  ReporteSaldoInventario(DateOnly dtFechaCorte)
        {
            try
            {
                _objContext.Database.SetCommandTimeout(360);
                var objParamFeCorte = new SqlParameter("@pfechaCorte", SqlDbType.Char, 10) { Value = dtFechaCorte.ToString("yyyy/MM/dd") };
                List<SaldoBodegaDto> lstSaldoBodega = await _objContext
                                 .Set<SaldoBodegaDto>()
                                 .FromSqlRaw("EXEC spr_SaldoInventario @pfechaCorte", objParamFeCorte)
                                 .AsNoTracking()
                                 .ToListAsync();
                return lstSaldoBodega;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"Error en ReporteCommatPri: {objException.Message}");
                throw;
            }
        }
        #endregion


        #region Saldo materia Prima Excel
        public async Task ObtenerDatosProd(List<InvValDataDto> lstInvVal, string dtFechaCorte)
        {
            try
            {
                List<string> lstCodProducto = lstInvVal.Select(obj => obj.strProd.Trim()).Distinct().ToList();
                List<string> lstTalDescReempla = lstInvVal.Select(obj => obj.strNomTal.Trim().Replace('-', '/')).Distinct().ToList();
                List<string> lstTalDescOri = lstInvVal.Select(obj => obj.strNomTal.Trim()).Distinct().ToList();
                lstTalDescReempla = lstTalDescReempla.Select(obj => obj.Replace("MEDIUN", "MEDIUM")).ToList();
                lstTalDescOri.AddRange(lstTalDescReempla);
                var lstInfoProd = await _objContext.TbProduc
                                        .AsNoTracking()
                                        .SelectManyBatchAsync
                                        (
                    keySelector: pro => pro.ProCodcor,
                    values: lstCodProducto,
                    selector: filter =>
                        from pro in filter
                        join ftc in _objContext.TbEmbalaFichaTecnica.AsNoTracking() on pro.ProCodcor equals ftc.EftId.ToString()
                        join col in _objContext.TbColor.AsNoTracking() on pro.ProClas05 equals col.ColCodigo
                        join med in _objContext.TbMedida.AsNoTracking() on pro.ProUnimed equals med.MedCodigo
                        join emb in _objContext.TbEmbala.AsNoTracking() on pro.ProEmbala equals emb.EmbCodigo
                        where lstCodProducto.Contains(pro.ProCodcor) && pro.ProClas03 == "PT" && ftc.EftCantidad != 0
                        select new
                        {
                            prodCod = pro.ProCodcor,
                            empCodigo = ftc.EftId,
                            tipoLote = "REP",
                            medCodigo = med.MedCodigo,
                            embCodigo = emb.EmbCodigo,
                        }
                    );
                var lstBoditeXFecha = await (
                        from bit in _objContext.TbBoditesaldMes.AsNoTracking()
                        where bit.BitFecha == dtFechaCorte
                        select new 
                        {
                            TalCodigo = bit.BitCodtal,
                            bit.BitCodbod,
                            bit.BitLote,
                            bit.BitProduc,
                            BitLibras= Math.Round((decimal)bit.BitLibras,2),
                            bit.BitSdocaja
                        }
                        ).ToListAsync();
                var lookupBodite = lstBoditeXFecha.ToLookup(x => (x.BitProduc.Trim(),(int) x.BitLote, x.BitCodbod.Trim(), x.BitLibras/*, x.BitSdocaja, x.BitLibras*/));
                var lstCodTalla = await _objContext.TbTallas.AsNoTracking()
                    .SelectManyBatchAsync(
                    keySelector: tal => tal.TalDescri,
                    values: lstTalDescOri, 
                    selector: filter =>
                        from tal in filter
                        where tal.TalEstado == "AC"
                        select new
                        {
                            tal.TalCodigo,
                            tal.TalTipo,
                            TalDescri = tal.TalDescri.Trim().Replace('/', '-')
                        }
                    );

                var dicBodite = new Dictionary<(string Prod, int Lote, string Bodega), Queue<short>>();
                var dicTallaAsignada = new Dictionary<(string Prod, int Lote, string CodigoTalla), short>();
                var gruposExcel = lstInvVal
                        .GroupBy(x => (
                            Prod: x.strProd.Trim(),
                            Lote: x.intLote,
                            CodigoTalla: x.strCodigoTalla.Trim()
                        ));

                foreach (var grupo in gruposExcel)
                {
                    short tallaEncontrada = 0;

                    // Intentar encontrar la talla en Bodite probando cada fila del grupo
                    foreach (var fila in grupo)
                    {
                        var keyBodite = (
                            fila.strProd.Trim(),
                            fila.intLote,
                            fila.strCam.Trim()
                        );

                        if (dicBodite.TryGetValue(keyBodite, out var cola) && cola.Count > 0)
                        {
                            tallaEncontrada = cola.Dequeue(); // ← consume UNA sola vez por grupo
                            break;
                        }
                    }

                    // Fallback si Bodite no resolvió
                    if (tallaEncontrada == 0)
                    {
                        var primeraFila = grupo.First();
                        var objFallback = lstCodTalla.FirstOrDefault(t =>
                            t.TalDescri == primeraFila.strNomTal &&
                            t.TalTipo == primeraFila.strClas01);

                        if (objFallback != null)
                            tallaEncontrada = (short)objFallback.TalCodigo;
                        //else
                        //    _objLogger.LogInformation(
                        //        $"Fallback fallido - Lote {primeraFila.intLote} " +
                        //        $"CodigoTalla: {primeraFila.strCodigoTalla} " +
                        //        $"Prod: {primeraFila.strProd}");
                    }

                    dicTallaAsignada[grupo.Key] = tallaEncontrada;
                }

                foreach (var bit in lstBoditeXFecha)
                {
                    var key = (bit.BitProduc.Trim(), (int)bit.BitLote, bit.BitCodbod.Trim());

                    if (!dicBodite.ContainsKey(key))
                    {
                        dicBodite[key] = new Queue<short>();
                    }
                    // Encolamos el código de talla (asegurando el tipo short)
                    dicBodite[key].Enqueue((short)bit.TalCodigo);
                }

                foreach (var objInvVal in lstInvVal)
                {
                    // Asignación de InfoProd (sin cambios)
                    var objInfoProd = lstInfoProd.FirstOrDefault(obj => obj.prodCod == objInvVal.strProd);
                    if (objInfoProd != null)
                    {
                        objInvVal.stEmpCodigo = (short)objInfoProd.empCodigo;
                        objInvVal.strTipoLote = objInfoProd.tipoLote;
                        objInvVal.btMedCodigo = (byte)objInfoProd.medCodigo;
                        objInvVal.strEmbCodigo = objInfoProd.embCodigo;
                    }
                    else
                    {
                        objInvVal.stEmpCodigo = 0;
                        objInvVal.strTipoLote = "PFR";
                        objInvVal.btMedCodigo = 0;
                        objInvVal.strEmbCodigo = "";
                    }

                    // Asignación de talla desde el diccionario pre-calculado
                    var keyGrupo = (
                        Prod: objInvVal.strProd.Trim(),
                        Lote: objInvVal.intLote,
                        CodigoTalla: objInvVal.strCodigoTalla.Trim()
                    );

                    objInvVal.stTalCodigo = dicTallaAsignada.TryGetValue(keyGrupo, out var talla)
                        ? talla
                        : (short)0;
                    short? stTalCodigo = null;
                    if (objInvVal.strNomTal.Replace('/', '-') == "21-25")
                    {
                        stTalCodigo = (short?)lstCodTalla.FirstOrDefault(obj => obj.TalDescri.Replace('-', '/') == "21/25" && obj.TalTipo.Trim() == objInvVal.strClas01.Trim())?.TalCodigo;
                    }
                    else if (objInvVal.strNomTal.Replace('N', 'M') == "MEDIUM")
                    {
                        stTalCodigo = (short?)lstCodTalla.FirstOrDefault(obj => obj.TalDescri.Replace('N', 'M') == "MEDIUM" && obj.TalTipo.Trim() == objInvVal.strClas01.Trim())?.TalCodigo;
                    }

                    if (stTalCodigo != null)
                    {
                        objInvVal.stTalCodigo = (short)stTalCodigo;
                        //_objLogger.LogInformation(
                        //    $"Fallback {objInvVal.strNomTal} - Lote {objInvVal.intLote} " +
                        //    $"CodigoTalla: {objInvVal.strNomTal} " +
                        //    $"CodigoTalla: {objInvVal.stTalCodigo} " +
                        //    $"Prod: {objInvVal.strProd}");
                    }
                }

            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerDatosProd] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task CrearSaldoMatPrimExcel(List<InvValDataDto> lstInvVal)
        {
            try
            {

                List<TbMateriaPrimaSaldo> lstMatPrimaFechaCorte = lstInvVal
                    .Select(objInvVal => new TbMateriaPrimaSaldo
                    {
                        MpsEmpCodigo =(short) objInvVal.stEmpCodigo,
                        MpsTipo = "I",
                        MpsTipoLote = objInvVal.strTipoLote,
                        MpsFecha = objInvVal.dtFecha,
                        MpsBodCodigo = objInvVal.strCam,
                        MpsRloNumero = objInvVal.intLote,
                        MpsTalCodigo = (short)objInvVal.stTalCodigo,
                        MpsMedCodigo = (byte)objInvVal.btMedCodigo,
                        MpsEmbCodigo = objInvVal.strEmbCodigo,
                        MpsProCodcor = objInvVal.strProd,
                        MpsMasters = (decimal)objInvVal.dcMaster,
                        MpsLibras = (decimal)objInvVal.dcLibras,
                        MpsCostoUnitario = (decimal)objInvVal.dcCosto,
                        MpsCostoTotal = (decimal)objInvVal.dcTotal,
                        MpsEstado = "AC",
                        MpsUsuarioCrea = "ADMINISTRA"
                    }).ToList();
                // 2. Inserción Eficiente
                //var lstValoresVacios = lstMatPrimaFechaCorte.Where(obj => obj.MpsTalCodigo == 0).ToList();
                if (lstMatPrimaFechaCorte.Any())
                {
                    // Agregamos toda la lista al contexto
                    await _objCostosContext.TbMateriaPrimaSaldo.AddRangeAsync(lstMatPrimaFechaCorte);

                    // Guardamos todo en un solo bloque de transacciones
                    await _objCostosContext.SaveChangesAsync();
                }

                Console.WriteLine($"Éxito: {lstMatPrimaFechaCorte.Count} registros insertados.");
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[CrearSaldoMatPrimExcel] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task CrearInvMatPrimExcel(List<InvValDataDto> lstInvVal, RequestDataDto objRequest)
        {
            List<string> lstAccionesBorrar = new List<string>() { "D", "U" };
            List<DateOnly> lstFechaCorte ;
            try
            {
                var dtUltFechaCorte = await _objCostosContext.TbMateriaPrimaSaldo
                        .Where(mps => mps.MpsTipo == "I")
                        .MaxAsync(mps => (DateOnly?)mps.MpsFechaCorte);
                lstFechaCorte = await  ConsultarFechaCorteInv();
                if (lstAccionesBorrar.Contains(objRequest.strAccion))
                {
                    var lstRegistrosExistentes = await _objCostosContext.TbMateriaPrimaSaldo
                        .Where(mps => mps.MpsFechaCorte == objRequest.dtFechaCorte && mps.MpsTipo == "I")
                        .ToListAsync();
                    if (lstRegistrosExistentes.Any())
                    {
                        _objCostosContext.TbMateriaPrimaSaldo.RemoveRange(lstRegistrosExistentes);
                        await _objCostosContext.SaveChangesAsync();
                        _objLogger.LogInformation($"Registros existentes eliminados para FechaCorte: {objRequest.dtFechaCorte}");
                    }
                }
                if (dtUltFechaCorte.HasValue && objRequest.dtFechaCorte > dtUltFechaCorte.Value)
                {
                    var lstAnteriores = await _objCostosContext.TbMateriaPrimaSaldo
                        .Where(mps => mps.MpsEstado == "AC" && mps.MpsTipo == "I")
                        .ToListAsync();

                    if (lstAnteriores.Any())
                    {
                        foreach (var reg in lstAnteriores)
                        {
                            reg.MpsEstado = "AN"; // Marcamos como Anterior
                        }
                        _objLogger.LogInformation($"Se actualizaron {lstAnteriores.Count} registros a estado 'AN' por nueva fecha de corte {objRequest.dtFechaCorte}");
                        // No guardamos cambios aquí todavía, lo haremos al final con el SaveChanges del final
                    }
                }
                List<TbMateriaPrimaSaldo> lstMatPrimaFechaCorte = lstInvVal
                    .Select(objInvVal => new TbMateriaPrimaSaldo
                    {
                        MpsEmpCodigo = (short)objInvVal.stEmpCodigo,
                        MpsTipo = "I",
                        MpsTipoLote = objInvVal.strTipoLote,
                        MpsFecha = objInvVal.dtFecha,
                        MpsFechaCorte = objRequest.dtFechaCorte,
                        MpsBodCodigo = objInvVal.strCam,
                        MpsRloNumero = objInvVal.intLote,
                        MpsTalCodigo = (short)objInvVal.stTalCodigo,
                        MpsMedCodigo = (byte)objInvVal.btMedCodigo,
                        MpsEmbCodigo = objInvVal.strEmbCodigo,
                        MpsProCodcor = objInvVal.strProd,
                        MpsMasters = (decimal)objInvVal.dcMaster,
                        MpsLibras = (decimal)objInvVal.dcLibras,
                        MpsCostoUnitario = (decimal)objInvVal.dcCosto,
                        MpsCostoTotal = (decimal)objInvVal.dcTotal,
                        MpsEstado = "AC",
                        MpsUsuarioCrea = "ADMINISTRA"
                    }).ToList();
                // 2. Inserción Eficiente
                //var lstValoresVacios = lstMatPrimaFechaCorte.Where(obj => obj.MpsTalCodigo == 0).ToList();
                if (lstMatPrimaFechaCorte.Any())
                {
                    // Agregamos toda la lista al contexto
                    await _objCostosContext.TbMateriaPrimaSaldo.AddRangeAsync(lstMatPrimaFechaCorte);

                    // Guardamos todo en un solo bloque de transacciones
                    await _objCostosContext.SaveChangesAsync();
                }

                Console.WriteLine($"Éxito: {lstMatPrimaFechaCorte.Count} registros insertados.");
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[CrearSaldoMatPrimExcel] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        #endregion

        #region Materia Prima por movimiento inventario

        public async Task ObtenerInfoMovInventario(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                await using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);

                var lstMovimientos = await ObtenerMovimientosAsync(dtFechaInicio, dtFechaFin);

                if (!lstMovimientos.Any()) return;

                await using var objCostosCtx = await _objCostosFactory.CreateDbContextAsync();
                objCostosCtx.Database.SetCommandTimeout(180);

                var lstEntidades = lstMovimientos
                    .Select(dto => MapearAEntidad(dto, dtFechaInicio))
                    .ToList();

                // Eliminamos registros previos del mismo período para re-calcular
                var lstExistentes = await objCostosCtx.TbMateriaPrimaSaldo
                    .Where(x => x.MpsFecha >= dtFechaInicio.ToDateTime(new TimeOnly(0, 0)) && x.MpsFecha <= dtFechaFin.ToDateTime(new TimeOnly(0, 0)))
                    .ToListAsync();

                if (lstExistentes.Any())
                    objCostosCtx.TbMateriaPrimaSaldo.RemoveRange(lstExistentes);

                await objCostosCtx.TbMateriaPrimaSaldo.AddRangeAsync(lstEntidades);
                await objCostosCtx.SaveChangesAsync();
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ObtenerInfoMovInventario] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public async Task<List<InventarioVal>> ConsultarInvValorizado(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            List<InventarioVal> lstSaldoInicial;
            try
            {
                await using var ctx = await _objContextFactory.CreateDbContextAsync();
                ctx.Database.SetCommandTimeout(180);
                await using var objContext = await _objCostosFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                var dtFechaAnterior = dtFechaInicio.AddDays(-1);

                lstSaldoInicial = await (
                    from mts in objContext.TbMateriaPrimaSaldo.AsNoTracking()
                    where mts.MpsTipo == "I"
                    select new InventarioVal
                    {
                        //strTipo = "I",
                        strCodTip = "IVINI",
                        //strDescripcion = "1.IN",
                        intLote = (int)mts.MpsRloNumero,
                        //strProClas01 = g.Key.ProClas01,
                        //strProClas05 = g.Key.ProClas05,
                        //strTalDescri = g.Key.TalDescri,
                        dtFechaMov = (DateTime)mts.MpsFecha,
                        stTalCodigo = (short)mts.MpsTalCodigo,
                        strProCodcor = mts.MpsProCodcor.Trim(),
                        //strProDesesp = mts.MpsProDesesp,
                        strEmbCodigo = mts.MpsEmbCodigo,
                        strCodBod = mts.MpsBodCodigo,
                        intMedCodigo = (int)mts.MpsMedCodigo,
                        dcLibras = (decimal)mts.MpsLibras,
                        dcMasters = (decimal)mts.MpsMasters,
                        dcCostoTot = (decimal)mts.MpsCostoTotal,
                        dcCostoUnit = (decimal)mts.MpsCostoUnitario
                    }
                    ).ToListAsync();

                List<string> lstProdCod = lstSaldoInicial.Select(obj => obj.strProCodcor).Distinct().ToList();
                List<decimal> lstCodTal = lstSaldoInicial.Select(obj => Convert.ToDecimal(obj.stTalCodigo)).Distinct().ToList();
                var lstInfoProdCod = await ctx.TbProduc.AsNoTracking()
                            .SelectManyBatchAsync(
                                 keySelector: pro => pro.ProCodcor,
                                    values: lstProdCod,
                                    selector: filtered =>
                                    from pro in filtered
                                    join pres in ctx.TbProces.AsNoTracking() on pro.ProClas06 equals pres.ProCodigo
                                    select new
                                    {
                                        ProCodcor = pro.ProCodcor.Trim(),
                                        pro.ProDesesp,
                                        pro.ProClas01,
                                        pro.ProClas02,
                                        pro.ProClas05,
                                        pres.ProCodigo,
                                        pres.ProDescri
                                    }
                                );
                var lstInfoCodTal = await ctx.TbTallas.AsNoTracking()
                            .SelectManyBatchAsync(
                                 keySelector: tal => tal.TalCodigo,
                                    values: lstCodTal,
                                    selector: filtered =>
                                    from tal in filtered
                                    select new
                                    {
                                        tal.TalCodigo,
                                        tal.TalDescri
                                    }
                                );
                foreach (var sal in lstSaldoInicial)
                {
                    var objProduc = lstInfoProdCod.FirstOrDefault(obj => obj.ProCodcor == sal.strProCodcor);
                    var objTalla = lstInfoCodTal.FirstOrDefault(obj => obj.TalCodigo == (decimal)sal.stTalCodigo);
                    if (objProduc != null)
                    {
                        CtCtblXClaseTipo objKey = new CtCtblXClaseTipo(objProduc.ProClas05, objProduc.ProClas02);
                        sal.strProDesesp = objProduc.ProDesesp;
                        sal.strProClas01 = objProduc.ProClas01;
                        sal.strProClas05 = objProduc.ProClas05;
                        sal.strProdDescri = objProduc.ProDescri;
                        sal.strProCod = objProduc.ProCodigo;
                        sal.lgCuentaContable = InventarioVal._mapaDeCuentas.GetValueOrDefault(objKey, 000000);
                    }
                    if (objTalla != null)
                    {
                        sal.strTalDescri = objTalla.TalDescri;
                    }

                }
                return lstSaldoInicial;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ConsultarInvValorizado] Ocurrio un error: {objException.Message}");
                throw;
            } 
        }

        public async Task<List<DateOnly>> ConsultarFechaCorteInv()
        {
            List<DateOnly> lstFechaCorte;
            try
            {
                await using var objContext = await _objCostosFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);

                lstFechaCorte = await (
                    from mts in objContext.TbMateriaPrimaSaldo.AsNoTracking()
                    where mts.MpsTipo == "I"
                    group new { mts } by mts.MpsFechaCorte into g
                    select g.Key
                    ).ToListAsync();

                
                return lstFechaCorte;
            }
            catch (Exception objException)
            {
                _objLogger.LogError($"[MateriaPrima].[ConsultarInvValorizado] Ocurrio un error: {objException.Message}");
                throw;
            }
        }

        public  async Task<List<DiarioCosto>> ObtenerMovimientosAsync(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                await using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                // Cada llamada ya ejecuta el SQL y maneja su propio error.
                // Si un tramo falla, el log indica exactamente cuál falló.
                var lstSaldoInicial = await QrySaldoInicialCostAsync(objContext, dtFechaInicio); //await QrySaldoInicialAsync(objContext, dtFechaInicio);
                var lstLiqFresco = await QryLiqFrescoAsync(objContext, dtFechaInicio, dtFechaFin);
                var lstLiqOtrosProcesos = await QryLiqOtrosProcesosAsync(objContext, dtFechaInicio, dtFechaFin);
                var lstIngresos = await QryIngresosAsync(objContext, dtFechaInicio, dtFechaFin);
                var lstEgresos = await QryEgresosAsync(objContext, dtFechaInicio, dtFechaFin);
                var lstEgresosExp = await QryEgresosExportacionAsync(objContext, dtFechaInicio, dtFechaFin);
                var lstConsumosOtrosProcesos = await QryConsumosOtrosProcesosAsync(objContext, dtFechaInicio, dtFechaFin);

                return lstSaldoInicial
                    .Concat(lstLiqFresco)
                    .Concat(lstLiqOtrosProcesos)
                    .Concat(lstIngresos)
                    .Concat(lstEgresos)
                    .Concat(lstEgresosExp)
                    .Concat(lstConsumosOtrosProcesos)
                    //.OrderBy(x => x.intTipoMovInv)
                    //.ThenBy(x => x.intLote)
                    //.ThenBy(x=> x.dtFechaMov)
                    .Where(x => !new List<string>() {  "4405", "4406" }.Contains(x.strProCodcor))
                    .ToList();
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex, $"[MateriaPrima].[ObtenerMovimientosAsync] Error al obtener movimientos entre {dtFechaInicio} y {dtFechaFin}: {ex.Message}");
                throw;
            }
        }


        // ─────────────────────────────────────────────────────────────────────────
        //  TRAMO 1 — SALDO INICIAL (fecha = dtFechaInicio - 1 día)
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<DiarioCosto>> QrySaldoInicialCostAsync(
            CostManagementDbContext ctx,
            DateOnly dtFechaInicio)
        {
            List<DiarioCosto> lstDiarioCosto;
            try
            {
                await using var objContext = await _objCostosFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                var dtFechaAnterior = dtFechaInicio.AddDays(-1);

                lstDiarioCosto = await ( 
                    from mts in objContext.TbMateriaPrimaSaldo.AsNoTracking()
                    where mts.MpsTipo == "I"
                    select new DiarioCosto
                    {
                        strTipo = "I",
                        strCodTip = "SALDO",
                        strDescripcion = "1.SALDO INICIAL",
                        intLote = (int)mts.MpsRloNumero,
                        //strProClas01 = g.Key.ProClas01,
                        //strProClas05 = g.Key.ProClas05,
                        //strTalDescri = g.Key.TalDescri,
                        dtFechaMov = (DateTime)mts.MpsFecha,
                        stTalCodigo = (short)mts.MpsTalCodigo,
                        strProCodcor = mts.MpsProCodcor.Trim(),
                        //strProDesesp = mts.MpsProDesesp,
                        strEmbCodigo = mts.MpsEmbCodigo,
                        strCodBod = mts.MpsBodCodigo,
                        intMedCodigo = (int)mts.MpsMedCodigo,
                        dcLibras = (decimal)mts.MpsLibras,
                        dcMasters = (decimal)mts.MpsMasters,
                        dcCostoTot = (decimal)mts.MpsCostoTotal,
                        dcCostoUnit = (decimal)mts.MpsCostoUnitario
                    }
                    ).ToListAsync();

                List<string> lstProdCod = lstDiarioCosto.Select(obj => obj.strProCodcor).Distinct().ToList();
                List<decimal> lstCodTal = lstDiarioCosto.Select(obj => Convert.ToDecimal(obj.stTalCodigo)).Distinct().ToList();
                var lstInfoProdCod = await ctx.TbProduc.AsNoTracking()
                            .SelectManyBatchAsync(
                                 keySelector: pro => pro.ProCodcor,
                                    values: lstProdCod,
                                    selector: filtered =>
                                    from pro in filtered
                                    join pres in ctx.TbProces.AsNoTracking() on pro.ProClas06 equals pres.ProCodigo
                                    select new
                                    {
                                        ProCodcor = pro.ProCodcor.Trim(),
                                        pro.ProDesesp,
                                        pro.ProClas01,
                                        pro.ProClas05,
                                        pres.ProCodigo,
                                        pres.ProDescri
                                    }
                                );
                var lstInfoCodTal = await ctx.TbTallas.AsNoTracking()
                            .SelectManyBatchAsync(
                                 keySelector: tal => tal.TalCodigo,
                                    values: lstCodTal,
                                    selector: filtered =>
                                    from tal in filtered
                                    select new
                                    {
                                        tal.TalCodigo,
                                        tal.TalDescri
                                    }
                                );
                foreach (var sal in lstDiarioCosto)
                {
                    var objProduc = lstInfoProdCod.FirstOrDefault(obj => obj.ProCodcor == sal.strProCodcor);
                    var objTalla = lstInfoCodTal.FirstOrDefault(obj => obj.TalCodigo == (decimal)sal.stTalCodigo);
                    if (objProduc != null)
                    {
                        sal.strProDesesp = objProduc.ProDesesp;
                        sal.strProClas01 = objProduc.ProClas01;
                        sal.strProClas05 = objProduc.ProClas05;
                        sal.strProdDescri = objProduc.ProDescri;
                        sal.strProCod = objProduc.ProCodigo;
                    }
                    if (objTalla != null)
                    {
                        sal.strTalDescri = objTalla.TalDescri;
                    }

                }
                return lstDiarioCosto;
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex,
                    "[MateriaPrima].[QrySaldoInicialAsync] Error al obtener saldo inicial para fecha {Fecha}: {Mensaje}",
                    dtFechaInicio, ex.Message);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TRAMO 2 — LIQUIDACIÓN FRESCO (tb_liqtun)
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<DiarioCosto>> QryLiqFrescoAsync(
            CostManagementDbContext ctx,
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                var dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0));
                var dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));

                return await (
                    from liq in ctx.TbLiqtun
                    join lid in ctx.TbLitund on liq.LiqNumero equals lid.LidNumero
                    join rlo in ctx.TbReglot on liq.LiqLote equals (long)rlo.RloNumero
                    join pro in ctx.TbProduc on lid.LidProduc equals pro.ProCodcor
                    join pres in ctx.TbProces.AsNoTracking() on pro.ProClas06 equals pres.ProCodigo
                    join emb in ctx.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                    join med in ctx.TbMedida on pro.ProUnimed equals med.MedCodigo
                    join tal in ctx.TbTallas on lid.LidCodtal equals tal.TalCodigo
                    where rlo.RloFecha >= dtFeInicio
                       && rlo.RloFecha <= dtFeFin
                       && liq.LiqEstado == "AC"
                       && rlo.RloEstado != "AN"
                    group new { lid, emb, med } by new
                    {
                        lid.LidLote,
                        liq.LiqTunpla,
                        pro.ProClas01,
                        liq.LiqFecha,
                        pro.ProClas05,
                        tal.TalDescri,
                        tal.TalCodigo,
                        pro.ProCodcor,
                        pro.ProDesesp,
                        pro.ProEmbala,
                        med.MedCodigo,
                        pres.ProCodigo,
                        pres.ProDescri
                    }
                    into g
                    select new DiarioCosto
                    {
                        strTipo = "I",
                        strCodTip = "LIQFR",
                        strDescripcion = "2.LIQ FRESCO",
                        intLote = (int)g.Key.LidLote,
                        dtFechaMov = g.Key.LiqFecha,
                        strProClas01 = g.Key.ProClas01,
                        strProClas05 = g.Key.ProClas05,
                        strTalDescri = g.Key.TalDescri,
                        stTalCodigo = (short)g.Key.TalCodigo,
                        strProCodcor = g.Key.ProCodcor.Trim(),
                        strProDesesp = g.Key.ProDesesp,
                        strEmbCodigo = g.Key.ProEmbala,
                        strCodBod = g.Key.LiqTunpla,
                        strProdDescri = g.Key.ProDescri,
                        strProCod = g.Key.ProCodigo.Trim(),
                        intMedCodigo = (int)g.Key.MedCodigo,
                        dcLibras = (decimal)g.Sum(x => (double)x.lid.LidCanenv * x.emb.EmbPeso * x.med.MedFactor),
                        dcMasters = g.Sum(x => x.lid.LidCanenv)
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex,
                    "[MateriaPrima].[QryLiqFrescoAsync] Error en liquidación fresco [{Inicio} - {Fin}]: {Mensaje}",
                    dtFechaInicio, dtFechaFin, ex.Message);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TRAMO 3 — LIQUIDACIÓN OTROS PROCESOS (tb_liqvag)
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<DiarioCosto>> QryLiqOtrosProcesosAsync(
            CostManagementDbContext ctx,
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                var dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0));
                var dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));

                return await (
                    from liq in ctx.TbLiqvag
                    join lid in ctx.TbLitvad on liq.LiqNumero equals lid.LidNumero
                    join lot in ctx.TbLototr
                        on new { Num = liq.LiqLote, Tipo = liq.LiqTipo }
                        equals new { Num = lot.LotNumero, Tipo = lot.LotTipo }
                    join pro in ctx.TbProduc on lid.LidProduc equals pro.ProCodcor
                    join pres in ctx.TbProces.AsNoTracking() on pro.ProClas06 equals pres.ProCodigo
                    join tot in ctx.TbTiplot.AsNoTracking() on lot.LotTiplot equals tot.TipCodigo
                    join emb in ctx.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                    join med in ctx.TbMedida on pro.ProUnimed equals med.MedCodigo
                    join tal in ctx.TbTallas on lid.LidCodtal equals tal.TalCodigo
                    where lot.LotFecha >= dtFeInicio
                       && lot.LotFecha <= dtFeFin
                       && liq.LiqEstado != "AN"
                       && lot.LotEstado != "AN"
                    group new { lid, emb, med } by new
                    {
                        lid.LidLote,
                        liq.LiqTunpla,
                        liq.LiqFecha,
                        pro.ProClas01,
                        pro.ProClas05,
                        tal.TalDescri,
                        tal.TalCodigo,
                        pro.ProCodcor,
                        pro.ProDesesp,
                        pro.ProEmbala,
                        med.MedCodigo,
                        tot.TipCodigo,
                        tot.TipDescri
                    }
                    into g
                    select new DiarioCosto
                    {
                        strTipo = "I",
                        strCodTip = "LIQOP",
                        strDescripcion = "3.LIQ OTROS PROCESOS",
                        intLote = (int)g.Key.LidLote,
                        dtFechaMov = g.Key.LiqFecha,
                        strProClas01 = g.Key.ProClas01,
                        strProClas05 = g.Key.ProClas05,
                        strTalDescri = g.Key.TalDescri,
                        stTalCodigo = (short)g.Key.TalCodigo,
                        strProCodcor = g.Key.ProCodcor.Trim(),
                        strProDesesp = g.Key.ProDesesp,
                        strEmbCodigo = g.Key.ProEmbala,
                        strCodBod = g.Key.LiqTunpla,
                        strProCod = g.Key.TipCodigo.Trim(),
                        strProdDescri = g.Key.TipDescri,
                        intMedCodigo = (int)g.Key.MedCodigo,
                        dcLibras = (decimal)g.Sum(x => x.lid.LidCanenv * x.emb.EmbPeso * x.med.MedFactor),
                        dcMasters = (decimal)g.Sum(x => x.lid.LidCanenv)
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex,
                    "[MateriaPrima].[QryLiqOtrosProcesosAsync] Error en liquidación otros procesos [{Inicio} - {Fin}]: {Mensaje}",
                    dtFechaInicio, dtFechaFin, ex.Message);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TRAMO 4 — INGRESOS (tb_tracamauto, ingegr='I', excluye IVA e IBR)
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<DiarioCosto>> QryIngresosAsync(
            CostManagementDbContext ctx,
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                var dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0));
                var dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));

                return await (
                    from trc in ctx.TbTracamAuto
                    join tcd in ctx.TbTracadAuto on trc.TrcNumsec equals tcd.TcdNumero
                    join pro in ctx.TbProduc on tcd.TcdProduc equals pro.ProCodcor
                    join emb in ctx.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                    join med in ctx.TbMedida on pro.ProUnimed equals med.MedCodigo
                    join tal in ctx.TbTallas on tcd.TcdCodtal equals tal.TalCodigo
                    join trs in ctx.TbTransa
                        on new { Cod = trc.TrcTipo, Tipo = trc.TrcIngegr }
                        equals new { Cod = trs.TrsCodigo, Tipo = trs.TrsTipo }
                    where trc.TrcFecha >= dtFeInicio
                       && trc.TrcFecha <= dtFeFin
                       && trc.TrcEstado == "ac"
                       && trc.TrcIngegr == "I"
                       && !new List<string>() { "IVA", "IBR", "CNEI", "REPING", "IRP", "IAT", "ILB" }.Contains(trc.TrcTipo) 
                    group new { tcd, emb, med } by new
                    {
                        tcd.TcdLote,
                        trc.TrcCodcam,
                        trc.TrcFecha,
                        pro.ProClas01,
                        pro.ProClas05,
                        tal.TalDescri,
                        tal.TalCodigo,
                        pro.ProCodcor,
                        pro.ProDesesp,
                        pro.ProEmbala,
                        med.MedCodigo,
                        trs.TrsCodigo,
                        trs.TrsDescri
                    }
                    into g
                    select new DiarioCosto
                    {
                        strTipo = "I",
                        strCodTip = "INGR",
                        strDescripcion = "4.INGRESOS",
                        intLote = (int)g.Key.TcdLote,
                        dtFechaMov = g.Key.TrcFecha,
                        strProClas01 = g.Key.ProClas01,
                        strProClas05 = g.Key.ProClas05,
                        strTalDescri = g.Key.TalDescri,
                        stTalCodigo = (short)g.Key.TalCodigo,
                        strProCodcor = g.Key.ProCodcor.Trim(),
                        strProDesesp = g.Key.ProDesesp,
                        strEmbCodigo = g.Key.ProEmbala,
                        strCodBod = g.Key.TrcCodcam,
                        strProdDescri = g.Key.TrsDescri,
                        strProCod = g.Key.TrsCodigo.Trim(),
                        intMedCodigo = (int)g.Key.MedCodigo,
                        dcLibras = (decimal)g.Sum(x => (double)x.tcd.TcdCantid * x.emb.EmbPeso * x.med.MedFactor),
                        dcMasters = (decimal)g.Sum(x => x.tcd.TcdCantid)
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex,
                    "[MateriaPrima].[QryIngresosAsync] Error en ingresos [{Inicio} - {Fin}]: {Mensaje}",
                    dtFechaInicio, dtFechaFin, ex.Message);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TRAMO 5 — EGRESOS (tb_tracamauto, ingegr='E', excluye EX)
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<DiarioCosto>> QryEgresosAsync(
            CostManagementDbContext ctx,
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                var dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0));
                var dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));

                return await (
                    from trc in ctx.TbTracamAuto
                    join tcd in ctx.TbTracadAuto on trc.TrcNumsec equals tcd.TcdNumero
                    join pro in ctx.TbProduc on tcd.TcdProduc equals pro.ProCodcor
                    join pres in ctx.TbProces.AsNoTracking() on pro.ProClas06 equals pres.ProCodigo
                    join emb in ctx.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                    join med in ctx.TbMedida on pro.ProUnimed equals med.MedCodigo
                    join tal in ctx.TbTallas on tcd.TcdCodtal equals tal.TalCodigo
                    join trs in ctx.TbTransa
                        on new { Cod = trc.TrcTipo, Tipo = trc.TrcIngegr }
                        equals new { Cod = trs.TrsCodigo, Tipo = trs.TrsTipo }
                    where trc.TrcFecha >= dtFeInicio
                       && trc.TrcFecha <= dtFeFin
                       && trc.TrcEstado == "ac"
                       && trc.TrcIngegr == "E"
                       //&& trc.TrcTipo != "EX"
                       && !new List<string>() {"EX", "IVA", "IBR", "CNEI", "RMC",
                           "IRP", "LAB", "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", /*"REPROE", "DIR",*/
                           "IAT", "CC", "D4", "DEV","IQ", "LB" }.Contains(trc.TrcTipo)
                    group new { tcd, emb, med, trc } by new
                    {
                        tcd.TcdLote,
                        trc.TrcTipo,
                        trc.TrcFecha,
                        trc.TrcCodcam,
                        pro.ProClas01,
                        pro.ProClas05,
                        tal.TalDescri,
                        tal.TalCodigo,
                        pro.ProCodcor,
                        pro.ProDesesp,
                        pro.ProEmbala,
                        med.MedCodigo,
                        trs.TrsCodigo,
                        trs.TrsDescri
                    }
                    into g
                    select new DiarioCosto
                    {
                        strTipo = "E",
                        strCodTip = g.Key.TrcTipo,
                        strDescripcion = "5.EGRESOS " + g.Key.TrcTipo,
                        intLote = (int)g.Key.TcdLote,
                        dtFechaMov = g.Key.TrcFecha,
                        strProClas01 = g.Key.ProClas01,
                        strProClas05 = g.Key.ProClas05,
                        strTalDescri = g.Key.TalDescri,
                        stTalCodigo = (short)g.Key.TalCodigo,
                        strProCodcor = g.Key.ProCodcor.Trim(),
                        strProdDescri = g.Key.TrsDescri,
                        strProCod = g.Key.TrsCodigo.Trim(),
                        strProDesesp = g.Key.ProDesesp,
                        strEmbCodigo = g.Key.ProEmbala,
                        strCodBod = g.Key.TrcCodcam,
                        intMedCodigo = (int)g.Key.MedCodigo,
                        dcLibras = (decimal)g.Sum(x => -(double)x.tcd.TcdCantid * x.emb.EmbPeso * x.med.MedFactor),
                        dcMasters = (decimal)g.Sum(x => -x.tcd.TcdCantid)
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex,
                    "[MateriaPrima].[QryEgresosAsync] Error en egresos [{Inicio} - {Fin}]: {Mensaje}",
                    dtFechaInicio, dtFechaFin, ex.Message);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TRAMO 6 — EGRESOS EXPORTACIÓN (tb_tracamauto, ingegr='E', tipo='EX')
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<DiarioCosto>> QryEgresosExportacionAsync(
            CostManagementDbContext ctx,
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                var dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0));
                var dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));

                return await (
                    from trc in ctx.TbTracamAuto
                    join tcd in ctx.TbTracadAuto on trc.TrcNumsec equals tcd.TcdNumero
                    join pro in ctx.TbProduc on tcd.TcdProduc equals pro.ProCodcor
                    join emb in ctx.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                    join med in ctx.TbMedida on pro.ProUnimed equals med.MedCodigo
                    join tal in ctx.TbTallas on tcd.TcdCodtal equals tal.TalCodigo
                    join trs in ctx.TbTransa
                        on new { Cod = trc.TrcTipo, Tipo = trc.TrcIngegr }
                        equals new { Cod = trs.TrsCodigo, Tipo = trs.TrsTipo }
                    where trc.TrcFecha >= dtFeInicio
                       && trc.TrcFecha <= dtFeFin
                       && trc.TrcEstado == "ac"
                       && trc.TrcIngegr == "E"
                       && trc.TrcTipo == "EX"
                    group new { tcd, emb, med } by new
                    {
                        tcd.TcdLote,
                        trc.TrcTipo,
                        trc.TrcFecha,
                        trc.TrcCodcam,
                        pro.ProClas01,
                        pro.ProClas05,
                        tal.TalDescri,
                        tal.TalCodigo,
                        pro.ProCodcor,
                        pro.ProDesesp,
                        pro.ProEmbala,
                        med.MedCodigo,
                        trs.TrsCodigo,
                        trs.TrsDescri
                    }
                    into g
                    select new DiarioCosto
                    {
                        strTipo = "E",
                        strCodTip = g.Key.TrcTipo,
                        strDescripcion = "5.EGRESOS EXP",
                        intLote = (int)g.Key.TcdLote,
                        dtFechaMov = g.Key.TrcFecha,
                        strProClas01 = g.Key.ProClas01,
                        strProClas05 = g.Key.ProClas05,
                        strTalDescri = g.Key.TalDescri,
                        stTalCodigo = (short)g.Key.TalCodigo,
                        strProCodcor = g.Key.ProCodcor.Trim(),
                        strProDesesp = g.Key.ProDesesp,
                        strEmbCodigo = g.Key.ProEmbala,
                        strCodBod = g.Key.TrcCodcam,
                        strProdDescri = g.Key.TrsDescri,
                        strProCod = g.Key.TrsCodigo.Trim(),
                        intMedCodigo = (int)g.Key.MedCodigo,
                        dcLibras = (decimal)g.Sum(x => -(double)x.tcd.TcdCantid * x.emb.EmbPeso * x.med.MedFactor),
                        dcMasters = (decimal)g.Sum(x => -x.tcd.TcdCantid)
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex,
                    "[MateriaPrima].[QryEgresosExportacionAsync] Error en egresos exportación [{Inicio} - {Fin}]: {Mensaje}",
                    dtFechaInicio, dtFechaFin, ex.Message);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  TRAMO 7 — CONSUMOS OTROS PROCESOS (tb_relode + tb_lototr)
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<DiarioCosto>> QryConsumosOtrosProcesosAsync(
            CostManagementDbContext ctx,
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                var dtFeInicio = dtFechaInicio.ToDateTime(new TimeOnly(0, 0));
                var dtFeFin = dtFechaFin.ToDateTime(new TimeOnly(23, 59));

                return await (
                    from rld in ctx.TbRelode
                    join lot in ctx.TbLototr
                        on new { Num = rld.RldNumero, Tipo = rld.RldTipo }
                        equals new { Num = lot.LotNumero, Tipo = lot.LotTipo }
                    join pro in ctx.TbProduc on rld.RldProcod equals pro.ProCodcor
                    join tot in ctx.TbTiplot.AsNoTracking() on lot.LotTiplot equals tot.TipCodigo
                    join emb in ctx.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                    join med in ctx.TbMedida on pro.ProUnimed equals med.MedCodigo
                    join tal in ctx.TbTallas on rld.RldCodtal equals tal.TalCodigo
                    where lot.LotFecha >= dtFeInicio
                       && lot.LotFecha <= dtFeFin
                       && lot.LotEstado != "AN"
                    group new { rld, emb, med, lot } by new
                    {
                        lot.LotTipo,
                        rld.RldLote,
                        lot.LotFecha,
                        lot.LotCodbod,
                        pro.ProClas01,
                        pro.ProClas05,
                        tal.TalDescri,
                        tal.TalCodigo,
                        pro.ProCodcor,
                        pro.ProDesesp,
                        pro.ProEmbala,
                        med.MedCodigo,
                        tot.TipCodigo,
                        tot.TipDescri
                    }
                    into g
                    select new DiarioCosto
                    {
                        strTipo = "E",
                        strCodTip = "CSOTRO",
                        strDescripcion = "6.CONSUMOS OTRO PROCESOS " + g.Key.LotTipo,
                        intLote = (int)g.Key.RldLote,
                        dtFechaMov = g.Key.LotFecha ?? DateTime.Now,
                        strProClas01 = g.Key.ProClas01,
                        strProClas05 = g.Key.ProClas05,
                        strTalDescri = g.Key.TalDescri,
                        stTalCodigo = (short)g.Key.TalCodigo,
                        strProCodcor = g.Key.ProCodcor.Trim(),
                        strProDesesp = g.Key.ProDesesp,
                        strEmbCodigo = g.Key.ProEmbala,
                        strCodBod = g.Key.LotCodbod,
                        intMedCodigo = (int)g.Key.MedCodigo,
                        strProdDescri = g.Key.TipDescri,
                        strProCod = g.Key.TipCodigo.Trim(),
                        dcLibras = (decimal)g.Sum(x => -x.rld.RldCantid * x.emb.EmbPeso * x.med.MedFactor),
                        dcMasters = (decimal)g.Sum(x => -x.rld.RldCantid)
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                _objLogger.LogError(ex,
                    "[MateriaPrima].[QryConsumosOtrosProcesosAsync] Error en consumos otros procesos [{Inicio} - {Fin}]: {Mensaje}",
                    dtFechaInicio, dtFechaFin, ex.Message);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  MAPEO DTO → ENTIDAD TbMateriaPrimaSaldo
        // ─────────────────────────────────────────────────────────────────────────

        private static TbMateriaPrimaSaldo MapearAEntidad(
            DiarioCosto dto,
            DateOnly dtFechaInicio)
        {
            return new TbMateriaPrimaSaldo
            {
                // ── Campos del movimiento ────────────────────────────────────────
                MpsTipo = dto.strTipo,
                MpsTrsCodigo = dto.strCodTip,          // ← nuevo campo char(6) NOT NULL
                MpsFecha = dtFechaInicio.ToDateTime(new TimeOnly(0, 0)),
                MpsRloNumero = dto.intLote,
                MpsTalCodigo = dto.stTalCodigo,
                MpsEmbCodigo = dto.strEmbCodigo,
                MpsMedCodigo = (byte)dto.intMedCodigo,
                MpsBodCodigo = dto.strCodBod,
                MpsProCodcor = dto.strProCodcor,

                // ── Cantidades (costos se calculan en paso posterior) ────────────
                MpsMasters = dto.dcMasters,
                MpsLibras = dto.dcLibras,
                MpsCostoUnitario = 0m,
                MpsCostoTotal = 0m,

                // ── Control ──────────────────────────────────────────────────────
                MpsEstado = "AC",
                MpsUsuarioCrea = "SISTEMA",
                MpsFechaCrea = DateTime.Now,
                MpsEquipoCrea = Environment.MachineName,

                // ── Campos referenciales (bodega se puede complementar después) ──
                MpsEmpCodigo = 1
            };
        }
        #endregion

    }
}
