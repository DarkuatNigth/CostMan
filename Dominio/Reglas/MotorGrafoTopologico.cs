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
                .Select(x => new LoteRpcKeyXSec(x.intLotNumero, x.intLoteUnificado))
                .ToHashSet();

            var (lstOrden, lstCiclos) = MatPrimaReproceso.OrdenarLotesTopologicamente(lstTodos, lstDependencias, lstDependientes);
            if (lstCiclos.Any()) _objLogger.LogWarning($"[Topo] {lstCiclos.Count} lote(s) en ciclo → NV4.");

            var objIndice = objDataProceso.lstLiqRepro.ToLookup(x => new LoteRpcKeyXSec(x.intLotNumero, x.intLoteUnificado));

            // Topo-loop
            foreach (var objLote in lstOrden)
            {
                bool blTieneInter = lstDependencias.ContainsKey(objLote);

                if (blTieneInter)
                    foreach (var fuente in lstDependencias[objLote])
                        TransferirPrecioFuente(fuente, objLote, objIndice, objDataProceso.lstLiqRepro);

                _objMotorProrra.Ejecutar(
                    objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                    strVersionCtx: blTieneInter ? "CHAIN" : "ROOT",
                    objLotesPermitidos: new HashSet<LoteRpcKeyXSec> { objLote },
                    objIndiceXLote: objIndice);
            }

            // NV4: barrido global de rezagados
            _objMotorProrra.Ejecutar(
                objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                strVersionCtx: "NV4", objLotesPermitidos: null, objIndiceXLote: null);

            // Propagación post-NV4
            PropagacionPostNV4(objDataProceso.lstLiqRepro, objDataProceso.lstLiqFresco, lstPrecioFrsUni, lstPrecioFrsDir,
                               objIndice, lstDependencias, lstDependientes);
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
                .GroupBy(x => (x.intProdCod, x.intCodTal))
                .ToDictionary(
                    g => g.Key,
                    g => {
                        decimal lbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal dol = g.Sum(x => x.dbCostoTotal);
                        return lbs > 0 ? dol / lbs : 0m;
                    });

            if (!dcFuenteCosto.Any())
            {
                _objLogger.LogWarning($"[TransferirPrecio] Fuente {objLoteFuente.intLoteSecuencial} sin PROCESADO costeado.");
                return;
            }

            // Fallback global: promedio ponderado de RECIBIDO ya costeados
            Dictionary<LoteRpcKeyXProdTal, decimal> dictGlobal = lstGlobal != null
                ? MatPrimaReproceso.GenerarPromedioPonderadoGlobal(lstGlobal)
                : null;

            foreach (var rec in objIndice[objLoteDestino].Where(x =>
                x.strAgrupacion == "1. RECIBIDO" &&
                x.dbCostoXSecuencial == 0 &&
                x.intLoteOrigen == objLoteFuente.intLoteUnificado &&
                !_lstNotTipCod.Contains(x.strTipCod)))
            {
                var objKey = (rec.intProdCod, rec.intCodTal);
                decimal dcPrecio = 0m;
                string strNivel = NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod));

                if (dcFuenteCosto.TryGetValue(objKey, out decimal p1) && p1 > 0)
                {
                    dcPrecio = p1;
                    //strNivel = "NV" + NivelCosteo.ObtenerNivel(rec.strTipCod) + "-CHAIN";
                }
                else if (dictGlobal?.TryGetValue(new LoteRpcKeyXProdTal(rec.intProdCod, rec.intCodTal), out decimal p2) == true && p2 > 0)
                {
                    dcPrecio = p2;
                    //strNivel = "NV" + NivelCosteo.ObtenerNivel(rec.strTipCod) + "-REF";
                }

                if (dcPrecio > 0)
                {
                    _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, strNivel);
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
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependientes)
        {
            var objIndiceUnif = lstRpc
                .Select(x => new LoteRpcKeyXSec(x.intLotNumero, x.intLoteUnificado))
                .Distinct()
                .GroupBy(k => k.intLoteUnificado)
                .ToDictionary(g => g.Key, g => g.ToList());

            var lstImposibles = new HashSet<LoteRpcKeyXSec>();

            for (int intPasada = 1; intPasada <= _intMaxPasadas; intPasada++)
            {
                var conProcCosteado = lstRpc
                    .Where(x => x.strAgrupacion == "2. PROCESADO" && x.dbCostoXSecuencial > 0)
                    .Select(x => new LoteRpcKeyXSec(x.intLotNumero, x.intLoteUnificado))
                    .ToHashSet();

                var objDesbloqueados = new HashSet<LoteRpcKeyXSec>();

                foreach (var rec in lstRpc.Where(x =>
                    x.strAgrupacion == "1. RECIBIDO" &&
                    x.dbCostoXSecuencial == 0 &&
                    !_lstNotTipCod.Contains(x.strTipCod) &&
                    objIndiceUnif.ContainsKey(x.intLoteOrigen)).ToList())
                {
                    var objKey = new LoteRpcKeyXSec(rec.intLotNumero, rec.intLoteUnificado);
                    if (lstImposibles.Contains(objKey)) continue;

                    bool blEsLogro = false;
                    foreach (var objFuente in objIndiceUnif[rec.intLoteOrigen])
                    {
                        if (objFuente.intLoteSecuencial == objKey.intLoteSecuencial &&
                            objFuente.intLoteUnificado == objKey.intLoteUnificado) continue;

                        if (conProcCosteado.Contains(objFuente))
                        {
                            TransferirPrecioFuente(objFuente, objKey, objIndice, lstRpc);
                            if (rec.dbCostoXSecuencial > 0) { objDesbloqueados.Add(objKey); blEsLogro = true; break; }
                        }
                    }

                    if (!blEsLogro && objIndiceUnif[rec.intLoteOrigen].Any(f => conProcCosteado.Contains(f)))
                    {
                        lstImposibles.Add(objKey);
                        _objLogger.LogWarning($"[PropPost] Lote {objKey.intLoteSecuencial} imposible para Prod {rec.intProdCod}.");
                    }
                }

                // Fill residual para imposibles
                if (lstImposibles.Any())
                {
                    var dictRef = lstRpc
                        .Where(x => x.strAgrupacion == "1. RECIBIDO" && x.dbCostoXSecuencial > 0 && x.dbLibras > 0)
                        .GroupBy(x => new LoteRpcKeyXProdTal(x.intProdCod, x.intCodTal))
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
                        var key = new LoteRpcKeyXProdTal(rec.intProdCod, rec.intCodTal);
                        if (dictRef.TryGetValue(key, out decimal dcPrecio) && dcPrecio > 0)
                        {
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod)));
                        }
                    }
                }

                if (!objDesbloqueados.Any()) { _objLogger.LogInformation($"[PropPost] Convergencia en pasada {intPasada}."); break; }

                _objLogger.LogInformation($"[PropPost] Pasada {intPasada}: {objDesbloqueados.Count} desbloqueados.");

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

                    int nv = NivelCosteo.DeterminarNivelLote(objLote, objIndice);
                    _objMotorProrra.Ejecutar(
                        lstRpc, lstFresco, lstUni, lstDir,
                        strVersionCtx: $"NV{nv}-POST",
                        objLotesPermitidos: new HashSet<LoteRpcKeyXSec> { objLote },
                        objIndiceXLote: objIndice);
                }
            }
        }
    }
}
