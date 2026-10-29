using CostManagement.Aplicación.DTos;

namespace CostManagement.Dominio.Reglas
{
    public static class ProcesoCatalogoParamBuilder
    {
        // MPE/COP/TAR/EXC ya tienen bloques propios fuera de procesos base.
        private static readonly HashSet<string> _hshFueraDelBloque = new(StringComparer.OrdinalIgnoreCase)
        { "MPE", "COP", "TAR", "EXC" };

        public static List<ProcesoCatalogoParamDto> Construir(IEnumerable<ProcesoCostoMantenimientoDto>? lstCatalogo) =>
            (lstCatalogo ?? Array.Empty<ProcesoCostoMantenimientoDto>())
                .Where(x => x != null && x.blActivo && !x.blEsTarifa && !x.blSinProceso && !x.blEsFichaTecnica
                    && !string.IsNullOrWhiteSpace(x.strCodigo)
                    && string.Equals(x.strModoValor, TipoValorProceso.Calculado, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(x.strEtapaCodigo?.Trim(), "DM", StringComparison.OrdinalIgnoreCase)
                    && !_hshFueraDelBloque.Contains(x.strCodigo.Trim()))
                .Select(x => new ProcesoCatalogoParamDto
                {
                    strPcCodigo = x.strCodigo.Trim().ToUpperInvariant(), strNombre = x.strNombre.Trim(),
                    strEtapaCodigo = (x.strEtapaCodigo ?? string.Empty).Trim().ToUpperInvariant(),
                    intOrden = x.intOrden, blConocido = CatalogoProcesosDinamicos.EsConocido(x.strCodigo)
                })
                .OrderBy(x => x.strEtapaCodigo, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.intOrden).ThenBy(x => x.strPcCodigo, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
