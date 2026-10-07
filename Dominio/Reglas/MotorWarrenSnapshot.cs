using CostManagement.Aplicación.DTos;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Warren calculado desde el mismo snapshot visible en Angular.
    ///
    /// No consulta Producción ni SONG.
    /// - Cola actual: dólares visibles del bloque Total Procesos (PFR + RPC).
    /// - Libras objetivo: exclusivamente PFR Cola.
    /// - Base de absorción: exclusivamente PFR Entero de procesos absorbentes.
    /// - Warren solo traslada dólares de Entero hacia Cola.
    /// </summary>
    public static class MotorWarrenSnapshot
    {
        private const decimal dcTolerancia = 0.0001m;

        public static WarrenResultadoDto Calcular(
            IEnumerable<WarrenBaseProcesoSnapshotDto> lstBase,
            decimal dcObjetivoWarren)
        {
            if (dcObjetivoWarren <= 0m)
                throw new ArgumentOutOfRangeException(nameof(dcObjetivoWarren));

            List<WarrenBaseProcesoSnapshotDto> lstFilas =
                (lstBase ?? Array.Empty<WarrenBaseProcesoSnapshotDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.strDescripcion))
                .ToList();

            if (lstFilas.Count == 0)
                throw new InvalidOperationException(
                    "No existe snapshot Warren para calcular el cierre.");

            decimal dcLibrasCola = lstFilas
                .Select(x => x.dcLibrasPfrCola)
                .Where(x => x > 0m)
                .DefaultIfEmpty(0m)
                .Max();

            if (dcLibrasCola <= 0m)
                throw new InvalidOperationException(
                    "No existen libras PFR Cola en el snapshot Warren.");

            decimal dcDolaresColaActual = Math.Round(
                lstFilas.Sum(x => x.dcMontoColaVisible),
                6);

            decimal dcCostoColaActual = dcDolaresColaActual / dcLibrasCola;
            decimal dcDolaresObjetivo = dcObjetivoWarren * dcLibrasCola;
            decimal dcAjusteTotal = Math.Max(0m, dcDolaresObjetivo - dcDolaresColaActual);
            decimal dcDiferencia = dcAjusteTotal > 0m
                ? dcObjetivoWarren - dcCostoColaActual
                : 0m;

            decimal dcBaseAbsorcion = lstFilas
                .Where(x => x.blAbsorbe)
                .Sum(x => x.dcMontoPfrEntero);

            if (dcAjusteTotal > 0m && dcBaseAbsorcion <= 0m)
                throw new InvalidOperationException(
                    "No existe base PFR Entero para absorber Warren.");

            if (dcAjusteTotal > dcBaseAbsorcion && dcBaseAbsorcion > 0m)
                throw new InvalidOperationException(
                    $"El ajuste Warren ({dcAjusteTotal:N2}) supera la base PFR Entero ({dcBaseAbsorcion:N2}).");

            var lstDetalle = new List<WarrenProcesoDto>();

            foreach (WarrenBaseProcesoSnapshotDto objBase in lstFilas)
            {
                if (objBase.blAbsorbe &&
                    objBase.dcMontoPfrEntero > 0m &&
                    objBase.intCodigo <= 0)
                {
                    throw new InvalidOperationException(
                        $"El proceso absorbente {objBase.strDescripcion} no tiene intCodigo PFR en el snapshot Warren.");
                }

                // intCodigo = 0 es válido para filas RPC que participan en el costo
                // visible de Cola pero no se persisten como parámetro Warren.
                if (objBase.intCodigo <= 0)
                    continue;

                var objDetalle = new WarrenProcesoDto
                {
                    intCodigo = objBase.intCodigo,
                    strDescripcion = objBase.strDescripcion,
                    dcLibrasEntero = objBase.dcLibrasPfrEntero,
                    dcLibrasCola = objBase.dcLibrasPfrCola,
                    dcMontoEnteroOriginal = objBase.dcMontoPfrEntero,
                    dcMontoColaOriginal = objBase.dcMontoPfrCola,
                    dcMontoEnteroWarren = objBase.dcMontoPfrEntero,
                    dcMontoColaWarren = objBase.dcMontoPfrCola,
                    dcCostoUnitarioEnteroOriginal = objBase.dcLibrasPfrEntero > 0m
                        ? objBase.dcMontoPfrEntero / objBase.dcLibrasPfrEntero
                        : 0m,
                    dcCostoUnitarioColaOriginal = objBase.dcLibrasPfrCola > 0m
                        ? objBase.dcMontoPfrCola / objBase.dcLibrasPfrCola
                        : 0m
                };

                objDetalle.dcCostoUnitarioEnteroWarren =
                    objDetalle.dcCostoUnitarioEnteroOriginal;
                objDetalle.dcCostoUnitarioColaWarren =
                    objDetalle.dcCostoUnitarioColaOriginal;

                if (objBase.blAbsorbe &&
                    dcAjusteTotal > 0m &&
                    dcBaseAbsorcion > 0m &&
                    objBase.dcMontoPfrEntero > 0m)
                {
                    objDetalle.dcPorcentajeAbsorcion =
                        objBase.dcMontoPfrEntero / dcBaseAbsorcion;

                    objDetalle.dcAjusteWarren =
                        objDetalle.dcPorcentajeAbsorcion * dcAjusteTotal;

                    objDetalle.dcUnitarioExtraidoEntero =
                        objBase.dcLibrasPfrEntero > 0m
                            ? objDetalle.dcAjusteWarren / objBase.dcLibrasPfrEntero
                            : 0m;

                    objDetalle.dcMontoEnteroWarren =
                        objBase.dcMontoPfrEntero - objDetalle.dcAjusteWarren;

                    if (objDetalle.dcMontoEnteroWarren < -0.01m)
                        throw new InvalidOperationException(
                            $"Warren generó monto negativo en Entero para {objBase.strDescripcion}.");

                    decimal dcBaseLibrasCola = objBase.dcLibrasPfrCola > 0m
                        ? objBase.dcLibrasPfrCola
                        : dcLibrasCola;

                    if (objBase.dcLibrasPfrCola <= 0m)
                    {
                        objDetalle.dcLibrasCola = dcLibrasCola;
                        objDetalle.blUsaBaseGlobalCola = true;
                    }

                    objDetalle.dcUnitarioAgregadoCola = dcBaseLibrasCola > 0m
                        ? objDetalle.dcAjusteWarren / dcBaseLibrasCola
                        : 0m;

                    objDetalle.dcMontoColaWarren =
                        objBase.dcMontoPfrCola + objDetalle.dcAjusteWarren;
                }

                objDetalle.dcCostoUnitarioEnteroWarren =
                    objDetalle.dcLibrasEntero > 0m
                        ? objDetalle.dcMontoEnteroWarren / objDetalle.dcLibrasEntero
                        : 0m;

                objDetalle.dcCostoUnitarioColaWarren =
                    objDetalle.dcLibrasCola > 0m
                        ? objDetalle.dcMontoColaWarren / objDetalle.dcLibrasCola
                        : 0m;

                lstDetalle.Add(objDetalle);
            }

            decimal dcAjusteDistribuido = lstDetalle.Sum(x => x.dcAjusteWarren);
            if (Math.Abs(dcAjusteDistribuido - dcAjusteTotal) > 0.01m)
                throw new InvalidOperationException(
                    $"El ajuste Warren no fue distribuido completamente. Esperado: {dcAjusteTotal:N2}; distribuido: {dcAjusteDistribuido:N2}.");

            if (dcAjusteTotal > 0m)
            {
                decimal dcSumaPorcentaje = lstDetalle.Sum(x => x.dcPorcentajeAbsorcion);
                if (Math.Abs(dcSumaPorcentaje - 1m) > dcTolerancia)
                    throw new InvalidOperationException(
                        $"El porcentaje de absorción Warren no suma 100%. Resultado: {dcSumaPorcentaje:P4}.");
            }

            decimal dcUnitarioFinal =
                (dcDolaresColaActual + dcAjusteTotal) / dcLibrasCola;

            decimal dcObjetivoEsperado = dcAjusteTotal > 0m
                ? dcObjetivoWarren
                : dcCostoColaActual;

            if (Math.Abs(dcUnitarioFinal - dcObjetivoEsperado) > dcTolerancia)
                throw new InvalidOperationException(
                    $"Warren no alcanzó el objetivo. Objetivo: {dcObjetivoEsperado:N4}; resultado: {dcUnitarioFinal:N4}.");

            return new WarrenResultadoDto
            {
                dcObjetivoWarren = dcObjetivoWarren,
                dcCostoColaActual = dcCostoColaActual,
                dcDiferenciaWarren = dcDiferencia,
                dcAjusteTotal = dcAjusteTotal,
                dcBaseAbsorcionEntero = dcBaseAbsorcion,
                dcLibrasCola = dcLibrasCola,
                lstDetalle = lstDetalle
            };
        }
    }
}
