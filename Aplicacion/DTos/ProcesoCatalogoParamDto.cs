using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    // contrato de Table10 para las filas dinámicas de ParamProc.
    public sealed class ProcesoCatalogoParamDto
    {
        // AListaDeDiccionarios usa Column; JsonProperty conserva el contrato al serializar el DTO directamente.
        [Column("pcCodigo")]
        [JsonProperty("pcCodigo")] public string strPcCodigo { get; set; } = string.Empty;
        [Column("nombre")]
        [JsonProperty("nombre")] public string strNombre { get; set; } = string.Empty;
        [Column("etapaCodigo")]
        [JsonProperty("etapaCodigo")] public string strEtapaCodigo { get; set; } = string.Empty;
        [Column("orden")]
        [JsonProperty("orden")] public int intOrden { get; set; }
        [Column("conocido")]
        [JsonProperty("conocido")] public bool blConocido { get; set; }
    }
}
