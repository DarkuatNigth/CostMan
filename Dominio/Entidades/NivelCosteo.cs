namespace CostManagement.Dominio.Entidades
{
    public static class NivelCosteo
    {
        private static readonly IReadOnlyDictionary<string, int> _mapaDeNivel =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
            // NV2 — Lote raíz sin dependencias
            { "B1",   2 }, { "B3",   2 }, { "DE",   2 }, { "R7",   2 },
            { "R6",   2 }, { "EN1",  2 }, { "UNI",  2 }, { "CDI",  2 },

            // NV3 — Primera cadena dependiente
            { "R1",   3 }/*, { "CDI",  3 }*/, { "R2",   3 }, { "R3",   3 },
            { "RS",   3 }, { "REC",  3 }, { "SH2",  3 }, { "CAM",  3 },
            { "BP",   3 },

            // NV4 — Segunda cadena dependiente / cierre
            { "EPP",  4 }, { "BD",   4 }, { "ENTER",4 },
            { "EZP",  4 }, { "VF",   4 }, { "P4",   4 }, { "PYD",  4 },
            { "PYD4", 4 }, { "P3",   4 }, { "PYDS", 4 }, { "PYDTO",4 },
            { "PYD1", 4 }, { "EZ",   4 },

            // NV5 — Cadenas profundas / saldos finales
            { "LB04", 5 }, { "DV",   5 }, { "RVVL", 5 }, { "BDP",  5 },
            { "VE",   5 }, { "VR",   5 }, { "RLL",  5 }, { "RPY", 5 },
            };

        // Códigos que solo se procesan como especiales en NV2
        public static readonly IReadOnlyCollection<string> TipCodsEspecialesNV2 =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UNI", "R7", "CDI" };

        /// <summary>Devuelve el nivel de costeo para un strTipCod dado.
        /// Si no está en el mapa retorna 2 (comportamiento conservador).</summary>
        public static int ObtenerNivel(string tipCod)
            => !string.IsNullOrWhiteSpace(tipCod) && _mapaDeNivel.TryGetValue(tipCod.Trim(), out int nv)
                ? nv : 2;

        /// <summary>Etiqueta textual del nivel: "NV2", "NV3", etc.</summary>
        public static string EtiquetaNivel(int nivel) => $"NV{nivel}";

        /// <summary>Indica si el tipCod es un proceso especial exclusivo de NV2.</summary>
        public static bool EsEspecialNV2(string tipCod)
            => !string.IsNullOrWhiteSpace(tipCod) && TipCodsEspecialesNV2.Contains(tipCod.Trim());

        /// <summary>Devuelve todos los tipCod que pertenecen a un nivel.</summary>
        public static IEnumerable<string> TipCodsDeNivel(int nivel)
            => _mapaDeNivel.Where(kv => kv.Value == nivel).Select(kv => kv.Key);

  
        /// <summary>
        /// Retorna true SOLO si el tipCod tiene método especial de asignación (UNI, R7)
        /// Y su nivel propio en el mapa es 2.
        /// CDI es especial en el sentido de que usa AsignarCostoReprocesoColaDir,
        /// pero su nivel es 3 → en esta versión CDI usa prorrateo normal (más correcto).
        /// Si se quiere que CDI mantenga su método especial, cambiar la condición.
        /// </summary>
        public static bool AplicaMetodoEspecial(string tipCod)
        {
            if (string.IsNullOrWhiteSpace(tipCod)) return false;
            string t = tipCod.Trim().ToUpper();
            List<string> lstEspecialesNV2 = new List<string> { "UNI", "R7" , "CDI" };
            // Solo UNI y R7 tienen nivel 2 Y método especial
            // CDI (nivel 3) se excluye aquí → usa prorrateo estándar desde NV3
            return lstEspecialesNV2.Contains(t) && ObtenerNivel(t) == 2;
        }
        public static int DeterminarNivelLote(
                LoteRpcKeyXSec lote,
                ILookup<LoteRpcKeyXSec, MatPrimaReproceso> indiceXLote)
        {
            var niveles = indiceXLote[lote]
                .Where(x => x.strAgrupacion == "2. PROCESADO")
                .Select(x => NivelCosteo.ObtenerNivel(x.strTipCod))
                .ToList();

            return niveles.Any() ? niveles.Max() : 2;
        }
    }
}
