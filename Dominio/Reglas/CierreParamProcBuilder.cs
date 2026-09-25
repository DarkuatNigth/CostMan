using CostManagement.Aplicación.DTos;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Convierte el resumen por origen generado por CostoProductivo en los
    /// registros PFR/RPC que se almacenan en tb_parametroCosteo.
    /// </summary>
    public static class CierreParamProcBuilder
    {
        private static readonly HashSet<string> _soloPfr =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "LOGISTICA",
                "RECEPCION",
                "CLASIFICACION"
            };

        private static readonly HashSet<string> _noPersistir =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "MATERIAL EMPAQUE"
            };

        public static List<ProcesoResultadoDto> ConstruirParametros(
            DataProcesoParam objData,
            IEnumerable<CostoProductivoProcesoOrigenDto> lstResumenOrigen)
        {
            ArgumentNullException.ThrowIfNull(objData);

            List<CostoProductivoProcesoOrigenDto> resumen =
                (lstResumenOrigen ?? Array.Empty<CostoProductivoProcesoOrigenDto>())
                .Select(x =>
                {
                    x.Recalcular();
                    return x;
                })
                .ToList();

            List<ProcesoResultadoDto> salida = new();

            AgregarOrigen(
                salida,
                objData.lstProcesoFrs ?? new List<ProcesoResultadoDto>(),
                resumen,
                "PFR");

            AgregarOrigen(
                salida,
                objData.lstProcesoRpc ?? new List<ProcesoResultadoDto>(),
                resumen,
                "RPC");

            CostoProductivoProcesoOrigenDto? logistica = resumen.FirstOrDefault(x =>
                Normalizar(x.strOrigen) == "PFR" &&
                Normalizar(x.strProceso) == "LOGISTICA");

            if (logistica == null || logistica.dcTotalDolares <= 0m)
            {
                throw new InvalidOperationException(
                    "El cierre no contiene el proceso Logística PFR con dólares distribuidos.");
            }

            return salida;
        }

        private static void AgregarOrigen(
            List<ProcesoResultadoDto> salida,
            IEnumerable<ProcesoResultadoDto> catalogo,
            IReadOnlyCollection<CostoProductivoProcesoOrigenDto> resumen,
            string origen)
        {
            foreach (ProcesoResultadoDto src in catalogo)
            {
                string descripcion = Normalizar(src.strDescripcion);
                if (src.intCodigo <= 0 || string.IsNullOrWhiteSpace(descripcion))
                    continue;

                if (_noPersistir.Contains(descripcion))
                    continue;

                // LOG/REC/CLA son exclusivamente PFR.
                if (origen == "RPC" && _soloPfr.Contains(descripcion))
                    continue;

                CostoProductivoProcesoOrigenDto? monto = resumen.FirstOrDefault(x =>
                    Normalizar(x.strOrigen) == origen &&
                    Normalizar(x.strProceso) == descripcion);

                // El cierre se reconstruye desde la tabla intermedia contable.
                // No se arrastran montos del snapshot anterior: si el proceso no
                // recibió dólares en el costo productivo actual, se persiste en 0.
                decimal valor = monto?.dcTotalDolares ?? 0m;
                decimal libras = monto?.dcTotalLibras ?? 0m;
                decimal unitario = libras > 0m
                    ? valor / libras
                    : 0m;

                salida.Add(new ProcesoResultadoDto
                {
                    intCodigo = src.intCodigo,
                    intCodDet = src.intCodDet,
                    strEstado = "CE",
                    strCodTip = src.strCodTip,
                    blEditable = src.blEditable,
                    strTipoLote = origen,
                    strDescripcion = src.strDescripcion,
                    dcValor = Math.Round(valor, 5),
                    dcLibras = Math.Round(libras, 5),
                    dcCostUnitario = Math.Round(unitario, 5)
                });
            }
        }

        private static string Normalizar(string? valor)
        {
            return (valor ?? string.Empty)
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
