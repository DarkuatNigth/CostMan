using CostManagement.API.Controllers;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagementService.Infraestructura.EF_Core;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.Intrinsics.Arm;

namespace CostManagement.Infraestructura.Repository.Services
{
    public class ProcesoParametro : IProcesoParametro
    {
        private readonly CostManagementDbContext _objContext;
        private readonly SongDbContext _objSongContext;
        private readonly CostosDbContext _objCostosContext;
        private readonly ILogger<ProcesoParametro> _objLogger;
        private readonly IOptions<ParametrosConfig> _objConfig;
        private readonly IDbContextFactory<CostosDbContext> _objContextFactory;
        public ProcesoParametro(
            ILogger<ProcesoParametro> objLogger,
            CostManagementDbContext objContext,
            CostosDbContext objCostosContext,
            SongDbContext objSongContext,
            IOptions<ParametrosConfig> objConfig,
            IDbContextFactory<CostosDbContext> objContextFactory)
        {
            _objLogger = objLogger;
            _objContext = objContext;
            _objCostosContext = objCostosContext;
            _objSongContext = objSongContext;
            _objConfig = objConfig;
            _objContextFactory = objContextFactory;
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesosFrescoConValores(DateOnly fechaCorte)
        {
            try
            {
                HashSet<string> hshNoTraer = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IQF", "BRINE", "Hidratacion", "Cocido", "Pelado", "Decorado"};
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                using var transaction = await objContext.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadUncommitted
                    );
                List<ProcesoResultadoDto> lstResultados = await (from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                                                                     // Simulamos el LEFT OUTER JOIN con la condición de fecha
                                                                 join parametro in objContext.TbParametroCosteo
                                                                      on new { Id = (int)proceso.PrId ,Fecha = fechaCorte, tipo = "PFR" }
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
                return lstResultados;
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[ObtenerProcesosConValores] Ocurrio un error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesosReproConValores(DateOnly fechaCorte)
        {
            try
            {

                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                using var transaction = await objContext.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadUncommitted
                    );
                List<ProcesoResultadoDto> lstResultados = await (from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                                                                     // Simulamos el LEFT OUTER JOIN con la condición de fecha
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
                return lstResultados;
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[ObtenerProcesosConValores] Ocurrio un error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RegistrarParamCosteoPfr(DateOnly fechaCorte, GuardarParametrosRequest objParam)
        {
            // Iniciamos una transacción para asegurar la integridad (Delete + Insert)
            using var transaction = await _objCostosContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Identificar los IDs de proceso que vienen en la lista para el borrado selectivo
                var idsProcesos = objParam.LstValores.Select(d => (byte)d.intCodigo).ToList();

                // 2. Eliminar registros existentes para esa fecha y esos procesos (Equivalente al DELETE JOIN)
                var registrosAEliminar = _objCostosContext.TbParametroCosteo
                    .Where(p => p.PcFecha == fechaCorte && idsProcesos.Contains(p.PcPrId));

                _objCostosContext.TbParametroCosteo.RemoveRange(registrosAEliminar);

                // Guardamos cambios del borrado para evitar conflictos de llave primaria si aplica
                await _objCostosContext.SaveChangesAsync();

                // 3. Mapear el DTO a la entidad de la base de datos
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

                // 4. Insertar los nuevos registros
                await _objCostosContext.TbParametroCosteo.AddRangeAsync(nuevosRegistros);
                await _objCostosContext.SaveChangesAsync();

                // Confirmar transacción
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _objLogger.LogError($"[ProcesoParametro].[RegistrarParametrosCosteo] Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ProcesoResultadoDto>> ConsultarProcesoTarifa(DateOnly fechaCorte)
        {
            List<ProcesoResultadoDto> lstResultados;
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstResultados = await (from proceso in objContext.TbProcesoCosteo.AsNoTracking().Where(obj => obj.PrEstado == "AC")
                                                                     // Simulamos el LEFT OUTER JOIN con la condición de fecha
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
                                                                     blEditable  = (bool)proceso.PrEditable,
                                                                     strTipoLote = "RPC",
                                                                     dcValor = p != null ? p.PcMonto : 0,
                                                                     dcLibras = p != null ? p.PcLibras : 0,
                                                                     dcCostUnitario = p != null ? p.PcCotoUnitario : 0,
                                                                 }).ToListAsync();
                return lstResultados;
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[ConsultarProcesoTarifa] Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> ConsultarCatalogoXCab(int intCab)
        {
            List<string> lstParametros = new List<string>();
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstParametros = await (
                                         from cab in objContext.TbCatalogoCab 
                                         join det in objContext.TbCatalogoDet on cab.CatId equals det.DetIdCab
                                         where cab.CatId.Equals(intCab)
                                         select det.DetCodigo
                                         )
                                         .ToListAsync();
                return lstParametros;
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[ConsultarCatalogoXCab] Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> ConsultarCatalogoXDes(string strDescp)
        {
            List<string> lstParametros = new List<string>();
            try
            {
                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                lstParametros = await (
                                         from cab in objContext.TbCatalogoCab
                                         join det in objContext.TbCatalogoDet on cab.CatId equals det.DetIdCab
                                         where cab.CatDescripcion.Equals(strDescp)
                                         select det.DetCodigo
                                         )
                                         .ToListAsync();
                return lstParametros;
            }
            catch (Exception ex)
            {
                _objLogger.LogError($"[ProcesoParametro].[ConsultarCatalogoXCab] Error: {ex.Message}");
                throw;
            }
        }



    }
}
