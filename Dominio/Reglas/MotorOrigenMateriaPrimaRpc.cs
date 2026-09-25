using CostManagement.Dominio.Entidades;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Participación de materia prima con origen Fresco dentro de un lote de reproceso.
    ///
    /// IMPORTANTE:
    /// - No modifica el costeo real de reproceso.
    /// - Solo sirve para presentar el resumen de materia prima sin contar dos veces
    ///   lo que ya fue reconocido como LIQ_PFR.
    /// - Se calculan porcentajes separados para libras y dólares porque un lote RPC
    ///   puede mezclar materias primas con costos unitarios diferentes.
    /// </summary>
    public sealed class ParticipacionOrigenFrescoRpc
    {
        public decimal dcLibrasRecibidas { get; init; }
        public decimal dcLibrasOrigenFresco { get; init; }

        public decimal dcDolaresRecibidos { get; init; }
        public decimal dcDolaresOrigenFresco { get; init; }

        public decimal dcPorcentajeFrescoLibras { get; init; }
        public decimal dcPorcentajeFrescoDolares { get; init; }
    }

    /// <summary>
    /// Identifica qué proporción del reproceso proviene de liquidaciones Fresco.
    ///
    /// La trazabilidad sigue las mismas llaves que ya usa CostMan:
    /// - Fresco directo: (LoteOrigen, Producto, Talla) contra LoteFrsKey.
    /// - Reproceso encadenado: el RECIBIDO apunta con intLoteOrigen al
    ///   intLoteUnificado de un PROCESADO anterior, manteniendo Producto + Talla.
    ///
    /// Así se evita descontar solamente el primer salto. Si Fresco pasa por uno o
    /// más reprocesos antes de llegar a VAG, su participación se sigue propagando.
    /// </summary>
    public static class MotorOrigenMateriaPrimaRpc
    {
        private readonly record struct FuenteRpcKey(
            int intLoteUnificado,
            int intCodProd,
            int intCodTal);

        private const decimal TOLERANCIA = 0.0000001m;

        public static Dictionary<LoteRpcKeyXSec, ParticipacionOrigenFrescoRpc> Calcular(
            IReadOnlyCollection<LiquidacionResultado>? lstFresco,
            IReadOnlyCollection<MatPrimaReproceso>? lstRpcCompleto)
        {
            var lstFrs = lstFresco?.ToList() ?? new List<LiquidacionResultado>();
            var lstRpc = lstRpcCompleto?.ToList() ?? new List<MatPrimaReproceso>();

            if (lstRpc.Count == 0)
                return new Dictionary<LoteRpcKeyXSec, ParticipacionOrigenFrescoRpc>();

            var hsFresco = lstFrs
                .Where(x => x.intCodProd.HasValue && x.intLidCodTal.HasValue)
                .Select(x => new LoteFrsKey(
                    x.intLote,
                    x.intCodProd!.Value,
                    x.intLidCodTal!.Value))
                .ToHashSet();

            var lstRecibido = lstRpc
                .Where(x =>
                    string.Equals(
                        x.strAgrupacion,
                        "1. RECIBIDO",
                        StringComparison.OrdinalIgnoreCase)
                    && x.dbLibras > 0)
                .ToList();

            var lstProcesado = lstRpc
                .Where(x =>
                    string.Equals(
                        x.strAgrupacion,
                        "2. PROCESADO",
                        StringComparison.OrdinalIgnoreCase)
                    && x.dbLibras > 0)
                .ToList();

            var lookupRecibido = lstRecibido
                .ToLookup(x => x.objLoteKey);

            // Un RECIBIDO de una etapa posterior puede apuntar al lote unificado de
            // un PROCESADO anterior. Producto + talla evitan tomar una fuente errónea.
            var lookupFuenteRpc = lstProcesado
                .ToLookup(x => new FuenteRpcKey(
                    x.intLoteUnificado,
                    x.intCodProd,
                    x.intCodTal));

            var lstLotes = lstRpc
                .Select(x => x.objLoteKey)
                .Distinct()
                .ToList();

            var resultado = lstLotes.ToDictionary(
                x => x,
                _ => new ParticipacionOrigenFrescoRpc());

            // Reutilizamos el mismo grafo que ya usa CostMan para respetar el orden
            // fuente -> destino cuando hay varios niveles de reproceso.
            var (dependencias, dependientes) =
                MatPrimaReproceso.DetectarCadenasDependencia(lstRpc);

            var (orden, lotesEnCiclo) =
                MatPrimaReproceso.OrdenarLotesTopologicamente(
                    lstLotes,
                    dependencias,
                    dependientes);

            foreach (var lote in orden)
            {
                resultado[lote] = CalcularLote(
                    lote,
                    lookupRecibido,
                    lookupFuenteRpc,
                    hsFresco,
                    resultado);
            }

            // Un ciclo debería ser una anomalía de trazabilidad, pero no dejamos el
            // resumen inutilizable. Se resuelve por aproximación iterativa limitada.
            if (lotesEnCiclo.Count > 0)
            {
                int maxPasadas = Math.Max(5, lotesEnCiclo.Count * 2);

                for (int pasada = 0; pasada < maxPasadas; pasada++)
                {
                    decimal maxDelta = 0m;

                    foreach (var lote in lotesEnCiclo)
                    {
                        ParticipacionOrigenFrescoRpc anterior = resultado[lote];

                        ParticipacionOrigenFrescoRpc nuevo = CalcularLote(
                            lote,
                            lookupRecibido,
                            lookupFuenteRpc,
                            hsFresco,
                            resultado);

                        maxDelta = Math.Max(
                            maxDelta,
                            Math.Abs(
                                nuevo.dcPorcentajeFrescoLibras -
                                anterior.dcPorcentajeFrescoLibras));

                        maxDelta = Math.Max(
                            maxDelta,
                            Math.Abs(
                                nuevo.dcPorcentajeFrescoDolares -
                                anterior.dcPorcentajeFrescoDolares));

                        resultado[lote] = nuevo;
                    }

                    if (maxDelta <= TOLERANCIA)
                        break;
                }
            }

            return resultado;
        }

        private static ParticipacionOrigenFrescoRpc CalcularLote(
            LoteRpcKeyXSec lote,
            ILookup<LoteRpcKeyXSec, MatPrimaReproceso> lookupRecibido,
            ILookup<FuenteRpcKey, MatPrimaReproceso> lookupFuenteRpc,
            HashSet<LoteFrsKey> hsFresco,
            IReadOnlyDictionary<LoteRpcKeyXSec, ParticipacionOrigenFrescoRpc> participacionCalculada)
        {
            decimal lbsTotal = 0m;
            decimal lbsFresco = 0m;

            decimal dolTotal = 0m;
            decimal dolFresco = 0m;

            foreach (MatPrimaReproceso recibido in lookupRecibido[lote])
            {
                decimal lbs = Math.Max(0m, Convert.ToDecimal(recibido.dbLibras));
                decimal dol = Math.Max(0m, recibido.dbCostoTotal);

                lbsTotal += lbs;
                dolTotal += dol;

                decimal pctFrescoLbs;
                decimal pctFrescoDol;

                // CASO 1: entrada directa desde una liquidación Fresco.
                var keyFresco = new LoteFrsKey(
                    recibido.intLoteOrigen,
                    recibido.intCodProd,
                    recibido.intCodTal);

                if (hsFresco.Contains(keyFresco))
                {
                    pctFrescoLbs = 1m;
                    pctFrescoDol = 1m;
                }
                else
                {
                    // CASO 2: entrada proveniente de un reproceso anterior.
                    // Se hereda el porcentaje Fresco de ese lote fuente.
                    var keyFuente = new FuenteRpcKey(
                        recibido.intLoteOrigen,
                        recibido.intCodProd,
                        recibido.intCodTal);

                    (pctFrescoLbs, pctFrescoDol) = CalcularParticipacionFuenteRpc(
                        lote,
                        lookupFuenteRpc[keyFuente],
                        participacionCalculada);
                }

                lbsFresco += lbs * pctFrescoLbs;
                dolFresco += dol * pctFrescoDol;
            }

            decimal pctLbs = lbsTotal > 0m
                ? Clamp01(lbsFresco / lbsTotal)
                : 0m;

            // Si todavía no existe costo en los RECIBIDOS, usar la participación
            // física como fallback para no volver a contabilizar Fresco en dólares.
            decimal pctDol = dolTotal > 0m
                ? Clamp01(dolFresco / dolTotal)
                : pctLbs;

            return new ParticipacionOrigenFrescoRpc
            {
                dcLibrasRecibidas = lbsTotal,
                dcLibrasOrigenFresco = lbsFresco,
                dcDolaresRecibidos = dolTotal,
                dcDolaresOrigenFresco = dolFresco,
                dcPorcentajeFrescoLibras = pctLbs,
                dcPorcentajeFrescoDolares = pctDol
            };
        }

        private static (decimal pctLbs, decimal pctDol) CalcularParticipacionFuenteRpc(
            LoteRpcKeyXSec loteDestino,
            IEnumerable<MatPrimaReproceso> posiblesFuentes,
            IReadOnlyDictionary<LoteRpcKeyXSec, ParticipacionOrigenFrescoRpc> participacionCalculada)
        {
            decimal pesoLbs = 0m;
            decimal frescoLbs = 0m;

            decimal pesoDol = 0m;
            decimal frescoDol = 0m;

            foreach (MatPrimaReproceso fuente in posiblesFuentes)
            {
                // Evitar autorreferencia del mismo lote.
                if (fuente.objLoteKey == loteDestino)
                    continue;

                if (!participacionCalculada.TryGetValue(
                    fuente.objLoteKey,
                    out ParticipacionOrigenFrescoRpc? participacion))
                {
                    continue;
                }

                decimal lbs = Math.Max(0m, Convert.ToDecimal(fuente.dbLibras));
                decimal dol = Math.Max(0m, fuente.dbCostoTotal);

                pesoLbs += lbs;
                frescoLbs += lbs * participacion.dcPorcentajeFrescoLibras;

                pesoDol += dol;
                frescoDol += dol * participacion.dcPorcentajeFrescoDolares;
            }

            decimal pctLbs = pesoLbs > 0m
                ? Clamp01(frescoLbs / pesoLbs)
                : 0m;

            decimal pctDol = pesoDol > 0m
                ? Clamp01(frescoDol / pesoDol)
                : pctLbs;

            return (pctLbs, pctDol);
        }

        private static decimal Clamp01(decimal valor)
        {
            if (valor < 0m) return 0m;
            if (valor > 1m) return 1m;
            return valor;
        }
    }
}
