using CostManagement.API.Controllers;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Infraestructura.EF_Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace CostManagement.Infraestructura.Repository.Services
{
    public class ProcesoParametro : IProcesoParametro
    {
        private readonly IDbContextFactory<CostosDbContext> _objContextFactory;
        private readonly ILogger<ProcesoParametro> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;

        public ProcesoParametro(
            ILogger<ProcesoParametro> objLogger,
            IOptions<ParametrosConfig> objConfig,
            IDbContextFactory<CostosDbContext> objContextFactory)
        {
            _objLogger = objLogger;
            _objConfig = objConfig;
            _objContextFactory = objContextFactory;
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesosFrescoConValores(DateOnly fechaCorte)
        {
            try
            {
                HashSet<string> hshNoTraer = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "IQF", "BRINE", "Hidratacion", "Cocido", "Pelado", "Decorado" };

                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        return await (
                            from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                            join parametro in objContext.TbParametroCosteo
                                 on new { Id = (int)proceso.PrId, Fecha = fechaCorte, tipo = "PFR" }
                                 equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                 into joinParam
                            from p in joinParam.DefaultIfEmpty()
                            where String.IsNullOrEmpty(proceso.PrTipCodigo) && !hshNoTraer.Contains(proceso.PrDescri)
                            select new ProcesoResultadoDto
                            {
                                intCodigo = proceso.PrId,
                                intCodDet = p != null ? p.PcId : 0,
                                strEstado = p != null ? p.PcEstado : "AC",
                                strDescripcion = proceso.PrDescri,
                                blEditable = (bool)proceso.PrEditable,
                                strTipoLote = "PFR",
                                dcValor = p != null ? p.PcMonto : 0,
                                dcLibras = p != null ? p.PcLibras : 0,
                                dcCostUnitario = p != null ? p.PcCotoUnitario : 0,
                            }).ToListAsync();
                    },
                    nivelAislamiento: IsolationLevel.ReadUncommitted,
                    blRequiereCommit: false);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarProcesosFrescoConValores), ex);
                throw;
            }
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesosReproConValores(DateOnly fechaCorte)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        return await (
                            from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                            join parametro in objContext.TbParametroCosteo
                                 on new { Id = (int)proceso.PrId, Fecha = fechaCorte, tipo = "RPC" }
                                 equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                 into joinParam
                            from p in joinParam.DefaultIfEmpty()
                            where String.IsNullOrEmpty(proceso.PrTipCodigo)
                            select new ProcesoResultadoDto
                            {
                                intCodigo = proceso.PrId,
                                intCodDet = p != null ? p.PcId : 0,
                                strEstado = p != null ? p.PcEstado : "AC",
                                strDescripcion = proceso.PrDescri,
                                blEditable = (bool)proceso.PrEditable,
                                strTipoLote = "RPC",
                                dcValor = p != null ? p.PcMonto : 0,
                                dcLibras = p != null ? p.PcLibras : 0,
                                dcCostUnitario = p != null ? p.PcCotoUnitario : 0,
                            }).ToListAsync();
                    },
                    nivelAislamiento: IsolationLevel.ReadUncommitted,
                    blRequiereCommit: false);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarProcesosReproConValores), ex);
                throw;
            }
        }

        public async Task<bool> RegistrarParamCosteoPfr(DateOnly fechaCorte, GuardarParametrosRequest objParam)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        var idsProcesos = objParam.LstValores.Select(d => (byte)d.intCodigo).ToList();

                        var registrosAEliminar = objContext.TbParametroCosteo
                            .Where(p => p.PcFecha == fechaCorte && idsProcesos.Contains(p.PcPrId));

                        objContext.TbParametroCosteo.RemoveRange(registrosAEliminar);
                        await objContext.SaveChangesAsync();

                        var nuevosRegistros = objParam.LstValores.Select(d => new TbParametroCosteo
                        {
                            PcPrId = (byte)d.intCodigo,
                            PcFecha = fechaCorte,
                            PcMonto = d.dcValor,
                            PcCotoUnitario = Math.Round(d.dcCostUnitario ?? 0m, 5),
                            PcLibras = d.dcLibras,
                            PcTipoLote = d.strTipoLote,
                            PcEstado = "CE",
                            PcUsuarioCrea = objParam.strUsuario,
                            PcFechaCrea = DateTime.Now
                        }).ToList();

                        await objContext.TbParametroCosteo.AddRangeAsync(nuevosRegistros);
                        await objContext.SaveChangesAsync();
                        return true;
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(RegistrarParamCosteoPfr), ex);
                throw;
            }
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesoTarifa(DateOnly fechaCorte)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        return await (
                            from proceso in objContext.TbProcesoCosteo.AsNoTracking().Where(obj => obj.PrEstado == "AC")
                            join parametro in objContext.TbParametroCosteo.AsNoTracking()
                                 on new { Id = (int)proceso.PrId, Fecha = fechaCorte, tipo = "RPC" }
                                 equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                 into joinParam
                            from p in joinParam.DefaultIfEmpty()
                            where !String.IsNullOrEmpty(proceso.PrTipCodigo)
                            select new ProcesoResultadoDto
                            {
                                intCodigo = proceso.PrId,
                                intCodDet = p != null ? p.PcId : 0,
                                strEstado = p != null ? p.PcEstado : "AC",
                                strDescripcion = proceso.PrDescri,
                                strCodTip = (string)proceso.PrTipCodigo,
                                blEditable = (bool)proceso.PrEditable,
                                strTipoLote = "RPC",
                                dcValor = p != null ? p.PcMonto : 0,
                                dcLibras = p != null ? p.PcLibras : 0,
                                dcCostUnitario = p != null ? p.PcCotoUnitario : 0,
                            }).ToListAsync();
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarProcesoTarifa), ex);
                throw;
            }
        }

        public async Task<List<string>> ConsultarCatalogoXCab(int intCab)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        return await (
                            from cab in objContext.TbCatalogoCab
                            join det in objContext.TbCatalogoDet on cab.CatId equals det.DetIdCab
                            where cab.CatId.Equals(intCab)
                            select det.DetCodigo
                            ).ToListAsync();
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarCatalogoXCab), ex);
                throw;
            }
        }

        public async Task<List<string>> ConsultarCatalogoXDes(string strDescp)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objContextFactory,
                    async objContext =>
                    {
                        return await (
                            from cab in objContext.TbCatalogoCab
                            join det in objContext.TbCatalogoDet on cab.CatId equals det.DetIdCab
                            where cab.CatDescripcion.Equals(strDescp)
                            select det.DetCodigo
                            ).ToListAsync();
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarCatalogoXDes), ex);
                throw;
            }
        }
    }
}
