using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Dominio.Reglas
{
    public class MotorAsignacionPrecios
    {
        private readonly HashSet<int> _lstProdReproSinTal = new HashSet<int> { 5907, 5908, 5909, 5910, 5911, 5912 };
        private static readonly HashSet<string> _lstProcNoCont =
            new(StringComparer.OrdinalIgnoreCase) { "SALDO", "LIQFR", "LIQOP", "INGR" };
        private readonly List<int> _lstTallaEspecial = new List<int> { 128, 131 };
        private const decimal COSTO_ESPECIAL = 2.00m;
        private const string NIVEL_DEFECTO = "NV1";

        private const string _strCodTipSal = "SALDO";
        private const string _strTipoMovE = "E";
        private const string _strTipoMovI = "I";
        private static readonly HashSet<string> _lstCodSubTip =
            new(StringComparer.OrdinalIgnoreCase) { "ET2","ET1", "CAM" };
        private const string _strCodEtiEgre = "ET2";
        private const string _strCodTiploEti = "CAM";
        private const string _strCodEtiIngre = "ET1";
        private const int _intDecimalesCu = 4;   // 4 decimales para costo unitario
        private const int _intDecimalesCt = 2;   // 2 decimales para costo total
        private readonly ILogger _objLogger;
        private enum FuenteCosto { Ninguna, Frs, Rpc, Sld }


        public MotorAsignacionPrecios( ILogger logger)
        {
            _objLogger = logger;
        }
        public void AsignarCostRecibiXFrsMovCam(
            List<PrecioFrsXMov> lstPrecioLiqOtrProc,
            List<PrecioFrsXMov> lstPrecioFrsXMovCam,
            DataProcesoParam objDataProceso)
        {

            if (objDataProceso.lstLiqRepro == null) throw new ArgumentNullException(nameof(objDataProceso.lstLiqRepro));
            if (lstPrecioLiqOtrProc == null) throw new ArgumentNullException(nameof(lstPrecioLiqOtrProc));
            if (lstPrecioFrsXMovCam == null) throw new ArgumentNullException(nameof(lstPrecioFrsXMovCam));
            if (objDataProceso.lstLiqFresco == null) throw new ArgumentNullException(nameof(objDataProceso.lstLiqFresco));
            // Lógica de diccionarios
            var dictCostoProdXTalla = LiquidacionResultado.GenerarDiccionarioCostoXTalla(objDataProceso.lstLiqFresco);
            var dictLiqOtrCostoProdXTalla = PrecioFrsXMov.GenerarDiccionarioCostoXTalla(lstPrecioLiqOtrProc);
            var dictLiqMovCamCostoProdXTalla = PrecioFrsXMov.GenerarDiccionarioCostoXTalla(lstPrecioFrsXMovCam);

            var lstMatPrimaRpcFilt = MatPrimaReproceso.GenerarLstFiltRec(objDataProceso.lstLiqRepro);
            foreach (var liq in lstMatPrimaRpcFilt)
            {
                var keyBuscada = (liq.intLoteOrigen, liq.intProdCod, liq.intCodTal);

                // Aplicar jerarquía de precios
                if (dictCostoProdXTalla.TryGetValue(keyBuscada, out var precioPromedio))
                {
                    AsignarPrecio(liq, (decimal)precioPromedio, NIVEL_DEFECTO);
                }
                else if (dictLiqOtrCostoProdXTalla.TryGetValue(keyBuscada, out var precioOtroProc))
                {
                    AsignarPrecio(liq, (decimal)precioOtroProc, NIVEL_DEFECTO);
                }
                else if (dictLiqMovCamCostoProdXTalla.TryGetValue(keyBuscada, out var precioMovCam))
                {
                    AsignarPrecio(liq, (decimal)precioMovCam, NIVEL_DEFECTO);
                }
            }
        }


        public void EjecutarAsignacionPorSaldo(
            List<MatPrimaReproceso> lstMatPrimaReproceso,
            ILookup<(string, short), decimal> lstPreciosProm,
            ILookup<(int, string, short), decimal> lstPrecios)
        {
            if (lstMatPrimaReproceso == null) throw new ArgumentNullException(nameof(lstMatPrimaReproceso));
            if (lstPreciosProm == null) throw new ArgumentNullException(nameof(lstPreciosProm));
            if (lstPrecios == null) throw new ArgumentNullException(nameof(lstPrecios));
            List<MatPrimaReproceso> lstMatPrimaRpcFilt;
            lstMatPrimaRpcFilt = MatPrimaReproceso.GenerarLstFiltRec(lstMatPrimaReproceso);

            foreach (var objLiq in lstMatPrimaRpcFilt)
            {
                // Regla 1: Talla Especial
                if (_lstProdReproSinTal.Contains(objLiq.intProdCod) && _lstTallaEspecial.Contains(objLiq.intCodTal))
                {
                    AsignarPrecio(objLiq, COSTO_ESPECIAL, NIVEL_DEFECTO);
                    continue;
                }

                var keyVal = (objLiq.intLoteOrigen, objLiq.intProdCod.ToString(), (short)objLiq.intCodTal);
                var keyPreProm = (objLiq.intProdCod.ToString(), (short)objLiq.intCodTal);

                // Regla 2: Precio por Lote e Inventario
                var objPrecioLote = lstPrecios[keyVal];

                if (objPrecioLote.Any())
                {
                    AsignarPrecio(objLiq, objPrecioLote.Average(), NIVEL_DEFECTO);
                }
                var objPrecioProm = lstPreciosProm[keyPreProm];
                if (objPrecioProm.Any() && objLiq.dbCostoXSecuencial == 0)
                {
                    AsignarPrecio(objLiq, objPrecioProm.Average(), NIVEL_DEFECTO);
                }
            }
        }

        public void RendimientoReproPlanRecibProc(List<MatPrimaReproceso> lstTotalLibrasRecProc)
        {
            if (lstTotalLibrasRecProc == null) throw new ArgumentNullException(nameof(lstTotalLibrasRecProc));
            decimal dcCostoProc, dcCostTotalProc, dcCostoTotalMatEmp, dcTotalDol;
            var lstRendiLookup = (
                    from lbsRecProc in lstTotalLibrasRecProc
                    //where lbsRecProc.intCodCopacking == 0
                    group new { lbsRecProc } by new
                    {
                        //lbsRecProc.strTipCod
                        lbsRecProc.intLotNumero,
                        lbsRecProc.intLoteUnificado
                    } into g
                    let totalRecibido = g
                        .Where(obj =>
                        obj.lbsRecProc.strAgrupacion == "1. RECIBIDO")
                        .Sum(obj => obj.lbsRecProc.dbLibras)
                    let totalProcesado = g
                        .Where(obj =>
                        obj.lbsRecProc.strAgrupacion == "2. PROCESADO")
                        .Sum(obj => obj.lbsRecProc.dbLibras)
                    select new
                    {
                        //g.Key.strTipCod,
                        g.Key.intLotNumero,
                        g.Key.intLoteUnificado,
                        Rendimiento = totalRecibido != 0 ? (totalProcesado / totalRecibido) : 0
                    }
                    ).ToList()
                    .ToLookup( x=> /*x.strTipCod*/(x.intLotNumero, x.intLoteUnificado));

            foreach (var objLiq in lstTotalLibrasRecProc)
            {
                var objkey = (objLiq.intLotNumero, objLiq.intLoteUnificado);
                var objValRendi = lstRendiLookup[objkey].FirstOrDefault();
                if (objValRendi != null)
                    objLiq.dbRendimiento = objValRendi.Rendimiento;
                if (objValRendi == null )
                {
                    var obj = objkey;
                }
                if (objLiq.strAgrupacion != "2. PROCESADO" || objLiq.dbLibras <= 0 || objLiq.dcCostoTotXLibra != null) continue;

                dcCostTotalProc = (objLiq.dcCostTotalProc ?? 0m);
                dcCostoTotalMatEmp = (objLiq.dcCostoTotalMatEmp ?? 0m);
                dcTotalDol = (decimal)objLiq.dbCostoTotal;
                dcCostoProc = (objLiq.dcTarifaProc ?? 0m); 
                objLiq.dcTotalDolSum = dcCostTotalProc + dcCostoTotalMatEmp + dcTotalDol + dcCostoProc;
                objLiq.dcCostoTotXLibra = Math.Truncate((objLiq.dcTotalDolSum / (decimal)objLiq.dbLibras) * 100m) / 100m;
                objLiq.dcValidador = (decimal)objLiq.dcCostoTotXLibra - (decimal)objLiq.dbCostoXSecuencial; 
            }
        }


        public void AsignarPrecio(MatPrimaReproceso objLiq, decimal precio, string nivel)
        {
            if (objLiq.blExcluidoCosteo && objLiq.strAgrupacion == "1. RECIBIDO") return; 
            objLiq.dbCostoXSecuencial = Math.Round(precio, 4);
            objLiq.dbCostoTotal = Math.Round(precio * (decimal)objLiq.dbLibras, 2);
            if (nivel != null) objLiq.strNivel = nivel;        
        }


        public void AsignarCostHidra(List<CostoMovArtDto> lstCostoPromedio, List<MatPrimaReproceso> lstTotalLibrasRecProc)
        {


            if (lstCostoPromedio == null) throw new ArgumentNullException(nameof(lstCostoPromedio));
            if (lstTotalLibrasRecProc == null) throw new ArgumentNullException(nameof(lstTotalLibrasRecProc));
            var dictCosto = lstCostoPromedio
                .ToDictionary(x => x.intItemCod, x => x.dcPrecioProm );
            // dcValorSal es constante para todos los ítems — calcularlo fuera del loop
            if (!dictCosto.TryGetValue(18623, out decimal dcSalBase))
                dcSalBase = 0m;

            foreach (var liqOtr in lstTotalLibrasRecProc.Where(x =>
                x.strAgrupacion == "2. PROCESADO" &&
                x.intCodCopacking == 0 &&
                !string.IsNullOrEmpty(x.strRecNombre)))
            {
                liqOtr.dcValorSal = Math.Round(dcSalBase, 2);
                liqOtr.dcValorHidra = dictCosto.TryGetValue((int)liqOtr.intRtCodItem, out decimal v) ? Math.Round(v, 2) : 0m;
            }
        }

        #region Costos Especiales Reproceso
        // ── Métodos especiales (llamados desde MotorProrrateo) ─────────────────
        public void AsignarCostoUniMovCam(
            MatPrimaReproceso item,
            List<long> lstLoteOrigen,
            List<PrecioFrsXMov> lstPrecioFrsUni)
        {
            var lstCodProd = new HashSet<int> { 320, 321 };
            var filtrado = lstPrecioFrsUni.Where(x => lstLoteOrigen.Contains(x.intLidLote)).ToList();
            var lookup = filtrado
                .Where(x => x.strProClasePago == item.strClaseProd
                         && lstCodProd.Contains(x.intProCodcor)
                         && (int)x.srtTcdCodtal == item.intCodTal)
                .ToLookup(x => (x.strProClasePago, x.srtTcdCodtal));

            var precio = lookup[(item.strClaseProd, (short)item.intCodTal)].FirstOrDefault();
            if (precio != null)
                AsignarPrecio(item, (decimal)precio.dbLidPrecio, null); // strNivel ya lo puso el caller
        }

        public void AsignarCostoReprocesoColaDir(
            MatPrimaReproceso item,
            List<PrecioFrsXMov> lstPrecioFrsDir)
        {
            var lstCodProd = new HashSet<int> { 5349, 321 };
            var filtrado = lstPrecioFrsDir
                .Where(x => x.intLidLote == item.intLoteUnificado
                         && lstCodProd.Contains(x.intProCodcor))
                .ToList();
            var lookup = filtrado
                .Where(x => x.strProClasePago == item.strClaseProd
                         && (int)x.srtTcdCodtal == item.intCodTal)
                .ToLookup(x => (x.strProClasePago, x.srtTcdCodtal));

            var precio = lookup[(item.strClaseProd, (short)item.intCodTal)].FirstOrDefault();
            if (precio != null)
                AsignarPrecio(item, (decimal)precio.dbLidPrecio, null);
        }

        public void AsignarCostoDescExtraPola(
            MatPrimaReproceso item,
            List<long> lstLoteOrigen,
            List<LiquidacionResultado> lstFresco,
            decimal dcTotalLibrasExtra)
        {
            decimal totDol = lstFresco
                .Where(x => lstLoteOrigen.Contains(x.intLote))
                .GroupBy(x => x.intLote)
                .Select(g => (decimal)g.Sum(x => x.dcTotalDol)!)
                .FirstOrDefault();

            if (totDol > 0 && dcTotalLibrasExtra > 0)
                AsignarPrecio(item, totDol / dcTotalLibrasExtra, null);
        }
        #endregion

        #region Costos Saldos
        public void AsignarCostoSaldo(
     List<DiarioCosto> lstDiarioCosto,
     List<LiquidacionResultado> lstFrsValorizado,
     List<MatPrimaReproceso> lstRpcValorizado)
        {
            var objContext = new ContextoCostos(
                DictPorLoteFrs: DiarioCosto.ConstruirDictPorLoteFrs(lstFrsValorizado),
                DictPromFrs: DiarioCosto.ConstruirDictPromedioFrs(lstFrsValorizado),
                DictPorLoteRpc: DiarioCosto.ConstruirDictPorLoteRpc(lstRpcValorizado),
                DictPromRpc: DiarioCosto.ConstruirDictPromedioRpc(lstRpcValorizado),
                DictPorLoteSld: DiarioCosto.ConstruirDictPorLoteSld(lstDiarioCosto)
            );

            int contadorFrs = 0;
            int contadorRpc = 0;
            int contadorSld = 0;

            // 2. Valorizar en paralelo
            Parallel.ForEach(lstDiarioCosto,
                () => (Frs: 0, Rpc: 0, Sld: 0), 
                (diario, _, local) =>
            {

                //if (diario.intLote == 189569 && (diario.strProCod == "CAM" || diario.strCodTip == "CSOTRO"))
                //{
                //    _objLogger.LogDebug(
                //        "[CostoValorizacionMotor] {Cod} FRS (lote) → " +
                //        "Lote: {Lote} | Prod: {Prod} | Talla: {Talla} ",
                //        diario.strProCod, diario.intLote, diario.strProCodcor, diario.stTalCodigo);
                //}
                var fuente = AsignarCostoDiario(diario, objContext);
                switch (fuente)
                {
                    case FuenteCosto.Frs:
                        _objLogger.LogDebug("[CostoValorizacionMotor] FRS → Lote: {Lote} | Prod: {Prod}", diario.intLote, diario.strProCodcor);
                        return (local.Frs + 1, local.Rpc, local.Sld); // Incrementamos FRS local

                    case FuenteCosto.Rpc:
                        _objLogger.LogDebug("[CostoValorizacionMotor] RPC → Lote: {Lote} | Prod: {Prod}", diario.intLote, diario.strProCodcor);
                        return (local.Frs, local.Rpc + 1, local.Sld); // Incrementamos RPC local

                    case FuenteCosto.Sld:
                        _objLogger.LogDebug("[CostoValorizacionMotor] RPC → Lote: {Lote} | Prod: {Prod}", diario.intLote, diario.strProCodcor);
                        return (local.Frs, local.Rpc, local.Sld + 1); // Incrementamos RPC local

                    default:
                        // Si no se valorizó y no es SALDO, advertencia
                        if (diario.strCodTip != "SALDO")
                        {
                            //_objLogger.LogWarning("[CostoValorizacionMotor] Sin costo → Lote: {Lote}", diario.intLote);
                        }
                        return local;
                }
                //bool valorizado = diario.strTipo == _strTipoMovE
                //    ? AplicarCostoExport(diario, dictPromFrs, dictPromRpc, ref contadorFrs, ref contadorRpc)
                //    : AplicarCostoMovimiento(diario, dictFrs, dictRpc, ref contadorFrs, ref contadorRpc);

                //if (!valorizado && diario.strCodTip != "SALDO")
                //    _objLogger.LogWarning(
                //        "[CostoValorizacionMotor] Sin costo → Lote: {Lote} | Prod: {Prod} | Talla: {Talla} | Tipo: {Tipo}",
                //        diario.intLote, diario.strProCodcor, diario.stTalCodigo, diario.strCodTip);
            },
                local =>                                          // merge thread-safe al final
                {
                    Interlocked.Add(ref contadorFrs, local.Frs);
                    Interlocked.Add(ref contadorRpc, local.Rpc);
                    Interlocked.Add(ref contadorSld, local.Sld);
                }

            );

            _objLogger.LogInformation(
                "[CostoValorizacionMotor] Valorización completada. FRS: {Frs} | RPC: {Rpc} | SLD: {Sld} | Sin costo: {SinCosto}",
                contadorFrs,
                contadorRpc,
                contadorSld,
                lstDiarioCosto.Count - (contadorFrs + contadorRpc + contadorSld));
        }

        private FuenteCosto AsignarCostoDiario(DiarioCosto diario, ContextoCostos ctx)
        {
            var codTip = diario.strProCod; 
            FuenteCosto objFuente;
            bool blEsEgresoEtiqueteo = _strCodTiploEti.Equals(codTip);
            switch (diario.strTipo, blEsEgresoEtiqueteo)
            {
                case (_strTipoMovE, false):
                    objFuente = AplicarCostoExport(diario, ctx);
                    break;

                case (_strTipoMovE, true):
                case (_strTipoMovI, true):
                case (_strTipoMovI, false):
                    objFuente = AplicarCostoMovimiento(diario, ctx);
                    break;

                default:
                    objFuente = FuenteCosto.Ninguna;
                    break;
            }


            //if (objFuente == FuenteCosto.Ninguna && diario.strCodTip != _strCodTipSal)
            //    _objLogger.LogWarning(
            //        "[CostoValorizacionMotor] Sin costo → " +
            //        "Tipo: {Tipo} | CodTip: {CodTip} | Lote: {Lote} | Prod: {Prod} | Talla: {Talla}",
            //        diario.strTipo, diario.strCodTip,
            //        diario.intLote, diario.strProCodcor, diario.stTalCodigo);

            return objFuente;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  RAMA 1 — ETIQUETEO  (ET1 ingreso / ET2 egreso)
        //  El egreso ET2 debe tener el mismo costo que su ingreso ET1,
        //  por eso busca por lote específico y NO usa promedio ponderado.
        //  Orden: FRS → RPC
        // ═════════════════════════════════════════════════════════════════════

        private FuenteCosto AplicarCostoEtiqueteo(
            DiarioCosto diario,
            ContextoCostos ctx,
            string cod)
        {
            decimal dcCostoRpc, dcCostoFrs;

            var key = new PromLoteXProdTal(
                diario.intLote,
                diario.strProCodcor,
                (int)diario.stTalCodigo);


            
            if (ctx.DictPorLoteFrs.TryGetValue(key, out var costoFrs))
            {
                    AplicarCosto(diario, costoFrs);
                if (diario.intLote == 187575 && (diario.strProCod == "CAM" || diario.strCodTip == "CSOTRO"))
                    _objLogger.LogInformation(
                    "[CostoValorizacionMotor] {Cod} FRS (lote) → " +
                    "Lote: {Lote} | Prod: {Prod} | Talla Desc: {TallaDesc} | Talla Cod: {Talla} | Costo: {Costo}",
                    cod, diario.intLote, diario.strProCodcor,diario.strTalDescri, diario.stTalCodigo, costoFrs);
                return FuenteCosto.Frs;
            }

            if (ctx.DictPorLoteRpc.TryGetValue(key, out var costoRpc))
            {
                AplicarCosto(diario, costoRpc);
                if (diario.intLote == 187575 && (diario.strProCod == "CAM" || diario.strCodTip == "CSOTRO"))
                    _objLogger.LogInformation(
                    "[CostoValorizacionMotor] {Cod} RPC (lote) → " +
                    "Lote: {Lote} | Prod: {Prod} | Talla Desc: {TallaDesc} | Talla Cod: {Talla} | Costo: {Costo}",
                    cod, diario.intLote, diario.strProCodcor, diario.strTalDescri, diario.stTalCodigo,  costoRpc);
                return FuenteCosto.Rpc;
            }

            return FuenteCosto.Ninguna;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  RAMA 2 — EGRESOS DE EXPORTACIÓN  (tipo E, distinto de ET2)
        //  Usa promedio ponderado del período porque el lote de exportación
        //  puede consolidar materia prima de distintos lotes de origen.
        //  Orden: FRS prom → RPC prom
        // ═════════════════════════════════════════════════════════════════════

        private FuenteCosto AplicarCostoExport(DiarioCosto diario, ContextoCostos ctx)
        {
            var key = new PromXProdTal(diario.strProCodcor, (int)diario.stTalCodigo);

            if (ctx.DictPromFrs.TryGetValue(key, out var costoProm))
            {
                AplicarCosto(diario, costoProm);
                _objLogger.LogDebug(
                    "[CostoValorizacionMotor] EXPORT FRS (prom) → " +
                    "Prod: {Prod} | Talla: {Talla} | Costo: {Costo}",
                    diario.strProCodcor, diario.stTalCodigo, costoProm);
                return FuenteCosto.Frs;
            }

            if (ctx.DictPromRpc.TryGetValue(key, out var costoPromRpc))
            {
                AplicarCosto(diario, costoPromRpc);
                _objLogger.LogDebug(
                    "[CostoValorizacionMotor] EXPORT RPC (prom) → " +
                    "Prod: {Prod} | Talla: {Talla} | Costo: {Costo}",
                    diario.strProCodcor, diario.stTalCodigo, costoPromRpc);
                return FuenteCosto.Rpc;
            }

            return FuenteCosto.Ninguna;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  RAMA 3 — INGRESOS NORMALES  (LIQFR, LIQOP, INGR…)
        //  SALDO ya llega con costo desde la BD → se omite.
        //  Solo se valorizan los tipos dentro de _procNoCont.
        //  Orden: FRS lote → RPC lote
        // ═════════════════════════════════════════════════════════════════════

        private FuenteCosto AplicarCostoMovimiento(DiarioCosto objDiario, ContextoCostos ctx)
        {
            if (_strCodTipSal.Equals(objDiario.strCodTip)/* && _strCodTiploEti.Equals(objDiario.strProCod)*/)
                return FuenteCosto.Ninguna;

            var key = new PromLoteXProdTal(
                objDiario.intLote,
                objDiario.strProCodcor,
                (int)objDiario.stTalCodigo);

            if (objDiario.intLote == 187762 && (objDiario.strProCod == "CAM" || objDiario.strCodTip == "CSOTRO") && objDiario.strProCodcor.Equals("5009"))
                _objLogger.LogInformation(
                    "[CostoValorizacionMotor] MOV ENTRA → " +
                    "Proceso :{proc} | TipoProc: {tipProc} | Lote: {Lote} | Prod: {Prod} | Talla: {Talla} ",
                    objDiario.strDescripcion, objDiario.strTipo,objDiario.intLote, objDiario.strProCodcor, objDiario.stTalCodigo);
            if (ctx.DictPorLoteFrs.TryGetValue(key, out var costoFrs))
            {
                AplicarCosto(objDiario, costoFrs);

                if (objDiario.intLote == 187762 && (objDiario.strProCod == "CAM" || objDiario.strCodTip == "CSOTRO") && objDiario.strProCodcor.Equals("5009"))
                    _objLogger.LogInformation(
                        "[CostoValorizacionMotor] MOV FRS → " +
                    "Proceso :{proc} | TipoProc: {tipProc} | Lote: {Lote} | Prod: {Prod} | Talla: {Talla} | Costo: {Costo}",
                    objDiario.strDescripcion, objDiario.strTipo, objDiario.intLote, objDiario.strProCodcor, objDiario.stTalCodigo, costoFrs);
                //_objLogger.LogInformation(
                //    "[CostoValorizacionMotor] MOV FRS → " +
                //    "Lote: {Lote} | Prod: {Prod} | Talla: {Talla} | Costo: {Costo}",
                //    objDiario.intLote, objDiario.strProCodcor, objDiario.stTalCodigo, costoFrs);
                return FuenteCosto.Frs;
            }
            else if (ctx.DictPorLoteRpc.TryGetValue(key, out var costoRpc))
            {
                AplicarCosto(objDiario, costoRpc);
                if (objDiario.intLote == 187762 && (objDiario.strProCod == "CAM" || objDiario.strCodTip == "CSOTRO") && objDiario.strProCodcor.Equals("5009"))
                    _objLogger.LogInformation(
                        "[CostoValorizacionMotor] MOV RPC → " +
                    "Proceso :{proc} | TipoProc: {tipProc} | Lote: {Lote} | Prod: {Prod} | Talla: {Talla} | Costo: {Costo} ",
                    objDiario.strDescripcion, objDiario.strTipo, objDiario.intLote, objDiario.strProCodcor, objDiario.stTalCodigo, costoRpc);
                return FuenteCosto.Rpc;
            }
            else if (ctx.DictPorLoteSld.TryGetValue(key, out var costoSld))
            {
                AplicarCosto(objDiario, costoSld);
                if (objDiario.intLote == 187762 && (objDiario.strProCod == "CAM" || objDiario.strCodTip == "CSOTRO") && objDiario.strProCodcor.Equals("5009"))
                    _objLogger.LogInformation(
                        "[CostoValorizacionMotor] MOV SLD → " +
                    "Proceso :{proc} | TipoProc: {tipProc} | Lote: {Lote} | Prod: {Prod} | Talla: {Talla} | Costo: {Costo} ",
                    objDiario.strDescripcion, objDiario.strTipo, objDiario.intLote, objDiario.strProCodcor, objDiario.stTalCodigo, costoSld);
                return FuenteCosto.Sld;
            }



                return FuenteCosto.Ninguna;
        }

        private static void AplicarCosto(DiarioCosto objDiario, decimal dcCostoXLibra)
        {
            objDiario.dcCostoUnit = Truncar(dcCostoXLibra, _intDecimalesCu);
            objDiario.dcCostoTot = Truncar(dcCostoXLibra * objDiario.dcLibras, _intDecimalesCt);
        }
        private static decimal Truncar(decimal valor, int decimales)
        {
            decimal factor = (decimal)Math.Pow(10, decimales);
            return Math.Truncate(valor * factor) / factor;
        }
        #endregion


    }

}

