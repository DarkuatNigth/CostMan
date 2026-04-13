using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CostManagementService.Aplicación.DTos
{
    public class RequestMatPrimDto
    {
        [JsonProperty("fechaInicio")]
        public DateOnly dtFechaInicio { get; set; }

        [JsonProperty("fechaFin")]
        public DateOnly dtFechaFin { get; set; }

        [JsonProperty("usuarioCrea")]
        public string strUsuarioCrea { get; set; }

        [JsonProperty("lstMatPrim")]
        public List<MatPrimDto> lstMatPrim { get; set; }
    }

    public class MatPrimDto
    {
        [JsonProperty("Lote")]
        public int intLote { get; set; }

        [JsonProperty("CodProd")]
        public string strCodProd { get; set; }

        [JsonProperty("Talla")]
        public string strTalla { get; set; }

        [JsonProperty("Libras")]
        public decimal dcLibras { get; set; }

        [JsonProperty("valorNuevo")]
        public decimal dcValorNuevo { get; set; }

        [JsonProperty("razon")]
        public string strMotivo { get; set; }
    }
}
