namespace CostManagement.Dominio.Reglas
{
    public static class PremioVigencia
    {
        // vigencia del premio por tipo: AQUA desde 2026-02-01 y VIVO desde 2026-05-01; ASC, BAP4 y demás siempre.
        public static bool EsVigente(string? strCertificado, DateOnly dtFecha) =>
            (strCertificado ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "AQUA" => dtFecha >= new DateOnly(2026, 2, 1),
                "VIVO" => dtFecha >= new DateOnly(2026, 5, 1),
                _ => true
            };
    }
}
