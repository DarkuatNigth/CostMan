using CostManagement.API.Controllers;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Infraestructura.EF_Core;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data;

namespace CostManagement.Infraestructura.Repository.Services
{
    public class ProcesoParametro : IProcesoParametro
    {
        private readonly IDbContextFactory<CostManagementDbContext> _objContextFactory;
        private readonly IDbContextFactory<CostosDbContext> _objCostosFactory;
        private readonly IDbContextFactory<SongDbContext> _objSongFactory;
        private readonly ILogger<ProcesoParametro> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;

        public ProcesoParametro(
            ILogger<ProcesoParametro> objLogger,
            IOptions<ParametrosConfig> objConfig,
            IDbContextFactory<CostManagementDbContext> objContextFactory,
            IDbContextFactory<CostosDbContext> objCostosFactory,
            IDbContextFactory<SongDbContext> objSongFactory)
        {
            _objLogger = objLogger;
            _objConfig = objConfig;
            _objCostosFactory = objCostosFactory;
            _objSongFactory = objSongFactory;
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesosFrescoConValores(DateOnly fechaCorte)
        {
            try
            {
                HashSet<string> hshNoTraer = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "IQF", "BRINE", "Hidratacion", "Cocido", "Pelado", "Decorado", "Descongelado" };

                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
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
                    _objCostosFactory,
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
                    _objCostosFactory,
                    async objContext =>
                    {
                        var idsProcesos = objParam.LstValores.Select(d => (byte)d.intCodigo).ToList();

                        var tiposEntrada = objParam.LstValores
                            .Where(x => !string.IsNullOrWhiteSpace(x.strTipoLote))
                            .Select(x => x.strTipoLote!.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var registrosAEliminar = objContext.TbParametroCosteo
                            .Where(p => p.PcFecha == fechaCorte && idsProcesos.Contains(p.PcPrId) && tiposEntrada.Contains(p.PcTipoLote));

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
                    _objCostosFactory,
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
                    _objCostosFactory,
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
                    _objCostosFactory,
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

        public async Task<List<DistribucionCostoDto>> ConsultarDistribucion(int anio, int mes)
        {
            List<DistribucionCostoDto> lstDistribucion = new List<DistribucionCostoDto>();
            List<TbMaecta> lstMaecCuentas, lstCentroCosto ;
            List<TbPlantaProcOe> lstPlantaProc = new List<TbPlantaProcOe>();
            List<string> lstCodCuentas, lstCodBodega, lstCodCentroCosto;
            Dictionary<int,string> dicPlantaProc = new Dictionary<int, string>() { { 1, "SONGA1" }, { 11, "SONGA2" }, { 12, "COMIN" } };
            try
            {
                lstDistribucion = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        return await (
                            from d in objContext.TbDistribucionCosto.AsNoTracking()
                            where d.DpEstado == "AC"
                               && d.DpAnio == anio
                               && d.DpMes == mes
                            orderby d.DpPaCodigo, d.DpCtaNatura descending, d.DpCtaNumero
                            select new DistribucionCostoDto(d)).ToListAsync();
                    });
                lstCodCuentas = lstDistribucion.Select(d => d.strCtaNumero).Distinct().ToList();
                lstCodBodega = lstDistribucion.Select(d => d.intPaCodigo.ToString()).Distinct().ToList();
                lstMaecCuentas = await ManejoContext<SongDbContext>.EjecutarAsync(
                                    _objSongFactory,
                                    async objContext =>
                                    {
                                        return await objContext.TbMaecta.AsNoTracking()
                                            .SelectManyBatchAsync(
                                                keySelector: c => c.CtaNumero,
                                                values: lstCodCuentas,
                                                selector: filtered =>
                                                        from c in filtered
                                                        select c
                                            );
                                    });
                //lstPlantaProc = await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                //                    _objContextFactory,
                //                    async objContext =>
                //                    {
                //                        return await objContext.TbPlantaProcOe.AsNoTracking().ToListAsync();
                //                    });
                //lstPlantaProc = lstPlantaProc.Where(c => lstCodBodega.Contains(c.PaCodigo)).ToList();
                lstCodCentroCosto = lstMaecCuentas.Select(c => c.CtaRelaci!).Distinct().ToList();
                lstCentroCosto = await ManejoContext<SongDbContext>.EjecutarAsync(
                                    _objSongFactory,
                                    async objContext =>
                                    {
                                        return await objContext.TbMaecta.AsNoTracking()
                                            .SelectManyBatchAsync(
                                                keySelector: c => c.CtaNumero,
                                                values: lstCodCentroCosto,
                                                selector: filtered =>
                                                        from c in filtered
                                                        select c
                                            );
                                    });
                Dictionary<string, TbMaecta> dictMaecCuentas = lstMaecCuentas.ToDictionary(c => c.CtaNumero.Trim());
                Dictionary<string, TbMaecta> dictCentroCosto = lstCentroCosto.ToDictionary(c => c.CtaNumero.Trim());
                //Dictionary<int, TbPlantaProcOe> dictPlantaProc = lstPlantaProc.ToDictionary(c => Convert.ToInt32(c.PaCodigo));
                foreach (var obj in lstDistribucion)
                {
                    TbMaecta? objCuenta = dictMaecCuentas!.GetValueOrDefault(obj.strCtaNumero, null);
                    //TbPlantaProcOe? objPlanta = dictPlantaProc!.GetValueOrDefault(obj.intPaCodigo ?? 0, null);
                    if (objCuenta != null)
                    {
                        string strCentroCosto = dictCentroCosto!.GetValueOrDefault(objCuenta.CtaRelaci!.Trim(), null)?.CtaDeslar ?? string.Empty;
                        obj.InicializarMaectCuenta(objCuenta, strCentroCosto);
                    }
                    //if (objPlanta != null)
                    //{
                    //    obj.InicializarPlanta(objPlanta.PaDescri!);
                    //}
                    obj.InicializarPlanta(dicPlantaProc!.GetValueOrDefault(obj.intPaCodigo, ""));
                }
                return lstDistribucion;
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarDistribucion), ex);
                throw;
            }
        }
        public async Task<bool> CrearOActualizarDistribucion(List<DistribucionCostoDto> objRegistros)
        {
            bool blEjecuto = false;
            try
            {
                blEjecuto = await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        foreach (var obj in objRegistros)
                        {
                            if (obj.strCtaNatura == "C")
                            {
                                _objLogger.LogInformation($"Registros Cuenta Haber: {obj}");
                            }
                            var registroExistente = await objContext.TbDistribucionCosto.AsTracking()
                                .FirstOrDefaultAsync(d => d.DpId == obj.intCodigo);
                            if (registroExistente != null)
                            {
                                registroExistente.DpPorcentaje = obj.dcPorcentaje;
                                registroExistente.DpMonto = obj.dcMonto;
                                registroExistente.DpValorKw = obj.dcValorKw;
                                registroExistente.DpUsuarioMod = obj.strUsuario;
                                registroExistente.DpFechaMod = DateTime.Now;
                            }
                            else
                            {
                                var nuevoRegistro = new TbDistribucionCosto
                                {
                                    DpTipo = obj.strTipo,
                                    DpFechaCorte = obj.dtFechaCorte,
                                    DpAnio = (short)obj.intAnio,
                                    DpMes = (short)obj.intMes,
                                    DpPorcentaje = obj.dcPorcentaje,
                                    DpMonto = obj.dcMonto,
                                    DpValorKw = obj.dcValorKw,
                                    DpCtaNumero = obj.strCtaNumero,
                                    DpCtaNatura = obj.strCtaNatura,
                                    DpCodigoMedidor = obj.strCodMedidor,
                                    DpPaCodigo = (byte)obj.intPaCodigo,
                                    DpEstado = "AC",
                                    DpUsuarioCrea = obj.strUsuario,
                                    DpFechaCrea = DateTime.Now
                                };
                                await objContext.TbDistribucionCosto.AddAsync(nuevoRegistro);
                            }
                        }
                        int filasAfectadas = await objContext.SaveChangesAsync();
                        // Si filasAfectadas == 0, EF Core no generó sentencias SQL (sigue sin rastrear)
                        _objLogger.LogInformation($"Registros impactados en Base de Datos: {filasAfectadas}");

                        return filasAfectadas > 0;
                    }, intTimeout: 180,
    nivelAislamiento: null,
    blRequiereCommit: true // Explicitamente indicado
                           );
                return blEjecuto;
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarDistribucion), ex);
                throw;
            }
        }

        public async Task<List<HaberDistribucionDTO>> GetHaberesDistribucion(int anio)
        {
            List<HaberDistribucionDTO> lstDistiDto;
            try
            {
                var lstDto = await ManejoContext<CostosDbContext>.EjecutarAsync(
                   _objCostosFactory,
                   async objContext =>
                   {
                       return await objContext.TbDistribucionCosto
                                .AsNoTracking()  
                                .Where(d => d.DpAnio == anio
                                         && d.DpCtaNatura == "D"      
                                         && d.DpEstado == "AC")
                                .Select(d => new HaberDistribucionDTO(d))
                                .ToListAsync();
                   });

                lstDistiDto = lstDto
                        .GroupBy(d => new { d.Tipo, d.Bolsa, d.Mes })
                        .Select(g => new HaberDistribucionDTO
                        {
                            Tipo = g.Key.Tipo,
                            Bolsa = g.Key.Bolsa,
                            Mes = g.Key.Mes,
                            Monto = g.Sum(x => x.Monto),    // ← SUMA
                            ValorKw = g.Sum(x => x.ValorKw)
                        })
                        .ToList();

                return lstDistiDto;
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(_objLogger, nameof(ProcesoParametro), nameof(ConsultarDistribucion), ex);
                throw;
            }
        }


        public async Task<List<ProcesoResultadoDto>> ConsultarParametrosWarren(DateOnly fechaCorte)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        var tiposWarren = new[] { "WEN", "WSH" };

                        return await (
                            from parametro in objContext.TbParametroCosteo.AsNoTracking()
                            join proceso in objContext.TbProcesoCosteo.AsNoTracking()
                                on parametro.PcPrId equals proceso.PrId
                            where parametro.PcFecha == fechaCorte
                               && tiposWarren.Contains(parametro.PcTipoLote)
                               && proceso.PrEstado == "AC"
                            select new ProcesoResultadoDto
                            {
                                intCodigo = proceso.PrId,
                                intCodDet = parametro.PcId,
                                strEstado = parametro.PcEstado,
                                strDescripcion = proceso.PrDescri,
                                strCodTip = proceso.PrTipCodigo,
                                blEditable = proceso.PrEditable ?? false,
                                strTipoLote = parametro.PcTipoLote,
                                dcValor = parametro.PcMonto,
                                dcLibras = parametro.PcLibras,
                                dcCostUnitario = parametro.PcCotoUnitario
                            }
                        ).ToListAsync();
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarParametrosWarren),
                    ex);
                throw;
            }
        }

        public async Task<decimal?> ConsultarObjetivoWarren(DateOnly fechaCorte)
        {
            return await ManejoContext<CostosDbContext>.EjecutarAsync(
                _objCostosFactory,
                async ctx => await ctx.TbParametroCosteo
                    .AsNoTracking()
                    .Where(x => x.PcFecha == fechaCorte && x.PcTipoLote == "WOB")
                    .OrderByDescending(x => x.PcId)
                    .Select(x => (decimal?)x.PcMonto)
                    .FirstOrDefaultAsync());
        }

        public async Task<List<CostoProcesoParticionDto>> ConsultarCostoProcesoParticion(int anio,int mes)
        {
            return await ManejoContext<CostosDbContext>.EjecutarAsync(
                _objCostosFactory,
                async ctx => await ctx.TbDistribucionProcesoProductivo
                    .AsNoTracking()
                    .Where(x => x.DpAnio == anio && x.DpMes == mes && x.DpEstado == "AC")
                    .GroupBy(x => x.DpPcCodigo ?? string.Empty)
                    .Select(g => new CostoProcesoParticionDto
                    {
                        strPcCodigo = g.Key,
                        strProceso = string.Empty,
                        dcDolaresEntero = g.Sum(x => x.DpMontoEntero),
                        dcDolaresCola = g.Sum(x => x.DpMontoCola),
                        dcDolaresValorAgregado = g.Sum(x => x.DpMontoVag)
                    })
                    .ToListAsync());
        }

        public async Task<bool> RegistrarParametrosWarren(
            DateOnly fechaCorte,
            WarrenResultadoDto objWarren,
            string strUsuario)
        {
            try
            {
                if (objWarren == null || objWarren.lstDetalle == null || !objWarren.lstDetalle.Any())
                    throw new ArgumentException("No existe detalle Warren para registrar.");

                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        var ids = objWarren.lstDetalle
                            .Where(x => x.intCodigo > 0)
                            .Select(x => (byte)x.intCodigo)
                            .Distinct()
                            .ToList();

                        // IMPORTANTE: borrar únicamente Warren. No tocar PFR/RPC.
                        var anteriores = objContext.TbParametroCosteo
                            .Where(x => x.PcFecha == fechaCorte
                                     && ((ids.Contains(x.PcPrId)
                                          && (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH"))
                                         || x.PcTipoLote == "WOB"));

                        objContext.TbParametroCosteo.RemoveRange(anteriores);
                        await objContext.SaveChangesAsync();

                        var usuario = string.IsNullOrWhiteSpace(strUsuario) ? "SISTEMA" : strUsuario.Trim();
                        var equipo = Environment.MachineName;
                        var fecha = DateTime.Now;
                        var nuevos = new List<TbParametroCosteo>();

                        foreach (var d in objWarren.lstDetalle.Where(x => x.intCodigo > 0))
                        {
                            nuevos.Add(new TbParametroCosteo
                            {
                                PcPrId = (byte)d.intCodigo,
                                PcFecha = fechaCorte,
                                PcTipoLote = "WEN",
                                PcLibras = Math.Round(d.dcLibrasEntero, 5),
                                PcCotoUnitario = Math.Round(d.dcCostoUnitarioEnteroWarren, 5),
                                PcMonto = Math.Round(d.dcMontoEnteroWarren, 5),
                                PcEstado = "CE",
                                PcUsuarioCrea = usuario,
                                PcFechaCrea = fecha,
                                PcEquipoCrea = equipo
                            });

                            nuevos.Add(new TbParametroCosteo
                            {
                                PcPrId = (byte)d.intCodigo,
                                PcFecha = fechaCorte,
                                PcTipoLote = "WSH",
                                PcLibras = Math.Round(d.dcLibrasCola, 5),
                                PcCotoUnitario = Math.Round(d.dcCostoUnitarioColaWarren, 5),
                                PcMonto = Math.Round(d.dcMontoColaWarren, 5),
                                PcEstado = "CE",
                                PcUsuarioCrea = usuario,
                                PcFechaCrea = fecha,
                                PcEquipoCrea = equipo
                            });
                        }

                        await objContext.TbParametroCosteo.AddRangeAsync(nuevos);
                        await objContext.SaveChangesAsync();
                        return true;
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(RegistrarParametrosWarren),
                    ex);
                throw;
            }
        }

    }
}
