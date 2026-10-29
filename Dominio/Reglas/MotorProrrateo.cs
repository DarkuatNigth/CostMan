using CostManagement.Infraestructura.Utils;
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
            IReadOnlyDictionary<string, int> mapaNiveles,
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
                .Select(x => NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles))
                .DefaultIfEmpty(2).Max();

            // using var objTiempoNivel = MedicionTiempos.IniciarTramo($"prorrateo-NV{intNivelLote}", () => lstItemsLote.Count);
            if (intNivelLote > 2)
            {
                var lstFiltRecResid = lstItemsLote
                    .Where(x => x.strAgrupacion == "1. RECIBIDO"
                             && x.dbCostoXSecuencial == 0
                             && !x.blExcluidoCosteo
                             && !NivelCosteo.AplicaMetodoEspecial(x.strTipCod, mapaNiveles))
                    .ToList();
                if (lstFiltRecResid.Any())
                {
                    // medir reconstruccion global; no cachear costos que cambian por lote.
                    // using var objTiempoResidual = MedicionTiempos.IniciarTramo("prorrateo-promedio-residual-global");

                    // Una muestra por nivel destino antes de asignar; se renueva en cada pasada.
                    var dicRefPorNivel = lstFiltRecResid
                        .Select(rec => NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)).Distinct()
                        .ToDictionary(intNivelDestino => intNivelDestino, intNivelDestino =>
                            MatPrimaReproceso.GenerarPromedioRespaldo(lstTodos, mapaNiveles, intNivelDestino, ConfigRespaldoRpc.Modo));

                    var indiceDisponible = new MatPrimaReproceso.IndiceDisponibleHistorico(lstTodos);
                    indiceDisponible.BloquearProcesadosAgotados();
                    // Con la bandera apagada se conserva el orden previo.
                    var recibidosOrdenados = ConfigRespaldoRpc.HistoricoPonderado
                        ? lstFiltRecResid.OrderBy(x => NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles)).ToList() : lstFiltRecResid;
                    // Pasada 1: todo DISP antes de que HIST absorba fuentes de niveles superiores.
                    // Se reutiliza el índice: filas vivas y disponible actualizado por Confirmar.
                    if (ConfigRespaldoRpc.HistoricoPonderado)
                        foreach (var rec in recibidosOrdenados)
                        {
                            decimal dcPrecioDisponible = MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstTodos, indiceDisponible);
                            if (dcPrecioDisponible <= 0) continue;
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecioDisponible, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                            indiceDisponible.Confirmar(rec);
                        }
                    // Pasada 2: HIST para los pendientes y luego RESP; sin bandera, DISP → RESP como antes.
                    foreach (var rec in recibidosOrdenados)
                    {
                        if (ConfigRespaldoRpc.HistoricoPonderado && rec.dbCostoXSecuencial != 0) continue;
                        // Prioridad: disponible del lote, histórico opcional y respaldo vigente.
                        // decimal dcPrecio = MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstTodos);
                        // decimal dcPrecio = MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstTodos, indiceDisponible);
                        decimal dcPrecio = ConfigRespaldoRpc.HistoricoPonderado ? 0m
                            : MatPrimaReproceso.ObtenerCostoDisponibleLote(rec, lstTodos, indiceDisponible);
                        if (dcPrecio > 0)
                        {
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                            indiceDisponible.Confirmar(rec);
                            continue;
                        }
                        if (ConfigRespaldoRpc.HistoricoPonderado)
                        {
                            dcPrecio = MatPrimaReproceso.ObtenerCostoHistorico(rec, lstTodos, mapaNiveles, indiceDisponible);
                            if (dcPrecio > 0)
                            {
                                _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                                indiceDisponible.Confirmar(rec);
                                continue;
                            }
                        }
                        var key = new LoteRpcKeyXProdTal(rec.intCodProd, rec.intCodTal);
                        var dictRef = dicRefPorNivel[NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)];
                        if (dictRef.TryGetValue(key, out dcPrecio) && dcPrecio > 0)
                        {
                            _objMotorAsigPrec.AsignarPrecio(rec, dcPrecio, NivelCosteo.EtiquetaNivel(NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)));
                            if (!rec.blExcluidoCosteo) rec.strOrigenCosto = "RESP";
                            // Temporal: MotorProrrateo no recibe ILogger; habilitarlo antes de activar esta traza.
                            // _objLogger.LogInformation($"[DiagPonderadoRpc] Lote={rec.intLotNumero} Nivel={NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles)} Prod={rec.intCodProd} Talla={rec.intCodTal} Precio={rec.dbCostoXSecuencial:F4} Modo={ConfigRespaldoRpc.Modo} NivelesFuente={(ConfigRespaldoRpc.Modo == ModoRespaldoRpc.Actual ? "Todos" : (NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles) - 1).ToString())}");
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
                .GroupBy(x => new LoteRpcNivelCosteo(x.intLotNumero, NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles)))
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.dbLibras));

            var lbsTot = lstItemsLote
                .Where(x => x.strAgrupacion == "1. RECIBIDO" && !x.blExcluidoCosteo)
                .GroupBy(x => new LoteRpcNivelCosteo(x.intLotNumero, NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles)))
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.dbLibras));

            // Libras RECIBIDO sin costear que son material de laboratorio (LB04):
            // no tienen costo de origen por naturaleza, no deben bloquear el costeo
            // del lote si representan una porción menor (≤5%) del total del nivel.
            var lbsFaltanteLB04 = lstItemsLote
                .Where(x => x.strAgrupacion == "1. RECIBIDO"
                         && x.dbCostoXSecuencial == 0
                         && !x.blExcluidoCosteo
                         && string.Equals(x.strTipCod?.Trim(), "LB04", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => new LoteRpcNivelCosteo(x.intLotNumero, NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles)))
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(x => x.dbLibras));

            // ── BLOQUE C: Costear cada PROCESADO sin precio ────────────────────
            foreach (var objItemLote in lstItemsLote.Where(x =>
                x.strAgrupacion == "2. PROCESADO" && x.dbCostoXSecuencial == 0))
            {
                var objKeySec = new LoteRpcKeyXSec(objItemLote.intLotNumero, objItemLote.intLoteUnificado);
                int intNivel = NivelCosteo.ObtenerNivel(objItemLote.strTipCod, mapaNiveles);
                var strNivelLabel = NivelCosteo.EtiquetaNivel(intNivel);

                bool blEsRecibidoCosteado = dicDolRec.TryGetValue(objKeySec, out decimal dcTotalDolRec);
                bool blEsLbsProceso = dicLbsProc.TryGetValue(objKeySec, out decimal dcLbsTotalProc);

                // ── Métodos especiales (UNI, R7, CDI en NV2) ──────────────────
                if (NivelCosteo.AplicaMetodoEspecial(objItemLote.strTipCod, mapaNiveles))
                {
                    // aislar UNI/R7/CDI (MotorAsignacionPrecios) del prorrateo general.
                    // using var objTiempoEspecial = MedicionTiempos.IniciarTramo($"asignacion-especial-{objItemLote.strTipCod}");
                    objItemLote.strNivel = strNivelLabel;
                    var lstLotOrigen = MatPrimaReproceso.ObtenerLstLoteXRepro(
                        // el indice contiene exactamente las filas del lote; conserva Distinct y orden.
                        objIndiceXLote != null ? objIndiceXLote[objKeySec] : lstTodos, objItemLote);

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
