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

namespace CostManagement.Infraestructura.Repository.Services
{
    public partial class ProcesoParametro : IProcesoParametro
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

    }
}
