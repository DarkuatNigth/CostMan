using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;

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
}
