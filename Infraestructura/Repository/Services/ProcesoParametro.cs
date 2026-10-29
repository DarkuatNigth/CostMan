using CostManagement.API.Controllers;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Infraestructura.EF_Core;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
            _objContextFactory = objContextFactory;
            _objCostosFactory = objCostosFactory;
            _objSongFactory = objSongFactory;
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesosFrescoConValores(DateOnly fechaCorte)
        {
            try
            {
                HashSet<string> hshNoTraer = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "IQF", "BRINE", "Hidratacion", "Cocido", "Pelado", "Descongelado" };

                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        // distinguir vínculos a procesos de los tipos tarifarios.
                        var codigosProceso = await objContext.TbProcesocosto.AsNoTracking().Select(x => x.PcCodigo).ToListAsync();
                        return await (
                            from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                            join parametro in objContext.TbParametroCosteo
                                 on new { Id = (int)proceso.PrId, Fecha = fechaCorte, tipo = "PFR" }
                                 equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                 into joinParam
                            from p in joinParam.DefaultIfEmpty()
                            where (String.IsNullOrEmpty(proceso.PrTipCodigo) || codigosProceso.Contains(proceso.PrTipCodigo)) && !hshNoTraer.Contains(proceso.PrDescri)
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
                        // distinguir vínculos a procesos de los tipos tarifarios.
                        var codigosProceso = await objContext.TbProcesocosto.AsNoTracking().Select(x => x.PcCodigo).ToListAsync();
                        return await (
                            from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                            join parametro in objContext.TbParametroCosteo
                                 on new { Id = (int)proceso.PrId, Fecha = fechaCorte, tipo = "RPC" }
                                 equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                 into joinParam
                            from p in joinParam.DefaultIfEmpty()
                            where (String.IsNullOrEmpty(proceso.PrTipCodigo) || codigosProceso.Contains(proceso.PrTipCodigo))
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
                        // distinguir vínculos a procesos de los tipos tarifarios.
                        var codigosProceso = await objContext.TbProcesocosto.AsNoTracking().Select(x => x.PcCodigo).ToListAsync();
                        return await (
                            from proceso in objContext.TbProcesoCosteo.AsNoTracking().Where(obj => obj.PrEstado == "AC")
                            join parametro in objContext.TbParametroCosteo.AsNoTracking()
                                 on new { Id = (int)proceso.PrId, Fecha = fechaCorte, tipo = "RPC" }
                                 equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                 into joinParam
                            from p in joinParam.DefaultIfEmpty()
                            where !String.IsNullOrEmpty(proceso.PrTipCodigo) && !codigosProceso.Contains(proceso.PrTipCodigo)
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
                foreach (var obj in lstDistribucion)
                {
                    TbMaecta? objCuenta = dictMaecCuentas!.GetValueOrDefault(obj.strCtaNumero, null);
                    if (objCuenta != null)
                    {
                        string strCentroCosto = dictCentroCosto!.GetValueOrDefault(objCuenta.CtaRelaci!.Trim(), null)?.CtaDeslar ?? string.Empty;
                        obj.InicializarMaectCuenta(objCuenta, strCentroCosto);
                    }
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
                        // Antes: var tiposWarren = new[] { "WEN", "WSH" }; // Solo unitarios.
                        var tiposWarren = new[] { "WEN", "WSH", "WAD" };

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
                    .Where(x => x.DpAnio == anio && x.DpMes == mes && x.DpEstado == "AC" && x.DpEcCodigo != "DM")
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
                                          // Antes: (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH"); ahora también se reemplaza el monto WAD.
                                          && (x.PcTipoLote == "WEN" || x.PcTipoLote == "WSH" || x.PcTipoLote == "WAD"))
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
                            // WAD conserva el monto firmado; libras y unitario son informativos.
                            nuevos.Add(new TbParametroCosteo
                            {
                                PcPrId = (byte)d.intCodigo,
                                PcFecha = fechaCorte,
                                PcTipoLote = "WAD",
                                PcLibras = Math.Round(d.dcLibrasCola, 5),
                                PcCotoUnitario = Math.Round(d.dcUnitarioAgregadoCola, 5),
                                PcMonto = Math.Round(d.dcAjusteWarren, 5),
                                PcEstado = "CE",
                                PcUsuarioCrea = usuario,
                                PcFechaCrea = fecha,
                                PcEquipoCrea = equipo
                            });
                        }

                        // IMPORTANTE: se borró WOB arriba junto con WEN/WSH; sin este
                        // bloque, ConsultarObjetivoWarren() deja de encontrar el
                        // objetivo después de usar este endpoint legado.
                        WarrenProcesoDto objCabecera = objWarren.lstDetalle
                            .FirstOrDefault(x => x.intCodigo > 0)
                            ?? throw new InvalidOperationException(
                                "No existe un proceso válido para guardar la cabecera Warren.");

                        nuevos.Add(new TbParametroCosteo
                        {
                            PcPrId = (byte)objCabecera.intCodigo,
                            PcFecha = fechaCorte,
                            PcTipoLote = "WOB",
                            PcLibras = Math.Round(objWarren.dcLibrasCola, 5),
                            PcCotoUnitario = Math.Round(objWarren.dcCostoColaActual, 5),
                            PcMonto = Math.Round(objWarren.dcObjetivoWarren, 5),
                            PcEstado = "CE",
                            PcUsuarioCrea = usuario,
                            PcFechaCrea = fecha,
                            PcEquipoCrea = equipo
                        });

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

        #region Diarios de Movimientos

        public async Task<List<DiarioMovimientoCuentaDto>> ConsultarConfiguracionDiarioMovimiento(string? strPcCodigo = null)
        {
            try
            {
                List<DiarioMovimientoCuentaDto> config = await ManejoContext<CostosDbContext>.EjecutarAsync(
                        _objCostosFactory,
                        async ctx =>
                        {
                            DbConnection cn = ctx.Database.GetDbConnection();

                            if (cn.State != ConnectionState.Open)
                                await cn.OpenAsync();

                            await using DbCommand cmd = cn.CreateCommand();

                            cmd.CommandText = "costos.sp_diarioMovimientos_configuracion";

                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.CommandTimeout = 180;

                            DbParameter p = cmd.CreateParameter();
                            p.ParameterName = "@pcCodigo";
                            p.DbType = DbType.String;
                            p.Size = 5;
                            p.Value = string.IsNullOrWhiteSpace(strPcCodigo)
                                    ? DBNull.Value
                                    : strPcCodigo.Trim();

                            cmd.Parameters.Add(p);

                            await using DbDataReader rd = await cmd.ExecuteReaderAsync();

                            return rd.MapToList<DiarioMovimientoCuentaDto>();
                        },
                        180);

                /*
                 * SGCAM guarda el número de cuenta.
                 * Clave/descripción/naturaleza se toman del maestro SONG.
                 * Se usa WhereInBatchAsync por compatibilidad de la instancia.
                 */
                List<string> cuentas = config
                        .Select(x => x.strCuenta.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .ToList();

                List<TbMaecta> maestras = await ManejoContext<SongDbContext>.EjecutarAsync(
                        _objSongFactory,
                        async ctx =>
                        {
                            return await ctx.TbMaecta
                                .AsNoTracking()
                                .WhereInBatchAsync(x => x.CtaNumero, cuentas);
                        });

                Dictionary<string, TbMaecta> porCuenta = maestras
                        .GroupBy(x => x.CtaNumero.Trim())
                        .ToDictionary(
                            x => x.Key,
                            x => x.First(),
                            StringComparer.OrdinalIgnoreCase);

                foreach (DiarioMovimientoCuentaDto row in config)
                {
                    row.ResolverRol();

                    if (!porCuenta.TryGetValue(
                        row.strCuenta.Trim(),
                        out TbMaecta? maestra))
                    {
                        continue;
                    }

                    row.strClave = maestra.CtaClave?.Trim();

                    row.strDescripcion = (
                            maestra.CtaDeslar ??
                            maestra.CtaDescor ??
                            row.strCuenta
                        ).Trim();

                    row.strNaturalezaCuenta = maestra.CtaNatura?.Trim();
                }

                return config;
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarConfiguracionDiarioMovimiento),
                    ex);

                throw;
            }
        }

        public async Task<List<DiarioMovimientoPersistenciaDto>> ConsultarDiarioMovimientoPeriodo(
            int intAnio,
            int intMes,
            string? strPcCodigo = null)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        IQueryable<TbDistribucionProcesoProductivo> query = ctx.TbDistribucionProcesoProductivo
                                .AsNoTracking()
                                .Where(x =>
                                    x.DpAnio == intAnio &&
                                    x.DpMes == intMes &&
                                    x.DpEstado == "AC" &&
                                    x.DpEcCodigo == "DM");

                        if (!string.IsNullOrWhiteSpace(strPcCodigo))
                        {
                            string pc = strPcCodigo.Trim();

                            query = query.Where(x =>
                                x.DpPcCodigo == pc);
                        }

                        return await query
                            .Select(x =>
                                new DiarioMovimientoPersistenciaDto
                                {
                                    intAnio = x.DpAnio,
                                    intMes = x.DpMes,
                                    dtFechaCorte = x.DpFechaCorte,
                                    strEtapaCodigo = x.DpEcCodigo,
                                    strProcesoCodigo = x.DpPcCodigo ?? string.Empty,
                                    strCuenta = x.DpCtaNumero ?? string.Empty,

                                    strCentroCodigo = x.DpCenCodigo,
                                    strSubcentroCodigo = x.DpSubCodigo,
                                    dcMontoEntero = x.DpMontoEntero,
                                    dcMontoCola = x.DpMontoCola,
                                    dcMontoVag = x.DpMontoVag,
                                    strAgrupacion = x.DpAgrupacionCentro,

                                    strGrupo = x.DpGrupoCentro,
                                    strNaturaleza = x.DpCtaNatura
                                })
                            .ToListAsync();
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarDiarioMovimientoPeriodo),
                    ex);

                throw;
            }
        }

        public async Task<List<DiarioMovimientoPersistenciaDto>> ConsultarCostoVentaSalidaHistorico(
            int intAnio,
            int intMes,
            List<string> lstFacturaKey)
        {
            List<string> ids = (lstFacturaKey ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => $"FACT:{x.Trim()}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ids.Count == 0)
                return new();

            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        IQueryable<TbDistribucionProcesoProductivo> baseQuery = ctx.TbDistribucionProcesoProductivo
                                .AsNoTracking()
                                .Where(x =>
                                    x.DpAnio == intAnio &&
                                    x.DpMes == intMes &&
                                    x.DpEstado == "AC" &&
                                    x.DpEcCodigo == "DM" &&
                                    x.DpPcCodigo == "DVS");

                        List<TbDistribucionProcesoProductivo> rows = await baseQuery.WhereInBatchAsync(x => x.DpAgrupacionCentro, ids);

                        return rows.Select(x =>
                            new DiarioMovimientoPersistenciaDto
                            {
                                intAnio = x.DpAnio,
                                intMes = x.DpMes,
                                dtFechaCorte = x.DpFechaCorte,
                                strEtapaCodigo = x.DpEcCodigo,
                                strProcesoCodigo = x.DpPcCodigo ?? string.Empty,
                                strCuenta = x.DpCtaNumero ?? string.Empty,
                                strCentroCodigo = x.DpCenCodigo,
                                strSubcentroCodigo = x.DpSubCodigo,
                                dcMontoEntero = x.DpMontoEntero,
                                dcMontoCola = x.DpMontoCola,
                                dcMontoVag = x.DpMontoVag,
                                strAgrupacion = x.DpAgrupacionCentro,
                                strGrupo = x.DpGrupoCentro,
                                strNaturaleza = x.DpCtaNatura
                            }).ToList();
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarCostoVentaSalidaHistorico),
                    ex);

                throw;
            }
        }

        public async Task<bool> GuardarDiarioMovimientoPeriodo(
            int intAnio,
            int intMes,
            DateOnly dtFechaCorte,
            IEnumerable<string> lstProcesosReemplazar,
            List<DiarioMovimientoPersistenciaDto> lstFilas,
            string strUsuario)
        {
            HashSet<string> procesos = (lstProcesosReemplazar ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (procesos.Count == 0)
                throw new ArgumentException(
                    "Debe indicar los procesos DM a reemplazar.");

            string usuario = string.IsNullOrWhiteSpace(strUsuario)
                    ? "SISTEMA"
                    : strUsuario.Trim();

            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                        _objCostosFactory,
                        async ctx =>
                        {
                            DateTime ahora = DateTime.Now;
                            string equipo = Environment.MachineName;

                            List<TbDistribucionProcesoProductivo> anteriores = await ctx.TbDistribucionProcesoProductivo
                                    .AsTracking()
                                    .Where(x =>
                                        x.DpAnio == intAnio &&
                                        x.DpMes == intMes &&
                                        x.DpEstado == "AC" &&
                                        x.DpEcCodigo == "DM")
                                    .ToListAsync();

                            anteriores = anteriores
                                .Where(x =>
                                    procesos.Contains(
                                        x.DpPcCodigo ?? string.Empty))
                                .ToList();

                            foreach (TbDistribucionProcesoProductivo x in anteriores)
                            {
                                x.DpEstado = "AN";
                                x.DpUsuarioEli = usuario;
                                x.DpFechaEli = ahora;
                                x.DpEquipoEli = equipo;
                            }

                            List<TbDistribucionProcesoProductivo> nuevos = (lstFilas ?? new())
                                .Where(x =>
                                    procesos.Contains(x.strProcesoCodigo) &&
                                    Math.Abs(x.dcTotal) > 0.0000001m)
                                .Select(x =>
                                    new TbDistribucionProcesoProductivo
                                    {
                                        DpAnio = checked((short)intAnio),
                                        DpMes = checked((short)intMes),
                                        DpFechaCorte = dtFechaCorte,

                                        DpEcCodigo = "DM",
                                        DpPcCodigo = DiarioMovimientoPersistencia.Limitar(x.strProcesoCodigo, 5),

                                        DpCtaNumero = DiarioMovimientoPersistencia.Limitar(x.strCuenta, 13),

                                        DpCenCodigo = DiarioMovimientoPersistencia.Limitar(x.strCentroCodigo, 10),

                                        DpSubCodigo = DiarioMovimientoPersistencia.Limitar(x.strSubcentroCodigo, 10),

                                        DpMontoEntero = Math.Round(x.dcMontoEntero, 5),

                                        DpMontoCola = Math.Round(x.dcMontoCola, 5),

                                        DpMontoVag = Math.Round(x.dcMontoVag, 5),

                                        DpAgrupacionCentro = DiarioMovimientoPersistencia.Limitar(x.strAgrupacion, 250),

                                        DpGrupoCentro = DiarioMovimientoPersistencia.Limitar(x.strGrupo, 150),

                                        DpCtaNatura = string.Equals(
                                                x.strNaturaleza,
                                                "H",
                                                StringComparison.OrdinalIgnoreCase)
                                                ? "H"
                                                : "D",

                                        DpEstado = "AC",
                                        DpUsuarioCrea = usuario,
                                        DpFechaCrea = ahora,
                                        DpEquipoCrea = equipo
                                    })
                                .ToList();

                            if (nuevos.Count > 0)
                            {
                                await ctx.TbDistribucionProcesoProductivo
                                    .AddRangeAsync(nuevos);
                            }

                            int afectados = await ctx.SaveChangesAsync();

                            _objLogger.LogInformation(
                                "[DiariosMovimiento] Periodo={Anio}-{Mes:00}; " +
                                "Anulados={Anulados}; Nuevos={Nuevos}; Afectados={Afectados}.",
                                intAnio,
                                intMes,
                                anteriores.Count,
                                nuevos.Count,
                                afectados);

                            return afectados > 0 ||
                                   (anteriores.Count == 0 && nuevos.Count == 0);
                        });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(GuardarDiarioMovimientoPeriodo),
                    ex);

                throw;
            }
        }

        public async Task<bool> GuardarCostoVentaSalidaHistorico(
            List<DiarioMovimientoPersistenciaDto> lstFilasDvs,
            string strUsuario)
        {
            List<DiarioMovimientoPersistenciaDto> filas = (lstFilasDvs ?? new())
                .Where(x =>
                    x.strProcesoCodigo == "DVS" &&
                    !string.IsNullOrWhiteSpace(x.strAgrupacion) &&
                    x.dcTotal != 0m)
                .ToList();

            if (filas.Count == 0)
                return true;

            string usuario = string.IsNullOrWhiteSpace(strUsuario)
                    ? "SISTEMA"
                    : strUsuario.Trim();

            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                        _objCostosFactory,
                        async ctx =>
                        {
                            DateTime ahora = DateTime.Now;
                            string equipo = Environment.MachineName;
                            int insertados = 0;

                            /*
                             * DVS es APPEND-ONLY.
                             * Nunca se reemplaza silenciosamente el costo con el que
                             * salió una factura histórica.
                             */
                            foreach (var periodo in filas.GroupBy(x =>
                                new { x.intAnio, x.intMes }))
                            {
                                List<TbDistribucionProcesoProductivo> existentes = await ctx.TbDistribucionProcesoProductivo
                                        .AsNoTracking()
                                        .Where(x =>
                                            x.DpAnio == periodo.Key.intAnio &&
                                            x.DpMes == periodo.Key.intMes &&
                                            x.DpEstado == "AC" &&
                                            x.DpEcCodigo == "DM" &&
                                            x.DpPcCodigo == "DVS")
                                        .ToListAsync();

                                HashSet<string> keys = existentes.Select(x =>
                                        $"{x.DpAgrupacionCentro}|{x.DpCenCodigo}|{x.DpSubCodigo}")
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                                foreach (DiarioMovimientoPersistenciaDto row in periodo)
                                {
                                    string key = $"{row.strAgrupacion}|{row.strCentroCodigo}|{row.strSubcentroCodigo}";

                                    if (keys.Contains(key))
                                        continue;

                                    await ctx.TbDistribucionProcesoProductivo.AddAsync(
                                        new TbDistribucionProcesoProductivo
                                        {
                                            DpAnio = checked((short)row.intAnio),
                                            DpMes = checked((short)row.intMes),
                                            DpFechaCorte = row.dtFechaCorte,

                                            DpEcCodigo = "DM",
                                            DpPcCodigo = "DVS",
                                            DpCtaNumero = DiarioMovimientoPersistencia.Limitar(row.strCuenta, 13),

                                            DpCenCodigo = DiarioMovimientoPersistencia.Limitar(row.strCentroCodigo, 10),

                                            DpSubCodigo = DiarioMovimientoPersistencia.Limitar(row.strSubcentroCodigo, 10),

                                            DpMontoEntero = Math.Round(row.dcMontoEntero, 5),
                                            DpMontoCola = Math.Round(row.dcMontoCola, 5),
                                            DpMontoVag = Math.Round(row.dcMontoVag, 5),

                                            DpAgrupacionCentro = DiarioMovimientoPersistencia.Limitar(row.strAgrupacion, 250),

                                            DpGrupoCentro = DiarioMovimientoPersistencia.Limitar(row.strGrupo, 150),

                                            DpCtaNatura = "H",
                                            DpEstado = "AC",
                                            DpUsuarioCrea = usuario,
                                            DpFechaCrea = ahora,
                                            DpEquipoCrea = equipo
                                        });

                                    keys.Add(key);
                                    insertados++;
                                }
                            }

                            if (insertados > 0)
                                await ctx.SaveChangesAsync();

                            return true;
                        });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(GuardarCostoVentaSalidaHistorico),
                    ex);

                throw;
            }
        }

        public async Task<List<NotaCreditoRetornoContenedorDto>> ConsultarNotasCreditoRetornoContenedor(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                return await ManejoContext<SongDbContext>.EjecutarAsync(
                    _objSongFactory,
                    async ctx =>
                    {
                        DbConnection cn = ctx.Database.GetDbConnection();

                        if (cn.State != ConnectionState.Open)
                            await cn.OpenAsync();

                        await using DbCommand cmd = cn.CreateCommand();

                        cmd.CommandText = ValueObjectsRetornoContenedor.SqlNotasCreditoRetorno;

                        cmd.CommandType = CommandType.Text;

                        cmd.CommandTimeout = 180;

                        DbParameter pInicio = cmd.CreateParameter();
                        pInicio.ParameterName = "@fein";
                        pInicio.DbType = DbType.String;
                        pInicio.Size = 10;
                        pInicio.Value = dtFechaInicio.ToString("yyyy/MM/dd");

                        DbParameter pFin = cmd.CreateParameter();
                        pFin.ParameterName = "@fefi";
                        pFin.DbType = DbType.String;
                        pFin.Size = 10;
                        pFin.Value = dtFechaFin.ToString("yyyy/MM/dd");

                        cmd.Parameters.Add(pInicio);
                        cmd.Parameters.Add(pFin);

                        await using DbDataReader rd = await cmd.ExecuteReaderAsync();

                        return rd.MapToList<NotaCreditoRetornoContenedorDto>();
                    },
                    180);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarNotasCreditoRetornoContenedor),
                    ex);

                throw;
            }
        }

        public async Task<List<FacturaCostoSalidaDto>> ConsultarFacturasCostoSalida(
            DateOnly dtFechaInicio,
            DateOnly dtFechaFin)
        {
            try
            {
                return await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                    _objContextFactory,
                    async ctx =>
                    {
                        var pDesde = new SqlParameter(
                            "@desde",
                            SqlDbType.VarChar,
                            10)
                        {
                            Value = dtFechaInicio.ToString("yyyy/MM/dd")
                        };

                        var pHasta = new SqlParameter(
                            "@hasta",
                            SqlDbType.VarChar,
                            10)
                        {
                            Value = dtFechaFin.ToString("yyyy/MM/dd")
                        };

                        var pTipo = new SqlParameter(
                            "@tipo",
                            SqlDbType.VarChar,
                            1)
                        {
                            Value = "P"
                        };

                        using DbCommand cmd = ctx.Database.GetDbConnection().CreateCommand();

                        cmd.CommandText = "EXEC SPE_repfactpesoreal @desde, @hasta, @tipo";

                        cmd.CommandTimeout = 180;

                        cmd.Parameters.Add(pDesde);
                        cmd.Parameters.Add(pHasta);
                        cmd.Parameters.Add(pTipo);

                        if (cmd.Connection!.State != ConnectionState.Open)
                            await cmd.Connection.OpenAsync();

                        using DbDataReader rd = await cmd.ExecuteReaderAsync();

                        return DataReaderMapper.MapToList<FacturaCostoSalidaDto>(rd);
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarFacturasCostoSalida),
                    ex);

                throw;
            }
        }

        #endregion

        #region Configuración costo productivo

    /// <summary>
    /// Parte del mismo ProcesoParametro existente.
    ///
    /// IMPORTANTE:
    /// - No usa SPs ni DbCommand para el mantenimiento.
    /// - Se asume que las tablas de configuración YA fueron incorporadas al
    ///   modelo de CostosDbContext mediante Scaffold-DbContext.
    /// - Toda lectura/escritura se hace con EF Core sobre ConnectionCostos.
    /// - Las consultas son deliberadamente simples y se termina de relacionar
    ///   la información en memoria, evitando LINQ complejo innecesario.
    /// </summary>
        private static readonly HashSet<string> _hshFuentesIqfConfiguracion =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "50305010261", "50305010007", "50305010008", "50305010009",
                "50305010011", "50305010016", "50305010031", "50305010032",
                "50305010033", "50305010036", "50305010037", "50305010041",
                "50305010050", "50305010059", "50305010284", "50305010070",
                "50305010072", "50305010073", "50305010074", "50305010075",
                "50305010076", "50305010094", "50305010097", "50305010104",
                "50305010105", "50305010106", "50305010112", "50305010114",
                "50305010115", "50305010125", "50305010140", "50305010162",
                "50305010210", "50305010215", "50305010216", "50305010217",
                "50305010218", "50305010219", "50305010220", "50305010239",
                "50305010240", "50305010258", "50305010278", "50305010272",
                "50305010324", "50305010350", "50305010044"
            };

        // reutilizar la constante de diarios del dominio.
        private const string EtapaDiariosCodigo = CostManagement.Dominio.Reglas.DiarioMovimientoCodigos.Etapa;
        private const string MensajeEtapaDiarios = "La etapa DM pertenece a los diarios contables y no se administra desde esta pantalla.";

        private const int EmpresaSongCodigo = 1; // SONG -> 1 (valor fijo temporal de interfaz: SONGA)
        public async Task<CostoProductivoMantenimientoDto>
            ConsultarMantenimientoCostoProductivo(bool incluirCuentas = true)
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        // Se leen tablas independientes. La composición se hace en
                        // memoria para que el SQL generado por EF siga siendo básico.
                        var etapas = await ctx.TbEtapacosto
                            .AsNoTracking()
                            .OrderBy(x => x.EcOrden)
                            .ThenBy(x => x.EcCodigo)
                            .ToListAsync();

                        var procesos = await ctx.TbProcesocosto
                            .AsNoTracking()
                            .OrderBy(x => x.PcOrden)
                            .ThenBy(x => x.PcCodigo)
                            .ToListAsync();

                        var tiposLote = await ctx.TbTipolote
                            .AsNoTracking()
                            .OrderBy(x => x.TlCodigo)
                            .ToListAsync();

                        var tiposProceso = await ctx.TbTipoOtrosProcesos
                            .AsNoTracking()
                            .OrderBy(x => x.ToNivel)
                            .ThenBy(x => x.ToCodigo)
                            .ToListAsync();

                        // composición de catálogos entre bases en memoria.
                        var tiplotActivos = await ConsultarTiplotActivosConfiguracionAsync();
                        var tiposExistentes = tiposProceso.Select(x => NormalizarCfg(x.ToCodigo))
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                        var lotesActivos = tiposLote.Where(x => EstadoActivo(x.TlEstado))
                            .Select(x => NormalizarCfg(x.TlCodigo)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        var nivelesPorLote = tiposProceso
                            .GroupBy(x => NormalizarCfg(x.ToTlCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.GroupBy(x => x.ToNivel)
                                .OrderByDescending(n => n.Count()).ThenBy(n => n.Key)
                                .Select(n => (int)n.Key).First(), StringComparer.OrdinalIgnoreCase);

                        var matriz = await ctx.TbConfigTipoLiquidacionProcesoCosto
                            .AsNoTracking()
                            .OrderBy(x => x.CgToCodigo)
                            .ThenBy(x => x.CgPcCodigo)
                            .ToListAsync();

                        // lectura unificada de procesos y ParamProc.
                        var paramRows = await ctx.TbProcesoCosteo.AsNoTracking().OrderBy(x => x.PrId).ToListAsync();

                        var cuentas = await ctx.TbConfigCuentaCosto
                            .AsNoTracking()
                            .OrderBy(x => x.CcEcCodigo)
                            .ThenBy(x => x.CcCenCodigo)
                            .ThenBy(x => x.CcSubCodigo)
                            .ThenBy(x => x.CcCtaNumero)
                            .ToListAsync();

                        // conteo informativo de diarios por centro, separado del productivo.
                        var cantidadCuentasDiarios = cuentas
                            .Where(x => NormalizarCfg(x.CcEcCodigo) == EtapaDiariosCodigo && EstadoActivo(x.EcEstado))
                            .GroupBy(x => NormalizarCfg(x.CcCenCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
                        cuentas = cuentas.Where(x => NormalizarCfg(x.CcEcCodigo) != EtapaDiariosCodigo).ToList();
                        Dictionary<string, string> dicEtapas = etapas
                            .GroupBy(x => NormalizarCfg(x.EcCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                g => g.Key,
                                g => (g.First().EcNombre ?? string.Empty).Trim(),
                                StringComparer.OrdinalIgnoreCase);

                        Dictionary<string, string> dicTiposLote = tiposLote
                            .GroupBy(x => NormalizarCfg(x.TlCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                g => g.Key,
                                g => (g.First().TlNombre ?? string.Empty).Trim(),
                                StringComparer.OrdinalIgnoreCase);

                        HashSet<string> procesosEnMatriz = matriz
                            .Select(x => NormalizarCfg(x.CgPcCodigo))
                            .Where(x => x.Length > 0)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var procesosSnapshot = await ConsultarProcesosSnapshotConfiguracionAsync(ctx,
                            incluirCuentas ? cuentas : cuentas.Where(x => EstadoActivo(x.EcEstado)));
                        Dictionary<string, int> dicCantidadCuentas = ContarCuentasActivasPorProceso(cuentas, procesosSnapshot);

                        var resultado = new CostoProductivoMantenimientoDto
                        {
                            lstEtapas = etapas.Select(x => new EtapaCostoMantenimientoDto
                            {
                                blEsDiario = NormalizarCfg(x.EcCodigo) == EtapaDiariosCodigo,
                                strCodigo = NormalizarCfg(x.EcCodigo),
                                strNombre = (x.EcNombre ?? string.Empty).Trim(),
                                intOrden = x.EcOrden,
                                blActivo = EstadoActivo(x.EcEstado)
                            }).ToList(),

                            lstProcesos = procesos.Select(x =>
                            {
                                string etapaCodigo = NormalizarCfg(x.PcEcCodigo);
                                string etapaNombre;
                                dicEtapas.TryGetValue(etapaCodigo, out etapaNombre);

                                return new ProcesoCostoMantenimientoDto
                                {
                                    strCodigo = NormalizarCfg(x.PcCodigo),
                                    strNombre = (x.PcNombre ?? string.Empty).Trim(),
                                    strEtapaCodigo = etapaCodigo,
                                    strEtapa = etapaNombre ?? string.Empty,
                                    intOrden = x.PcOrden,
                                    blActivo = EstadoActivo(x.PcEstado),
                                    blEnMatriz = procesosEnMatriz.Contains(NormalizarCfg(x.PcCodigo)),
                                    intCantidadCuentas = dicCantidadCuentas.GetValueOrDefault(NormalizarCfg(x.PcCodigo))
                                };
                            }).ToList(),

                            lstTiposLote = tiposLote.Select(x => new TipoLoteMantenimientoDto
                            {
                                strCodigo = NormalizarCfg(x.TlCodigo),
                                strNombre = (x.TlNombre ?? string.Empty).Trim(),
                                blActivo = EstadoActivo(x.TlEstado)
                            }).ToList(),

                            lstTiposProcesoDisponibles = tiplotActivos
                                .Where(x => !tiposExistentes.Contains(NormalizarCfg(x.TipCodigo)))
                                .OrderBy(x => x.TipCodigo.Trim(), StringComparer.OrdinalIgnoreCase)
                                .Select(x => new TipoProcesoDisponibleDto
                                {
                                    strCodigo = x.TipCodigo.Trim(),
                                    strNombre = (x.TipDescri ?? string.Empty).Trim(),
                                    strTipoLoteCodigo = (x.TipLote ?? string.Empty).Trim(),
                                    strTipoLote = dicTiposLote.GetValueOrDefault(NormalizarCfg(x.TipLote), string.Empty),
                                    intNivelSugerido = nivelesPorLote.GetValueOrDefault(NormalizarCfg(x.TipLote), 2),
                                    blLoteValido = lotesActivos.Contains(NormalizarCfg(x.TipLote))
                                }).ToList(),

                            lstTiposProceso = tiposProceso.Select(x =>
                            {
                                string tipoLoteCodigo = NormalizarCfg(x.ToTlCodigo);
                                string tipoLoteNombre;
                                dicTiposLote.TryGetValue(tipoLoteCodigo, out tipoLoteNombre);

                                return new TipoProcesoMantenimientoDto
                                {
                                    strCodigo = NormalizarCfg(x.ToCodigo),
                                    strNombre = (x.ToNombre ?? string.Empty).Trim(),
                                    strTipoLoteCodigo = tipoLoteCodigo,
                                    strTipoLote = tipoLoteNombre ?? string.Empty,
                                    intNivel = x.ToNivel,
                                    blActivo = EstadoActivo(x.ToEstado),
                                    blEsNuevo = false
                                };
                            }).ToList(),

                            lstMatriz = matriz.Select(x => new AplicacionCostoMantenimientoDto
                            {
                                strProcesoCodigo = NormalizarCfg(x.CgPcCodigo),
                                strTipoCodigo = NormalizarCfg(x.CgToCodigo),
                                strRegla = ReglaCostoMantenimientoDto.Normalizar(x.CgConfig),
                                blActivo = EstadoActivo(x.CgEstado)
                            }).ToList(),

                            lstCuentas = incluirCuentas
                                ? ConstruirCuentasMantenimiento(cuentas, dicEtapas, procesosSnapshot)
                                : new List<CuentaCostoMantenimientoDto>()
                        };

                        CompletarProcesosConParamProc(resultado.lstProcesos, paramRows, matriz, tiposProceso);
                        // Completar primero
                        // impide que los ParamProc de diarios reaparezcan como filas sin proceso.
                        var codigosDiarios = procesos.Where(x => NormalizarCfg(x.PcEcCodigo) == EtapaDiariosCodigo)
                            .Select(x => NormalizarCfg(x.PcCodigo)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        foreach (var proceso in resultado.lstProcesos.Where(x => x.blEsDiario))
                            proceso.intCantidadCuentas = cantidadCuentasDiarios.GetValueOrDefault(NormalizarCfg(proceso.strCodigo));
                        resultado.lstMatriz.RemoveAll(x => codigosDiarios.Contains(NormalizarCfg(x.strProcesoCodigo)));

                        if (incluirCuentas)
                        {
                            Dictionary<string, string> dicProcesoNombreC = procesos
                                .GroupBy(x => NormalizarCfg(x.PcCodigo), StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(g => g.Key, g => (g.First().PcNombre ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);

                            await EnriquecerCuentasMantenimientoAsync(resultado.lstCuentas, dicProcesoNombreC);
                        }

                        return resultado;
                    },
                    intTimeout: 180);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarMantenimientoCostoProductivo),
                    ex);
                throw;
            }
        }

        private static List<CuentaCostoMantenimientoDto> ConstruirCuentasMantenimiento(
            List<TbConfigCuentaCosto> cuentas,
            Dictionary<string, string> dicEtapas,
            Dictionary<string, string> procesosSnapshot)
        {
            return cuentas.Where(x => NormalizarCfg(x.CcEcCodigo) != EtapaDiariosCodigo).Select(x =>
            {
                string etapaCodigo = NormalizarCfg(x.CcEcCodigo);
                string etapaNombre;
                dicEtapas.TryGetValue(etapaCodigo, out etapaNombre);

                var dto = new CuentaCostoMantenimientoDto
                {
                    intId = x.CcId,
                    intEmpresa = x.CcEmpCodigo,
                    strEtapaCodigo = etapaCodigo,
                    strEtapa = etapaNombre ?? string.Empty,
                    strCentroCodigo = NormalizarCfg(x.CcCenCodigo),
                    strSubcentroCodigo = NormalizarCfg(x.CcSubCodigo),
                    strCuenta = NormalizarCfg(x.CcCtaNumero),
                    blActivo = EstadoActivo(x.EcEstado),
                    blEsNuevo = false
                };

                ResolverCuentaConfiguracion(dto, ObtenerProcesoGuardadoOSnapshot(x, procesosSnapshot));
                return dto;
            }).ToList();
        }

        /// <summary>Nombres reales de cuenta / centro / subcentro / auxiliar desde tb_maecta (SONG), en lotes.</summary>
        private async Task EnriquecerCuentasMantenimientoAsync(
            List<CuentaCostoMantenimientoDto> cuentas,
            Dictionary<string, string> dicProcesoNombre)
        {
            // Ronda 1: códigos configurados. Ronda 2: CtaRelaci (auxiliar) que falte.
            Dictionary<string, TbMaecta> dicMaecta = await ConsultarMaectaPorCodigosAsync(
                cuentas.Select(x => x.strCuenta)
                    .Concat(cuentas.Select(x => x.strCentroCodigo))
                    .Concat(cuentas.Select(x => x.strSubcentroCodigo)));

            Dictionary<string, TbMaecta> dicAuxiliares = await ConsultarMaectaPorCodigosAsync(
                dicMaecta.Values
                    .Where(m => !dicMaecta.ContainsKey((m.CtaRelaci ?? string.Empty).Trim()))
                    .Select(m => m.CtaRelaci ?? string.Empty));

            foreach (CuentaCostoMantenimientoDto dto in cuentas)
            {
                dicMaecta.TryGetValue(dto.strCuenta, out TbMaecta? maCuenta);
                dicMaecta.TryGetValue(dto.strCentroCodigo, out TbMaecta? maCentro);
                dicMaecta.TryGetValue(dto.strSubcentroCodigo, out TbMaecta? maSub);
                string auxCodigo = (maCuenta?.CtaRelaci ?? string.Empty).Trim();
                TbMaecta? maAux;
                if (!dicMaecta.TryGetValue(auxCodigo, out maAux)) dicAuxiliares.TryGetValue(auxCodigo, out maAux);

                dto.strCuentaNombre = NombreMaecta(maCuenta);
                dto.strCentroNombre = NombreMaecta(maCentro);
                dto.strSubcentroNombre = NombreMaecta(maSub);
                dto.strAuxiliarCodigo = auxCodigo;
                dto.strAuxiliarNombre = NombreMaecta(maAux);

                bool soloUno = dto.strProcesoResuelto.IndexOf('+') < 0;
                dto.strProcesoCodigo = soloUno ? dto.strProcesoResuelto : string.Empty;
                dto.strProcesoNombre = soloUno
                    ? dicProcesoNombre.GetValueOrDefault(dto.strProcesoResuelto, string.Empty)
                    : dto.strProcesoResuelto;
            }
        }

        /// <summary>Segunda llamada del front: cuentas configuradas con toda su configuración y nombres reales.</summary>
        public async Task<List<CuentaCostoMantenimientoDto>> ConsultarCuentasMantenimientoCostoProductivo()
        {
            try
            {
                var datos = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        var etapas = await ctx.TbEtapacosto.AsNoTracking().ToListAsync();
                        var procesos = await ctx.TbProcesocosto.AsNoTracking().ToListAsync();
                        var cuentas = await ctx.TbConfigCuentaCosto
                            .AsNoTracking()
                            .OrderBy(x => x.CcEcCodigo)
                            .ThenBy(x => x.CcCenCodigo)
                            .ThenBy(x => x.CcSubCodigo)
                            .ThenBy(x => x.CcCtaNumero)
                            .ToListAsync();

                        cuentas = cuentas.Where(x => NormalizarCfg(x.CcEcCodigo) != EtapaDiariosCodigo).ToList();
                        // lectura por lotes solo de las cuentas devueltas sin proceso guardado.
                        var procesosSnapshot = await ConsultarProcesosSnapshotConfiguracionAsync(ctx, cuentas);
                        return (etapas, procesos, cuentas, procesosSnapshot);
                    },
                    180);

                Dictionary<string, string> dicEtapas = datos.etapas
                    .GroupBy(x => NormalizarCfg(x.EcCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => (g.First().EcNombre ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);

                Dictionary<string, string> dicProcesoNombre = datos.procesos
                    .GroupBy(x => NormalizarCfg(x.PcCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => (g.First().PcNombre ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);

                List<CuentaCostoMantenimientoDto> lista = ConstruirCuentasMantenimiento(datos.cuentas, dicEtapas, datos.procesosSnapshot);
                await EnriquecerCuentasMantenimientoAsync(lista, dicProcesoNombre);
                return lista;
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarCuentasMantenimientoCostoProductivo),
                    ex);
                throw;
            }
        }

        /// <summary>
        /// Snapshot liviano que consume el motor. Se carga una sola vez por flujo
        /// de costeo y luego se consulta en memoria.
        /// </summary>
        public async Task<ConfiguracionCostoProductivoRuntimeDto>
            ConsultarConfiguracionRuntimeCostoProductivo()
        {
            try
            {
                return await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        var procesos = await ctx.TbProcesocosto
                            .AsNoTracking()
                            .Where(x => x.PcEstado == "AC" && x.PcEcCodigo != EtapaDiariosCodigo)
                            .OrderBy(x => x.PcOrden)
                            .ToListAsync();

                        var tipos = await ctx.TbTipoOtrosProcesos
                            .AsNoTracking()
                            .Where(x => x.ToEstado == "AC")
                            .ToListAsync();

                        var matriz = await ctx.TbConfigTipoLiquidacionProcesoCosto
                            .AsNoTracking()
                            .Where(x => x.CgEstado == "AC")
                            .ToListAsync();

                        Dictionary<string, TbProcesocosto> dicProcesos = procesos
                            .GroupBy(x => NormalizarCfg(x.PcCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        Dictionary<string, TbTipoOtrosProcesos> dicTipos = tipos
                            .GroupBy(x => NormalizarCfg(x.ToCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        var aplicaciones = new List<ProcesoCostoAplicacionDto>();

                        foreach (var item in matriz)
                        {
                            string pc = NormalizarCfg(item.CgPcCodigo);
                            string tipo = NormalizarCfg(item.CgToCodigo);

                            TbProcesocosto proceso;
                            TbTipoOtrosProcesos tipoProceso;

                            if (!dicProcesos.TryGetValue(pc, out proceso))
                                continue;

                            if (!dicTipos.TryGetValue(tipo, out tipoProceso))
                                continue;

                            aplicaciones.Add(new ProcesoCostoAplicacionDto
                            {
                                strPcCodigo = pc,
                                strPcNombre = (proceso.PcNombre ?? string.Empty).Trim(),
                                dcTarifa = null,
                                strTipoProcesoCodigo = tipo,
                                strTipoProceso = (tipoProceso.ToNombre ?? string.Empty).Trim(),
                                strConfig = ReglaCostoMantenimientoDto.Normalizar(item.CgConfig)
                            });
                        }

                        return new ConfiguracionCostoProductivoRuntimeDto
                        {
                            lstProcesos = procesos.Select(x => new ProcesoCostoMantenimientoDto
                            {
                                strUso = "COSTEO",
                                strCodigo = NormalizarCfg(x.PcCodigo),
                                strNombre = (x.PcNombre ?? string.Empty).Trim(),
                                strEtapaCodigo = NormalizarCfg(x.PcEcCodigo),
                                intOrden = x.PcOrden,
                                blActivo = true,
                                blEnMatriz = matriz.Any(m =>
                                    string.Equals(
                                        NormalizarCfg(m.CgPcCodigo),
                                        NormalizarCfg(x.PcCodigo),
                                        StringComparison.OrdinalIgnoreCase))
                            }).ToList(),

                            lstAplicaciones = aplicaciones,

                            lstNiveles = tipos.Select(x => new TipoProcesoNivelRuntimeDto
                            {
                                strCodigo = NormalizarCfg(x.ToCodigo),
                                intNivel = x.ToNivel
                            }).ToList()
                        };
                    },
                    intTimeout: 180);
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarConfiguracionRuntimeCostoProductivo),
                    ex);
                throw;
            }
        }

        public async Task<bool> GuardarMantenimientoCostoProductivo(
            GuardarCostoProductivoMantenimientoRequest request)
        {
            return (await GuardarMantenimientoCostoProductivoConResultado(request)).blGuardado;
        }

        public async Task<GuardarCostoProductivoMantenimientoResponse> GuardarMantenimientoCostoProductivoConResultado(
            GuardarCostoProductivoMantenimientoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.strUsuario))
                throw new ArgumentException("El usuario es obligatorio.");

            // medir tanto el guardado confirmado como los fallos.
            // var tiempoTotal = System.Diagnostics.Stopwatch.StartNew();
            // var tiempoProcesos = new System.Diagnostics.Stopwatch();
            // var tiempoTipos = new System.Diagnostics.Stopwatch();
            // var tiempoMatriz = new System.Diagnostics.Stopwatch();
            // var tiempoCuentas = new System.Diagnostics.Stopwatch();
            // bool guardado = false;
            try
            {
                var respuesta = await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
                    async ctx =>
                    {
                        // mismo protocolo del cierre, misma conexion y transaccion EF.
                        await ctx.Database.ExecuteSqlRawAsync(
                            "DECLARE @intResultado int; EXEC @intResultado = sys.sp_getapplock @Resource={0}, " +
                            "@LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; " +
                            "IF @intResultado < 0 BEGIN RAISERROR(N'Otra persona está guardando la configuración; intente de nuevo en unos segundos.', 16, 1); RETURN; END;",
                            "costos.configCuentaCosto");
                        bool hayProcesos = request.lstProcesos?.Count > 0;
                        bool hayTipos = request.lstTiposProceso?.Count > 0;
                        bool hayMatriz = request.lstMatriz?.Count > 0;
                        var cuentasSolicitadas = request.lstCuentas ?? new List<CuentaCostoMantenimientoDto>();
                        // tiempoProcesos.Start();
                        string usuario = request.strUsuario.Trim();
                        string equipo = Environment.MachineName;
                        DateTime ahora = DateTime.Now;

                        // =====================================================
                        // 0. PROCESOS DE COSTO (tb_procesocosto) - INSERT / UPDATE por PcCodigo
                        // =====================================================
                        // carga de procesos movida a GuardarProcesosUnificadosAsync.
                        // var existentesProceso = await ctx.TbProcesocosto.AsTracking().ToListAsync();

                        HashSet<string> etapasValidas = hayProcesos
                            ? (await ctx.TbEtapacosto.AsNoTracking().Select(x => x.EcCodigo).ToListAsync())
                                .Select(NormalizarCfg).ToHashSet(StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        // Proyeccion minima para preservar la resolucion historica, incluido CLA + CAJ.
                        var cuentasActivasPrevias = hayProcesos
                            ? await ctx.TbConfigCuentaCosto.AsNoTracking()
                                .Where(x => (x.EcEstado ?? "").Trim().ToUpper() == "AC")
                                .Select(x => new TbConfigCuentaCosto
                                {
                                    EcEstado = x.EcEstado, CcPcCodigo = x.CcPcCodigo,
                                    CcEcCodigo = x.CcEcCodigo, CcCenCodigo = x.CcCenCodigo,
                                    CcSubCodigo = x.CcSubCodigo, CcCtaNumero = x.CcCtaNumero
                                }).ToListAsync()
                            : new List<TbConfigCuentaCosto>();
                        // adelantar la lectura acotada v12 para compartir un solo lote de snapshots
                        // entre el conteo previo y la detecci?n de transferencias del request.
                        var existentesCuenta = await CargarCuentasGuardadoConfiguracionAsync(ctx, cuentasSolicitadas);
                        // Omitir las cuentas DM identicas antes de escribir.
                        cuentasSolicitadas = cuentasSolicitadas.Where(dto =>
                        {
                            if (dto == null) throw new InvalidOperationException("Existe una cuenta nula.");
                            var anterior = existentesCuenta.FirstOrDefault(x => dto.intId > 0 && x.CcId == dto.intId);
                            bool destinoDiario = NormalizarCfg(dto.strEtapaCodigo) == EtapaDiariosCodigo;
                            if (anterior != null && NormalizarCfg(anterior.CcEcCodigo) == EtapaDiariosCodigo)
                            {
                                bool sinCambios = !dto.blEsNuevo && destinoDiario
                                    && dto.intEmpresa == anterior.CcEmpCodigo
                                    && NormalizarCfg(dto.strCuenta) == NormalizarCfg(anterior.CcCtaNumero)
                                    && NormalizarCfg(dto.strCentroCodigo) == NormalizarCfg(anterior.CcCenCodigo)
                                    && NormalizarCfg(dto.strSubcentroCodigo) == NormalizarCfg(anterior.CcSubCodigo)
                                    && NormalizarCfg(dto.strProcesoCodigo) == NormalizarCfg(anterior.CcPcCodigo)
                                    && dto.blActivo == EstadoActivo(anterior.EcEstado);
                                if (sinCambios) return false;
                                throw new InvalidOperationException(MensajeEtapaDiarios);
                            }
                            if (destinoDiario) throw new InvalidOperationException(MensajeEtapaDiarios);
                            return true;
                        }).ToList();
                        var procesosSnapshot = await ConsultarProcesosSnapshotConfiguracionAsync(ctx,
                            cuentasActivasPrevias.Concat(existentesCuenta));
                        Dictionary<string, int> dicCantidadPrevia = ContarCuentasActivasPorProceso(cuentasActivasPrevias, procesosSnapshot);

                                // entidad = new TbProcesocosto
                                // {
                                    // PcCodigo = codigo,
                                    // PcNombre = dto.strNombre.Trim(),
                                    // PcEcCodigo = NormalizarCfg(dto.strEtapaCodigo),
                                    // PcOrden = checked((short)(dto.intOrden > 0 ? dto.intOrden : existentesProceso.Count + 1)),
                                    // PcEstado = dto.blActivo ? "AC" : "AN",
                                    // PcUsuarioCrea = usuario,
                                    // PcFechaCrea = ahora,
                                    // PcEquipoCrea = equipo
                                // };

                                // await ctx.TbProcesocosto.AddAsync(entidad);
                                // dicProceso[codigo] = entidad;
                            // }

                            // _objLogger.LogInformation(
                                // "[ConfiguracionCostoProductivo] Proceso {Proceso} {Accion} Usuario={Usuario}",
                                // codigo, dto.blEsNuevo ? "INSERT" : "UPDATE", usuario);
                        // }

                        if (hayProcesos)
                            await GuardarProcesosUnificadosAsync(ctx, request.lstProcesos, usuario, equipo, ahora, dicCantidadPrevia, etapasValidas);
                        // tiempoProcesos.Stop();
                        // tiempoTipos.Start();

                        // =====================================================
                        // 1. TIPOS DE PROCESO / NIVEL
                        // =====================================================
                        var existentesTipo = hayTipos
                            ? await ctx.TbTipoOtrosProcesos.AsTracking().ToListAsync()
                            : new List<TbTipoOtrosProcesos>();

                        Dictionary<string, TbTipoOtrosProcesos> dicTipo = existentesTipo
                            .GroupBy(x => NormalizarCfg(x.ToCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        // Consultar PRODUCCION solo cuando se solicita algún tipo nuevo.
                        var tiposSolicitados = request.lstTiposProceso ?? new List<TipoProcesoMantenimientoDto>();
                        var tiplotActivos = tiposSolicitados.Any(x => x != null && !dicTipo.ContainsKey(NormalizarCfg(x.strCodigo)))
                            ? await ConsultarTiplotActivosConfiguracionAsync() : new List<TbTiplot>();
                        var dicTiplot = tiplotActivos.GroupBy(x => NormalizarCfg(x.TipCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                        var lotesValidos = hayTipos
                            ? (await ctx.TbTipolote.AsNoTracking().ToListAsync())
                                .Where(x => EstadoActivo(x.TlEstado)).Select(x => NormalizarCfg(x.TlCodigo))
                                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (TipoProcesoMantenimientoDto dto in tiposSolicitados)
                        {
                            if (dto == null)
                                throw new InvalidOperationException("Existe un tipo de proceso nulo.");

                            string codigo = NormalizarCfg(dto.strCodigo);
                            if (!dicTipo.ContainsKey(codigo))
                            {
                                if (!dicTiplot.TryGetValue(codigo, out var tiplot))
                                    throw new InvalidOperationException("El tipo " + codigo + " no existe o no está activo en tb_tiplot.");
                                dto.strCodigo = tiplot.TipCodigo.Trim();
                                dto.strNombre = (tiplot.TipDescri ?? string.Empty).Trim();
                                dto.strTipoLoteCodigo = (tiplot.TipLote ?? string.Empty).Trim();
                                if (!lotesValidos.Contains(NormalizarCfg(dto.strTipoLoteCodigo)))
                                    throw new InvalidOperationException("El lote " + dto.strTipoLoteCodigo + " del tipo " + codigo + " no existe o no está activo en tb_tipolote.");
                                if (dto.intNivel < 1 || dto.intNivel > 9)
                                    throw new InvalidOperationException("El nivel del tipo nuevo debe estar entre NV1 y NV9.");
                            }
                            else
                            {
                                ValidarTipoProcesoMantenimiento(dto);
                            }

                            TbTipoOtrosProcesos entidad;

                            if (dicTipo.TryGetValue(codigo, out entidad))
                            {
                                entidad.ToNombre = dto.strNombre.Trim();
                                entidad.ToTlCodigo = NormalizarCfg(dto.strTipoLoteCodigo);
                                entidad.ToNivel = checked((byte)dto.intNivel);
                                entidad.ToEstado = dto.blActivo ? "AC" : "AN";
                            }
                            else
                            {
                                entidad = new TbTipoOtrosProcesos
                                {
                                    // ToCodigo = codigo,
                                    ToCodigo = dto.strCodigo.Trim(),
                                    ToNombre = dto.strNombre.Trim(),
                                    ToTlCodigo = NormalizarCfg(dto.strTipoLoteCodigo),
                                    ToNivel = checked((byte)dto.intNivel),
                                    ToEstado = dto.blActivo ? "AC" : "AN",
                                    ToUsuarioCrea = usuario,
                                    ToFechaCrea = ahora,
                                    ToEquipoCrea = equipo
                                };

                                await ctx.TbTipoOtrosProcesos.AddAsync(entidad);
                                dicTipo[codigo] = entidad;
                            }
                        }

                        // Debe guardar primero los tipos nuevos porque la matriz tiene FK.
                        if (hayTipos) await ctx.SaveChangesAsync();
                        // tiempoTipos.Stop();
                        // tiempoMatriz.Start();

                        // =====================================================
                        // 2. MATRIZ PROCESO x TIPO LIQUIDACION
                        // =====================================================
                        var existentesMatriz = hayMatriz
                            ? await ctx.TbConfigTipoLiquidacionProcesoCosto.AsTracking().ToListAsync()
                            : new List<TbConfigTipoLiquidacionProcesoCosto>();

                        Dictionary<string, TbConfigTipoLiquidacionProcesoCosto> dicMatriz =
                            existentesMatriz
                                .GroupBy(
                                    x => ClaveMatriz(x.CgPcCodigo, x.CgToCodigo),
                                    StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        // proteger las celdas existentes de diarios.
                        var procesosDiariosMatriz = hayMatriz
                            ? (await ctx.TbProcesocosto.AsNoTracking().ToListAsync())
                                .Where(x => NormalizarCfg(x.PcEcCodigo) == EtapaDiariosCodigo)
                                .Select(x => NormalizarCfg(x.PcCodigo)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (AplicacionCostoMantenimientoDto dto in request.lstMatriz ?? new List<AplicacionCostoMantenimientoDto>())
                        {
                            string pc = NormalizarCfg(dto.strProcesoCodigo);
                            string tipo = NormalizarCfg(dto.strTipoCodigo);
                            string regla = ReglaCostoMantenimientoDto.Normalizar(dto.strRegla);

                            if (pc.Length == 0 || tipo.Length == 0)
                                throw new InvalidOperationException("Proceso y tipo son obligatorios en la matriz.");

                            if (!ReglaCostoMantenimientoDto.EsValida(regla))
                                throw new InvalidOperationException(
                                    "La regla '" + regla + "' no es válida para " + pc + "/" + tipo + ".");

                            if (procesosDiariosMatriz.Contains(pc))
                            {
                                if (dicMatriz.TryGetValue(ClaveMatriz(pc, tipo), out var diario)
                                    && ReglaCostoMantenimientoDto.Normalizar(diario.CgConfig) == regla
                                    && EstadoActivo(diario.CgEstado) == dto.blActivo) continue;
                                throw new InvalidOperationException(MensajeEtapaDiarios);
                            }
                            string clave = ClaveMatriz(pc, tipo);
                            TbConfigTipoLiquidacionProcesoCosto entidad;

                            if (dicMatriz.TryGetValue(clave, out entidad))
                            {
                                entidad.CgConfig = regla;
                                entidad.CgEstado = dto.blActivo ? "AC" : "AN";
                            }
                            else
                            {
                                entidad = new TbConfigTipoLiquidacionProcesoCosto
                                {
                                    CgPcCodigo = pc,
                                    CgToCodigo = tipo,
                                    CgConfig = regla,
                                    CgEstado = dto.blActivo ? "AC" : "AN",
                                    CgUsuarioCrea = usuario,
                                    CgFechaCrea = ahora,
                                    CgEquipoCrea = equipo
                                };

                                await ctx.TbConfigTipoLiquidacionProcesoCosto.AddAsync(entidad);
                                dicMatriz[clave] = entidad;
                            }
                        }

                        // reemplazado por upsert por clave fisica (Empresa+Cuenta+Centro+Subcentro)
                        // =====================================================
                        // 3. CUENTAS CONTABLES - upsert por clave física
                        //    INSERT si no existe; UPDATE/REACTIVAR si existe AN; duplicado si existe AC.
                        //    Anular = UPDATE a AN (nunca DELETE).
                        // =====================================================
                        // var existentesCuenta = await ctx.TbConfigCuentaCosto
                        // .AsTracking()
                        // .ToListAsync();
                        // tiempoMatriz.Stop();
                        // tiempoCuentas.Start();
                        // existentesCuenta ya se carg? bajo el mismo applock y transacci?n v12.

                        Dictionary<short, TbConfigCuentaCosto> dicCuenta = existentesCuenta
                            .ToDictionary(x => x.CcId, x => x);

                        // Filas ACTIVAS por clave física (se mantiene al día durante el request).
                        Dictionary<string, TbConfigCuentaCosto> dicActivas = new Dictionary<string, TbConfigCuentaCosto>(StringComparer.OrdinalIgnoreCase);
                        foreach (TbConfigCuentaCosto x in existentesCuenta.Where(c => EstadoActivo(c.EcEstado)))
                            dicActivas[ClaveCuenta(x.CcEmpCodigo, x.CcCtaNumero, x.CcCenCodigo, x.CcSubCodigo)] = x;

                        // Filas ANULADAS por clave física (la más reciente).
                        Dictionary<string, TbConfigCuentaCosto> dicAnuladas = existentesCuenta
                            .Where(c => !EstadoActivo(c.EcEstado))
                            .GroupBy(c => ClaveCuenta(c.CcEmpCodigo, c.CcCtaNumero, c.CcCenCodigo, c.CcSubCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CcId).First(), StringComparer.OrdinalIgnoreCase);

                        // El SaveChanges anterior incluye los procesos nuevos del request.
                        var codigosProcesoCuenta = cuentasSolicitadas.Where(x => x != null)
                            .Select(x => NormalizarCfg(x.strProcesoCodigo)).Where(x => x.Length > 0).Distinct().ToList();
                        var procesosCuenta = (await ctx.TbProcesocosto.AsNoTracking()
                            .WhereInBatchAsync(x => (x.PcCodigo ?? "").Trim().ToUpper(), codigosProcesoCuenta))
                            .GroupBy(x => NormalizarCfg(x.PcCodigo), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        int cuentasInsert = 0, cuentasUpdate = 0, cuentasReactivadas = 0, cuentasAnuladas = 0;
                        // solo una reasignación de una cuenta activa invalida valorizaciones.
                        bool hayTransferencia = false;

                        foreach (CuentaCostoMantenimientoDto dto in cuentasSolicitadas)
                        {
                            ValidarCuentaMantenimiento(dto);

                            string procesoCuenta = NormalizarCfg(dto.strProcesoCodigo);
                            void ValidarProcesoCuenta(TbConfigCuentaCosto? anterior)
                            {
                                bool requiereProceso = anterior == null
                                    || (dto.blActivo && !EstadoActivo(anterior.EcEstado))
                                    || NormalizarCfg(anterior.CcEcCodigo) != NormalizarCfg(dto.strEtapaCodigo)
                                    || NormalizarCfg(anterior.CcPcCodigo) != procesoCuenta;
                                if (procesoCuenta.Length == 0)
                                {
                                    if (requiereProceso)
                                        throw new InvalidOperationException("Debe indicar el proceso productivo de la cuenta " + dto.strCuenta + ".");
                                    return;
                                }
                                if (!procesosCuenta.TryGetValue(procesoCuenta, out var procesoSeleccionado))
                                    throw new InvalidOperationException("El proceso " + procesoCuenta + " de la cuenta " + dto.strCuenta + " no existe.");
                                if (!EstadoActivo(procesoSeleccionado.PcEstado))
                                    throw new InvalidOperationException("El proceso " + procesoCuenta + " de la cuenta " + dto.strCuenta + " no está activo.");
                                if (NormalizarCfg(procesoSeleccionado.PcEcCodigo) != NormalizarCfg(dto.strEtapaCodigo))
                                    throw new InvalidOperationException("El proceso " + procesoCuenta + " no pertenece a la etapa " + dto.strEtapaCodigo + " de la cuenta " + dto.strCuenta + ".");
                            }

                            short empresa = checked((short)dto.intEmpresa);
                            string claveNueva = ClaveCuenta(empresa, dto.strCuenta, dto.strCentroCodigo, dto.strSubcentroCodigo);
                            TbConfigCuentaCosto entidad;

                            if (dto.intId > 0 && dicCuenta.TryGetValue((short)dto.intId, out entidad))
                            {
                                // transferencia sobre el mismo CcId; valida destino y persiste proceso/etapa
                                // dentro de la transacción existente, con auditoría Mod y sin inactivar el origen.
                                ValidarProcesoCuenta(entidad);
                                string? procesoGuardadoOSnapshot = ObtenerProcesoGuardadoOSnapshot(entidad, procesosSnapshot);
                                string procesoAnterior = NormalizarCfg(procesoGuardadoOSnapshot);
                                if (procesoGuardadoOSnapshot == null)
                                {
                                    var cuentaAnterior = new CuentaCostoMantenimientoDto
                                    {
                                        strEtapaCodigo = NormalizarCfg(entidad.CcEcCodigo),
                                        strCentroCodigo = NormalizarCfg(entidad.CcCenCodigo),
                                        strSubcentroCodigo = NormalizarCfg(entidad.CcSubCodigo),
                                        strCuenta = NormalizarCfg(entidad.CcCtaNumero)
                                    };
                                    ResolverCuentaConfiguracion(cuentaAnterior);
                                    procesoAnterior = NormalizarCfg(NormalizarCfg(cuentaAnterior.strProcesoResuelto).Split('+')[0]);
                                }
                                string etapaAnterior = NormalizarCfg(entidad.CcEcCodigo);
                                if (EstadoActivo(entidad.EcEstado) && dto.blActivo
                                    && ((procesoAnterior.Length > 0 && procesoAnterior != procesoCuenta)
                                        || (etapaAnterior.Length > 0 && etapaAnterior != NormalizarCfg(dto.strEtapaCodigo))))
                                    hayTransferencia = true;
                                entidad.CcPcCodigo = procesoCuenta.Length > 0 ? procesoCuenta : null;
                                string claveActual = ClaveCuenta(entidad.CcEmpCodigo, entidad.CcCtaNumero, entidad.CcCenCodigo, entidad.CcSubCodigo);
                                bool eraActiva = EstadoActivo(entidad.EcEstado);

                                // Duplicado: va a quedar ACTIVA con una clave que ya usa OTRA fila activa.
                                TbConfigCuentaCosto otra;
                                if (dto.blActivo && dicActivas.TryGetValue(claveNueva, out otra) && otra.CcId != entidad.CcId)
                                    throw new InvalidOperationException(
                                        "La cuenta " + NormalizarCfg(dto.strCuenta) + " (centro " + NormalizarCfg(dto.strCentroCodigo) +
                                        ", subcentro " + NormalizarCfg(dto.strSubcentroCodigo) + ") ya está activa en la empresa " + empresa + ".");

                                entidad.CcEmpCodigo = empresa;
                                entidad.CcEcCodigo = NormalizarCfg(dto.strEtapaCodigo);
                                entidad.CcCenCodigo = NormalizarCfg(dto.strCentroCodigo);
                                entidad.CcSubCodigo = NormalizarCfg(dto.strSubcentroCodigo);
                                entidad.CcCtaNumero = NormalizarCfg(dto.strCuenta);
                                entidad.EcEstado = dto.blActivo ? "AC" : "AN";
                                entidad.EcUsuarioMod = usuario;
                                entidad.EcFechaMod = ahora;
                                entidad.EcEquipoMod = equipo;

                                if (!dto.blActivo)
                                {
                                    entidad.EcUsuarioEli = usuario;
                                    entidad.EcFechaEli = ahora;
                                    entidad.EcEquipoEli = equipo;
                                }
                                else
                                {
                                    entidad.EcUsuarioEli = null;
                                    entidad.EcFechaEli = null;
                                    entidad.EcEquipoEli = null;
                                }

                                dicActivas.Remove(claveActual);
                                if (dto.blActivo) dicActivas[claveNueva] = entidad;

                                string accion = dto.blActivo ? (eraActiva ? "UPDATE" : "REACTIVAR") : "ANULAR";
                                if (accion == "UPDATE") cuentasUpdate++; else if (accion == "REACTIVAR") cuentasReactivadas++; else cuentasAnuladas++;

                                _objLogger.LogInformation(
                                    "[ConfiguracionCostoProductivo] Cuenta {Cuenta} {Accion} Empresa={Empresa} Centro={Centro} Subcentro={Sub} Usuario={Usuario}",
                                    entidad.CcCtaNumero, accion, empresa, entidad.CcCenCodigo, entidad.CcSubCodigo, usuario);
                            }
                            else
                            {
                                // Alta: la clave física decide INSERT / REACTIVAR / duplicado.
                                if (dicActivas.ContainsKey(claveNueva))
                                    throw new InvalidOperationException(
                                        "La cuenta " + NormalizarCfg(dto.strCuenta) + " (centro " + NormalizarCfg(dto.strCentroCodigo) +
                                        ", subcentro " + NormalizarCfg(dto.strSubcentroCodigo) + ") ya está activa en la empresa " + empresa + ".");

                                if (!dto.blActivo)
                                    continue; // alta inactiva de algo que no está activo: sin efecto

                                if (dicAnuladas.TryGetValue(claveNueva, out entidad))
                                {
                                    // proteger filas DM tambien al reactivar por clave fisica.
                                    if (NormalizarCfg(entidad.CcEcCodigo) == EtapaDiariosCodigo)
                                        throw new InvalidOperationException(MensajeEtapaDiarios);
                                    ValidarProcesoCuenta(entidad);
                                    entidad.CcPcCodigo = procesoCuenta;
                                    entidad.CcEcCodigo = NormalizarCfg(dto.strEtapaCodigo);
                                    entidad.EcEstado = "AC";
                                    entidad.EcUsuarioMod = usuario;
                                    entidad.EcFechaMod = ahora;
                                    entidad.EcEquipoMod = equipo;
                                    entidad.EcUsuarioEli = null;
                                    entidad.EcFechaEli = null;
                                    entidad.EcEquipoEli = null;
                                    dicAnuladas.Remove(claveNueva);
                                    cuentasReactivadas++;
                                }
                                else
                                {
                                    ValidarProcesoCuenta(null);
                                    entidad = new TbConfigCuentaCosto
                                    {
                                        CcEmpCodigo = empresa,
                                        CcPcCodigo = procesoCuenta,
                                        CcEcCodigo = NormalizarCfg(dto.strEtapaCodigo),
                                        CcCenCodigo = NormalizarCfg(dto.strCentroCodigo),
                                        CcSubCodigo = NormalizarCfg(dto.strSubcentroCodigo),
                                        CcCtaNumero = NormalizarCfg(dto.strCuenta),
                                        EcEstado = "AC",
                                        EcUsuarioCrea = usuario,
                                        EcFechaCrea = ahora,
                                        EcEquipoCrea = equipo
                                    };

                                    await ctx.TbConfigCuentaCosto.AddAsync(entidad);
                                    cuentasInsert++;
                                }

                                dicActivas[claveNueva] = entidad;

                                _objLogger.LogInformation(
                                    "[ConfiguracionCostoProductivo] Cuenta {Cuenta} {Accion} Empresa={Empresa} Centro={Centro} Subcentro={Sub} Usuario={Usuario}",
                                    entidad.CcCtaNumero, entidad.CcId > 0 ? "REACTIVAR" : "INSERT", empresa, entidad.CcCenCodigo, entidad.CcSubCodigo, usuario);
                            }
                        }

                        int afectados = await ctx.SaveChangesAsync();
                        // tiempoCuentas.Stop(); // incluye persistencia final.

                        // el mismo contexto/transacción revierte cuentas y anulaciones ante cualquier fallo.
                        var valorizacionesAnuladas = new ValorizacionesAnuladasDto
                        {
                            // blHayTransferencia = hayTransferencia
                            blHayTransferencia = hayTransferencia,
                            blRequiereAnulacion = hayTransferencia
                        };
                        // La anulación se solicita explícitamente por tipo mediante SP.

                        // reemplazado por log de procesos y acciones de cuentas
                        _objLogger.LogInformation(
                            "[ConfiguracionCostoProductivo] Guardado EF completado. " +
                            "Procesos={Procesos}; Tipos={Tipos}; Matriz={Matriz}; CuentasInsert={Ins}; CuentasUpdate={Upd}; CuentasReactivadas={Rea}; CuentasAnuladas={Anu}; SaveChanges={Afectados}; Usuario={Usuario}",
                            request.lstProcesos == null ? 0 : request.lstProcesos.Count,
                            request.lstTiposProceso == null ? 0 : request.lstTiposProceso.Count,
                            request.lstMatriz == null ? 0 : request.lstMatriz.Count,
                            cuentasInsert, cuentasUpdate, cuentasReactivadas, cuentasAnuladas,
                            afectados,
                            usuario);

                        // Si el request trae únicamente valores idénticos a la BD, EF
                        // puede devolver 0. La operación igualmente fue válida.
                        // return true;
                        return new GuardarCostoProductivoMantenimientoResponse
                        {
                            blGuardado = true,
                            objValorizacionesAnuladas = valorizacionesAnuladas
                        };
                    },
                    intTimeout: 180,
                    // Serializable evita duplicados por concurrencia (no hay índice único de negocio).
                    // nivelAislamiento: IsolationLevel.ReadCommitted,
                    // nivelAislamiento: IsolationLevel.Serializable,
                    nivelAislamiento: IsolationLevel.ReadCommitted,
                    blRequiereCommit: true);

                // cantidades confirmadas; se registra después del commit.
                var anuladas = respuesta.objValorizacionesAnuladas;
                _objLogger.LogInformation(
                    "[ConfiguracionCostoProductivo] ValorizacionesAnuladas HayTransferencia={HayTransferencia}; FrescoCabeceras={FrescoCabeceras}; FrescoDetalles={FrescoDetalles}; ReproCabeceras={ReproCabeceras}; ReproDetalles={ReproDetalles}; Usuario={Usuario}",
                    anuladas.blHayTransferencia, anuladas.intFrescoCabeceras, anuladas.intFrescoDetalles,
                    anuladas.intReproCabeceras, anuladas.intReproDetalles, request.strUsuario.Trim());
                // guardado = true; // commit confirmado.
                return respuesta;
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(GuardarMantenimientoCostoProductivo),
                    ex);
                // EF puede envolver SqlException en DbUpdateException.
                for (Exception? causa = ex; causa != null; causa = causa.InnerException)
                    if (causa is Microsoft.Data.SqlClient.SqlException errorSql
                        && errorSql.Errors.Cast<Microsoft.Data.SqlClient.SqlError>().Any(x => x.Number == -2 || x.Number == 1222))
                        throw new InvalidOperationException("La base de datos está ocupada por otra transacción abierta; vuelva a intentar o pida revisar transacciones abiertas en SGCAM.", ex);
                throw;
            }
            // finally
            // {
                // tiempoTotal.Stop();
                // tiempoProcesos.Stop();
                // tiempoTipos.Stop();
                // tiempoMatriz.Stop();
                // tiempoCuentas.Stop();
                // _objLogger.LogInformation(
                    // "[ConfiguracionCostoProductivo] DuracionGuardado TotalMs={TotalMs}; CuentasMs={CuentasMs}; ProcesosMs={ProcesosMs}; TiposMs={TiposMs}; MatrizMs={MatrizMs}; Guardado={Guardado}",
                    // tiempoTotal.ElapsedMilliseconds, tiempoCuentas.ElapsedMilliseconds,
                    // tiempoProcesos.ElapsedMilliseconds, tiempoTipos.ElapsedMilliseconds, tiempoMatriz.ElapsedMilliseconds, guardado);
            // }
        }

        // tracking por CcId y clave fisica, conservando duplicados historicos.
        private static async Task<List<TbConfigCuentaCosto>> CargarCuentasGuardadoConfiguracionAsync(
            CostosDbContext ctx, List<CuentaCostoMantenimientoDto> cuentas)
        {
            var ids = cuentas.Where(x => x != null && x.intId > 0)
                .Select(x => (short)x.intId).Distinct().ToList();
            var filas = await ctx.TbConfigCuentaCosto.AsTracking().WhereInBatchAsync(x => x.CcId, ids);
            var claves = cuentas.Where(x => x != null).Select(x => new
                {
                    Empresa = checked((short)x.intEmpresa), Cuenta = NormalizarCfg(x.strCuenta),
                    Centro = NormalizarCfg(x.strCentroCodigo), Subcentro = NormalizarCfg(x.strSubcentroCodigo)
                })
                .Concat(filas.Select(x => new
                {
                    Empresa = x.CcEmpCodigo, Cuenta = NormalizarCfg(x.CcCtaNumero),
                    Centro = NormalizarCfg(x.CcCenCodigo), Subcentro = NormalizarCfg(x.CcSubCodigo)
                })).Distinct().ToList();
            foreach (var grupo in claves.GroupBy(x => new { x.Empresa, x.Centro, x.Subcentro }))
            {
                var numeros = grupo.Select(x => x.Cuenta).Distinct().ToList();
                for (int inicio = 0; inicio < numeros.Count; inicio += 2000)
                {
                    var lote = numeros.Skip(inicio).Take(2000).ToList();
                    // WhereInBatchAsync deduplica por cuenta con mas de 2000 valores.
                    // Los lotes explicitos conservan TODAS las filas activas e inactivas por clave.
                    filas.AddRange(await ctx.TbConfigCuentaCosto.AsTracking()
                        .Where(x => x.CcEmpCodigo == grupo.Key.Empresa
                            && (x.CcCenCodigo ?? "").Trim().ToUpper() == grupo.Key.Centro
                            && (x.CcSubCodigo ?? "").Trim().ToUpper() == grupo.Key.Subcentro)
                        .WhereInBatchAsync(x => (x.CcCtaNumero ?? "").Trim().ToUpper(), lote));
                }
            }
            return filas.GroupBy(x => x.CcId).Select(g => g.First()).ToList();
        }

        // sin filtros de período/cierre; los detalles se procesan mientras sus cabeceras siguen AC.
        private static async Task AnularValorizacionesPorTransferenciaAsync(
            CostosDbContext ctx, ValorizacionesAnuladasDto resultado, string usuario, string equipo, DateTime ahora)
        {
            resultado.intFrescoDetalles = await AnularValorizacionesEnLotesAsync(ctx,
                ctx.TbMateriaPrimaFrescoValorizadaDetalle.AsTracking()
                    .Where(x => x.MfdEstado == "AC" && ctx.TbMateriaPrimaFrescoValorizada
                        .Any(c => c.MfId == x.MfdMfId && c.MfEstado == "AC"))
                    .OrderBy(x => x.MfdId),
                x =>
                {
                    x.MfdEstado = "AN";
                    x.MfdUsuarioMod = usuario;
                    x.MfdFechaMod = ahora;
                    x.MfdEquipoMod = equipo;
                });
            resultado.intFrescoCabeceras = await AnularValorizacionesEnLotesAsync(ctx,
                ctx.TbMateriaPrimaFrescoValorizada.AsTracking().Where(x => x.MfEstado == "AC").OrderBy(x => x.MfId),
                x =>
                {
                    x.MfEstado = "AN";
                    x.MfUsuarioEli = usuario;
                    x.MfFechaEli = ahora;
                    x.MfEquipoEli = equipo;
                });
            resultado.intReproDetalles = await AnularValorizacionesEnLotesAsync(ctx,
                ctx.TbMateriaPrimaReproValorizadaDetalle.AsTracking()
                    .Where(x => x.MrdEstado == "AC" && ctx.TbMateriaPrimaReproValorizada
                        .Any(c => c.MrId == x.MrdMrId && c.MrEstado == "AC"))
                    .OrderBy(x => x.MrdId),
                x =>
                {
                    x.MrdEstado = "AN";
                    x.MrdUsuarioMod = usuario;
                    x.MrdFechaMod = ahora;
                    x.MrdEquipoMod = equipo;
                });
            resultado.intReproCabeceras = await AnularValorizacionesEnLotesAsync(ctx,
                ctx.TbMateriaPrimaReproValorizada.AsTracking().Where(x => x.MrEstado == "AC").OrderBy(x => x.MrId),
                x =>
                {
                    x.MrEstado = "AN";
                    x.MrUsuarioEli = usuario;
                    x.MrFechaEli = ahora;
                    x.MrEquipoEli = equipo;
                });
        }

        // entidades en lotes de 2000, sin DELETE, MERGE ni listas traducidas a OPENJSON.
        private static async Task<int> AnularValorizacionesEnLotesAsync<T>(
            CostosDbContext ctx, IQueryable<T> activasOrdenadas, Action<T> anular) where T : class
        {
            int cantidad = 0;
            while (true)
            {
                var lote = await activasOrdenadas.Take(2000).ToListAsync();
                if (lote.Count == 0) return cantidad;
                foreach (var entidad in lote) anular(entidad);
                await ctx.SaveChangesAsync();
                cantidad = checked(cantidad + lote.Count);
                // Liberar únicamente este lote; conservar el resto del seguimiento del guardado.
                foreach (var entidad in lote) ctx.Entry(entidad).State = EntityState.Detached;
            }
        }

        /// <summary>Maestro completo de cuentas SONG, sin filtro de periodo.</summary>
        public async Task<CuentasDisponiblesResultadoDto> ConsultarCuentasDisponibles(CuentasDisponiblesFiltroDto filtro)
        {
            try
            {
                filtro ??= new CuentasDisponiblesFiltroDto();

                int pagina = filtro.intPagina < 1 ? 1 : filtro.intPagina;
                int cantidad = filtro.intCantidad < 1 ? 50 : Math.Min(filtro.intCantidad, 500);

                var maestras = await ManejoContext<SongDbContext>.EjecutarAsync(
                    _objSongFactory,
                    async ctx => await ctx.TbMaecta.AsNoTracking()
                        .Where(m => m.CtaNivel == "5" && m.CtaEstado == "AC")
                        .Select(m => new { m.CtaNumero, m.CtaDeslar, m.CtaDescor, m.CtaRelaci })
                        .ToListAsync(),
                    180);
                var cuentasMaestro = maestras
                    .GroupBy(m => m.CtaNumero.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First()).ToList();

                // CtaRelaci enlaza cuenta -> auxiliar/subcentro -> centro; consultas por lote.
                Dictionary<string, TbMaecta> auxiliares = await ConsultarMaectaPorCodigosAsync(
                    cuentasMaestro.Select(m => m.CtaRelaci ?? string.Empty));
                Dictionary<string, TbMaecta> centros = await ConsultarMaectaPorCodigosAsync(
                    auxiliares.Values.Select(m => m.CtaRelaci ?? string.Empty));

                var config = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx => await ctx.TbConfigCuentaCosto.AsNoTracking().ToListAsync(),
                    180);

                // mantener el mapa completo de activas, incluidas DM.
                Dictionary<string, TbConfigCuentaCosto> dicActivas = config
                    .Where(c => EstadoActivo(c.EcEstado))
                    .GroupBy(c => ClaveCuenta(c.CcEmpCodigo, c.CcCtaNumero, c.CcCenCodigo, c.CcSubCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // conservar el proceso incluso para cuentas configuradas anuladas.
                var procesosConfigurados = config.Where(c => c.CcPcCodigo != null)
                    .GroupBy(c => ClaveCuenta(c.CcEmpCodigo, c.CcCtaNumero, c.CcCenCodigo, c.CcSubCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(c => EstadoActivo(c.EcEstado))
                        .ThenByDescending(c => c.CcId).First(), StringComparer.OrdinalIgnoreCase);

                var etapas = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx => await ctx.TbEtapacosto.AsNoTracking().ToListAsync(),
                    180);

                Dictionary<string, string> dicEtapas = etapas
                    .GroupBy(x => NormalizarCfg(x.EcCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => (g.First().EcNombre ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);

                string texto = (filtro.strTexto ?? string.Empty).Trim();
                string fCuenta = (filtro.strCuenta ?? string.Empty).Trim();
                string fCentro = (filtro.strCentro ?? string.Empty).Trim();
                string fSub = (filtro.strSubcentro ?? string.Empty).Trim();
                string fProceso = NormalizarCfg(filtro.strProcesoCodigo);

                // Claves ANULADAS: no están "asignadas"; se reactivarán al agregarlas.
                HashSet<string> clavesAnuladas = config
                    .Where(c => !EstadoActivo(c.EcEstado))
                    .Select(c => ClaveCuenta(c.CcEmpCodigo, c.CcCtaNumero, c.CcCenCodigo, c.CcSubCodigo))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var procesosCat = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx => await ctx.TbProcesocosto.AsNoTracking().ToListAsync(),
                    180);

                Dictionary<string, string> dicProcesoNombre = procesosCat
                    .GroupBy(x => NormalizarCfg(x.PcCodigo), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => (g.First().PcNombre ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);

                var items = new List<CuentaDisponibleDto>();

                foreach (var m in cuentasMaestro)
                {
                    string cuenta = m.CtaNumero.Trim();
                    string sub = (m.CtaRelaci ?? string.Empty).Trim();
                    auxiliares.TryGetValue(sub, out TbMaecta? auxiliar);
                    // conservar la cuenta aunque falte el eslabón; código/nombre vacíos.
                    if (auxiliar == null) sub = string.Empty;
                    string centro = (auxiliar?.CtaRelaci ?? string.Empty).Trim();
                    centros.TryGetValue(centro, out TbMaecta? maCentro);
                    if (maCentro == null) centro = string.Empty;
                    string clave = ClaveCuenta(filtro.intEmpresa, cuenta, centro, sub);

                    dicActivas.TryGetValue(clave, out TbConfigCuentaCosto? asignada);
                    // las asignadas NO se ocultan; salen deshabilitadas con mensaje (solo se ocultan con soloDisponibles).
                    if (asignada != null && filtro.blSoloDisponibles) continue;
                    bool anulada = asignada == null && clavesAnuladas.Contains(clave);

                    var dto = new CuentaDisponibleDto
                    {
                        intEmpresa = filtro.intEmpresa,
                        strCuenta = cuenta,
                        strCuentaNombre = (!string.IsNullOrWhiteSpace(m.CtaDeslar) ? m.CtaDeslar : (m.CtaDescor ?? string.Empty)).Trim(),
                        // En SONG CtaRelaci identifica tanto al auxiliar como al subcentro.
                        strAuxiliarCodigo = sub,
                        strAuxiliarNombre = NombreMaecta(auxiliar),
                        strCentroCodigo = centro,
                        strCentroNombre = NombreMaecta(maCentro),
                        strSubcentroCodigo = sub,
                        strSubcentroNombre = NombreMaecta(auxiliar),
                        blAsignada = asignada != null,
                        blAnulada = anulada,
                        strMensaje = anulada ? "Cuenta anulada: se reactivará al agregarla." : string.Empty
                    };

                    if (asignada != null)
                    {
                        var prev = new CuentaCostoMantenimientoDto
                        {
                            strEtapaCodigo = asignada.CcEcCodigo,
                            strCentroCodigo = asignada.CcCenCodigo,
                            strSubcentroCodigo = asignada.CcSubCodigo,
                            strCuenta = asignada.CcCtaNumero
                        };
                        ResolverCuentaConfiguracion(prev, asignada.CcPcCodigo);

                        dto.strEtapaCodigo = NormalizarCfg(asignada.CcEcCodigo);
                        dto.strEtapaNombre = dicEtapas.GetValueOrDefault(dto.strEtapaCodigo, string.Empty);
                        dto.strProcesoActualCodigo = prev.strProcesoResuelto;
                        dto.strProcesoActualNombre = string.Join(" + ", prev.strProcesoResuelto
                            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(pc => dicProcesoNombre.GetValueOrDefault(NormalizarCfg(pc), pc)));
                        dto.strMensaje = "Esta cuenta ya está en el grupo " + dto.strProcesoActualNombre +
                            " (etapa " + (dto.strEtapaNombre.Length > 0 ? dto.strEtapaNombre : dto.strEtapaCodigo) + ").";
                    }

                    // Prioridad del proceso persistido antes de filtrar el catálogo.
                    // mensaje de asignacion para DM; disponibilidad y filtros sin cambios.
                    if (asignada != null && NormalizarCfg(asignada.CcEcCodigo) == EtapaDiariosCodigo)
                        dto.strMensaje = "Esta cuenta ya está asignada a los diarios contables (etapa DM).";
                    if (procesosConfigurados.TryGetValue(clave, out var cuentaConfigurada))
                    {
                        dto.strProcesoActualCodigo = NormalizarCfg(cuentaConfigurada.CcPcCodigo);
                        dto.strProcesoActualNombre = dicProcesoNombre.GetValueOrDefault(dto.strProcesoActualCodigo, string.Empty);
                    }

                    if (fCuenta.Length > 0 && cuenta.IndexOf(fCuenta, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (fCentro.Length > 0 && centro.IndexOf(fCentro, StringComparison.OrdinalIgnoreCase) < 0 &&
                        dto.strCentroNombre.IndexOf(fCentro, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (fSub.Length > 0 && sub.IndexOf(fSub, StringComparison.OrdinalIgnoreCase) < 0 &&
                        dto.strSubcentroNombre.IndexOf(fSub, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (fProceso.Length > 0 && dto.strProcesoActualCodigo.IndexOf(fProceso, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    if (texto.Length > 0 &&
                        cuenta.IndexOf(texto, StringComparison.OrdinalIgnoreCase) < 0 &&
                        dto.strCuentaNombre.IndexOf(texto, StringComparison.OrdinalIgnoreCase) < 0 &&
                        centro.IndexOf(texto, StringComparison.OrdinalIgnoreCase) < 0 &&
                        dto.strCentroNombre.IndexOf(texto, StringComparison.OrdinalIgnoreCase) < 0 &&
                        sub.IndexOf(texto, StringComparison.OrdinalIgnoreCase) < 0 &&
                        dto.strSubcentroNombre.IndexOf(texto, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    items.Add(dto);
                }

                // snapshots únicamente para las cuentas de la página devuelta.
                var itemsPagina = items.OrderBy(x => x.strCuenta, StringComparer.Ordinal)
                    .Skip((pagina - 1) * cantidad).Take(cantidad).ToList();
                var snapshots = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async ctx => await ctx.TbDistribucionProcesoProductivo.AsNoTracking()
                        .Where(x => x.DpEstado == "AC" && x.DpEcCodigo != "DM")
                        // Ordenar solo en memoria para evitar desbordamiento smallint en SQL.
                        .WhereInBatchAsync(x => x.DpCtaNumero, itemsPagina.Select(x => x.strCuenta).Distinct().ToList()),
                    180);
                var ultimoPorCuenta = snapshots
                    .GroupBy(x => NormalizarCfg(x.DpCtaNumero), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => (int)x.DpAnio * 100 + x.DpMes)
                        .ThenByDescending(x => x.DpId).First(), StringComparer.OrdinalIgnoreCase);
                foreach (var item in itemsPagina)
                {
                    // el proceso configurado prevalece sobre el snapshot.
                    string claveItem = ClaveCuenta(filtro.intEmpresa, item.strCuenta, item.strCentroCodigo, item.strSubcentroCodigo);
                    if (procesosConfigurados.ContainsKey(claveItem))
                        continue;
                    if (!ultimoPorCuenta.TryGetValue(NormalizarCfg(item.strCuenta), out var snapshot))
                        continue; // Sin snapshot se conserva ResolverCuentaConfiguracion.
                    item.strProcesoActualCodigo = NormalizarCfg(snapshot.DpPcCodigo);
                    item.strProcesoActualNombre = dicProcesoNombre.GetValueOrDefault(item.strProcesoActualCodigo, string.Empty);
                    if (item.blAsignada)
                        item.strMensaje = "Esta cuenta ya está en el grupo " + item.strProcesoActualNombre +
                            " (etapa " + (item.strEtapaNombre.Length > 0 ? item.strEtapaNombre : item.strEtapaCodigo) + ").";
                }

                return new CuentasDisponiblesResultadoDto
                {
                    intTotal = items.Count,
                    intPagina = pagina,
                    intCantidad = cantidad,
                    lstItems = itemsPagina
                };
            }
            catch (Exception ex)
            {
                ManejoLog<ProcesoParametro>.Error(
                    _objLogger,
                    nameof(ProcesoParametro),
                    nameof(ConsultarCuentasDisponibles),
                    ex);
                throw;
            }
        }

        private static string ClaveCuenta(int empresa, string cuenta, string centro, string sub)
        {
            return empresa + "|" + NormalizarCfg(cuenta) + "|" + NormalizarCfg(centro) + "|" + NormalizarCfg(sub);
        }

        // una lectura por lotes, acotada a cuentas sin cc_pcCodigo.
        // El snapshot no tiene empresa: su clave es cuenta + centro + subcentro.
        private static async Task<Dictionary<string, string>> ConsultarProcesosSnapshotConfiguracionAsync(
            CostosDbContext ctx, IEnumerable<TbConfigCuentaCosto> cuentas)
        {
            var sinProceso = cuentas.Where(x => x.CcPcCodigo == null).ToList();
            var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (sinProceso.Count == 0) return resultado;

            var claves = sinProceso.Select(x => ClaveCuenta(0, x.CcCtaNumero, x.CcCenCodigo, x.CcSubCodigo))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var numeros = sinProceso.Select(x => NormalizarCfg(x.CcCtaNumero))
                .Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (numeros.Count == 0) return resultado;

            var snapshots = new List<TbDistribucionProcesoProductivo>();
            // lotes expl?citos <= 2000 evitan que WhereInBatchAsync deduplique
            // por DpCtaNumero y pierda per?odos, centros o procesos de la misma cuenta.
            for (int inicio = 0; inicio < numeros.Count; inicio += 2000)
            {
                var lote = numeros.Skip(inicio).Take(2000).ToList();
                snapshots.AddRange(await ctx.TbDistribucionProcesoProductivo.AsNoTracking()
                    .Where(x => x.DpEstado == "AC" && x.DpEcCodigo != "DM")
                    .Select(x => new TbDistribucionProcesoProductivo
                    {
                        DpCtaNumero = x.DpCtaNumero, DpCenCodigo = x.DpCenCodigo,
                        DpSubCodigo = x.DpSubCodigo, DpPcCodigo = x.DpPcCodigo,
                        DpAnio = x.DpAnio, DpMes = x.DpMes, DpId = x.DpId,
                        DpMontoEntero = x.DpMontoEntero, DpMontoCola = x.DpMontoCola,
                        DpMontoVag = x.DpMontoVag
                    })
                    .WhereInBatchAsync(x => x.DpCtaNumero, lote));
            }

            // toda aritm?tica de a?o/mes y el filtro centro/subcentro van en memoria.
            foreach (var grupo in snapshots
                .Where(x => claves.Contains(ClaveCuenta(0, x.DpCtaNumero, x.DpCenCodigo, x.DpSubCodigo)))
                .GroupBy(x => ClaveCuenta(0, x.DpCtaNumero, x.DpCenCodigo, x.DpSubCodigo), StringComparer.OrdinalIgnoreCase))
            {
                var ordenados = grupo.OrderByDescending(x => ((int)x.DpAnio * 100 + x.DpMes))
                    .ThenByDescending(x => x.DpId).ToList();
                var ultimo = ordenados[0];
                // Dentro del ?ltimo per?odo, sumar los montos por proceso; DpId desempata.
                var proceso = ordenados.Where(x => x.DpAnio == ultimo.DpAnio && x.DpMes == ultimo.DpMes)
                    .Where(x => !string.IsNullOrWhiteSpace(x.DpPcCodigo))
                    .GroupBy(x => NormalizarCfg(x.DpPcCodigo), StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Sum(x => x.DpMontoEntero + x.DpMontoCola + x.DpMontoVag))
                    .ThenByDescending(g => g.Max(x => x.DpId))
                    .Select(g => g.Key).FirstOrDefault();
                if (proceso != null) resultado[grupo.Key] = proceso;
            }
            return resultado;
        }

        // solo NULL habilita snapshot; sin proceso utilizable se conservan las reglas v13.
        private static string? ObtenerProcesoGuardadoOSnapshot(
            TbConfigCuentaCosto cuenta, Dictionary<string, string> procesosSnapshot)
        {
            return cuenta.CcPcCodigo ?? procesosSnapshot.GetValueOrDefault(
                ClaveCuenta(0, cuenta.CcCtaNumero, cuenta.CcCenCodigo, cuenta.CcSubCodigo));
        }

        /// <summary>Cantidad de cuentas ACTIVAS por proceso resuelto (CLA + CAJ cuenta para ambos).</summary>
        // el conteo comparte exactamente la resoluci?n usada por la lectura de cuentas.
        private static Dictionary<string, int> ContarCuentasActivasPorProceso(
            IEnumerable<TbConfigCuentaCosto> cuentas, Dictionary<string, string> procesosSnapshot)
        {
            var dic = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (TbConfigCuentaCosto x in cuentas.Where(c => EstadoActivo(c.EcEstado)
                && NormalizarCfg(c.CcEcCodigo) != EtapaDiariosCodigo))
            {
                var dto = new CuentaCostoMantenimientoDto
                {
                    strEtapaCodigo = x.CcEcCodigo,
                    strCentroCodigo = x.CcCenCodigo,
                    strSubcentroCodigo = x.CcSubCodigo,
                    strCuenta = x.CcCtaNumero
                };

                ResolverCuentaConfiguracion(dto, ObtenerProcesoGuardadoOSnapshot(x, procesosSnapshot));

                foreach (string pc in dto.strProcesoResuelto.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    dic[NormalizarCfg(pc)] = dic.GetValueOrDefault(NormalizarCfg(pc)) + 1;
            }

            return dic;
        }

        /// <summary>tb_maecta por código en lotes (WhereInBatchAsync, sin join cross-database).</summary>
        private async Task<Dictionary<string, TbMaecta>> ConsultarMaectaPorCodigosAsync(IEnumerable<string> codigos)
        {
            List<string> lista = codigos
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<TbMaecta> maestras = await ManejoContext<SongDbContext>.EjecutarAsync(
                _objSongFactory,
                async ctx => await ctx.TbMaecta.AsNoTracking()
                    .Select(m => new TbMaecta
                    {
                        CtaNumero = m.CtaNumero,
                        CtaDeslar = m.CtaDeslar,
                        CtaDescor = m.CtaDescor,
                        CtaRelaci = m.CtaRelaci
                    })
                    .WhereInBatchAsync(x => x.CtaNumero, lista));

            return maestras
                .GroupBy(x => x.CtaNumero.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        // lectura independiente de PRODUCCION, sin joins entre bases.
        private async Task<List<TbTiplot>> ConsultarTiplotActivosConfiguracionAsync()
        {
            return await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                _objContextFactory,
                async ctx => await ctx.TbTiplot.AsNoTracking()
                    .Where(x => x.TipEstado == "AC").ToListAsync());
        }

        private static string NombreMaecta(TbMaecta? m)
        {
            if (m == null) return string.Empty;
            return (!string.IsNullOrWhiteSpace(m.CtaDeslar) ? m.CtaDeslar : (m.CtaDescor ?? string.Empty)).Trim();
        }

        private static void ValidarTipoProcesoMantenimiento(TipoProcesoMantenimientoDto dto)
        {
            if (dto == null)
                throw new InvalidOperationException("Existe un tipo de proceso nulo.");

            if (string.IsNullOrWhiteSpace(dto.strCodigo))
                throw new InvalidOperationException("El código del tipo de proceso es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.strNombre))
                throw new InvalidOperationException("El nombre del tipo de proceso es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.strTipoLoteCodigo))
                throw new InvalidOperationException("El tipo de lote es obligatorio.");

            if (dto.intNivel < 1 || dto.intNivel > 5)
                throw new InvalidOperationException("El nivel debe estar entre NV1 y NV5.");
        }

        private static void ValidarCuentaMantenimiento(CuentaCostoMantenimientoDto dto)
        {
            if (dto == null)
                throw new InvalidOperationException("Existe una cuenta nula.");

            if (dto.intEmpresa <= 0)
                throw new InvalidOperationException("La empresa debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(dto.strEtapaCodigo) ||
                string.IsNullOrWhiteSpace(dto.strCentroCodigo) ||
                string.IsNullOrWhiteSpace(dto.strSubcentroCodigo) ||
                string.IsNullOrWhiteSpace(dto.strCuenta))
            {
                throw new InvalidOperationException(
                    "Etapa, centro, subcentro y cuenta son obligatorios.");
            }
        }

        // COSTEO se acepta como alias de PRODUCTIVO para clientes anteriores.
        internal static void ValidarUsoProceso(string? strUso, string? strEtapa)
        {
            string strEtapaNormalizada = (strEtapa ?? string.Empty).Trim().ToUpperInvariant();
            string strUsoNormalizado = (strUso ?? string.Empty).Trim().ToUpperInvariant();
            if (strUsoNormalizado.Length == 0)
                strUsoNormalizado = strEtapaNormalizada == EtapaDiariosCodigo ? "DIARIO" : "PRODUCTIVO";
            if (strUsoNormalizado == "COSTEO") strUsoNormalizado = "PRODUCTIVO";
            if (strUsoNormalizado != "PRODUCTIVO" && strUsoNormalizado != "DIARIO")
                throw new InvalidOperationException("El tipo de proceso debe ser Productivo o Diario.");
            if ((strUsoNormalizado == "DIARIO") != (strEtapaNormalizada == EtapaDiariosCodigo))
                throw new InvalidOperationException("Diario requiere la etapa DM; Productivo no permite la etapa DM.");
        }

        private static void ValidarProcesoMantenimiento(ProcesoCostoMantenimientoDto dto, HashSet<string> etapasValidas)
        {
            if (dto == null)
                throw new InvalidOperationException("Existe un proceso nulo.");

            // proteger también la validación de mantenimiento compartida.
            ValidarUsoProceso(dto.strUso, dto.strEtapaCodigo);

            string codigo = NormalizarCfg(dto.strCodigo);

            if (codigo.Length == 0 || codigo.Length > 5)
                throw new InvalidOperationException("El código del proceso es obligatorio (máximo 5 caracteres).");

            if (string.IsNullOrWhiteSpace(dto.strNombre) || dto.strNombre.Trim().Length > 100)
                throw new InvalidOperationException("El nombre del proceso es obligatorio (máximo 100 caracteres).");

            if (!etapasValidas.Contains(NormalizarCfg(dto.strEtapaCodigo)))
                throw new InvalidOperationException("La etapa '" + dto.strEtapaCodigo + "' no existe.");

            if (dto.intOrden < 0 || dto.intOrden > short.MaxValue)
                throw new InvalidOperationException("El orden del proceso no es válido.");
        }

        private static string ClaveMatriz(string pc, string tipo)
        {
            return NormalizarCfg(pc) + "|" + NormalizarCfg(tipo);
        }

        private static string NormalizarCfg(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool EstadoActivo(string value)
        {
            return string.Equals(
                (value ?? string.Empty).Trim(),
                "AC",
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Solo previsualiza cómo la configuración será interpretada por el flujo
        // /// actual de costo productivo. No persiste proceso/driver porque esas
        // /// columnas no existen en tb_configCuentaCosto.
        /// usa el proceso persistido o la regla heredada cuando es NULL.
        /// </summary>
        // private static void ResolverCuentaConfiguracion(CuentaCostoMantenimientoDto item)
        private static void ResolverCuentaConfiguracion(CuentaCostoMantenimientoDto item, string? procesoConfigurado = null)
        {
            string etapa = NormalizarCfg(item.strEtapaCodigo);
            string centro = NormalizarCfg(item.strCentroCodigo);
            string sub = NormalizarCfg(item.strSubcentroCodigo);
            string cuenta = NormalizarCfg(item.strCuenta);
            string sufijo = cuenta.Length >= 4 ? cuenta.Substring(cuenta.Length - 4) : cuenta;

            string proceso = string.Empty;

            if (etapa == "MP")
                proceso = "MPE";
            else if (etapa == "CP")
                proceso = "COP";
            else if (etapa == "PP")
            {
                if (centro.StartsWith("504", StringComparison.OrdinalIgnoreCase) || sub == "5030111")
                    proceso = "LOG";
                else if (sub == "5030101")
                    proceso = "REC";
                else if (sub == "5030142")
                    proceso = "COD";
                else
                    proceso = "CLA";
            }
            else if (etapa == "PS")
            {
                if (sub == "5030201") proceso = "DES";
                else if (sub == "5030304") proceso = "HID";
                else if (sub == "5030310") proceso = "COC";
                else if (sub == "5030307") proceso = "DSC";
                else proceso = "PEL";
            }
            else if (etapa == "PR")
            {
                proceso = sub == "5030113" ? "DEC" : "RET";
            }
            else if (etapa == "PC")
            {
                if (_hshFuentesIqfConfiguracion.Contains(cuenta))
                    proceso = "IQF";
                else if (centro.StartsWith("503", StringComparison.OrdinalIgnoreCase))
                    proceso = "BRI";
                else
                    proceso = "TUN";
            }
            else if (etapa == "CD")
            {
                proceso = EsSufijoFijo(sufijo) ? "CDF" : "CDV";
            }
            else if (etapa == "CI")
            {
                proceso = EsSufijoFijo(sufijo) ? "CIF" : "CIV";
            }

            // Regla especial ya existente en el pase: una cuenta física puede
            // alimentar dos procesos productivos.
            if (cuenta == "50301040104" && etapa == "PP")
                proceso = "CLA + CAJ";

            // fuera de 503/504 no inferir un proceso por las reglas heredadas.
            // La asignación explícita conserva prioridad, también para cuentas de exportación.
            bool admiteReglaHeredada = cuenta.StartsWith("503", StringComparison.Ordinal)
                || cuenta.StartsWith("504", StringComparison.Ordinal);
            if (!admiteReglaHeredada)
                proceso = string.Empty;

            // solo NULL conserva la regla heredada.
            if (procesoConfigurado != null)
                proceso = NormalizarCfg(procesoConfigurado);

            string driver = proceso;
            if (etapa == "MP" && admiteReglaHeredada)
            {
                driver = sub == "5040606" || sub == "5040904"
                    ? "CIV"
                    : "DIRECTO_CUENTA";
            }

            item.strProcesoResuelto = proceso;
            item.strDriverResuelto = driver;
            item.strModoMonto = proceso.IndexOf("IQF", StringComparison.OrdinalIgnoreCase) >= 0
                ? "DEBE_MES"
                : "NETO_MES";
            item.blRequiereCreditoRetiro =
                proceso.IndexOf("IQF", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool EsSufijoFijo(string sufijo)
        {
            return sufijo == "0033" ||
                   sufijo == "0097" ||
                   sufijo == "0239" ||
                   sufijo == "0240";
        }
        #endregion

        #region Procesos unificados

        // resolución compartida para lectura y escritura, compatible con el pase SQL pendiente.
        private static string NormalizarNombreProcesoUnificado(string nombre) =>
            new string((nombre ?? string.Empty).Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray()).Normalize(NormalizationForm.FormC).ToUpperInvariant();

        private static string ClaveNombreProcesoUnificado(string nombre) =>
            new string(NormalizarNombreProcesoUnificado(nombre).Where(c => !char.IsWhiteSpace(c) && c != '.').ToArray());

        private static string ModoValorProcesoUnificado(TbProcesoCosteo? fila, HashSet<string> codigosProceso)
        {
            if (fila == null) return TipoValorProceso.Calculado;
            string tip = NormalizarCfg(fila.PrTipCodigo);
            return tip.Length > 0 && !codigosProceso.Contains(tip) ? TipoValorProceso.Tarifa
                : fila.PrEditable == true ? TipoValorProceso.DolaresTotales : TipoValorProceso.Calculado;
        }

        private static TbProcesoCosteo? ResolverParamProcUnificado(string codigo, string nombre,
            List<TbProcesoCosteo> filas, List<TbConfigTipoLiquidacionProcesoCosto> matriz, HashSet<string> tipos)
        {
            var vinculada = filas.FirstOrDefault(x => NormalizarCfg(x.PrTipCodigo) == codigo);
            if (vinculada != null) return vinculada;
            var tiposTarifa = matriz.Where(x => NormalizarCfg(x.CgPcCodigo) == codigo
                && EstadoActivo(x.CgEstado) && NormalizarCfg(x.CgConfig) == "TAR")
                .Select(x => NormalizarCfg(x.CgToCodigo)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            vinculada = filas.FirstOrDefault(x => tipos.Contains(NormalizarCfg(x.PrTipCodigo))
                && tiposTarifa.Contains(NormalizarCfg(x.PrTipCodigo)));
            return vinculada ?? filas.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.PrTipCodigo)
                && ClaveNombreProcesoUnificado(x.PrDescri) == ClaveNombreProcesoUnificado(nombre));
        }

        private static void CompletarProcesosConParamProc(
            List<ProcesoCostoMantenimientoDto> procesos, List<TbProcesoCosteo> paramRows,
            List<TbConfigTipoLiquidacionProcesoCosto> matriz, List<TbTipoOtrosProcesos> tipos)
        {
            var codigosProceso = procesos.Select(x => NormalizarCfg(x.strCodigo))
                .Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var tiposPorCodigo = tipos.GroupBy(x => NormalizarCfg(x.ToCodigo))
                .ToDictionary(g => g.Key, g => (g.First().ToNombre ?? string.Empty).Trim());
            var codigosTipo = tiposPorCodigo.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var asignadas = new HashSet<TbProcesoCosteo>();
            string Resumir(List<string> valores) => string.Join(", ", valores.Take(3))
                + (valores.Count > 3 ? " (+" + (valores.Count - 3) + ")" : string.Empty);
            foreach (var p in procesos)
            {
                string codigo = NormalizarCfg(p.strCodigo);
                var fila = ResolverParamProcUnificado(codigo, p.strNombre, paramRows, matriz, codigosTipo);
                if (fila != null) asignadas.Add(fila);
                // consumir el vínculo heredado sin exponer ParamProc ni matriz de diarios.
                p.blEsDiario = NormalizarCfg(p.strEtapaCodigo) == EtapaDiariosCodigo;
                p.strUso = p.blEsDiario ? "DIARIO" : "COSTEO";
                if (p.blEsDiario)
                {
                    p.intParamId = 0;
                    p.strModoValor = TipoValorProceso.Calculado;
                    p.strTipoProcesoCodigo = string.Empty;
                    p.strTipoProceso = string.Empty;
                    p.blEsTarifa = false;
                    p.blEsFichaTecnica = false;
                    p.strTiposFicha = string.Empty;
                    p.blEnMatriz = false;
                    continue;
                }
                p.intParamId = fila?.PrId ?? 0;
                p.strModoValor = ModoValorProcesoUnificado(fila, codigosProceso);
                var tarifas = matriz.Where(x => NormalizarCfg(x.CgPcCodigo) == codigo
                    && EstadoActivo(x.CgEstado) && NormalizarCfg(x.CgConfig) == "TAR")
                    .Select(x => NormalizarCfg(x.CgToCodigo)).Distinct().ToList();
                // indicadores informativos de las celdas FIC activas.
                var fichas = matriz.Where(x => NormalizarCfg(x.CgPcCodigo) == codigo
                    && EstadoActivo(x.CgEstado) && NormalizarCfg(x.CgConfig) == "FIC")
                    .Select(x => NormalizarCfg(x.CgToCodigo)).Distinct().OrderBy(x => x).ToList();
                p.blEsFichaTecnica = fichas.Count > 0;
                p.strTiposFicha = Resumir(fichas);
                bool filaTarifa = p.strModoValor == TipoValorProceso.Tarifa;
                p.blEsTarifa = filaTarifa || tarifas.Count > 0;
                var codigos = filaTarifa ? new List<string> { NormalizarCfg(fila!.PrTipCodigo) } : tarifas;
                p.strTipoProcesoCodigo = Resumir(codigos);
                p.strTipoProceso = Resumir(codigos.Select(x => tiposPorCodigo.GetValueOrDefault(x, string.Empty)).ToList());
            }
            foreach (var fila in paramRows.Where(x => !asignadas.Contains(x)))
            {
                string modo = ModoValorProcesoUnificado(fila, codigosProceso);
                string tipo = modo == TipoValorProceso.Tarifa ? NormalizarCfg(fila.PrTipCodigo) : string.Empty;
                procesos.Add(new ProcesoCostoMantenimientoDto
                {
                    strUso = "COSTEO",
                    blSinProceso = true, strCodigo = string.Empty, strNombre = (fila.PrDescri ?? string.Empty).Trim(),
                    intParamId = fila.PrId, blActivo = EstadoActivo(fila.PrEstado), strModoValor = modo,
                    strTipoProcesoCodigo = tipo, strTipoProceso = tiposPorCodigo.GetValueOrDefault(tipo, string.Empty),
                    blEsTarifa = modo == TipoValorProceso.Tarifa, strEtapaCodigo = string.Empty,
                    intCantidadCuentas = 0, blEnMatriz = false
                });
            }
        }

        private async Task GuardarProcesosUnificadosAsync(
            CostosDbContext ctx, List<ProcesoCostoMantenimientoDto> procesos, string usuario, string equipo,
            DateTime ahora, Dictionary<string, int> dicCantidadPrevia, HashSet<string> etapasValidas)
        {
            var existentes = await ctx.TbProcesocosto.AsTracking().ToListAsync();
            var filas = await ctx.TbProcesoCosteo.AsTracking().OrderBy(x => x.PrId).ToListAsync();
            var matriz = await ctx.TbConfigTipoLiquidacionProcesoCosto.AsTracking().ToListAsync();
            var tipos = await ctx.TbTipoOtrosProcesos.AsTracking().ToListAsync();
            var codigosProceso = existentes.Select(x => NormalizarCfg(x.PcCodigo)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var codigosTipo = tipos.Select(x => NormalizarCfg(x.ToCodigo)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            // reservar también los códigos de PRODUCCION, incluidos los inactivos.
            var codigosTiplot = await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                _objContextFactory,
                async contextoProduccion => await contextoProduccion.TbTiplot.AsNoTracking()
                    .Select(x => x.TipCodigo.Trim()).ToListAsync());
            var ocupados = codigosProceso.Concat(codigosTipo).Concat(codigosTiplot.Select(NormalizarCfg))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var registros = new List<(string Codigo, string Accion, string Modo, TbProcesoCosteo? Fila)>();
            foreach (var dto in procesos ?? new List<ProcesoCostoMantenimientoDto>())
            {
                if (dto == null) throw new InvalidOperationException("Existe un proceso nulo.");
                string etapa = NormalizarCfg(dto.strEtapaCodigo);
                ValidarUsoProceso(dto.strUso, dto.strEtapaCodigo);
                var procesoPrevio = existentes.FirstOrDefault(x => NormalizarCfg(x.PcCodigo) == NormalizarCfg(dto.strCodigo));
                bool eraDiario = procesoPrevio != null && NormalizarCfg(procesoPrevio.PcEcCodigo) == EtapaDiariosCodigo;
                if (eraDiario || etapa == EtapaDiariosCodigo)
                {
                    if (dto.blSinProceso)
                        throw new InvalidOperationException("Los procesos de diarios no permiten configurar ParamProc.");
                    if (!dto.blEsNuevo && (procesoPrevio == null || !eraDiario || etapa != EtapaDiariosCodigo))
                        throw new InvalidOperationException("No se puede cambiar el código ni el uso o etapa de un proceso de diarios.");
                    string codigoDiario = NormalizarCfg(dto.strCodigo);
                    if (dto.blEsNuevo)
                    {
                        codigoDiario = dto.blEditarCodigo && codigoDiario.Length > 0
                            ? codigoDiario : GenerarCodigoProceso(dto.strNombre, ocupados);
                        if (!Regex.IsMatch(codigoDiario, "^[A-Z0-9]{3,5}$"))
                            throw new InvalidOperationException("El código del proceso debe contener de 3 a 5 caracteres A-Z o 0-9.");
                        if (ocupados.Contains(codigoDiario))
                            throw new InvalidOperationException("El código " + codigoDiario + " ya pertenece a un proceso, tipo de proceso o tb_tiplot.");
                        dto.strCodigo = codigoDiario;
                        dto.intOrden = existentes.Where(x => NormalizarCfg(x.PcEcCodigo) == EtapaDiariosCodigo)
                            .Select(x => (int)x.PcOrden).DefaultIfEmpty(0).Max() + 1;
                        dto.blActivo = true;
                    }
                    else if (dto.intOrden != procesoPrevio!.PcOrden)
                        throw new InvalidOperationException("El orden de un proceso de diarios no se puede modificar.");
                    ValidarProcesoMantenimiento(dto, etapasValidas);
                    if (!dto.blEsNuevo && !dto.blActivo)
                    {
                        // cc_pcCodigo no vincula diarios; el proceso está en cc_cenCodigo.
                        var cuentasDiarios = await ctx.TbConfigCuentaCosto.AsNoTracking()
                            .Where(x => x.CcEcCodigo == EtapaDiariosCodigo && x.EcEstado == "AC")
                            .Select(x => x.CcCenCodigo).ToListAsync();
                        if (cuentasDiarios.Any(x => NormalizarCfg(x) == codigoDiario))
                            throw new InvalidOperationException("El proceso de diarios tiene cuentas activas en la configuración de diarios; no se puede inactivar desde esta pantalla.");
                    }
                    var entidadDiario = dto.blEsNuevo ? new TbProcesocosto
                    {
                        PcCodigo = codigoDiario, PcEcCodigo = EtapaDiariosCodigo,
                        PcOrden = checked((short)dto.intOrden), PcUsuarioCrea = usuario,
                        PcFechaCrea = ahora, PcEquipoCrea = equipo
                    } : procesoPrevio!;
                    entidadDiario.PcNombre = (dto.strNombre ?? string.Empty).Trim();
                    entidadDiario.PcEstado = dto.blActivo ? "AC" : "AN";
                    if (dto.blEsNuevo)
                    {
                        await ctx.TbProcesocosto.AddAsync(entidadDiario);
                        existentes.Add(entidadDiario);
                        codigosProceso.Add(codigoDiario);
                        ocupados.Add(codigoDiario);
                    }
                    registros.Add((codigoDiario, dto.blEsNuevo ? "INSERT" : "UPDATE", "DIARIO", null));
                    // Sin ParamProc, TAR ni cambios de cuentas: no genera transferencia productiva.
                    continue;
                }
                string modo = NormalizarCfg(dto.strModoValor);
                if (!TipoValorProceso.EsValido(modo)) throw new InvalidOperationException("El modo de valor '" + dto.strModoValor + "' no es válido.");
                string nombre = (dto.strNombre ?? string.Empty).Trim();
                string tipo = NormalizarCfg(dto.strTipoProcesoCodigo);
                if (dto.blSinProceso)
                {
                    var huerfana = filas.FirstOrDefault(x => x.PrId == dto.intParamId);
                    if (dto.intParamId <= 0 || huerfana == null)
                        throw new InvalidOperationException("La fila de ParamProc " + dto.intParamId + " no existe.");
                    if (existentes.Any(x => ResolverParamProcUnificado(NormalizarCfg(x.PcCodigo), x.PcNombre, filas, matriz, codigosTipo) == huerfana))
                        throw new InvalidOperationException("La fila de ParamProc está vinculada a un proceso; vuelva a consultar el mantenimiento.");
                    string modoAnterior = ModoValorProcesoUnificado(huerfana, codigosProceso);
                    string tipoVisible = modoAnterior == TipoValorProceso.Tarifa ? NormalizarCfg(huerfana.PrTipCodigo) : string.Empty;
                    if (nombre != (huerfana.PrDescri ?? string.Empty).Trim() || tipo != tipoVisible)
                        throw new InvalidOperationException("Una fila sin proceso no permite cambiar su descripción ni su tipo de proceso.");
                    if ((modoAnterior == TipoValorProceso.Tarifa) != (modo == TipoValorProceso.Tarifa))
                        throw new InvalidOperationException("Una fila sin proceso no permite cambiar hacia o desde el modo TARIFA.");
                    if (modoAnterior != TipoValorProceso.Tarifa) huerfana.PrEditable = modo == TipoValorProceso.DolaresTotales;
                    AuditarParamProcUnificado(huerfana, dto.blActivo, usuario, equipo, ahora);
                    registros.Add((string.Empty, "UPDATE", modo, huerfana));
                    continue;
                }

                string codigo = NormalizarCfg(dto.strCodigo);
                if (dto.blEsNuevo)
                {
                    codigo = dto.blEditarCodigo && codigo.Length > 0 ? codigo : GenerarCodigoProceso(nombre, ocupados);
                    if (!Regex.IsMatch(codigo, "^[A-Z0-9]{3,5}$"))
                        throw new InvalidOperationException("El código del proceso debe contener de 3 a 5 caracteres A-Z o 0-9.");
                    if (ocupados.Contains(codigo)) throw new InvalidOperationException("El código " + codigo + " ya pertenece a un proceso, tipo de proceso o tb_tiplot.");
                    dto.strCodigo = codigo;
                    // Ignorar el orden del cliente antes de validar; existentes incluye las altas previas del guardado.
                    dto.intOrden = existentes
                        .Where(x => NormalizarCfg(x.PcEcCodigo) == NormalizarCfg(dto.strEtapaCodigo))
                        .Select(x => (int)x.PcOrden).DefaultIfEmpty(0).Max() + 1;
                }
                ValidarProcesoMantenimiento(dto, etapasValidas);
                var entidad = existentes.FirstOrDefault(x => NormalizarCfg(x.PcCodigo) == codigo);
                if (!dto.blEsNuevo && entidad == null) throw new InvalidOperationException("El proceso " + codigo + " no existe; su código no se puede modificar.");
                var fila = ResolverParamProcUnificado(codigo, entidad?.PcNombre ?? nombre, filas, matriz, codigosTipo);
                // no modificar indirectamente un ParamProc compartido con diarios.
                if (fila != null && existentes.Any(x => NormalizarCfg(x.PcEcCodigo) == EtapaDiariosCodigo
                    && ResolverParamProcUnificado(NormalizarCfg(x.PcCodigo), x.PcNombre, filas, matriz, codigosTipo) == fila))
                    throw new InvalidOperationException(MensajeEtapaDiarios);
                if (!dto.blEsNuevo && dto.intParamId > 0 && fila?.PrId != dto.intParamId)
                    throw new InvalidOperationException("El vínculo de ParamProc cambió; vuelva a consultar el proceso " + codigo + ".");
                string modoPrevio = ModoValorProcesoUnificado(fila, codigosProceso);
                // Quitar TAR/FIC activas antes de cambiar a un modo incompatible.
                if (entidad != null && modo != modoPrevio
                    && (modo == TipoValorProceso.Calculado || modo == TipoValorProceso.DolaresTotales)
                    && matriz.Any(x => NormalizarCfg(x.CgPcCodigo) == codigo && EstadoActivo(x.CgEstado)
                        && (NormalizarCfg(x.CgConfig) == "TAR" || NormalizarCfg(x.CgConfig) == "FIC")))
                    throw new InvalidOperationException("El proceso tiene tarifa/ficha técnica en la matriz; quite esas celdas primero");
                string tipoAnterior = fila == null ? string.Empty : NormalizarCfg(fila.PrTipCodigo);
                if (entidad != null && fila != null && modoPrevio != TipoValorProceso.Tarifa
                    && nombre != (entidad.PcNombre ?? string.Empty).Trim())
                    throw new InvalidOperationException("No se puede renombrar el proceso " + codigo + ": su descripción vinculada es llave del costeo.");
                if (entidad != null && !dto.blActivo && EstadoActivo(entidad.PcEstado) && dicCantidadPrevia.GetValueOrDefault(codigo) > 0)
                    throw new InvalidOperationException("El proceso " + codigo + " tiene " + dicCantidadPrevia[codigo] + " cuentas activas. Inactívelas antes de inactivar el proceso.");

                if (modo == TipoValorProceso.Tarifa)
                {
                    if (!tipos.Any(x => NormalizarCfg(x.ToCodigo) == tipo && EstadoActivo(x.ToEstado)))
                        throw new InvalidOperationException("TARIFA requiere un tipo de proceso existente y activo.");
                    if (codigosProceso.Contains(tipo) || tipo == codigo)
                        throw new InvalidOperationException("El tipo de tarifa no puede coincidir con un código de proceso.");
                    if (filas.Any(x => x != fila && NormalizarCfg(x.PrTipCodigo) == tipo))
                        throw new InvalidOperationException("Otra fila de ParamProc ya utiliza el tipo de proceso " + tipo + ".");
                }
                bool crearFila = fila == null && (dto.blEsNuevo || modo != TipoValorProceso.Calculado);
                if (crearFila || modo == TipoValorProceso.Tarifa)
                {
                    if (nombre.Length == 0 || nombre.Length > 60)
                        throw new InvalidOperationException("La descripción de ParamProc es obligatoria y admite como máximo 60 caracteres.");
                    if (filas.Any(x => x != fila && ClaveNombreProcesoUnificado(x.PrDescri) == ClaveNombreProcesoUnificado(nombre)))
                        throw new InvalidOperationException("Ya existe otra fila de ParamProc con la descripción '" + nombre + "'.");
                }
                if (entidad == null)
                {
                    entidad = new TbProcesocosto
                    {
                        PcCodigo = codigo, PcUsuarioCrea = usuario, PcFechaCrea = ahora, PcEquipoCrea = equipo
                    };
                    await ctx.TbProcesocosto.AddAsync(entidad);
                    existentes.Add(entidad);
                    codigosProceso.Add(codigo);
                    ocupados.Add(codigo);
                }
                entidad.PcNombre = nombre;
                entidad.PcEcCodigo = NormalizarCfg(dto.strEtapaCodigo);
                entidad.PcOrden = checked((short)dto.intOrden);
                entidad.PcEstado = dto.blActivo ? "AC" : "AN";
                if (crearFila)
                {
                    if (filas.Count >= byte.MaxValue) throw new InvalidOperationException("Se alcanzó el máximo de procesos de ParamProc (255).");
                    fila = new TbProcesoCosteo
                    {
                        PrEmpCodigo = checked((short)EmpresaSongCodigo), PrDescri = nombre,
                        PrUsuarioCrea = usuario, PrFechaCrea = ahora, PrEquipoCrea = equipo
                    };
                    await ctx.TbProcesoCosteo.AddAsync(fila);
                    filas.Add(fila);
                }
                if (fila != null)
                {
                    fila.PrTipCodigo = modo == TipoValorProceso.Tarifa ? tipo : codigo;
                    fila.PrEditable = modo != TipoValorProceso.Calculado;
                    if (modo == TipoValorProceso.Tarifa) fila.PrDescri = nombre;
                    AuditarParamProcUnificado(fila, dto.blActivo, usuario, equipo, ahora);
                }
                if (modoPrevio == TipoValorProceso.Tarifa && modo != TipoValorProceso.Tarifa)
                    foreach (var celda in matriz.Where(x => NormalizarCfg(x.CgPcCodigo) == codigo
                        && NormalizarCfg(x.CgToCodigo) == tipoAnterior && NormalizarCfg(x.CgConfig) == "TAR"))
                        celda.CgConfig = "NO";
                if (modo == TipoValorProceso.Tarifa)
                {
                    var celda = matriz.FirstOrDefault(x => NormalizarCfg(x.CgPcCodigo) == codigo && NormalizarCfg(x.CgToCodigo) == tipo);
                    if (celda == null)
                    {
                        celda = new TbConfigTipoLiquidacionProcesoCosto
                        {
                            CgPcCodigo = codigo, CgToCodigo = tipo,
                            CgUsuarioCrea = usuario, CgFechaCrea = ahora, CgEquipoCrea = equipo
                        };
                        await ctx.TbConfigTipoLiquidacionProcesoCosto.AddAsync(celda);
                        matriz.Add(celda);
                    }
                    celda.CgConfig = "TAR";
                    celda.CgEstado = "AC";
                }
                registros.Add((codigo, dto.blEsNuevo ? "INSERT" : "UPDATE", modo, fila));
            }
            // Conserva la transacción del llamador y obtiene los identity para el log estructurado.
            await ctx.SaveChangesAsync();
            foreach (var registro in registros)
                _objLogger.LogInformation("Proceso {Codigo} {Accion} Modo={Modo} ParamId={Id} Usuario={Usuario}",
                    registro.Codigo, registro.Accion, registro.Modo, registro.Fila?.PrId ?? 0, usuario);
        }

        private static void AuditarParamProcUnificado(TbProcesoCosteo fila, bool activo, string usuario, string equipo, DateTime ahora)
        {
            fila.PrEstado = activo ? "AC" : "AN";
            fila.PrUsuarioMod = usuario; fila.PrFechaMod = ahora; fila.PrEquipoMod = equipo;
            fila.PrUsuarioEli = activo ? null : usuario;
            fila.PrFechaEli = activo ? null : ahora;
            fila.PrEquipoEli = activo ? null : equipo;
        }

        private static string GenerarCodigoProceso(string nombre, HashSet<string> ocupados)
        {
            var conectores = new HashSet<string> { "DE", "DEL", "LA", "LAS", "EL", "LOS", "Y", "E", "EN", "A", "PARA", "CON", "POR" };
            var palabras = Regex.Split(NormalizarNombreProcesoUnificado(nombre), @"\s+")
                .Select(x => Regex.Replace(x, "[^A-Z0-9]", string.Empty))
                .Where(x => x.Length > 0 && !conectores.Contains(x)).ToList();
            if (palabras.Count == 0) throw new InvalidOperationException("El nombre no contiene palabras para generar un código de proceso.");
            string primera = palabras[0];
            var candidatos = new List<string>();
            for (int longitud = 3; longitud <= 5; longitud++)
                if (primera.Length >= longitud) candidatos.Add(primera.Substring(0, longitud));
            candidatos.Add(string.Concat(palabras.Take(3).Select(x => x[0])));
            if (primera.Length >= 2)
                for (int digito = 1; digito <= 9; digito++) candidatos.Add(primera.Substring(0, 2) + digito);
            var codigo = candidatos.FirstOrDefault(x => Regex.IsMatch(x, "^[A-Z0-9]{3,5}$")
                && !ocupados.Contains(x));
            return codigo ?? throw new InvalidOperationException("No se pudo generar un código libre de 3 a 5 caracteres. Marque Editar código e ingrese uno disponible.");
        }
        #endregion

    }
}
