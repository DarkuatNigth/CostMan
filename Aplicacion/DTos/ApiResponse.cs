using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CostManagement.Aplicación.DTos
{
    public class ApiResponse<T>
    {

        [JsonProperty("Codigo")]
        public bool blStatus { get; set; }


        [JsonProperty("Description")]
        public string strMensaje { get; set; }


        [JsonProperty("Dt")]
        public T objData { get; set; }
    }
    public class RangoFechasRequest
    {
        [Required]
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public DateTime dtFechaInicio { get; set; }

        [Required]
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public DateTime dtFechaFin { get; set; }
    }
    public sealed class MaterialEmpaqueAuditoriaRequest : RangoFechasRequest
    {
        public string? strProducto { get; set; }
        public int? intLote { get; set; }
        public bool blSoloErrores { get; set; } = false;
    }

    public sealed class DiagnosticoDisponibleLoteRequest : RangoFechasRequest
    {
        [Required]
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public int intLote { get; set; }

        [Required]
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public int intCodProd { get; set; }

        [Required]
        public string strTalla { get; set; } = string.Empty;
    }
}
