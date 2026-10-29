using Newtonsoft.Json;

namespace CostManagement.Aplicación.DTos
{
    /// <summary>Catálogo de modos de valor de un proceso de ParamProc (tb_procesoCosteo).</summary>
    public static class TipoValorProceso
    {
        public const string Calculado = "CALCULADO";
        public const string DolaresTotales = "DOLARES_TOTALES";
        public const string Tarifa = "TARIFA";

        public static bool EsValido(string valor) =>
            valor == Calculado || valor == DolaresTotales || valor == Tarifa;
    }

    // RUN-002 v4: ya no hay endpoints propios de ParamProc.
    // public sealed class ProcesoParamMantenimientoDto
    // {
        // [JsonProperty("id")] public int intId { get; set; }
        // [JsonProperty("empresa")] public int intEmpresa { get; set; } = 1;
        // [JsonProperty("descripcion")] public string strDescripcion { get; set; } = string.Empty;
        // [JsonProperty("activo")] public bool blActivo { get; set; } = true;
        // [JsonProperty("editable")] public bool blEditable { get; set; }
        // [JsonProperty("tipCodigo")] public string? strTipCodigo { get; set; }
        // [JsonProperty("modoValor")] public string strModoValor { get; set; } = TipoValorProceso.Calculado;
        // [JsonProperty("esTarifa")] public bool blEsTarifa => strModoValor == TipoValorProceso.Tarifa;
        // [JsonProperty("esNuevo")] public bool blEsNuevo { get; set; }
    // }
//
    // public sealed class ProcesosParamMantenimientoDto
    // {
        // [JsonProperty("procesos")]
        // public List<ProcesoParamMantenimientoDto> lstProcesos { get; set; } = new List<ProcesoParamMantenimientoDto>();
    // }
//
    // public sealed class GuardarProcesosParamMantenimientoRequest
    // {
        // [JsonProperty("usuario")] public string strUsuario { get; set; } = string.Empty;
        // [JsonProperty("procesos")]
        // public List<ProcesoParamMantenimientoDto> lstProcesos { get; set; } = new List<ProcesoParamMantenimientoDto>();
    // }
}
