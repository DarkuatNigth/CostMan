using CostManagement.Aplicación.DTos;
using Newtonsoft.Json;

namespace CostManagementService.Aplicación.DTos
{
    public class RequestDataDto
    {
        [JsonProperty("InvVal")]
        public List<InvValDataDto> lstInvVal { get; set; }
        
        [JsonProperty("TieneFechCort")]
        public bool blFechaCorte { get; set; }
        
        [JsonProperty("FechaCorte")]
        public DateOnly dtFechaCorte { get; set; }

        [JsonProperty("Accion")]
        public string strAccion { get; set; }

    }
}
