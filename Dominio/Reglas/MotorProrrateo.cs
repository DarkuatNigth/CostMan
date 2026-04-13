using CostManagement.Dominio.Entidades;

namespace CostManagement.Dominio.Reglas
{
    public class MotorProrrateo
    {
        private readonly MotorAsignacionPrecios _objMotorAsigPrec;

        public MotorProrrateo(MotorAsignacionPrecios motorPrecios)
            => _objMotorAsigPrec = motorPrecios;

        public void Ejecutar(
            List<MatPrimaReproceso> lstTodos,
            List<LiquidacionResultado> lstFresco,
            List<PrecioFrsXMov> lstPrecioFrsUni,
            List<PrecioFrsXMov> lstPrecioFrsDir,
            string strVersionCtx,
            HashSet<LoteRpcKeyXSec> objLotesPermitidos = null,
            ILookup<LoteRpcKeyXSec, MatPrimaReproceso> objIndiceXLote = null)
        {
            bool blUsarIndice = objIndiceXLote != null && objLotesPermitidos.Any();

            // Materializar ítems del lote actual
            var lstItemsLote = blUsarIndice
                ? objIndiceXLote![objLotesPermitidos!.First()].ToList()
                : objLotesPermitidos == null
                    ? lstTodos
                    : lstTodos.Where(x =>
                        objLotesPermitidos.Contains(new LoteRpcKeyXSec(x.intLotNumero, x.intLoteUnificado)))
                    .ToList();

            bool EsPermitido(MatPrimaReproceso x) =>
                objLotesPermitidos == null ||
                objLotesPermitidos.Contains(new LoteRpcKeyXSec(x.intLotNumero, x.intLoteUnificado));

            // ── BLOQUE A: Fill RECIBIDO = 0 con promedio ponderado de RECIBIDO costeados
            //    Solo para lotes NV3+. Fuente: RECIBIDO ya costeados del universo completo.
            //    NV1 y NV2 no necesitan este paso.
            int intNivelLote = lstItemsLote
                .Where(x => x.strAgrupacion == "2. PROCESADO")
                .Select(x => NivelCosteo.ObtenerNivel(x.strTipCod))
                .DefaultIfEmpty(2).Max();

            if (intNivelLote > 2)
            {
                var lstFiltRecResid = lstItemsLote
                    .Where(x => x.strAgrupacion == "1. RECIBIDO"
                             && x.dbCostoXSecuencial == 0
                             && !NivelCosteo.AplicaMetodoEspecial(x.strTipCod))
                    .ToList();
                if (lstFiltRecResid.Any())
                {
                    // Fuente CORRECTA: RECIBIDO ya costeados de TODO el universo
                    var dictRef = lstTodos
                        .Where(x => x.strAgrupacion == "1. RECIBIDO"
                                 && x.dbCostoXSecuencial > 0
                                 && x.dbLibras > 0)
                        .GroupBy(x => new LoteRpcKeyXProdTal(x.intProdCod, x.intCodTal))
                        .ToDictionary(
                            g => g.Key,
                            g =>
                            {
                                decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                                decimal totDol = g.Sum(x => x.dbCostoTotal);
                                return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                            });

                    foreach (var rec in lstFiltRecResid)
                    {
                        var key = new LoteRpcKeyXProdTal(rec.intProdCod, rec.intCodTal);
                        if (dictRef.TryGetValue(key, out decimal dcPrecio) && dcPrecio > 0)
                        {
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod)));
                        }
                    }
                }
            }

            // ── BLOQUE B: Diccionarios de prorrateo ────────────────────────────
            var dicDolRec = MatPrimaReproceso.GenerarDiccionarioReciXFil(
                lstItemsLote,
                x => x.strAgrupacion == "1. RECIBIDO" && x.dbCostoXSecuencial != 0 && EsPermitido(x));

            var dicLbsProc = MatPrimaReproceso.GenerarDiccionarioProceXFil(
                lstItemsLote,
                x => x.strAgrupacion == "2. PROCESADO" && EsPermitido(x));

            // Guardia de completitud: lbs con costo vs total por (lote, nivel)
            var lbsCost = lstItemsLote
                .Where(x => x.strAgrupacion == "1. RECIBIDO" && x.dbCostoXSecuencial > 0)
                .GroupBy(x => new LoteRpcNivelCosteo(x.intLotNumero, NivelCosteo.ObtenerNivel(x.strTipCod)))
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.dbLibras));

            var lbsTot = lstItemsLote
                .Where(x => x.strAgrupacion == "1. RECIBIDO")
                .GroupBy(x => new LoteRpcNivelCosteo(x.intLotNumero, NivelCosteo.ObtenerNivel(x.strTipCod)))
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.dbLibras));

            // ── BLOQUE C: Costear cada PROCESADO sin precio ────────────────────
            foreach (var objItemLote in lstItemsLote.Where(x =>
                x.strAgrupacion == "2. PROCESADO" && x.dbCostoXSecuencial == 0))
            {
                var objKeySec = new LoteRpcKeyXSec(objItemLote.intLotNumero, objItemLote.intLoteUnificado);
                int intNivel = NivelCosteo.ObtenerNivel(objItemLote.strTipCod);
                var strNivelLabel = NivelCosteo.EtiquetaNivel(intNivel);

                bool blEsRecibidoCosteado = dicDolRec.TryGetValue(objKeySec, out decimal dcTotalDolRec);
                bool blEsLbsProceso = dicLbsProc.TryGetValue(objKeySec, out decimal dcLbsTotalProc);

                // ── Métodos especiales (UNI, R7, CDI en NV2) ──────────────────
                if (NivelCosteo.AplicaMetodoEspecial(objItemLote.strTipCod))
                {
                    objItemLote.strNivel = strNivelLabel;
                    var lstLotOrigen = MatPrimaReproceso.ObtenerLstLoteXRepro(lstTodos, objItemLote);

                    switch (objItemLote.strTipCod.Trim().ToUpper())
                    {
                        case "UNI":
                            _objMotorAsigPrec.AsignarCostoUniMovCam(objItemLote, lstLotOrigen, lstPrecioFrsUni);
                            break;
                        case "R7":
                            decimal lbsR7 = lstItemsLote
                                .Where(x => x.strAgrupacion == "2. PROCESADO"
                                         && x.intCodCopacking == objItemLote.intCodCopacking
                                         && x.intLoteUnificado == objItemLote.intLoteUnificado
                                         && x.strTipCod == objItemLote.strTipCod
                                         && x.intLotNumero == objItemLote.intLotNumero)
                                .GroupBy(x => x.intLotNumero)
                                .Select(g => (decimal)g.Sum(y => y.dbLibras))
                                .FirstOrDefault();
                            _objMotorAsigPrec.AsignarCostoDescExtraPola(objItemLote, lstLotOrigen, lstFresco, lbsR7);
                            break;
                        case "CDI":
                            _objMotorAsigPrec.AsignarCostoReprocesoColaDir(objItemLote, lstPrecioFrsDir);
                            break;
                    }

                    continue;
                }

                // ── Guardia de completitud: omitir si RECIBIDO del mismo nivel incompleto
                var keyGuardia = new LoteRpcNivelCosteo(objItemLote.intLotNumero, intNivel);
                decimal dcLbsTot = lbsTot.TryGetValue(keyGuardia, out var t) ? t : 0m;
                decimal dcLbsCost = lbsCost.TryGetValue(keyGuardia, out var c) ? c : 0m;
                if (dcLbsTot > 0 && dcLbsCost < dcLbsTot) continue; // RECIBIDO incompletos → esperar

                // ── Prorrateo general ──────────────────────────────────────────
                if (blEsRecibidoCosteado && blEsLbsProceso && dcLbsTotalProc > 0)
                {
                    decimal dcCostUniProrra = dcTotalDolRec / dcLbsTotalProc;
                    _objMotorAsigPrec.AsignarPrecio(objItemLote, dcCostUniProrra, strNivelLabel);
                }
            }
        }
    }
}
