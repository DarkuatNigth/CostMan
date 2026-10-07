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

        // Retractilado se calcula por tarifa (InfoProd.dcCostoRetrac / tb_tarifaDecoradosRetractilado)
        // en MotorProcesoParametro y es exactamente lo que ve el usuario en Table/Table1.
        // Si se usara el monto de la cuenta contable (como el resto de procesos CONTABLE),
        // se persistiría un valor distinto al que la fila mostró, rompiendo
        // "lo que veo en la fila = lo que guardo". Decorado no necesita esta excepción
        // porque su tarifa ya cuadra con el monto contable.
        private static readonly HashSet<string> _usaValorTarifaPropia =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "RETRACTILADO"
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

            decimal dcCostoUnitarioDescabezado = CalcularCostoUnitarioConsolidado(resumen, "Descabezado");

            AgregarOrigen(
                salida,
                objData.lstProcesoFrs ?? new List<ProcesoResultadoDto>(),
                resumen,
                "PFR",
                dcCostoUnitarioDescabezado);

            AgregarOrigen(
                salida,
                objData.lstProcesoRpc ?? new List<ProcesoResultadoDto>(),
                resumen,
                "RPC",
                dcCostoUnitarioDescabezado);

            CostoProductivoProcesoOrigenDto? logistica = resumen.FirstOrDefault(x =>
                Normalizar(x.strOrigen) == "PFR" &&
                Normalizar(x.strProceso) == "LOGISTICA");

            if (logistica == null || logistica.dcTotalDolares <= 0m)
            {
                throw new InvalidOperationException(
                    "El cierre no contiene el proceso Logística PFR con dólares distribuidos.");
            }

            List<ProcesoResultadoDto> lstDescabezado = salida.Where(x => Normalizar(x.strDescripcion) == "DESCABEZADO").ToList();
            if (lstDescabezado.Count > 0)
            {
                decimal cuMax = lstDescabezado.Max(x => x.dcCostUnitario ?? 0m);
                decimal cuMin = lstDescabezado.Min(x => x.dcCostUnitario ?? 0m);
                if (cuMax - cuMin > 0.00001m)
                    throw new InvalidOperationException(
                        "Descabezado PFR/RPC no comparte el mismo costo unitario consolidado.");
            }

            // BLOQUE TEMPORAL DE DEBUG (DESCABEZADO CU CONSOLIDADO) — mantener comentado.
            //var lstDebugSalidaDes = salida.Where(x => Normalizar(x.strDescripcion) == "DESCABEZADO")
            //    .Select(x => new { x.strTipoLote, x.dcValor, x.dcLibras, x.dcCostUnitario }).ToList();

            return salida;
        }

        private static void AgregarOrigen(
            List<ProcesoResultadoDto> salida,
            IEnumerable<ProcesoResultadoDto> catalogo,
            IReadOnlyCollection<CostoProductivoProcesoOrigenDto> resumen,
            string origen,
            decimal dcCostoUnitarioDescabezado)
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

                bool blHidratacionRpc =
                    origen == "RPC" && descripcion == "HIDRATACION";

                bool blUsaTarifaPropia =
                    _usaValorTarifaPropia.Contains(descripcion);

                decimal valor;
                decimal libras;

                if (blHidratacionRpc)
                {
                    // Table1 ya contiene CONTABLE + QUÍMICOS para la consulta actual.
                    valor = src.dcValor;
                    libras = src.dcLibras;
                }
                else if (blUsaTarifaPropia)
                {
                    // Usar exactamente el valor de tarifa que ya vio el usuario en
                    // Table/Table1, no el monto contable (ver comentario en
                    // _usaValorTarifaPropia).
                    valor = src.dcValor;
                    libras = src.dcLibras;
                }
                else if (monto != null)
                {
                    monto.Recalcular();
                    valor = monto.dcTotalDolares;
                    libras = monto.dcTotalLibras;
                }
                else
                {
                    // Procesos técnicos/tarifa que no nacen de cuenta contable.
                    // src NO es un snapshot histórico: es Table/Table1 del mismo GET.
                    libras = src.dcLibras;
                    decimal dcUnitarioSnapshot = src.dcCostUnitario ?? 0m;
                    valor = src.dcValor != 0m
                        ? src.dcValor
                        : Math.Round(dcUnitarioSnapshot * libras, 5);
                }

                decimal unitario = descripcion == "DESCABEZADO"
                    ? dcCostoUnitarioDescabezado
                    : (libras > 0m ? valor / libras : 0m);

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

        private static decimal CalcularCostoUnitarioConsolidado(
            IReadOnlyCollection<CostoProductivoProcesoOrigenDto> resumen,
            string strProceso)
        {
            string proceso = Normalizar(strProceso);
            List<CostoProductivoProcesoOrigenDto> filas = resumen.Where(x => Normalizar(x.strProceso) == proceso).ToList();
            decimal libras = filas.Sum(x => x.dcTotalLibras);
            decimal dolares = filas.Sum(x => x.dcTotalDolares);
            return libras > 0m ? dolares / libras : 0m;
        }
    }
}
