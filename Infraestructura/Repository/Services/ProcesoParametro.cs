using CostManagement.API.Controllers;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

        public async Task<List<ProcesoResultadoDto>> ObtenerProcesosFrescoConValores(DateOnly fechaCorte)
        {
            try
            {

                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                using var transaction = await objContext.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadUncommitted
                    );
                //List<string> lstNotIn = _objConfig.Value.lstNotInFresco;
                    //new List<string>() { "Pelado", "Hidratacion", "Retractilado", "Decorado" };
                List<ProcesoResultadoDto> lstResultados = await (from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                                                                     // Simulamos el LEFT OUTER JOIN con la condición de fecha
                                                                 join parametro in objContext.TbParametroCosteo
                                                                      on new { Id = (int)proceso.PrId ,Fecha = fechaCorte, tipo = "PFR" }
                                                                      equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                                                      into joinParam
                                                                 from p in joinParam.DefaultIfEmpty()
                                                                 //where !lstNotIn.Contains(proceso.PrDescri) &&  p.PcTipoLote == "PFR"
                                                                 select new ProcesoResultadoDto
                                                                 {
                                                                     intCodigo = proceso.PrId,
                                                                     intCodDet = p != null ? p.PcId : 0,
                                                                     strEstado = p != null ? p.PcEstado : "AC",
                                                                     strDescripcion = proceso.PrDescri,
                                                                     strTipoLote = "PFR",
                                                                     // Implementación del ISNULL(valor, 0)
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

        public async Task<List<ProcesoResultadoDto>> ObtenerProcesosReproConValores(DateOnly fechaCorte)
        {
            try
            {

                using var objContext = await _objContextFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(180);
                using var transaction = await objContext.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.ReadUncommitted
                    );
                //List<string> lstNotIn = _objConfig.Value.lstNotInFresco;
                //new List<string>() { "Pelado", "Hidratacion", "Retractilado", "Decorado" };
                List<ProcesoResultadoDto> lstResultados = await (from proceso in objContext.TbProcesoCosteo.Where(obj => obj.PrEstado == "AC")
                                                                     // Simulamos el LEFT OUTER JOIN con la condición de fecha
                                                                 join parametro in objContext.TbParametroCosteo
                                                                      on new { Id = (int)proceso.PrId, Fecha = fechaCorte, tipo = "RPC" }
                                                                      equals new { Id = (int)parametro.PcPrId, Fecha = parametro.PcFecha, tipo = parametro.PcTipoLote }
                                                                      into joinParam
                                                                 from p in joinParam.DefaultIfEmpty()
                                                                 //where !lstNotIn.Contains(proceso.PrDescri) &&   p.PcTipoLote == "RPC"
                                                                 select new ProcesoResultadoDto
                                                                 {
                                                                     intCodigo = proceso.PrId,
                                                                     intCodDet = p != null ? p.PcId : 0,
                                                                     strEstado = p != null ? p.PcEstado : "AC",
                                                                     strDescripcion = proceso.PrDescri,
                                                                     strTipoLote = "RPC",
                                                                     // Implementación del ISNULL(valor, 0)
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

        public async Task<bool> RegistrarParamCosteoPfr(List<ProcesoResultadoDto> detalles, DateOnly fechaCorte, string strUsuario)
        {
            // Iniciamos una transacción para asegurar la integridad (Delete + Insert)
            using var transaction = await _objCostosContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Identificar los IDs de proceso que vienen en la lista para el borrado selectivo
                var idsProcesos = detalles.Select(d => (byte)d.intCodigo).ToList();

                // 2. Eliminar registros existentes para esa fecha y esos procesos (Equivalente al DELETE JOIN)
                var registrosAEliminar = _objCostosContext.TbParametroCosteo
                    .Where(p => p.PcFecha == fechaCorte && idsProcesos.Contains(p.PcPrId));

                _objCostosContext.TbParametroCosteo.RemoveRange(registrosAEliminar);

                // Guardamos cambios del borrado para evitar conflictos de llave primaria si aplica
                await _objCostosContext.SaveChangesAsync();

                // 3. Mapear el DTO a la entidad de la base de datos
                var nuevosRegistros = detalles.Select(d => new TbParametroCosteo
                {
                    PcPrId = (byte)d.intCodigo,
                    PcFecha = fechaCorte,
                    PcMonto = d.dcValor,
                    PcCotoUnitario = d.dcCostUnitario,
                    PcLibras = d.dcLibras,
                    PcTipoLote = d.strTipoLote, 
                    PcEstado = "CE",
                    PcUsuarioCrea = strUsuario,
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

        public async Task ObtenerCostosProcesosMatPrimPFR(List<LiquidacionResultado> lstLiquidaciones, DateOnly dtFechaFin)
        {

            List<ProcesoResultadoDto> lstProcesos;
            Dictionary<string, decimal> dictCostoUnitario;
            decimal
                // Variables de Proceso Primario
                dcCostRecepcion, dcCostClasificacion, dcCostCodificacion, dcCostDescabezado,
                // Variables de Proceso Presentación
                dcCostDecorado, dcCostRetractilado,
                // Variables de Proceso Congelación
                dcCostBrine, dcCostIQF, dcCostTunel,
                // Variables de Proceso Secundario
                dcCostPelado, dcCostHidratacion, dcCostCocido,
                // Variables de Costos Directos/Indirectos
                dcCostDVaria, dcCostDFijos, dcCostIVaria, dcCostIFijos,

                dcCostCopacking
                ;
            List<int> lstCongTunel, lstCongIqf, lstCongBrine;
            List<string> lstProdCocido;

            try
            {

                lstProcesos = await ObtenerProcesosFrescoConValores(dtFechaFin);

                //Se obtienen las formas de congelamiento desde el catalogoDet
                lstCongTunel = (await ConsultarCatalogoXDes("Tipo Congelamiento Tunel")//ConsultarCatalogoXCab(_objConfig.Value.intTunelCabeceraId)
                    ).Select(int.Parse).ToList();
                lstCongIqf = (await ConsultarCatalogoXDes("Tipo Congelamiento Brine")//ConsultarCatalogoXCab(_objConfig.Value.intIqfCabId)
                    ).Select(int.Parse).ToList();
                lstCongBrine = (await ConsultarCatalogoXDes("Tipo Congelamiento IQF")
                    //await ConsultarCatalogoXCab(_objConfig.Value.intBrineCabId)
                    ).Select(int.Parse).ToList();
                //Se obtiene tipo de proceso cocido para entero 
                lstProdCocido = await ConsultarCatalogoXDes("Tipo Proceso Cocido");//await ConsultarCatalogoXCab(_objConfig.Value.intProdCocidoCabId);

                dictCostoUnitario = lstProcesos
                    .Where(p => !string.IsNullOrWhiteSpace(p.strDescripcion))
                    .ToDictionary(
                        p => p.strDescripcion.Trim(),
                        p => p.dcCostUnitario
                    );

                dcCostRecepcion = dictCostoUnitario.GetValueOrDefault("Recepcion");
                dcCostClasificacion = dictCostoUnitario.GetValueOrDefault("Clasificacion");
                dcCostCodificacion = dictCostoUnitario.GetValueOrDefault("Codificacion");
                dcCostDescabezado = dictCostoUnitario.GetValueOrDefault("Descabezado");

                dcCostDecorado = dictCostoUnitario.GetValueOrDefault("Decorado");
                dcCostRetractilado = dictCostoUnitario.GetValueOrDefault("Retractilado");

                dcCostBrine = dictCostoUnitario.GetValueOrDefault("Brine");
                dcCostIQF = dictCostoUnitario.GetValueOrDefault("IQF");
                dcCostTunel = dictCostoUnitario.GetValueOrDefault("Tunel");

                dcCostPelado = dictCostoUnitario.GetValueOrDefault("Pelado");
                dcCostHidratacion = dictCostoUnitario.GetValueOrDefault("Hidratacion");
                dcCostCocido = dictCostoUnitario.GetValueOrDefault("Cocido");

                dcCostDVaria = dictCostoUnitario.GetValueOrDefault("C.D.Variables");
                dcCostDFijos = dictCostoUnitario.GetValueOrDefault("C.D.Fijos");
                dcCostIVaria = dictCostoUnitario.GetValueOrDefault("C.I.Variables");
                dcCostIFijos = dictCostoUnitario.GetValueOrDefault("C.I.Fijos");

                dcCostCopacking = dictCostoUnitario.GetValueOrDefault("C.Copacking");


                foreach (var liq in lstLiquidaciones)
                {
                    // Proceso Primario
                    liq.ProcesoPrimario.dcRecepcion = Math.Round((decimal)liq.dcLibras * dcCostRecepcion, 4);
                    liq.ProcesoPrimario.dcClasificacion = Math.Round((decimal)liq.dcLibras * dcCostClasificacion, 4);
                    liq.ProcesoPrimario.dcCodificacion = Math.Round((decimal)liq.dcLibras * dcCostCodificacion, 4);

                    liq.dcCostRecepcion = dcCostRecepcion;
                    liq.dcCostClasificacion = dcCostClasificacion;
                    liq.dcCostCodificacion = dcCostCodificacion;

                    // Proceso Presentación
                    liq.ProcesoPresentacion.dcDecorado =
                        Math.Round((decimal)liq.dcLibrasDecorado * dcCostDecorado, 4);
                    liq.ProcesoPresentacion.dcRetractilado = Math.Round((decimal)(liq.dcLibrasRetractilado ?? 0) * dcCostRetractilado, 4);
                    liq.dcCostDecorado = Math.Round(dcCostDecorado, 4);
                    liq.dcCostRectra = Math.Round(dcCostRetractilado, 4);

                    // Proceso Congelación
                    if (liq.blBodEsBrine)
                    {
                        liq.ProcesoCongelacion.dcBrine = Math.Round((decimal)liq.dcLibras * dcCostBrine, 4);
                        liq.dcCostBrine = dcCostBrine;
                    }
                    else
                    {
                        liq.ProcesoCongelacion.dcBrine = 0;
                        liq.dcCostBrine = 0;
                    }

                    liq.ProcesoCongelacion.dcIQF = !lstCongTunel.Contains(liq.intProCongela) && liq.blBodEsBrine == false ? Math.Round((decimal)liq.dcLibras * dcCostIQF, 4) : 0;
                    liq.dcCostIQF = !lstCongTunel.Contains(liq.intProCongela) && liq.blBodEsBrine == false ? dcCostIQF : 0;
                    if (lstCongTunel.Contains(liq.intProCongela) && liq.strProClas03 == "PT")
                    {
                        liq.ProcesoCongelacion.dcTunel = Math.Round((decimal)liq.dcLibras * dcCostTunel, 4);
                        liq.dcCostTunel = dcCostTunel;
                    }
                    else
                    {
                        liq.ProcesoCongelacion.dcTunel = 0;
                        liq.dcCostTunel = 0;
                    }

                    // Proceso Secundario
                    liq.ProcesoSecundario.dcPelado = 0;//Math.Round((decimal)liq.dcLibras * dcCostPelado, 4);
                    liq.ProcesoSecundario.dcHidratacion = 0;
                    liq.ProcesoSecundario.dcCocido = 0;//Math.Round((decimal)liq.dcLibras *  dcCostCocido, 4);



                    if (liq.strProClas05 == "SH")
                    {
                        liq.ProcesoSecundario.dcDescabezado = Math.Round((decimal)liq.dcLibras * dcCostDescabezado, 4);
                        liq.dcCostDescabezado = dcCostDescabezado;
                    }
                    else
                    {
                        liq.ProcesoSecundario.dcDescabezado = 0;
                        liq.dcCostDescabezado = 0;
                    }

                    liq.dcCostPelado = 0;//dcCostPelado;
                    liq.dcCostHidratacion = 0;//dcCostHidratacion;
                    liq.dcCostCocido = 0;//dcCostCocido;

                    // Costos Directos / Fijos (Mapeado a ProcesoCostFijo)
                    liq.ProcesoCostFijo.dcCostoVariable = Math.Round((decimal)liq.dcLibras * dcCostDVaria, 4);
                    liq.ProcesoCostFijo.dcCostoFijo = Math.Round((decimal)liq.dcLibras * dcCostDFijos, 4);
                    liq.dcCostDirVaria = dcCostDVaria;
                    liq.dcCostDirFij = dcCostDFijos;
                    // Costos Indirectos / Variables
                    liq.ProcesoCostIndirecto.dcCostoFijo = Math.Round((decimal)liq.dcLibras * dcCostIVaria, 4);
                    liq.ProcesoCostIndirecto.dcCostoVariable = Math.Round((decimal)liq.dcLibras * dcCostIFijos, 4);
                    liq.dcCostIndVaria = dcCostIVaria;
                    liq.dcCostIndFij = dcCostIFijos;

                    liq.dcCostTotalProc =
                        (liq.ProcesoPrimario.dcRecepcion ?? 0) +
                        (liq.ProcesoPrimario.dcClasificacion ?? 0) +
                        (liq.ProcesoPrimario.dcCodificacion ?? 0) +
                        (liq.ProcesoPresentacion.dcDecorado ?? 0) +
                        (liq.ProcesoPresentacion.dcRetractilado ?? 0) +
                        (liq.ProcesoCongelacion.dcBrine ?? 0) +
                        (liq.ProcesoCongelacion.dcIQF ?? 0) +
                        (liq.ProcesoCongelacion.dcTunel ?? 0) +
                        (liq.ProcesoSecundario.dcPelado ?? 0) +
                        (liq.ProcesoSecundario.dcHidratacion ?? 0) +
                        (liq.ProcesoSecundario.dcCocido ?? 0) +
                        (liq.ProcesoSecundario.dcDescabezado ?? 0) +
                        (liq.ProcesoCostFijo.dcCostoVariable ?? 0) +
                        (liq.ProcesoCostFijo.dcCostoFijo ?? 0) +
                        (liq.ProcesoCostIndirecto.dcCostoVariable ?? 0) +
                        (liq.ProcesoCostIndirecto.dcCostoFijo ?? 0);
                    ////Proceso Presentacion
                    //liq.dcCostRecepcion + liq.dcCostClasificacion + liq.dcCostCodificacion +
                    //liq.dcCostDecorado + liq.dcCostRectra +
                    ////Proceso Secundario
                    ////liq.dcPelado + liq.dcHidratacion + liq.dcCocido +
                    ////Proceso Congelacion
                    //liq.dcCostBrine + liq.dcCostIQF + liq.dcCostTunel +
                    //// Costos Directos / Fijos (Mapeado a ProcesoCostFijo)
                    //liq.dcCostDirVaria + liq.dcCostDirFij +
                    //// Costos Indirectos / Variables
                    //liq.dcCostIndVaria + liq.dcCostIndFij;
                    //liq.dcCostoCopacking =  dcCostCopacking; 
                }
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"[ProcesoParametro].[ObtenerCostosProcesosMatPrimPFR] : {ObjException.Message}");
                throw;
            }
        }


        public async Task ObtenerCostosProcesosRepro(List<MatPrimaReproceso> lstLiquidaciones, DateOnly dtFechaFin)
        {

            List<ProcesoResultadoDto> lstProcesos;
            Dictionary<string, decimal> dictCostoUnitario;
            List<string> 
                lstProcPrimTiplot = new List<string>() { "DE", "R6", "R7", "UNI" },
                lstNotProcSecun = new List<string>() { "DE" },
                lstNotCostDirec = new List<string>() { 
                //Reprocesos
                "R1", "R2", "R3","RVVL","CDI", "LB04", "DV",  "RLL",  "RPY", "REC", "RS",
                //Brine
                "B1","B3",
                //Diferencia Pesos
                "BP", "BDP", 
                // VAG eti - ree
                 "VE", "VR",
                "CAM" },
                lstNotCostInd = new List<string>() { 
                //Reprocesos
                "R1", "R2", "R3","RVVL","CDI", "LB04", "DV",  "RLL",  "RPY", "REC", "RS",
                //Brine
                "B1","B3",
                //Diferencia Pesos
                "BP", "BDP", 
                // VAG eti - ree
                 "VE", "VR",

                "CAM" },
                lstNotCostConge = new List<string>() { /*"BP",*/
                    "CAM","RLL","R1","CDI","R2","REC","LB04","RPY","R3","RS","DV","RVVL","BDP","VE","VR" 
                },
                lstNotPresen = new List<string>() { "EN1", "SH2" };
            
            decimal
                // Variables de Proceso Primario
                dcCostRecepcion, dcCostClasificacion, dcCostCodificacion, dcCostDescabezado,
                // Variables de Proceso Presentación
                dcCostDecorado, dcCostRetractilado,
                // Variables de Proceso Congelación
                dcCostBrine, dcCostIQF, dcCostTunel,
                // Variables de Proceso Secundario
                dcCostPelado, dcCostHidratacion, dcCostCocido,
                // Variables de Costos Directos/Indirectos
                dcCostDirectos, dcCostFijos, dcCostIndirectos, dcCostVariables,

                dcCostCopacking, dcCostHidraHid, dcCostHidraSal
                ;
            List<int> lstCongTunel, lstCongIqf, lstCongBrine;
            List<string> lstProdCocido;

            try
            {
                lstProcesos = await ObtenerProcesosReproConValores(dtFechaFin);

                //Se obtienen las formas de congelamiento desde el catalogoDet  
                lstCongTunel = (await ConsultarCatalogoXCab(_objConfig.Value.intTunelCabeceraId)).Select(int.Parse).ToList();
                lstCongIqf = (await ConsultarCatalogoXCab(_objConfig.Value.intIqfCabId)).Select(int.Parse).ToList();
                lstCongBrine = (await ConsultarCatalogoXCab(_objConfig.Value.intBrineCabId)).Select(int.Parse).ToList();
                //Se obtiene tipo de proceso cocido para entero 
                lstProdCocido = await ConsultarCatalogoXCab(_objConfig.Value.intProdCocidoCabId);

                dictCostoUnitario = lstProcesos
                    .Where(p => !string.IsNullOrWhiteSpace(p.strDescripcion))
                    .ToDictionary(
                        p => p.strDescripcion.Trim(),
                        p => p.dcCostUnitario
                    );

                dcCostRecepcion = dictCostoUnitario.GetValueOrDefault("Recepcion");
                dcCostClasificacion = dictCostoUnitario.GetValueOrDefault("Clasificacion");
                dcCostCodificacion = dictCostoUnitario.GetValueOrDefault("Codificacion");
                dcCostDescabezado = dictCostoUnitario.GetValueOrDefault("Descabezado");

                dcCostDecorado = dictCostoUnitario.GetValueOrDefault("Decorado");
                dcCostRetractilado = dictCostoUnitario.GetValueOrDefault("Retractilado");

                dcCostBrine = dictCostoUnitario.GetValueOrDefault("Brine");
                dcCostIQF = dictCostoUnitario.GetValueOrDefault("IQF");
                dcCostTunel = dictCostoUnitario.GetValueOrDefault("Tunel");

                dcCostPelado = dictCostoUnitario.GetValueOrDefault("Pelado");
                dcCostHidratacion = dictCostoUnitario.GetValueOrDefault("Hidratacion");
                dcCostCocido = dictCostoUnitario.GetValueOrDefault("Cocido");

                dcCostDirectos = dictCostoUnitario.GetValueOrDefault("C.D.Variables");
                dcCostFijos = dictCostoUnitario.GetValueOrDefault("C.D.Fijos");
                dcCostIndirectos = dictCostoUnitario.GetValueOrDefault("C.I.Variables");
                dcCostVariables = dictCostoUnitario.GetValueOrDefault("C.I.Fijos");

                dcCostCopacking =  dictCostoUnitario.GetValueOrDefault("C.Copacking");


                foreach (var liq in lstLiquidaciones.Where(obj => obj.strAgrupacion == "2. PROCESADO"))
                {
                    if (lstProcPrimTiplot.Contains(liq.strTipCod))
                    {
                        // Proceso Primario
                        liq.ProcesoPrimario.dcRecepcion = Math.Round((decimal)liq.dbLibras * dcCostRecepcion, 4);
                        liq.ProcesoPrimario.dcClasificacion = Math.Round((decimal)liq.dbLibras * dcCostClasificacion, 4);
                        liq.ProcesoPrimario.dcCodificacion = Math.Round((decimal)liq.dbLibras * dcCostCodificacion, 4);

                        liq.dcRecepcion = dcCostRecepcion;
                        liq.dcClasificacion = dcCostClasificacion;
                        liq.dcCodificacion = dcCostCodificacion;
                    }

                    if (!lstNotPresen.Contains(liq.strTipCod))
                    {
                        // Proceso Presentación
                        liq.ProcesoPresentacion.dcDecorado = liq.blDecorado ? Math.Round((decimal)liq.dbLibras * dcCostDecorado, 4) : 0;
                        liq.ProcesoPresentacion.dcRetractilado = liq.blRetractilado ? Math.Round((decimal)liq.dcLibrasRetractilado * dcCostRetractilado, 4) : 0;
                        liq.dcDecorado = liq.blDecorado ? dcCostDecorado : 0;
                        liq.dcRetractilado = liq.blRetractilado ? dcCostRetractilado : 0;

                    }

                    if (!lstNotCostConge.Contains(liq.strTipCod))
                    {
                        // Proceso Congelación
                        liq.ProcesoCongelacion.dcBrine = liq.strCongeProduc.Trim().Equals("BRINE") ? Math.Round((decimal)liq.dbLibras * dcCostBrine, 4) : 0;
                        liq.dcBrine = liq.strCongeProduc.Trim().Equals("BRINE") ? dcCostBrine : 0;

                        liq.ProcesoCongelacion.dcTunel = liq.strCongeProduc.Trim().Equals("BLOCK") || liq.strCongeProduc.Trim().Equals("SEMI IQF") ? Math.Round((decimal)liq.dbLibras * dcCostTunel, 4) : 0;
                        liq.dcTunel = liq.strCongeProduc.Trim().Equals("BLOCK") || liq.strCongeProduc.Trim().Equals("SEMI IQF") ? dcCostTunel : 0;

                        liq.ProcesoCongelacion.dcIQF = liq.strCongeProduc.Trim().Equals("IQF") /*&& liq.strProClas03 == "PT"*/ ? Math.Round((decimal)liq.dbLibras * dcCostIQF, 4) : 0;
                        liq.dcIQF = liq.strCongeProduc.Trim().Equals("IQF") /*&& liq.strProClas03 == "PT" */? dcCostIQF : 0;

                        // Proceso Congelación
                        //liq.ProcesoCongelacion.dcBrine = liq.blBodEsBrine ? Math.Round((decimal)liq.dbLibras * dcCostBrine, 4) : 0;
                        //liq.dcBrine = liq.blBodEsBrine ? dcCostBrine : 0;

                        //liq.ProcesoCongelacion.dcTunel = lstCongTunel.Contains(liq.intProCongela) && liq.strProClas03 == "PT" ? Math.Round((decimal)liq.dbLibras * dcCostTunel, 4) : 0;
                        //liq.dcTunel = lstCongTunel.Contains(liq.intProCongela) && liq.strProClas03 == "PT" ? dcCostTunel : 0;

                        //liq.ProcesoCongelacion.dcIQF = !lstCongTunel.Contains(liq.intProCongela) && liq.blBodEsBrine == false ? Math.Round((decimal)liq.dbLibras * dcCostIQF, 4) : 0;
                        //liq.dcIQF = !lstCongTunel.Contains(liq.intProCongela) && liq.blBodEsBrine == false ? dcCostIQF : 0;
                    }

                    // Proceso Secundario
                    liq.ProcesoSecundario.dcPelado = liq.blPelado ? Math.Round((decimal)liq.dcLibrasPelado * dcCostPelado, 4) : 0;
                    liq.dcPelado = liq.blPelado ? dcCostPelado : 0;
                    decimal dcValorSal = ((liq.dcCthSallbs ?? 0m) * (liq.dcRecPorSal ?? 0m)) /2.2046m;
                    decimal dcValorHidra = ((liq.dcCthHidlbs ?? 0m) * (liq.dcRecPorHid ?? 0m)) / 2.2046m;
                    dcCostHidraSal = dcValorSal * (liq.dcValorSal ?? 0m);
                    dcCostHidraHid = (liq.dcCthHidlbs ?? 0m) * (liq.dcValorHidra ?? 0m);

                    liq.ProcesoSecundario.dcHidratacion = !String.IsNullOrEmpty(liq.strRecNombre) ? Math.Round(dcCostHidraSal + dcCostHidraHid, 4) : 0;
                    liq.dcHidratacion = !String.IsNullOrEmpty(liq.strRecNombre) ? dcCostHidraSal + dcCostHidraHid : 0;

                    liq.ProcesoSecundario.dcDescabezado = liq.blEsDescabezado ? Math.Round((decimal)liq.dbLibras * dcCostDescabezado, 4) : 0;
                    liq.dcDescabezado = liq.blEsDescabezado ? dcCostDescabezado : 0;


                    liq.ProcesoSecundario.dcCocido = !String.IsNullOrEmpty(liq.strRecTipo) && liq.strRecTipo == "COC" ?
                        Math.Round((decimal)liq.dbLibras * dcCostCocido, 4) : 0;
                    liq.dcCocido = !String.IsNullOrEmpty(liq.strRecTipo) && liq.strRecTipo == "COC" ?
                        Math.Round((decimal)liq.dbLibras * dcCostCocido, 4) : 0;


                    if ((!lstNotCostDirec.Contains(liq.strTipCod) && String.IsNullOrEmpty(liq.strRecNombre) && liq.strLotTipo == "VA")
                        || lstProcPrimTiplot.Contains(liq.strTipCod))
                    {
                        // Costos Directos / Fijos (Mapeado a ProcesoCostFijo)
                        liq.ProcesoCostFijo.dcCostoVariable = Math.Round((decimal)liq.dbLibras * dcCostDirectos, 4);
                        liq.ProcesoCostFijo.dcCostoFijo = Math.Round((decimal)liq.dbLibras * dcCostFijos, 4);
                        liq.dcCostFijVaria = dcCostDirectos;
                        liq.dcCostFijFijo = dcCostFijos;
                    }

                    if ((!lstNotCostInd.Contains(liq.strTipCod) && String.IsNullOrEmpty(liq.strRecNombre) && liq.strLotTipo == "VA")
                        || lstProcPrimTiplot.Contains(liq.strTipCod))
                    {
                        // Costos Indirectos / Variables
                        liq.ProcesoCostIndirecto.dcCostoFijo = Math.Round((decimal)liq.dbLibras * dcCostIndirectos, 4);
                        liq.ProcesoCostIndirecto.dcCostoVariable = Math.Round((decimal)liq.dbLibras * dcCostVariables, 4);
                        liq.dcCostIndirFijo = dcCostIndirectos;
                        liq.dcCostIndirVaria = dcCostVariables;
                    }

                    liq.dcCostoCopacking = liq.intCodCopacking != 0 ? Math.Round((decimal)liq.dbLibras * dcCostCopacking, 4) : 0;
                    liq.dcCostTotalProc =
                                (liq.ProcesoPrimario.dcRecepcion ?? 0) +
                                (liq.ProcesoPrimario.dcClasificacion ?? 0) +
                                (liq.ProcesoPrimario.dcCodificacion ?? 0) +
                                (liq.ProcesoPresentacion.dcDecorado ?? 0) +
                                (liq.ProcesoPresentacion.dcRetractilado ?? 0) +
                                // Proceso Congelación
                                (liq.ProcesoCongelacion.dcBrine ?? 0) +
                                (liq.ProcesoCongelacion.dcTunel ?? 0) +
                                (liq.ProcesoCongelacion.dcIQF ?? 0) +
                                // Proceso Secundario
                                (liq.ProcesoSecundario.dcPelado ?? 0) +
                                (liq.ProcesoSecundario.dcHidratacion ?? 0) +
                                (liq.ProcesoSecundario.dcDescabezado ?? 0) +
                                (liq.ProcesoSecundario.dcCocido ?? 0) +
                                // Costos Fijos y Indirectos
                                (liq.ProcesoCostFijo.dcCostoVariable ?? 0) +
                                (liq.ProcesoCostFijo.dcCostoFijo ?? 0) +
                                (liq.ProcesoCostIndirecto.dcCostoFijo ?? 0) +
                                (liq.ProcesoCostIndirecto.dcCostoVariable ?? 0) +
                                (liq.dcCostoCopacking ?? 0);
                        ////Proceso Presentacion
                        //liq.dcRecepcion + liq.dcClasificacion + liq.dcCodificacion +
                        //liq.dcDecorado + liq.dcRetractilado +
                        ////Proceso Secundario
                        //liq.dcPelado + liq.dcHidratacion + liq.dcCocido +
                        ////Proceso Congelacion
                        //liq.dcBrine + liq.dcIQF + liq.dcTunel +
                        //// Costos Directos / Fijos (Mapeado a ProcesoCostFijo)
                        //liq.dcCostFijVaria + liq.dcCostFijFijo +
                        //// Costos Indirectos / Variables
                        //liq.dcCostIndirFijo + liq.dcCostIndirVaria;
                }
            }
            catch (Exception ObjException)
            {
                _objLogger.LogError($"[ProcesoParametro].[ObtenerCostosProcesosMatPrimPFR] : {ObjException.Message}");
                throw;
            }
        }
    }
}
