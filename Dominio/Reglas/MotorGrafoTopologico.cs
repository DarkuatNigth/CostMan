using CostManagement.Infraestructura.Utils;
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
            MatPrimaReproceso.CalcularDisponiblePorLote(objDataProceso.lstLiqRepro);
            #region Medicion
            // using var objTiempoGrafo = MedicionTiempos.IniciarTramo("grafo-topologico", () => objDataProceso.lstLiqRepro.Count);
            #endregion
            // separar deteccion y ordenacion del costeo por nivel.
            // using var objTiempoDependencias = MedicionTiempos.IniciarTramo("grafo-deteccion-dependencias");
            var (lstDependencias, lstDependientes) = MatPrimaReproceso.DetectarCadenasDependencia(objDataProceso.lstLiqRepro);
            _objLogger.LogInformation($"[Topo] Deps: {lstDependencias.Count} | Fuentes: {lstDependientes.Count}");

            // objTiempoDependencias.Dispose();
            // using var objTiempoOrden = MedicionTiempos.IniciarTramo("grafo-orden-topologico");
            var lstTodos = objDataProceso.lstLiqRepro
                .Select(x => x.objLoteKey)
                .ToHashSet();

            var (lstOrden, lstCiclos) = MatPrimaReproceso.OrdenarLotesTopologicamente(lstTodos, lstDependencias, lstDependientes);
            // objTiempoOrden.Dispose();
            if (lstCiclos.Any()) _objLogger.LogWarning($"[Topo] {lstCiclos.Count} lote(s) en ciclo → NV4.");

            _objLogger.LogInformation(
                $"[DBG-{_DBG_LOTE}][Topo] EnOrden={lstOrden.Any(o => o.intLoteSecuencial == _DBG_LOTE)} " +
                $"EnCiclo={lstCiclos.Any(c => c.intLoteSecuencial == _DBG_LOTE)}");

            var objIndice = objDataProceso.lstLiqRepro.ToLookup(x => x.objLoteKey);

            // objTiempoGrafo.Dispose();
            // using var objTiempoProrrateo = MedicionTiempos.IniciarTramo("prorrateo-topologico-con-transferencias", () => objDataProceso.lstLiqRepro.Count);

            // Topo-loop
            foreach (var objLote in lstOrden)
            {
                // medir el nivel del lote sin reagrupar ni alterar lstOrden.
                int intNivel = objIndice[objLote].Where(x => x.strAgrupacion == "2. PROCESADO")
                    .Select(x => NivelCosteo.ObtenerNivel(x.strTipCod, objDataProceso.dicNivelesCosteoRuntime)).DefaultIfEmpty(2).Max();
                // using var objTiempoNivel = MedicionTiempos.IniciarTramo($"topologico-NV{intNivel}-lote-{objLote.intLoteSecuencial}");
                bool blTieneInter = lstDependencias.ContainsKey(objLote);

                if (objLote.intLoteSecuencial == _DBG_LOTE)
                    _objLogger.LogInformation(
                        $"[DBG-{_DBG_LOTE}][TopoLoop] Lote={objLote.intLoteSecuencial} Unif={objLote.intLoteUnificado} TieneInter={blTieneInter}" +
                        (blTieneInter ? $" Fuentes=[{string.Join(",", lstDependencias[objLote].Select(f => f.intLoteSecuencial))}]" : ""));

                if (blTieneInter)
                    foreach (var fuente in lstDependencias[objLote])
                        TransferirPrecioFuenteIndexada(fuente, objLote, objIndice, objDataProceso.dicNivelesCosteoRuntime);

                _objMotorProrra.Ejecutar(
                    objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir, objDataProceso.dicNivelesCosteoRuntime,
                    objLotesPermitidos: new HashSet<LoteRpcKeyXSec> { objLote },
                    objIndiceXLote: objIndice);
            }

            // objTiempoProrrateo.Dispose();

            // NV4: barrido global de rezagados
            // using var objTiempoBarrido1 = MedicionTiempos.IniciarTramo("prorrateo-NV4-pase1", () => objDataProceso.lstLiqRepro.Count);
            _objMotorProrra.Ejecutar(
                objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir, objDataProceso.dicNivelesCosteoRuntime,
                objLotesPermitidos: null, objIndiceXLote: null);
            // objTiempoBarrido1.Dispose();

            // Propagación post-NV4
            // using var objTiempoPropagacion1 = MedicionTiempos.IniciarTramo("propagacion-postNV4-pase1", () => objDataProceso.lstLiqRepro.Count);
            PropagacionPostNV4(objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                               objIndice, lstDependencias, lstDependientes, objDataProceso.dicNivelesCosteoRuntime);
            // objTiempoPropagacion1.Dispose();

            // NV4: barrido global de rezagados (segundo pase)
            // using var objTiempoBarrido2 = MedicionTiempos.IniciarTramo("prorrateo-NV4-pase2", () => objDataProceso.lstLiqRepro.Count);
            _objMotorProrra.Ejecutar(
                objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir, objDataProceso.dicNivelesCosteoRuntime,
                objLotesPermitidos: null, objIndiceXLote: null);
            // objTiempoBarrido2.Dispose();
            // Propagación post-NV4
            // using var objTiempoPropagacion2 = MedicionTiempos.IniciarTramo("propagacion-postNV4-pase2", () => objDataProceso.lstLiqRepro.Count);
            PropagacionPostNV4(objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                               objIndice, lstDependencias, lstDependientes, objDataProceso.dicNivelesCosteoRuntime);
            // objTiempoPropagacion2.Dispose();
            // NV4: barrido global de rezagados (tercer pase)
            // using var objTiempoBarrido3 = MedicionTiempos.IniciarTramo("prorrateo-NV4-pase3", () => objDataProceso.lstLiqRepro.Count);
            _objMotorProrra.Ejecutar(
                objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir, objDataProceso.dicNivelesCosteoRuntime,
                objLotesPermitidos: null, objIndiceXLote: null);
            // objTiempoBarrido3.Dispose();
            // Propagación post-NV4
            //PropagacionPostNV4(objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
            //                   objIndice, lstDependencias, lstDependientes);
            //_objMotorProrra.Ejecutar(
            //    objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
            //    objLotesPermitidos: null, objIndiceXLote: null);
            if (ConfigRespaldoRpc.RetrocosteoEspeciales)
                MatPrimaReproceso.RetrocostearEspeciales(objDataProceso.lstLiqRepro, objDataProceso.dicNivelesCosteoRuntime);
        }

        // ── Transferir precio PROCESADO(fuente) → RECIBIDO(destino) ───────────
        // GenerarCostoReciX solo consulta fuente y destino.
        // El lookup conserva el orden de las filas de cada lote y sus referencias mutables.
        // El metodo publico mantiene su semantica para otros consumidores con lstGlobal distinto.
        private void TransferirPrecioFuenteIndexada(LoteRpcKeyXSec fuente, LoteRpcKeyXSec destino,
            ILookup<LoteRpcKeyXSec, MatPrimaReproceso> indice, IReadOnlyDictionary<string, int> niveles)
        {
            var filas = fuente == destino ? indice[fuente].ToList()
                : indice[fuente].Concat(indice[destino]).ToList();
            TransferirPrecioFuente(fuente, destino, indice, niveles, filas);
        }

        public void TransferirPrecioFuente(
            LoteRpcKeyXSec objLoteFuente,
            LoteRpcKeyXSec objLoteDestino,
            ILookup<LoteRpcKeyXSec, MatPrimaReproceso> objIndice,
            IReadOnlyDictionary<string, int> mapaNiveles,
            List<MatPrimaReproceso> lstGlobal = null)
        {
            // cada transferencia conserva su estado y orden de aplicacion.
            // using var objTiempoTransferencia = MedicionTiempos.IniciarTramo(
                // $"transferencia-{objLoteFuente.intLoteSecuencial}-{objLoteDestino.intLoteSecuencial}");
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
                        decimal dol = g.Sum(x => MatPrimaReproceso.CalcularCostoTotalDolSum(x));
                        return lbs > 0 ? dol / lbs : 0m;
                    });

            var dicTotalesFuente = objIndice[objLoteFuente].Where(x => x.strAgrupacion == "2. PROCESADO"
                && x.dbCostoXSecuencial != 0 && x.intLotNumero == objLoteFuente.intLoteSecuencial && x.intLoteUnificado == objLoteFuente.intLoteUnificado)
                .GroupBy(x => x.objProdTalKey).ToDictionary(g => g.Key,
                    g => (dcTotal: g.Sum(x => MatPrimaReproceso.CalcularCostoTotalDolSum(x)), dcLibras: g.Sum(x => (decimal)x.dbLibras)));
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
            // eliminar promedios globales calculados por transferencia
            // cuyos unicos consumidores estaban comentados (sin efectos sobre las filas).

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
                string strNivel = NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles));

                if (dcFuenteCosto.TryGetValue(objKey, out decimal p1) && p1 > 0)
                    dcPrecio = p1;
                else if (dictOrigen?.TryGetValue(rec.objLotRpc, out decimal p2) == true && p2 > 0)
                    dcPrecio = p2;

                if (blDbgDest && rec.intCodProd == _DBG_PROD && rec.intCodTal == _DBG_TAL)
                    _objLogger.LogInformation(
                        $"[DBG-{_DBG_LOTE}][Transferir]   RECIBIDO Prod={rec.intCodProd} Tal={rec.intCodTal}: " +
                        $"dcFuente={dcFuenteCosto.ContainsKey(objKey)}({(dcFuenteCosto.TryGetValue(objKey, out var _px) ? _px : 0):F4}) " +
                        $"dictOrigen={(dictOrigen?.ContainsKey(rec.objLotRpc) == true)} → PrecioFinal={dcPrecio:F4}");

                if (dcPrecio > 0)
                {
                    if (dcFuenteCosto.TryGetValue(objKey, out decimal dcPrecioFuente) && dcPrecioFuente > 0
                        && dicTotalesFuente.TryGetValue(objKey, out var objFuente) && objFuente.dcLibras > 0m)
                        _objMotorAsigPrec.AsignarPrecioPorTotal(rec, objFuente.dcTotal, objFuente.dcLibras, strNivel);
                    else _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, strNivel);
                    if (!rec.blExcluidoCosteo)
                    {
                        rec.strOrigenCosto = "RPC";
                        // N1Excluyente depende del orden topológico: excluir el lote unificado desde su primer traslado.
                        foreach (var proc in objIndice.Where(g => g.Key.intLoteUnificado == objLoteFuente.intLoteUnificado)
                            .SelectMany(g => g).Where(x => x.strAgrupacion == "2. PROCESADO"))
                            proc.blUsadoComoFuente = true;
                    }
                }
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
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependientes,
            IReadOnlyDictionary<string, int> mapaNiveles)
        {
            var objIndiceUnif = lstRpc
                .Select(x => x.objLoteKey)
                .Distinct()
                .GroupBy(k => k.intLoteUnificado)
                .ToDictionary(g => g.Key, g => g.ToList());

            // claves estables y referencias vivas; costo filtrado en cada lectura.
            var objRecibidosPorItem = lstRpc.Where(x => x.strAgrupacion == "1. RECIBIDO")
                .ToLookup(x => (x.intLotNumero, x.intCodProd, x.intCodTal));

            // Clave granular por item (intLotNumero + intLoteUnificado + intCodProd + intCodTal).
            var lstImposibles = new HashSet<LoteRpcValKey>();

            for (int intPasada = 1; intPasada <= _intMaxPasadas; intPasada++)
            {
                // incluye cada iteracion interna, tambien las que salen por break.
                // using var objTiempoPasada = MedicionTiempos.IniciarTramo($"propagacion-iteracion-{intPasada}");
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
                        var lstCosteadosMismo = objRecibidosPorItem[(rec.intLotNumero, rec.intCodProd, rec.intCodTal)]
                            .Where(x => x.dbCostoXSecuencial > 0
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
                                string strNivelAR = NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles));
                                _objMotorAsigPrec.AsignarPrecioPorTotal(rec, dcDol, dcLbs, strNivelAR);
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
                            TransferirPrecioFuenteIndexada(objFuente, objKey, objIndice, mapaNiveles);
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

                    // Una muestra por nivel destino antes de asignar; se renueva en cada pasada.
                    var dicRefPorNivel = lstRpc.Where(x => x.strAgrupacion == "1. RECIBIDO" && x.dbCostoXSecuencial == 0
                        && !NivelCosteo.AplicaMetodoEspecial(x.strTipCod, mapaNiveles))
                        .Select(rec => NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)).Distinct()
                        .ToDictionary(intNivelDestino => intNivelDestino, intNivelDestino =>
                            MatPrimaReproceso.GenerarPromedioRespaldo(lstRpc, mapaNiveles, intNivelDestino, ConfigRespaldoRpc.Modo));

                    var recibidosOrdenados = lstRpc.Where(x => x.strAgrupacion == "1. RECIBIDO"
                        && x.dbCostoXSecuencial == 0 && !x.blExcluidoCosteo
                        && !NivelCosteo.AplicaMetodoEspecial(x.strTipCod, mapaNiveles)).ToList();
                    if (ConfigRespaldoRpc.HistoricoPonderado)
                        recibidosOrdenados = recibidosOrdenados.OrderBy(x => NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles)).ToList();
                    var indiceDisponible = recibidosOrdenados.Count > 0 ? new MatPrimaReproceso.IndiceDisponibleHistorico(lstRpc) : null;
                    indiceDisponible?.BloquearProcesadosAgotados();
                    // Pasada 1: todo DISP antes de que HIST absorba fuentes de niveles superiores.
                    // Se reutiliza el índice: filas vivas y disponible actualizado por Confirmar.
                    if (ConfigRespaldoRpc.HistoricoPonderado)
                        foreach (var rec in recibidosOrdenados)
                        {
                            decimal dcPrecioDisponible = MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstRpc, indiceDisponible);
                            if (dcPrecioDisponible <= 0) continue;
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecioDisponible, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                            indiceDisponible.Confirmar(rec);
                        }
                    // Pasada 2: HIST para los pendientes y luego RESP; sin bandera, DISP → RESP como antes.
                    foreach (var rec in recibidosOrdenados)
                    {
                        if (ConfigRespaldoRpc.HistoricoPonderado && rec.dbCostoXSecuencial != 0) continue;
                        bool blDbgFill = rec.intLotNumero == _DBG_LOTE && rec.intCodProd == _DBG_PROD && rec.intCodTal == _DBG_TAL;
                        // Prioridad: disponible del lote, histórico opcional y respaldo vigente.
                        // decimal dcPrecio = MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstRpc);
                        // decimal dcPrecio = MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstRpc, indiceDisponible);
                        decimal dcPrecio = ConfigRespaldoRpc.HistoricoPonderado ? 0m
                            : MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstRpc, indiceDisponible);
                        if (dcPrecio > 0)
                        {
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                            indiceDisponible.Confirmar(rec);
                            continue;
                        }
                        if (ConfigRespaldoRpc.HistoricoPonderado)
                        {
                            dcPrecio = MatPrimaReproceso.ObtenerCostoHistorico(rec, lstRpc, mapaNiveles, indiceDisponible);
                            if (dcPrecio > 0)
                            {
                                _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                                indiceDisponible.Confirmar(rec);
                                continue;
                            }
                        }
                        var key = rec.objProdTalKey;
                        var dictRef = dicRefPorNivel[NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)];
                        if (dictRef.TryGetValue(key, out dcPrecio) && dcPrecio > 0)
                        {
                            if (blDbgFill)
                                _objLogger.LogInformation(
                                    $"[DBG-{_DBG_LOTE}][PropPost][Fill] P{intPasada}: " +
                                    $"Asignando precio global Prod={rec.intCodProd} Tal={rec.intCodTal} Precio={dcPrecio:F4}");
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                            if (!rec.blExcluidoCosteo) rec.strOrigenCosto = "RESP";
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
                        foreach (var f in fuentesInt) TransferirPrecioFuente(f, objLote, objIndice, mapaNiveles);

                    _objMotorProrra.Ejecutar(
                        lstRpc, lstFresco, lstUni, lstDir, mapaNiveles,
                        objLotesPermitidos: new HashSet<LoteRpcKeyXSec> { objLote },
                        objIndiceXLote: objIndice);
                }
            }
        }
    }
}
