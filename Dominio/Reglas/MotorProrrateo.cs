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
            //string strVersionCtx,
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
                        .GroupBy(x => new LoteRpcKeyXProdTal(x.intCodProd, x.intCodTal))
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
                        var key = new LoteRpcKeyXProdTal(rec.intCodProd, rec.intCodTal);
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
                .Where(x => x.strAgrupacion == "1. RECIBIDO" && !x.blExcluidoCosteo)
                .GroupBy(x => new LoteRpcNivelCosteo(x.intLotNumero, NivelCosteo.ObtenerNivel(x.strTipCod)))
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.dbLibras));

            // Libras RECIBIDO sin costear que son material de laboratorio (LB04):
            // no tienen costo de origen por naturaleza, no deben bloquear el costeo
            // del lote si representan una porción menor (≤5%) del total del nivel.
            var lbsFaltanteLB04 = lstItemsLote
                .Where(x => x.strAgrupacion == "1. RECIBIDO"
                         && x.dbCostoXSecuencial == 0
                         && !x.blExcluidoCosteo
                         && string.Equals(x.strTipCod?.Trim(), "LB04", StringComparison.OrdinalIgnoreCase))
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
                if (dcLbsTot > 0 && dcLbsCost < dcLbsTot)
                {
                    decimal dcLbsFaltante = dcLbsTot - dcLbsCost;
                    decimal dcLbsFaltanteLB04 = lbsFaltanteLB04.TryGetValue(keyGuardia, out var lb04) ? lb04 : 0m;

                    // Excepción: se permite costear con lo disponible SOLO si
                    // (a) algo ya está costeado, (b) toda la falta es material de
                    // laboratorio (LB04, sin costo por naturaleza) y (c) esa falta
                    // no supera el 5% del total del nivel.
                    bool blSoloFaltaLB04 = dcLbsFaltante <= dcLbsFaltanteLB04;
                    bool blDentroDeTolerancia = (dcLbsFaltante / dcLbsTot) <= 0.05m;

                    if (dcLbsCost == 0m || !blSoloFaltaLB04 || !blDentroDeTolerancia)
                        continue; // RECIBIDO incompletos → esperar
                }

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
