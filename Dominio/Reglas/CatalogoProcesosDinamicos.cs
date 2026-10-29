using CostManagement.Aplicación.DTos;
namespace CostManagement.Dominio.Reglas
{
    /// <summary>decide qué procesos del catálogo se costean por el camino genérico.</summary>
    public static class CatalogoProcesosDinamicos
    {
        public const string CodigoPremio = "PMIO";   // control de cuadre del premio; no es proceso dinámico
        private static readonly HashSet<string> _hshConocidos = new(StringComparer.OrdinalIgnoreCase)
        { "LOG","REC","CLA","COD","CAJ","DES","PEL","HID","COC","DSC","DEC","RET","BRI","IQF","TUN","CDF","CDV","CIF","CIV","MPE","COP","TAR","EXC","PMIO" };
        private static readonly HashSet<string> _hshEtapasWarren = new(StringComparer.OrdinalIgnoreCase) { "PP", "CD", "CI" };

        public static bool EsConocido(string? strCodigo) => !string.IsNullOrWhiteSpace(strCodigo) && _hshConocidos.Contains(strCodigo.Trim());

        public static bool EsDinamico(ProcesoCostoMantenimientoDto? objProceso) =>
            objProceso != null && objProceso.blActivo && !objProceso.blEsTarifa && !objProceso.blEsFichaTecnica && !objProceso.blSinProceso
            && string.Equals(objProceso.strModoValor, TipoValorProceso.Calculado, StringComparison.OrdinalIgnoreCase)
            && !string.Equals((objProceso.strEtapaCodigo ?? string.Empty).Trim(), "DM", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(objProceso.strCodigo) && !EsConocido(objProceso.strCodigo);

        public static bool ParticipaWarren(ProcesoCostoMantenimientoDto? objProceso) =>
            objProceso != null && _hshEtapasWarren.Contains((objProceso.strEtapaCodigo ?? string.Empty).Trim());
    }
}
