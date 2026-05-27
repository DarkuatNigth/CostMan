using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Infraestructura.Repository.Interface;
using CostManagement.Infraestructura.Utils;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.Repository.Services
{
    public class CostoMaterialEmpaque : ICostoMaterialEmpaque
    {
        private readonly IDbContextFactory<CostosDbContext> _objCostosFactory;
        private readonly IDbContextFactory<SongDbContext> _objSongFactory;
        private readonly IDbContextFactory<CostManagementDbContext> _objProduccionFactory;
        private readonly ILogger<CostoMaterialEmpaque> _objLogger;

        public CostoMaterialEmpaque(
            IDbContextFactory<CostosDbContext> objCostosContext,
            IDbContextFactory<CostManagementDbContext> objProduccionFactory,
            IDbContextFactory<SongDbContext> objSongFactory,
            ILogger<CostoMaterialEmpaque> objLogger
            )
        {
            _objCostosFactory = objCostosContext;
            _objProduccionFactory = objProduccionFactory;
            _objSongFactory = objSongFactory;
            _objLogger = objLogger;
        }

        public async Task CrearRegistroCab(
            LiquidacionResultado liquidacion,
            CostoMatEmpaDto empaque,
            string usuario,
            string equipo)
        {
            try
            {
                await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        var cabecera = new TbCostoEmpaqueCab
                        {
                            CeEmpCodigo = (short)(liquidacion.strPlanta == "MILAGRO" ? 1 : 2),
                            CeFecha = liquidacion.dtFechaLiq ?? DateOnly.FromDateTime(DateTime.Now),
                            CeRloNumero = liquidacion.intLote,
                            CeProCodcor = empaque.strProCodCor ?? string.Empty,
                            CeLbsMaster = (decimal)(empaque.dcLibxMaster ?? 0),
                            CcCosto = 0,
                            CcMedCodigo = 1,
                            CcEmbCodigo = empaque.strEmbDescri?.Substring(0, Math.Min(6, empaque.strEmbDescri.Length)) ?? string.Empty,
                            CcEmbPeso = (decimal)(empaque.dcEmbPeso ?? 0),
                            CcEstado = "AC",
                            CcUsuarioCrea = usuario,
                            CcFechaCrea = DateTime.Now,
                            CcEquipoCrea = equipo
                        };

                        await objContext.TbCostoEmpaqueCab.AddAsync(cabecera);
                        await objContext.SaveChangesAsync();

                        ManejoLog<CostoMaterialEmpaque>.Informacion(_objLogger,
                            nameof(CostoMaterialEmpaque), nameof(CrearRegistroCab),
                            $"Cabecera creada con ID: {cabecera.CeId}");
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(CrearRegistroCab), objException);
                throw;
            }
        }

        public async Task CrearRegistroDet(
            int cabeceraId,
            List<CostoMatEmpaDto> detalles,
            string usuario,
            string equipo)
        {
            try
            {
                await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        decimal costoTotal = 0;

                        foreach (var detalle in detalles)
                        {
                            var detalleEmpaque = new TbCostoEmpaqueDet
                            {
                                CdCcId = cabeceraId,
                                CdEftId = detalle.intEftItem,
                                CdEftItem = detalle.strIteDesCor,
                                CdEftCantidad = (decimal)(detalle.dcEftCantidad ?? 0),
                                CdCostoPromedio = detalle.dcCosPro ?? 0,
                                CdCostoUltimoConsumo = (decimal)(detalle.dcCostUltConsumo ?? 0),
                                CdCostoDespericioBobina = detalle.dcProCostoDesperdicioBobina ?? 0,
                                CdOrigenCosto = "P",
                                CdEstado = "AC",
                                CdUsuarioCrea = usuario,
                                CdFechaCrea = DateTime.Now,
                                CdEquipoCrea = equipo
                            };

                            decimal costoDetalle = detalleEmpaque.CdOrigenCosto == "P"
                                ? detalleEmpaque.CdCostoPromedio
                                : detalleEmpaque.CdCostoUltimoConsumo;

                            costoTotal += costoDetalle * detalleEmpaque.CdEftCantidad;

                            await objContext.TbCostoEmpaqueDet.AddAsync(detalleEmpaque);
                        }

                        var cabecera = await objContext.TbCostoEmpaqueCab.FindAsync(cabeceraId);
                        if (cabecera != null)
                        {
                            cabecera.CcCosto = costoTotal;
                            cabecera.CcUsuarioMod = usuario;
                            cabecera.CcFechaMod = DateTime.Now;
                            cabecera.CcEquipoMod = equipo;
                        }

                        await objContext.SaveChangesAsync();

                        ManejoLog<CostoMaterialEmpaque>.Informacion(_objLogger,
                            nameof(CostoMaterialEmpaque), nameof(CrearRegistroDet),
                            $"Creados {detalles.Count} registros de detalle para cabecera ID: {cabeceraId}");
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(CrearRegistroDet), objException);
                throw;
            }
        }

        public async Task CrearCostoEmpaqueCompleto(List<CostoMatEmpProdXCietunDto> lstCostosEmpaque, string usuario, string equipo)
        {
            try
            {
                await ManejoContext<CostosDbContext>.EjecutarEnTransaccionAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        CostoMatEmpProdXCietunDto objEmpaqueInfo = lstCostosEmpaque.First();

                        var objCabecera = await objContext.TbCostoEmpaqueCab
                            .FirstOrDefaultAsync(c => c.CeRloNumero == objEmpaqueInfo.intLiqLote &&
                                                      c.CeProCodcor == objEmpaqueInfo.strProCodCor);

                        if (objCabecera == null)
                        {
                            objCabecera = new TbCostoEmpaqueCab
                            {
                                CeEmpCodigo = 1,
                                CeFecha = objEmpaqueInfo.dtFechaEgreso,
                                CeRloNumero = objEmpaqueInfo.intLiqLote,
                                CeProCodcor = objEmpaqueInfo.strProCodCor ?? string.Empty,
                                CeLbsMaster = (decimal)objEmpaqueInfo.dcLibrasXMasters,
                                CcCosto = 0,
                                CcMedCodigo = (byte)objEmpaqueInfo.dcMedCodigo!,
                                CcEmbCodigo = objEmpaqueInfo.strEmbCodigo!.Trim(),
                                CcEmbPeso = (decimal)objEmpaqueInfo.dbEmbPeso,
                                CcEstado = "AC",
                                CcUsuarioCrea = usuario,
                                CcFechaCrea = DateTime.Now,
                                CcEquipoCrea = equipo
                            };

                            await objContext.TbCostoEmpaqueCab.AddAsync(objCabecera);
                            await objContext.SaveChangesAsync();
                        }

                        var objDetallesExist = await objContext.TbCostoEmpaqueDet
                            .Where(d => d.CdCcId == objCabecera.CeId).ToListAsync();

                        if (objDetallesExist.Count == 0)
                        {
                            decimal dcCostoTotalAc = 0;
                            var lstDetEmpaque = new List<TbCostoEmpaqueDet>();

                            foreach (var objDetalle in lstCostosEmpaque)
                            {
                                decimal divisor = (decimal)objEmpaqueInfo.dcLibrasXMasters;
                                if (divisor == 0) divisor = 1;

                                decimal precioUnitario = (decimal)(objDetalle.dbPrecioUnit ?? 0);
                                decimal cantidadFicha = (decimal)objDetalle.dbEftCantidad;
                                decimal dcCostoProporcional = precioUnitario != 0 ? (precioUnitario * cantidadFicha) / divisor : 0;

                                lstDetEmpaque.Add(new TbCostoEmpaqueDet
                                {
                                    CdCcId = objCabecera.CeId,
                                    CdEftId = objDetalle.intEftItem,
                                    CdEftItem = objDetalle.strEftGrupo,
                                    CdEftCantidad = cantidadFicha,
                                    CdCostoPromedio = Math.Round(precioUnitario, 4),
                                    CdCostoUltimoConsumo = Math.Round((decimal)objDetalle.dbPrecioUltConsumo, 4),
                                    CdCostoDespericioBobina = objDetalle.dcCostoDesperdicioBobina,
                                    CdOrigenCosto = objDetalle.strEstadoFicha ?? "X",
                                    CdEstado = "AC",
                                    CdUsuarioCrea = usuario,
                                    CdFechaCrea = DateTime.Now,
                                    CdEquipoCrea = equipo
                                });

                                dcCostoTotalAc += dcCostoProporcional;
                            }

                            if (lstDetEmpaque.Any())
                            {
                                await objContext.TbCostoEmpaqueDet.AddRangeAsync(lstDetEmpaque);
                                objCabecera.CcCosto = Math.Round(dcCostoTotalAc, 4);
                            }
                        }

                        await objContext.SaveChangesAsync();
                    },
                    intTimeout: 120);
            }
            catch (Exception objException)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(CrearCostoEmpaqueCompleto), objException);
                throw;
            }
        }

        public async Task ObtenerCostoMaterialEmpaqueXLiqProd(List<LiquidacionResultado> lstResultado)
        {
            try
            {
                await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        Dictionary<LoteRpcKeyLoteXProd, Queue<decimal>> dictCostMatEmp;
                        Dictionary<LoteRpcKeyLoteXProd, Queue<List<LiquidacionResultado>>> dictColasProd;
                        decimal dcNuevoCosto;
                        decimal? dcUltCostValido = null;

                        var lstLoteSecuencial = lstResultado
                            .Select(x => x.intLote)
                            .Distinct().ToList();

                        var costosCabecera = await objContext.TbCostoEmpaqueCab.AsNoTracking()
                            .SelectManyBatchAsync(
                                keySelector: cab => cab.CeRloNumero,
                                values: lstLoteSecuencial,
                                selector: filtered =>
                                    from cab in filtered
                                    select new
                                    {
                                        Lote = cab.CeRloNumero,
                                        CodProd = cab.CeProCodcor.Trim(),
                                        dtFechaLote = cab.CeFecha,
                                        CcCosto = cab.CcCosto,
                                        CeLbsMaster = cab.CeLbsMaster
                                    }
                            );

                        dictColasProd = LiquidacionResultado.GenerarDiccionarioProcMatEmp(lstResultado);

                        dictCostMatEmp = costosCabecera
                            .GroupBy(x => new LoteRpcKeyLoteXProd(x.Lote, x.CodProd.ToString().Trim()))
                            .ToDictionary(g => g.Key, g => new Queue<decimal>(g.Select(c => c.CcCosto)));

                        foreach (var entry in dictColasProd)
                        {
                            if (!dictCostMatEmp.TryGetValue(entry.Key, out var colaCostos)) continue;

                            dcUltCostValido = null;

                            while (entry.Value.Count > 0)
                            {
                                var grupoTalla = entry.Value.Dequeue();

                                if (colaCostos.TryDequeue(out dcNuevoCosto))
                                    dcUltCostValido = dcNuevoCosto;

                                if (!dcUltCostValido.HasValue) continue;

                                foreach (var objLiq in grupoTalla)
                                {
                                    objLiq.dcCostoMatEmpaque = Math.Round(
                                        dcUltCostValido.Value, 4, MidpointRounding.ToZero);

                                    objLiq.dcCostoTotalMatEmp = (decimal)Math.Round(
                                        objLiq.dcLibras * (double)objLiq.dcCostoMatEmpaque,
                                        2, MidpointRounding.ToZero);
                                }
                            }
                        }
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(ObtenerCostoMaterialEmpaqueXLiqProd), objException);
                throw;
            }
        }

        public async Task ObtenerCostoMaterialEmpaqueXLiqProd(List<MatPrimaReproceso> lstMatPrimaReproceso)
        {
            try
            {
                await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    async objContext =>
                    {
                        List<MatPrimaReproceso> lstMatPrimaReproSinCostMatProd;
                        Dictionary<LoteRpcKeyLoteXProd, Queue<decimal>> dictCostMatEmp;
                        Dictionary<LoteRpcKeyLoteXProd, Queue<List<MatPrimaReproceso>>> dictColasProd;
                        decimal dcCostoUnit, dcNuevoCosto;
                        decimal? dcUltCostValido = null;

                        lstMatPrimaReproSinCostMatProd = lstMatPrimaReproceso.Where(
                            lbsRecProc => lbsRecProc.strAgrupacion == "2. PROCESADO"
                            ).ToList();

                        var lstLoteSecuencial = lstMatPrimaReproSinCostMatProd
                            .Select(x => x.intLotNumero)
                            .Distinct()
                            .ToList();

                        var objCostCabecera = await objContext.TbCostoEmpaqueCab
                            .AsNoTracking()
                            .SelectManyBatchAsync(
                                keySelector: cab => cab.CeRloNumero,
                                values: lstLoteSecuencial,
                                selector: filtered =>
                                    from cab in filtered
                                    select new
                                    {
                                        Lote = cab.CeRloNumero,
                                        CodProd = cab.CeProCodcor.Trim(),
                                        dtFechaLote = cab.CeFecha,
                                        CcCosto = cab.CcCosto,
                                        CeLbsMaster = cab.CeLbsMaster
                                    }
                            );

                        dictCostMatEmp = objCostCabecera
                            .GroupBy(x => new LoteRpcKeyLoteXProd(x.Lote, x.CodProd))
                            .ToDictionary(g => g.Key, g => new Queue<decimal>(g.Select(c => c.CcCosto)));

                        dictColasProd = MatPrimaReproceso.GenerarDiccionarioProcMatEmp(lstMatPrimaReproSinCostMatProd);

                        foreach (var entry in dictColasProd)
                        {
                            if (!dictCostMatEmp.TryGetValue(entry.Key, out var colaCostos)) continue;

                            dcUltCostValido = null;
                            while (entry.Value.Count > 0)
                            {
                                var grupoTalla = entry.Value.Dequeue();

                                if (colaCostos.TryDequeue(out dcNuevoCosto))
                                    dcUltCostValido = dcNuevoCosto;

                                if (!dcUltCostValido.HasValue) continue;

                                dcCostoUnit = Math.Round(dcUltCostValido.Value, 4, MidpointRounding.ToZero);

                                foreach (var objLiq in grupoTalla)
                                {
                                    objLiq.dcCostoMatEmpaque = dcCostoUnit;
                                    objLiq.dcCostoTotalMatEmp = (decimal)Math.Round(
                                        objLiq.dbLibras * (double)dcCostoUnit, 2, MidpointRounding.ToZero);
                                }
                            }
                        }
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(ObtenerCostoMaterialEmpaqueXLiqProd), objException);
                throw;
            }
        }

        public async Task<List<CostoMatEmpaDto>> ObtenerCostoEmpaqueXRangoFecha(DateOnly dtFechaInicio, DateOnly dtFechaFin)
        {
            try
            {
                var lstResultados = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    objContext =>
                        (
                            from cab in objContext.TbCostoEmpaqueCab
                            join det in objContext.TbCostoEmpaqueDet
                                on cab.CeId equals det.CdCcId
                            where cab.CeFecha >= dtFechaInicio && cab.CeFecha <= dtFechaFin
                            select new CostoMatEmpaDto
                            {
                                strProCodCor = cab.CeProCodcor,
                                strProDesEsp = cab.CeProCodcor,
                                strMarDescri = "N/A",
                                dcEftCantidad = (float)det.CdEftCantidad,
                                strAlmDescri = "",
                                dcEmbPeso = (float)cab.CcEmbPeso,
                                strMedDescri = cab.CcMedCodigo.ToString(),
                                dcLibxMaster = (float)cab.CeLbsMaster,
                                intEftItem = (int)det.CdEftId,
                                strIteDesCor = det.CdEftId.ToString(),
                                ftCostoXMaster = (float)Math.Round((det.CdCostoPromedio * det.CdEftCantidad), 4),
                                ftCostoXLibra = (float)Math.Round(((det.CdCostoPromedio * det.CdEftCantidad) / cab.CeLbsMaster), 4),
                                dcCosPro = det.CdCostoPromedio,
                                dcCostUltConsumo = (float)det.CdCostoUltimoConsumo,
                                intTotal = (double)cab.CcCosto,
                                strSubClase = "",
                                strEmbDescri = cab.CcEmbCodigo,
                                dcProCostoDesperdicioBobina = det.CdCostoDespericioBobina,
                                strEstadoEmpaque = det.CdOrigenCosto,
                                intLiqLote = cab.CeRloNumero,
                                dtFechaLote = cab.CeFecha,
                            }
                        ).ToListAsync());

                await LlenarInfoProdCostoEmpaque(lstResultados);
                await LlenarInfoItemCostoEmpaque(lstResultados);
                return lstResultados;
            }
            catch (Exception ex)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(ObtenerCostoEmpaqueXRangoFecha), ex);
                throw;
            }
        }

        public async Task<List<CostoMatEmpaDto>> ObtenerCostoEmpaqueXLote(List<int> lstLotesFrsRpc)
        {
            try
            {
                var lstResultados = await ManejoContext<CostosDbContext>.EjecutarAsync(
                    _objCostosFactory,
                    objContext =>
                        objContext.TbCostoEmpaqueCab
                            .AsNoTracking()
                            .SelectManyBatchAsync(
                                keySelector: cab => cab.CeRloNumero,
                                values: lstLotesFrsRpc,
                                selector: filtered =>
                                    from cab in filtered
                                    join det in objContext.TbCostoEmpaqueDet on cab.CeId equals det.CdCcId
                                    select new CostoMatEmpaDto
                                    {
                                        strProCodCor = cab.CeProCodcor,
                                        strProDesEsp = cab.CeProCodcor,
                                        strMarDescri = "N/A",
                                        dcEftCantidad = (float)det.CdEftCantidad,
                                        strAlmDescri = "",
                                        dcEmbPeso = (float)cab.CcEmbPeso,
                                        strMedDescri = cab.CcMedCodigo.ToString(),
                                        dcLibxMaster = (float)cab.CeLbsMaster,
                                        intEftItem = (int)det.CdEftId,
                                        strIteDesCor = det.CdEftId.ToString(),
                                        ftCostoXMaster = (float)Math.Round((det.CdCostoPromedio * det.CdEftCantidad), 4),
                                        ftCostoXLibra = (float)Math.Round(((det.CdCostoPromedio * det.CdEftCantidad) / cab.CeLbsMaster), 4),
                                        dcCosPro = det.CdCostoPromedio,
                                        dcCostUltConsumo = (float)det.CdCostoUltimoConsumo,
                                        intTotal = (double)cab.CcCosto,
                                        strSubClase = "",
                                        strEmbDescri = cab.CcEmbCodigo,
                                        dcProCostoDesperdicioBobina = det.CdCostoDespericioBobina,
                                        strEstadoEmpaque = det.CdOrigenCosto,
                                        intLiqLote = cab.CeRloNumero,
                                        dtFechaLote = cab.CeFecha,
                                    }
                            ));

                await LlenarInfoProdCostoEmpaque(lstResultados);
                await LlenarInfoItemCostoEmpaque(lstResultados);
                return lstResultados;
            }
            catch (Exception ex)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(ObtenerCostoEmpaqueXLote), ex);
                throw;
            }
        }

        public async Task LlenarInfoProdCostoEmpaque(List<CostoMatEmpaDto> lstInfoMatEmpaque)
        {
            try
            {
                await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                    _objProduccionFactory,
                    async objContext =>
                    {
                        var lstCodProduc = lstInfoMatEmpaque
                            .Select(e => e.strProCodCor)
                            .Distinct()
                            .ToList();

                        var lstInformacionProduc = await
                            objContext.TbProduc.AsNoTracking()
                            .SelectManyBatchAsync(
                                keySelector: p => p.ProCodcor.Trim(),
                                values: lstCodProduc,
                                selector: filtered =>
                                    from pro in filtered
                                    join col in objContext.TbColor on pro.ProClas05 equals col.ColCodigo
                                    join med in objContext.TbMedida on pro.ProUnimed equals med.MedCodigo
                                    join emb in objContext.TbEmbala on pro.ProEmbala equals emb.EmbCodigo
                                    join mrc in objContext.TbMarca on pro.ProClas04 equals mrc.MarCodigo
                                    join alm in objContext.TbAlmace on pro.ProUnialm equals alm.AlmCodigo
                                    select new
                                    {
                                        CodProd = pro.ProCodcor.Trim(),
                                        Descripcion = pro.ProDesesp.Trim(),
                                        MedDescri = med.MedDescri.Trim(),
                                        SubClase = col.ColDescri.Trim(),
                                        EmbCanti = emb.EmbCantid,
                                        AlmaDesc = alm.AlmDescri.Trim(),
                                        EmbDescri = emb.EmbDescri.Trim(),
                                        MarcDescri = mrc.MarDescri.Trim(),
                                        ProClas03 = pro.ProClas03.Trim()
                                    }
                            );

                        var infoLookup = lstInformacionProduc.ToLookup(x => x.CodProd);
                        foreach (var objEmpaque in lstInfoMatEmpaque)
                        {
                            var info = infoLookup[objEmpaque.strProCodCor].FirstOrDefault();
                            if (info != null)
                            {
                                objEmpaque.strProDesEsp = info.Descripcion;
                                objEmpaque.strMedDescri = info.MedDescri;
                                objEmpaque.strSubClase = info.SubClase;
                                objEmpaque.strAlmDescri = info.AlmaDesc;
                                objEmpaque.dcEmbCantid = (float)info.EmbCanti;
                                objEmpaque.strEmbDescri = info.EmbDescri;
                                objEmpaque.strMarDescri = info.MarcDescri;
                                objEmpaque.strProClas03 = info.ProClas03;
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(LlenarInfoProdCostoEmpaque), ex);
                throw;
            }
        }

        public async Task LlenarInfoItemCostoEmpaque(List<CostoMatEmpaDto> lstInfoMatEmpaque)
        {
            try
            {
                await ManejoContext<SongDbContext>.EjecutarAsync(
                    _objSongFactory,
                    async objContext =>
                    {
                        var lstCodItem = lstInfoMatEmpaque
                            .Select(e => e.intEftItem.ToString())
                            .Distinct()
                            .ToList();

                        var lstInformacionItem = await objContext.TbItem.AsNoTracking()
                            .SelectManyBatchAsync
                            (
                                keySelector: i => i.IteCodigo.Trim(),
                                values: lstCodItem,
                                selector: filtered =>
                                    from ite in filtered
                                    join lin in objContext.TbLinea on ite.LinCodigo equals lin.LinCodigo
                                    join grp in objContext.TbGrupo on new
                                    { A = ite.LinCodigo, B = ite.GrpCodigo } equals new
                                    { A = grp.LinCodigo, B = grp.GrpCodigo }
                                    select new
                                    {
                                        CodItem = ite.IteCodigo.Trim(),
                                        DescripcionCorta = ite.IteDescor.Trim(),
                                        Linea = lin.LinNombre.Trim(),
                                        Grupo = grp.GrpNombre.Trim()
                                    }
                            );

                        var infoLookup = lstInformacionItem.ToLookup(x => x.CodItem);
                        foreach (var objEmpaque in lstInfoMatEmpaque)
                        {
                            var info = infoLookup[objEmpaque.intEftItem.ToString()].FirstOrDefault();
                            if (info != null)
                            {
                                objEmpaque.strIteDesCor = info.DescripcionCorta;
                                objEmpaque.strLinea = info.Linea;
                                objEmpaque.strGrupo = info.Grupo;
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(LlenarInfoItemCostoEmpaque), ex);
                throw;
            }
        }

        public async Task<List<TratamientoProdDto>> ObtenerTratamientoProd(List<string> lstCodProd)
        {
            try
            {
                return await ManejoContext<CostManagementDbContext>.EjecutarAsync(
                    _objProduccionFactory,
                    async objContext =>
                    {
                        List<TratamientoProdDto> lstTratamientoCubas = await objContext.TbRelprodvar.AsNoTracking()
                            .SelectManyBatchAsync(
                                keySelector: p => p.RpvProduc!.Trim(),
                                values: lstCodProd,
                                selector: filtered =>
                                    from rpv in filtered
                                    join var in objContext.TbVarios on new { A = rpv.RpvCodigo, B = rpv.RpvGrupo } equals new { A = var.VarCodigo, B = var.VarGrupo }
                                    join pro in objContext.TbProduc on rpv.RpvProduc equals pro.ProCodcor
                                    join pces in objContext.TbProces on new { A = pro.ProClas06, B = "AC" } equals new { A = pces.ProCodigo, B = pces.ProEstado } into pcesGroup
                                    from pces in pcesGroup.DefaultIfEmpty()
                                    join rec in objContext.TblRecetahidrat.AsNoTracking()
                                    on new { A = rpv.RpvCodigo, B = pces.ProCocido == 3 ? "CRU" : "COC" } equals new { A = rec.RecCodigo.ToString(), B = rec.RecTipo } into recGroup
                                    from rec in recGroup.DefaultIfEmpty()
                                    where rpv.RpvGrupo == "TRA_2" && rpv.RpvPorcen != 0
                                    select new TratamientoProdDto
                                    (
                                        rpv.RpvProduc!.Trim(),
                                        var.VarDescri.Trim(),
                                        rpv.RpvPorcen,
                                        rpv.RpvMin,
                                        rec.RecTiempo
                                    )
                            );

                        List<TratamientoProdDto> lstTratamientoTumblers = await objContext.TbRelprodvar.AsNoTracking()
                            .SelectManyBatchAsync(
                                keySelector: p => p.RpvProduc!.Trim(),
                                values: lstCodProd,
                                selector: filtered =>
                                    from rpv in filtered
                                    join var in objContext.TbVarios on new { A = rpv.RpvCodigo, B = rpv.RpvGrupo } equals new { A = var.VarCodigo, B = var.VarGrupo }
                                    join pro in objContext.TbProduc on rpv.RpvProduc equals pro.ProCodcor
                                    join pces in objContext.TbProces on new { A = pro.ProClas06, B = "AC" } equals new { A = pces.ProCodigo, B = pces.ProEstado } into pcesGroup
                                    from pces in pcesGroup.DefaultIfEmpty()
                                    join rec in objContext.TblRecetahidrat.AsNoTracking()
                                    on new { A = rpv.RpvCodigo, B = pces.ProCocido == 3 ? "CRU" : "COC" } equals new { A = rec.RecCodigo.ToString(), B = rec.RecTipo } into recGroup
                                    from rec in recGroup.DefaultIfEmpty()
                                    where rpv.RpvGrupo == "TRA" && rpv.RpvPorcen != 0
                                    select new TratamientoProdDto
                                    (
                                        rpv.RpvProduc!.Trim(),
                                        var.VarDescri.Trim(),
                                        rpv.RpvPorcen,
                                        rpv.RpvMin,
                                        rec.RecTiempo
                                    )
                            );

                        return lstTratamientoCubas.Concat(lstTratamientoTumblers).ToList();
                    });
            }
            catch (Exception objException)
            {
                ManejoLog<CostoMaterialEmpaque>.Error(_objLogger, nameof(CostoMaterialEmpaque), nameof(ObtenerTratamientoProd), objException);
                throw;
            }
        }
    }
}
