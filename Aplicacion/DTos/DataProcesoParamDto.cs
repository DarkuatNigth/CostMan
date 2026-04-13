using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class DataProcesoParamDto
    {
        [JsonProperty("codigo")]
        [Column("codigo")]
        public int intId { get; set; }

        [JsonProperty("descripcion")]
        [Column("descripcion")]
        public string strDescripcion { get; set; }

        [JsonProperty("tipoData")]
        [Column("tipoData")]
        public string strTipoData { get; set; }



    }
}
