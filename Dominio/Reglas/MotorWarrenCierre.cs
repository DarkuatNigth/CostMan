using CostManagement.Aplicación.DTos;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Warren:
    ///
    /// - El objetivo se calcula sobre libras PFR/Fresco de Cola.
    /// - El costo actual parte del Total de Procesos Cola normal.
    /// - Warren solamente transfiere dólares de Entero hacia Cola.
    /// - RPC NO forma parte de la base de absorción.
    /// - RPC sí permanece dentro del costo normal visible de los procesos.
    /// - Valor Agregado no participa.
    /// </summary>
    public static class MotorWarrenCierre
    {
        private sealed record DefProceso(
            string Descripcion,
            bool Absorbe);

        /// <summary>
        /// Procesos que participan en la distribución Warren.
        ///
        /// Solamente los que tengan Absorbe=true forman parte
        /// de la base PFR Entero para el porcentaje de absorción.
        /// </summary>
        private static readonly List<DefProceso> _procesosWarren = new()
        {
            new("Logistica", true),
            new("Recepcion", true),
            new("Clasificacion", true),
            new("Cajas", true),
            new("Descabezado", true),
            new("Retractilado", true),
            new("Tunel", true),

            new("C.D.Variables", true),
            new("C.D.Fijos", true),
            new("C.I.Variables", true),
            new("C.I.Fijos", true),

            new("C.Copacking", false),
            new("Excedente M.E.", false)
        };


        /// <summary>
        /// Procesos que forman el Total Procesos normal de la pantalla.
        ///
        /// IMPORTANTE:
        /// aquí sí están incluidos procesos RPC, porque el Total Procesos
        /// visible contiene PFR + RPC.
        ///
        /// Material Empaque NO entra porque está fuera del bloque
        /// "Resumen Costo Producción".
        /// </summary>
        private static readonly HashSet<string> _procesosCostoProduccion =
            new(
                new[]
                {
                    "LOGISTICA",
                    "RECEPCION",
                    "CLASIFICACION",
                    "CAJAS",
                    "TUNEL",
                    "BRINE",
                    "IQF",
                    "DESCABEZADO",
                    "DESCONGELADO",
                    "HIDRATACION",
                    "COCIDO",
                    "PELADO",
                    "RETRACTILADO",
                    "DECORADO",

                    "C.D.VARIABLES",
                    "C.D.FIJOS",
                    "C.I.VARIABLES",
                    "C.I.FIJOS",

                    "C.COPACKING",
                    "EXCEDENTE M.E."
                },
                StringComparer.OrdinalIgnoreCase
            );


        public static WarrenResultadoDto Calcular(
            IEnumerable<CostoProductivoProcesoOrigenDto> resumenOrigen,
            IEnumerable<ProcesoResultadoDto> catalogoPfr,
            decimal objetivoWarren)
        {
            if (objetivoWarren <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(objetivoWarren));
            }


            // ============================================================
            // MATERIALIZAR RESUMEN COMPLETO
            // ============================================================

            List<CostoProductivoProcesoOrigenDto> resumen =
                (resumenOrigen ??
                    Array.Empty<CostoProductivoProcesoOrigenDto>())
                .ToList();


            if (resumen.Count == 0)
            {
                throw new InvalidOperationException(
                    "No existe resumen de costos para calcular Warren.");
            }


            // ============================================================
            // RESUMEN EXCLUSIVO PFR
            //
            // Se utiliza para:
            //
            // - libras Warren;
            // - base de absorción;
            // - detalle Warren por proceso.
            //
            // RPC nunca absorbe Warren.
            // ============================================================

            Dictionary<string, CostoProductivoProcesoOrigenDto> pfr =
                resumen
                    .Where(x =>
                        Normalizar(x.strOrigen) == "PFR")
                    .GroupBy(
                        x => Normalizar(x.strProceso),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => ConsolidarPfr(g),
                        StringComparer.OrdinalIgnoreCase);


            if (pfr.Count == 0)
            {
                throw new InvalidOperationException(
                    "No existe resumen PFR para calcular Warren.");
            }


            // ============================================================
            // CATÁLOGO PFR
            // ============================================================

            Dictionary<string, ProcesoResultadoDto> catalogo =
                (catalogoPfr ??
                    Array.Empty<ProcesoResultadoDto>())
                .Where(x =>
                    x.intCodigo > 0 &&
                    !string.IsNullOrWhiteSpace(
                        x.strDescripcion))
                .GroupBy(
                    x => Normalizar(
                        x.strDescripcion),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);


            // ============================================================
            // LIBRAS PFR COLA
            //
            // Warren SOLO utiliza Fresco.
            // ============================================================

            decimal librasColaGlobal =
                new[]
                {
                    "LOGISTICA",
                    "RECEPCION",
                    "CLASIFICACION"
                }
                .Select(x =>
                    pfr.GetValueOrDefault(x)
                        ?.dcLibrasCola ?? 0m)
                .FirstOrDefault(x => x > 0m);


            if (librasColaGlobal <= 0m)
            {
                librasColaGlobal =
                    pfr.Values
                        .Select(x =>
                            x.dcLibrasCola)
                        .DefaultIfEmpty(0m)
                        .Max();
            }


            if (librasColaGlobal <= 0m)
            {
                throw new InvalidOperationException(
                    "No existen libras PFR Cola para calcular Warren.");
            }


            // ============================================================
            // COSTO NORMAL ACTUAL DE COLA
            //
            // IMPORTANTE:
            //
            // Este valor representa el mismo Total Procesos Cola
            // mostrado a la izquierda en Angular.
            //
            // Aquí sí pueden existir:
            //
            // PFR + RPC
            //
            // porque Warren parte del costo normal existente y luego
            // mueve costo desde Entero hacia Cola.
            // ============================================================

            decimal costoProcesoCola =
                resumen
                    .Where(x =>
                        _procesosCostoProduccion.Contains(
                            Normalizar(x.strProceso)))
                    .Sum(x =>
                        x.dcDolaresCola);


            decimal costoColaActual =
                costoProcesoCola /
                librasColaGlobal;


            // ============================================================
            // OBJETIVO WARREN
            //
            // Ejemplo:
            //
            // Libras PFR Cola = 3,746,756.47
            // Objetivo        = 0.83
            //
            // Dólares objetivo:
            //
            // 3,746,756.47 × 0.83
            // ============================================================

            decimal dolaresColaObjetivo =
                objetivoWarren *
                librasColaGlobal;


            decimal dolaresFaltantes =
                dolaresColaObjetivo -
                costoProcesoCola;


            // ============================================================
            // WARREN SOLO PUEDE SER:
            //
            // ENTERO  --->  COLA
            //
            // Nunca Cola ---> Entero.
            // ============================================================

            decimal ajusteTotal =
                dolaresFaltantes > 0m
                    ? dolaresFaltantes
                    : 0m;


            decimal diferencia =
                ajusteTotal > 0m
                    ? objetivoWarren -
                      costoColaActual
                    : 0m;


            // ============================================================
            // DETALLE WARREN
            //
            // Aquí solamente necesitamos PFR porque únicamente PFR
            // absorbe la transferencia.
            // ============================================================

            var detalle =
                new List<WarrenProcesoDto>();


            foreach (
                DefProceso def
                in _procesosWarren)
            {
                string key =
                    Normalizar(
                        def.Descripcion);


                if (!catalogo.TryGetValue(
                    key,
                    out ProcesoResultadoDto? parametro))
                {
                    continue;
                }


                CostoProductivoProcesoOrigenDto?
                    proceso =
                        pfr.GetValueOrDefault(key);


                decimal montoEn =
                    proceso?.dcDolaresEntero ??
                    0m;


                decimal montoCo =
                    proceso?.dcDolaresCola ??
                    0m;


                decimal lbsEn =
                    proceso?.dcLibrasEntero ??
                    0m;


                decimal lbsCo =
                    proceso?.dcLibrasCola ??
                    0m;


                detalle.Add(
                    new WarrenProcesoDto
                    {
                        intCodigo =
                            parametro.intCodigo,

                        strDescripcion =
                            parametro.strDescripcion,

                        dcLibrasEntero =
                            lbsEn,

                        dcLibrasCola =
                            lbsCo,

                        dcMontoEnteroOriginal =
                            montoEn,

                        dcMontoColaOriginal =
                            montoCo,

                        dcMontoEnteroWarren =
                            montoEn,

                        dcMontoColaWarren =
                            montoCo,

                        dcCostoUnitarioEnteroOriginal =
                            lbsEn > 0m
                                ? montoEn / lbsEn
                                : 0m,

                        dcCostoUnitarioColaOriginal =
                            lbsCo > 0m
                                ? montoCo / lbsCo
                                : 0m,

                        dcCostoUnitarioEnteroWarren =
                            lbsEn > 0m
                                ? montoEn / lbsEn
                                : 0m,

                        dcCostoUnitarioColaWarren =
                            lbsCo > 0m
                                ? montoCo / lbsCo
                                : 0m
                    });
            }


            // ============================================================
            // BASE DE ABSORCIÓN
            //
            // EXCLUSIVAMENTE PFR ENTERO.
            // ============================================================

            decimal baseAbsorcion =
                detalle
                    .Where(x =>
                        EsAbsorbente(
                            x.strDescripcion))
                    .Sum(x =>
                        x.dcMontoEnteroOriginal);


            if (
                ajusteTotal > 0m &&
                baseAbsorcion <= 0m)
            {
                throw new InvalidOperationException(
                    "No existe base PFR Entero para absorber Warren.");
            }


            if (
                ajusteTotal > baseAbsorcion &&
                baseAbsorcion > 0m)
            {
                throw new InvalidOperationException(
                    $"El ajuste Warren ({ajusteTotal:N2}) " +
                    $"supera la base PFR Entero ({baseAbsorcion:N2}).");
            }


            // ============================================================
            // DISTRIBUIR AJUSTE
            // ============================================================

            foreach (
                WarrenProcesoDto item
                in detalle)
            {
                if (
                    !EsAbsorbente(
                        item.strDescripcion) ||
                    baseAbsorcion <= 0m ||
                    item.dcMontoEnteroOriginal <= 0m ||
                    ajusteTotal <= 0m)
                {
                    continue;
                }


                item.dcPorcentajeAbsorcion =
                    item.dcMontoEnteroOriginal /
                    baseAbsorcion;


                item.dcAjusteWarren =
                    item.dcPorcentajeAbsorcion *
                    ajusteTotal;


                // ========================================================
                // ENTERO
                // ========================================================

                item.dcUnitarioExtraidoEntero =
                    item.dcLibrasEntero > 0m
                        ? item.dcAjusteWarren /
                          item.dcLibrasEntero
                        : 0m;


                item.dcCostoUnitarioEnteroWarren =
                    item.dcCostoUnitarioEnteroOriginal -
                    item.dcUnitarioExtraidoEntero;


                item.dcMontoEnteroWarren =
                    item.dcMontoEnteroOriginal -
                    item.dcAjusteWarren;


                // Nunca permitir un Warren negativo en Entero.
                if (
                    item.dcMontoEnteroWarren <
                    -0.01m)
                {
                    throw new InvalidOperationException(
                        $"Warren generó monto negativo en Entero " +
                        $"para {item.strDescripcion}. " +
                        $"Original: {item.dcMontoEnteroOriginal:N2}; " +
                        $"Ajuste: {item.dcAjusteWarren:N2}.");
                }


                // ========================================================
                // COLA
                // ========================================================

                decimal baseCola =
                    item.dcLibrasCola > 0m
                        ? item.dcLibrasCola
                        : librasColaGlobal;


                if (
                    item.dcLibrasCola <= 0m &&
                    item.dcAjusteWarren > 0m)
                {
                    item.dcLibrasCola =
                        librasColaGlobal;

                    item.blUsaBaseGlobalCola =
                        true;
                }


                item.dcUnitarioAgregadoCola =
                    baseCola > 0m
                        ? item.dcAjusteWarren /
                          baseCola
                        : 0m;


                item.dcCostoUnitarioColaWarren =
                    item.dcCostoUnitarioColaOriginal +
                    item.dcUnitarioAgregadoCola;


                item.dcMontoColaWarren =
                    item.dcMontoColaOriginal +
                    item.dcAjusteWarren;


                if (
                    item.dcMontoColaWarren <
                    -0.01m)
                {
                    throw new InvalidOperationException(
                        $"Warren generó monto negativo en Cola " +
                        $"para {item.strDescripcion}.");
                }
            }


            // ============================================================
            // VALIDACIÓN 1
            //
            // Todo el ajuste debe haberse distribuido.
            // ============================================================

            decimal ajusteDistribuido =
                detalle.Sum(x =>
                    x.dcAjusteWarren);


            if (
                Math.Abs(
                    ajusteDistribuido -
                    ajusteTotal) > 0.01m)
            {
                throw new InvalidOperationException(
                    $"El ajuste Warren no fue distribuido completamente. " +
                    $"Esperado: {ajusteTotal:N2}; " +
                    $"Distribuido: {ajusteDistribuido:N2}.");
            }


            // ============================================================
            // VALIDACIÓN 2
            //
            // Total Procesos Cola normal + Warren
            // debe alcanzar exactamente el objetivo.
            //
            // NO sumar lstDetalle porque lstDetalle es únicamente
            // la base técnica PFR utilizada para absorber.
            // ============================================================

            decimal dolaresColaWarren =
                costoProcesoCola +
                ajusteTotal;


            decimal unitarioColaWarren =
                dolaresColaWarren /
                librasColaGlobal;


            decimal diferenciaObjetivo =
                Math.Round(
                    unitarioColaWarren -
                    (
                        ajusteTotal > 0m
                            ? objetivoWarren
                            : costoColaActual
                    ),
                    6);


            if (
                Math.Abs(
                    diferenciaObjetivo) >
                    0.0001m)
            {
                throw new InvalidOperationException(
                    $"Warren no alcanzó el objetivo esperado. " +
                    $"Objetivo: {objetivoWarren:N4}; " +
                    $"Resultado: {unitarioColaWarren:N4}; " +
                    $"Diferencia: {diferenciaObjetivo:N6}.");
            }


            return new WarrenResultadoDto
            {
                dcObjetivoWarren =
                    objetivoWarren,

                dcCostoColaActual =
                    costoColaActual,

                dcDiferenciaWarren =
                    diferencia,

                dcAjusteTotal =
                    ajusteTotal,

                dcBaseAbsorcionEntero =
                    baseAbsorcion,

                dcLibrasCola =
                    librasColaGlobal,

                lstDetalle =
                    detalle
            };
        }


        private static CostoProductivoProcesoOrigenDto ConsolidarPfr(
            IEnumerable<CostoProductivoProcesoOrigenDto> items)
        {
            CostoProductivoProcesoOrigenDto first =
                items.First();


            var result =
                new CostoProductivoProcesoOrigenDto
                {
                    strPcCodigo =
                        first.strPcCodigo,

                    strProceso =
                        first.strProceso,

                    strOrigen =
                        "PFR",

                    dcLibrasEntero =
                        items.Sum(x =>
                            x.dcLibrasEntero),

                    dcLibrasCola =
                        items.Sum(x =>
                            x.dcLibrasCola),

                    dcLibrasValorAgregado =
                        items.Sum(x =>
                            x.dcLibrasValorAgregado),

                    dcDolaresEntero =
                        items.Sum(x =>
                            x.dcDolaresEntero),

                    dcDolaresCola =
                        items.Sum(x =>
                            x.dcDolaresCola),

                    dcDolaresValorAgregado =
                        items.Sum(x =>
                            x.dcDolaresValorAgregado)
                };


            result.Recalcular();

            return result;
        }


        private static bool EsAbsorbente(
            string descripcion)
        {
            return _procesosWarren
                .FirstOrDefault(x =>
                    Normalizar(
                        x.Descripcion) ==
                    Normalizar(
                        descripcion))
                ?.Absorbe ??
                false;
        }


        private static string Normalizar(
            string? valor)
        {
            return
                (valor ??
                    string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U");
        }
    }
}