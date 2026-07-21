using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Dominio.Reglas
{
    public class MotorGrafoTopologico
    {
        private readonly MotorProrrateo _objMotorProrra;
        private readonly MotorAsignacionPrecios _objMotorAsigPrec;
        private readonly ILogger _objLogger;
        private const int _intMaxPasadas = 5;
        private static readonly List<string> _lstNotTipCod = new() { "UNI", "CDI", "R7" };
        private const int _DBG_LOTE   = 798991;
        private const int _DBG_PROD   = 6205;
        private const int _DBG_TAL    = 7;
        private const int _DBG_ORIGEN = 189601;

        public MotorGrafoTopologico(MotorProrrateo motorProrrateo, MotorAsignacionPrecios motorAsigPrec, ILogger logger)
        {
            _objMotorProrra = motorProrrateo;
            _objMotorAsigPrec = motorAsigPrec;
            _objLogger = logger;
        }

        // ── Punto de entrada principal ─────────────────────────────────────────
        public void CostearTodosLotesEnOrden(
            DataProcesoParam objDataProceso,
            List<PrecioFrsXMov> lstPrecioFrsUni,
            List<PrecioFrsXMov> lstPrecioFrsDir)
        {
            var (lstDependencias, lstDependientes) = MatPrimaReproceso.DetectarCadenasDependencia(objDataProceso.lstLiqRepro);
            _objLogger.LogInformation($"[Topo] Deps: {lstDependencias.Count} | Fuentes: {lstDependientes.Count}");

            var lstTodos = objDataProceso.lstLiqRepro
                .Select(x => x.objLoteKey)
                .ToHashSet();

            var (lstOrden, lstCiclos) = MatPrimaReproceso.OrdenarLotesTopologicamente(lstTodos, lstDependencias, lstDependientes);
            if (lstCiclos.Any()) _objLogger.LogWarning($"[Topo] {lstCiclos.Count} lote(s) en ciclo → NV4.");

            _objLogger.LogInformation(
                $"[DBG-{_DBG_LOTE}][Topo] EnOrden={lstOrden.Any(o => o.intLoteSecuencial == _DBG_LOTE)} " +
                $"EnCiclo={lstCiclos.Any(c => c.intLoteSecuencial == _DBG_LOTE)}");

            var objIndice = objDataProceso.lstLiqRepro.ToLookup(x => x.objLoteKey);

            // Topo-loop
            foreach (var objLote in lstOrden)
            {
                bool blTieneInter = lstDependencias.ContainsKey(objLote);

                if (objLote.intLoteSecuencial == _DBG_LOTE)
                    _objLogger.LogInformation(
                        $"[DBG-{_DBG_LOTE}][TopoLoop] Lote={objLote.intLoteSecuencial} Unif={objLote.intLoteUnificado} TieneInter={blTieneInter}" +
                        (blTieneInter ? $" Fuentes=[{string.Join(",", lstDependencias[objLote].Select(f => f.intLoteSecuencial))}]" : ""));

                if (blTieneInter)
                    foreach (var fuente in lstDependencias[objLote])
                        TransferirPrecioFuente(fuente, objLote, objIndice, objDataProceso.lstLiqRepro);

                _objMotorProrra.Ejecutar(
                    objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                    objLotesPermitidos: new HashSet<LoteRpcKeyXSec> { objLote },
                    objIndiceXLote: objIndice);
            }

            // NV4: barrido global de rezagados
            _objMotorProrra.Ejecutar(
                objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                objLotesPermitidos: null, objIndiceXLote: null);

            // Propagación post-NV4
            PropagacionPostNV4(objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                               objIndice, lstDependencias, lstDependientes);

            // NV4: barrido global de rezagados (segundo pase)
            _objMotorProrra.Ejecutar(
                objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                objLotesPermitidos: null, objIndiceXLote: null);
            // Propagación post-NV4
            PropagacionPostNV4(objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                               objIndice, lstDependencias, lstDependientes);
            // NV4: barrido global de rezagados (tercer pase)
            _objMotorProrra.Ejecutar(
                objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                objLotesPermitidos: null, objIndiceXLote: null);
            // Propagación post-NV4
            //PropagacionPostNV4(objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
            //                   objIndice, lstDependencias, lstDependientes);
            //_objMotorProrra.Ejecutar(
            //    objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
            //    objLotesPermitidos: null, objIndiceXLote: null);
        }

        // ── Transferir precio PROCESADO(fuente) → RECIBIDO(destino) ───────────
        public void TransferirPrecioFuente(
            LoteRpcKeyXSec objLoteFuente,
            LoteRpcKeyXSec objLoteDestino,
            ILookup<LoteRpcKeyXSec, MatPrimaReproceso> objIndice,
            List<MatPrimaReproceso> lstGlobal = null)
        {
            var dcFuenteCosto = objIndice[objLoteFuente]
                .Where(x => x.strAgrupacion == "2. PROCESADO"
                         && x.dbCostoXSecuencial != 0
                         && x.intLotNumero == objLoteFuente.intLoteSecuencial
                         && x.intLoteUnificado == objLoteFuente.intLoteUnificado)
                .GroupBy(x => x.objProdTalKey)
                .ToDictionary(
                    g => g.Key,
                    g => {
                        decimal lbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal dol = g.Sum(x => x.dbCostoTotal);
                        return lbs > 0 ? dol / lbs : 0m;
                    });

            bool blDbgDest = objLoteDestino.intLoteSecuencial == _DBG_LOTE;
            bool blDbgFte  = objLoteFuente.intLoteSecuencial  == _DBG_LOTE;

            if (blDbgDest || blDbgFte)
                _objLogger.LogInformation(
                    $"[DBG-{_DBG_LOTE}][Transferir] Fuente={objLoteFuente.intLoteSecuencial}(Unif={objLoteFuente.intLoteUnificado}) " +
                    $"→ Dest={objLoteDestino.intLoteSecuencial}(Unif={objLoteDestino.intLoteUnificado}) | " +
                    $"dcFuenteCosto: {dcFuenteCosto.Count} entrada(s) [{string.Join(",", dcFuenteCosto.Select(kv => $"({kv.Key.ProdCod},{kv.Key.Codtal})={kv.Value:F4}"))}]");

            if (!dcFuenteCosto.Any())
            {
                _objLogger.LogWarning($"[TransferirPrecio] Fuente {objLoteFuente.intLoteSecuencial} sin PROCESADO costeado.");
                if (blDbgDest)
                    _objLogger.LogWarning(
                        $"[DBG-{_DBG_LOTE}][Transferir] Fuente {objLoteFuente.intLoteSecuencial} sin PROCESADO costeado → return, " +
                        $"RECIBIDO de {_DBG_LOTE} no recibirá precio desde esta fuente.");
                return;
            }

            Dictionary<LoteRpcValKey, decimal> dictOrigen = lstGlobal != null
                ? MatPrimaReproceso.GenerarCostoReciX(lstGlobal, objLoteFuente, objLoteDestino)
                : null;
            Dictionary<LoteRpcKeyReci, decimal> dictProm = lstGlobal != null
                ? MatPrimaReproceso.GenerarPromedioPonderadoXLote(lstGlobal)
                : null;
            Dictionary<LoteRpcKeyXProdTal, decimal> dictGlobal = lstGlobal != null
                ? MatPrimaReproceso.GenerarPromedioPonderadoGlobal(lstGlobal)
                : null;

            if (blDbgDest)
            {
                _objLogger.LogInformation(
                    $"[DBG-{_DBG_LOTE}][Transferir] dictOrigen: {dictOrigen?.Count ?? 0} entrada(s)");

                // Auditar RECIBIDO del destino para el ítem buscado (Prod=_DBG_PROD, Tal=_DBG_TAL)
                var todosRec = objIndice[objLoteDestino]
                    .Where(x => x.strAgrupacion == "1. RECIBIDO" && x.intCodProd == _DBG_PROD && x.intCodTal == _DBG_TAL)
                    .ToList();

                if (!todosRec.Any())
                    _objLogger.LogWarning(
                        $"[DBG-{_DBG_LOTE}][Transferir] No existe RECIBIDO con Prod={_DBG_PROD} Tal={_DBG_TAL} en destino {_DBG_LOTE}.");

                foreach (var r in todosRec)
                {
                    bool bCosto = r.dbCostoXSecuencial == 0;
                    bool bOrig  = r.intLoteOrigen == objLoteFuente.intLoteUnificado;
                    bool bTip   = !_lstNotTipCod.Contains(r.strTipCod);
                    _objLogger.LogInformation(
                        $"[DBG-{_DBG_LOTE}][Transferir]   ITEM Prod={r.intCodProd} Tal={r.intCodTal} " +
                        $"Origen={r.intLoteOrigen}(esp={objLoteFuente.intLoteUnificado}) TipCod={r.strTipCod} Costo={r.dbCostoXSecuencial:F4} | " +
                        $"FiltCosto={bCosto} FiltOrig={bOrig} FiltTip={bTip} → {(bCosto && bOrig && bTip ? "PASA FILTRO" : "NO PASA")}");
                }
            }

            foreach (var rec in objIndice[objLoteDestino].Where(x =>
                x.strAgrupacion == "1. RECIBIDO" &&
                x.dbCostoXSecuencial == 0 &&
                x.intLoteOrigen == objLoteFuente.intLoteUnificado &&
                !_lstNotTipCod.Contains(x.strTipCod)))
            {
                LoteRpcKeyXProdTal objKey = rec.objProdTalKey;
                decimal dcPrecio = 0m;
                string strNivel = NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod));

                if (dcFuenteCosto.TryGetValue(objKey, out decimal p1) && p1 > 0)
                    dcPrecio = p1;
                else if (dictOrigen?.TryGetValue(rec.objLotRpc, out decimal p2) == true && p2 > 0)
                    dcPrecio = p2;
                //else if (dictProm?.TryGetValue(rec.objRpcValOrKey, out decimal p3) == true && p3 > 0)
                //    dcPrecio = p3;
                //else if (dictGlobal?.TryGetValue(rec.objProdTalKey, out decimal p4) == true && p4 > 0)
                //    dcPrecio = p4;

                if (blDbgDest && rec.intCodProd == _DBG_PROD && rec.intCodTal == _DBG_TAL)
                    _objLogger.LogInformation(
                        $"[DBG-{_DBG_LOTE}][Transferir]   RECIBIDO Prod={rec.intCodProd} Tal={rec.intCodTal}: " +
                        $"dcFuente={dcFuenteCosto.ContainsKey(objKey)}({(dcFuenteCosto.TryGetValue(objKey, out var _px) ? _px : 0):F4}) " +
                        $"dictOrigen={(dictOrigen?.ContainsKey(rec.objLotRpc) == true)} → PrecioFinal={dcPrecio:F4}");

                if (dcPrecio > 0)
                    _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, strNivel);
            }
        }

        // ── Propagación post-NV4: itera hasta convergencia ────────────────────
        private void PropagacionPostNV4(
            List<MatPrimaReproceso> lstRpc,
            List<LiquidacionResultado> lstFresco,
            List<PrecioFrsXMov> lstUni,
            List<PrecioFrsXMov> lstDir,
            ILookup<LoteRpcKeyXSec, MatPrimaReproceso> objIndice,
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependencias,
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependientes)
        {
            var objIndiceUnif = lstRpc
                .Select(x => x.objLoteKey)
                .Distinct()
                .GroupBy(k => k.intLoteUnificado)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Clave granular por ítem (intLotNumero + intLoteUnificado + intCodProd + intCodTal)
            var lstImposibles = new HashSet<LoteRpcValKey>();

            for (int intPasada = 1; intPasada <= _intMaxPasadas; intPasada++)
            {
                var conProcCosteado = lstRpc
                    .Where(x => x.strAgrupacion == "2. PROCESADO" && x.dbCostoXSecuencial > 0)
                    .Select(x => x.objLoteKey)
                    .ToHashSet();

                var objDesbloqueados = new HashSet<LoteRpcKeyXSec>();

                // ── Traza externa: estado del RECIBIDO buscado antes del loop ─────────────
                {
                    var dbgItems = lstRpc.Where(x =>
                        x.intLotNumero == _DBG_LOTE &&
                        x.strAgrupacion == "1. RECIBIDO" &&
                        x.intCodProd == _DBG_PROD &&
                        x.intCodTal == _DBG_TAL).ToList();

                    if (dbgItems.Any())
                    {
                        foreach (var dbgR in dbgItems)
                        {
                            bool bCosto   = dbgR.dbCostoXSecuencial == 0;
                            bool bTip     = !_lstNotTipCod.Contains(dbgR.strTipCod);
                            bool bEnIndic = objIndiceUnif.ContainsKey(dbgR.intLoteOrigen);
                            bool bAutoRef = dbgR.intLoteOrigen == dbgR.intLoteUnificado;
                            _objLogger.LogInformation(
                                $"[DBG-{_DBG_LOTE}][PropPost] P{intPasada}: RECIBIDO Prod={dbgR.intCodProd} Tal={dbgR.intCodTal} " +
                                $"Origen={dbgR.intLoteOrigen} Costo={dbgR.dbCostoXSecuencial:F4} TipCod={dbgR.strTipCod} | " +
                                $"FiltCosto={bCosto} FiltTip={bTip} FiltEnIndice={bEnIndic} AutoRef={bAutoRef} " +
                                $"Imposible={lstImposibles.Contains(dbgR.objValKey)}");

                            if (bEnIndic && !bAutoRef)
                            {
                                var fuentesOrig = objIndiceUnif[dbgR.intLoteOrigen];
                                _objLogger.LogInformation(
                                    $"[DBG-{_DBG_LOTE}][PropPost] P{intPasada}: objIndiceUnif[{dbgR.intLoteOrigen}] → {fuentesOrig.Count} lote(s)");
                                foreach (var f in fuentesOrig)
                                    _objLogger.LogInformation(
                                        $"[DBG-{_DBG_LOTE}][PropPost]   Fuente={f.intLoteSecuencial} Unif={f.intLoteUnificado} " +
                                        $"AutoRef={(f.intLoteSecuencial == dbgR.intLotNumero && f.intLoteUnificado == dbgR.intLoteUnificado)} " +
                                        $"ProcCosteado={conProcCosteado.Contains(f)}");
                            }
                        }
                    }
                    else
                        _objLogger.LogWarning(
                            $"[DBG-{_DBG_LOTE}][PropPost] P{intPasada}: RECIBIDO Prod={_DBG_PROD} Tal={_DBG_TAL} NO encontrado en lstRpc");
                }

                // ── Auditar PROCESADO del lote buscado ────────────────────────────────────
                {
                    var procLote = lstRpc.Where(x =>
                        x.intLotNumero == _DBG_LOTE && x.strAgrupacion == "2. PROCESADO").ToList();
                    if (procLote.Any())
                        _objLogger.LogInformation(
                            $"[DBG-{_DBG_LOTE}][PropPost] P{intPasada}: PROCESADO del lote → " +
                            string.Join(" | ", procLote.Select(p => $"Prod={p.intCodProd} Tal={p.intCodTal} Costo={p.dbCostoXSecuencial:F4}")));
                }

                foreach (var rec in lstRpc.Where(x =>
                    x.strAgrupacion == "1. RECIBIDO" &&
                    x.dbCostoXSecuencial == 0 &&
                    !_lstNotTipCod.Contains(x.strTipCod) &&
                    objIndiceUnif.ContainsKey(x.intLoteOrigen)).ToList())
                {
                    var objKey    = rec.objLoteKey;
                    var objValKey = rec.objValKey;
                    bool blDbgRec = rec.intLotNumero == _DBG_LOTE && rec.intCodProd == _DBG_PROD && rec.intCodTal == _DBG_TAL;

                    if (blDbgRec)
                    {
                        _objLogger.LogInformation($"[DBG-{_DBG_LOTE}][PropPost] P{intPasada}: RECIBIDO ENTRÓ AL LOOP FILTRADO");
                        if (lstImposibles.Contains(objValKey))
                            _objLogger.LogWarning($"[DBG-{_DBG_LOTE}][PropPost] P{intPasada}: En lstImposibles (granular) → skip");
                    }

                    if (lstImposibles.Contains(objValKey)) continue;

                    // ── Caso especial: autorreferencia (Origen == propio intLoteUnificado) ──
                    if (rec.intLoteOrigen == rec.intLoteUnificado)
                    {
                        // Buscar otros RECIBIDO del mismo lote con mismo prod/talla que ya tengan costo
                        var lstCosteadosMismo = lstRpc
                            .Where(x => x.intLotNumero == rec.intLotNumero
                                     && x.strAgrupacion == "1. RECIBIDO"
                                     && x.intCodProd == rec.intCodProd
                                     && x.intCodTal == rec.intCodTal
                                     && x.dbCostoXSecuencial > 0
                                     && x.dbLibras > 0)
                            .ToList();

                        if (blDbgRec)
                            _objLogger.LogInformation(
                                $"[DBG-{_DBG_LOTE}][PropPost][AutoRef] P{intPasada}: " +
                                $"Prod={rec.intCodProd} Tal={rec.intCodTal} Origen={rec.intLoteOrigen}=Unif → " +
                                $"Costeados mismo lote/prod/tal: {lstCosteadosMismo.Count}");

                        if (lstCosteadosMismo.Any())
                        {
                            decimal dcLbs = (decimal)lstCosteadosMismo.Sum(x => x.dbLibras);
                            decimal dcDol = lstCosteadosMismo.Sum(x => x.dbCostoTotal);
                            decimal dcPrecio = dcLbs > 0 ? Math.Round(dcDol / dcLbs, 4) : 0m;

                            if (blDbgRec)
                                _objLogger.LogInformation(
                                    $"[DBG-{_DBG_LOTE}][PropPost][AutoRef] P{intPasada}: " +
                                    $"PrecioPromedio={dcPrecio:F4} (Lbs={dcLbs:F2} Dol={dcDol:F4})");

                            if (dcPrecio > 0)
                            {
                                string strNivelAR = NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod));
                                _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, strNivelAR);
                                objDesbloqueados.Add(objKey);
                            }
                        }
                        else if (blDbgRec)
                            _objLogger.LogWarning(
                                $"[DBG-{_DBG_LOTE}][PropPost][AutoRef] P{intPasada}: " +
                                $"Sin costeados disponibles — no se marca imposible, reintenta próxima pasada");

                        // No se marca imposible: en pasadas siguientes puede haber más costeados
                        continue;
                    }

                    // ── Flujo normal: buscar precio desde fuentes externas ─────────────────
                    bool blEsLogro = false;
                    foreach (var objFuente in objIndiceUnif[rec.intLoteOrigen])
                    {
                        if (objFuente.intLoteSecuencial == objKey.intLoteSecuencial &&
                            objFuente.intLoteUnificado == objKey.intLoteUnificado) continue;

                        if (blDbgRec)
                            _objLogger.LogInformation(
                                $"[DBG-{_DBG_LOTE}][PropPost]   Intentando Fuente={objFuente.intLoteSecuencial} " +
                                $"ProcCosteado={conProcCosteado.Contains(objFuente)}");

                        if (conProcCosteado.Contains(objFuente))
                        {
                            TransferirPrecioFuente(objFuente, objKey, objIndice, lstRpc);
                            if (blDbgRec)
                                _objLogger.LogInformation(
                                    $"[DBG-{_DBG_LOTE}][PropPost]   PostTransferir Fuente={objFuente.intLoteSecuencial} " +
                                    $"→ Costo={rec.dbCostoXSecuencial:F4} Logro={rec.dbCostoXSecuencial > 0}");
                            if (rec.dbCostoXSecuencial > 0) { objDesbloqueados.Add(objKey); blEsLogro = true; break; }
                        }
                    }

                    if (!blEsLogro && objIndiceUnif[rec.intLoteOrigen].Any(f => conProcCosteado.Contains(f)))
                    {
                        if (blDbgRec)
                            _objLogger.LogWarning(
                                $"[DBG-{_DBG_LOTE}][PropPost] P{intPasada}: Marcado IMPOSIBLE (granular) — " +
                                $"fuentes costeadas existen pero TransferirPrecio no asignó precio a Prod={rec.intCodProd} Tal={rec.intCodTal}");
                        lstImposibles.Add(objValKey);
                    }
                }

                // ── Fill residual: promedio global para ítems aún sin precio ──────────────
                {
                    var dictRef = lstRpc
                        .Where(x => x.strAgrupacion == "1. RECIBIDO" && x.dbCostoXSecuencial > 0 && x.dbLibras > 0)
                        .GroupBy(x => x.objProdTalKey)
                        .ToDictionary(
                            g => g.Key,
                            g => {
                                decimal l = (decimal)g.Sum(x => x.dbLibras);
                                decimal d = g.Sum(x => x.dbCostoTotal);
                                return l > 0 ? Math.Round(d / l, 4) : 0m;
                            });

                    foreach (var rec in lstRpc.Where(x =>
                        x.strAgrupacion == "1. RECIBIDO" &&
                        x.dbCostoXSecuencial == 0 &&
                        !NivelCosteo.AplicaMetodoEspecial(x.strTipCod)))
                    {
                        bool blDbgFill = rec.intLotNumero == _DBG_LOTE && rec.intCodProd == _DBG_PROD && rec.intCodTal == _DBG_TAL;
                        var key = rec.objProdTalKey;
                        if (dictRef.TryGetValue(key, out decimal dcPrecio) && dcPrecio > 0)
                        {
                            if (blDbgFill)
                                _objLogger.LogInformation(
                                    $"[DBG-{_DBG_LOTE}][PropPost][Fill] P{intPasada}: " +
                                    $"Asignando precio global Prod={rec.intCodProd} Tal={rec.intCodTal} Precio={dcPrecio:F4}");
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod)));
                        }
                        else if (blDbgFill)
                            _objLogger.LogWarning(
                                $"[DBG-{_DBG_LOTE}][PropPost][Fill] P{intPasada}: " +
                                $"Sin precio global para Prod={rec.intCodProd} Tal={rec.intCodTal}");
                    }
                }

                if (!objDesbloqueados.Any()) break;

                var depsFilt = dependencias.Where(kv => objDesbloqueados.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => new HashSet<LoteRpcKeyXSec>(kv.Value.Where(f => objDesbloqueados.Contains(f))));
                var deptesFilt = dependientes.Where(kv => objDesbloqueados.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => new HashSet<LoteRpcKeyXSec>(kv.Value.Where(d => objDesbloqueados.Contains(d))));

                var (ordenPost, enCiclo) = MatPrimaReproceso.OrdenarLotesTopologicamente(objDesbloqueados, depsFilt, deptesFilt);
                if (enCiclo.Any()) _objLogger.LogWarning($"[PropPost] {enCiclo.Count} en ciclo.");

                foreach (var objLote in ordenPost)
                {
                    if (depsFilt.TryGetValue(objLote, out var fuentesInt))
                        foreach (var f in fuentesInt) TransferirPrecioFuente(f, objLote, objIndice);

                    _objMotorProrra.Ejecutar(
                        lstRpc, lstFresco, lstUni, lstDir,
                        objLotesPermitidos: new HashSet<LoteRpcKeyXSec> { objLote },
                        objIndiceXLote: objIndice);
                }
            }
        }
    }
}
