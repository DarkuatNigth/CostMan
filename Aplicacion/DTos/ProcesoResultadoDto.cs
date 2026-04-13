using CostManagement.Dominio.Entidades;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class ProcesoResultadoDto
    {
        [Column("Codigo")]
        [JsonProperty("Codigo")]
        public int intCodigo { get; set; }

        [JsonIgnore]
        public int intCodDet { get; set; }

        [JsonIgnore]
        public string? strEstado { get; set; }

        [Column("TipLote")]
        [JsonProperty("TipLote")]
        public string? strTipoLote { get; set; }

        [Column("Descripcion")]
        [JsonProperty("Descripcion")]   
        public string strDescripcion { get; set; }

        [Column("Valor")]
        [JsonProperty("Valor")]
        public decimal dcValor { get; set; }  // El ISNULL(pu_valor, 0)

        [Column("Libras_Proceso")]
        [JsonProperty("Libras_Proceso")]
        public decimal dcLibras { get; set; }

        [Column("Costo_Unitario")]
        [JsonProperty("Costo_Unitario")]
        public decimal dcCostUnitario { get; set; }


        /// <summary>Costo promedio ponderado por prod+talla — fuente FRS (para exportaciones).</summary>
        public static ConcurrentDictionary<string, decimal> ConstruirDictProc(
            List<ProcesoResultadoDto> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<string, decimal>();

            return new ConcurrentDictionary<string, decimal>(
                lst
                    .Where(p => !string.IsNullOrWhiteSpace(p.strDescripcion))
                .ToDictionary(
                        p => p.strDescripcion.Trim(),
                        p => p.dcCostUnitario)
                );
        }
    }

    public class GuardarParametrosRequest
    {
        [JsonProperty("anio")]
        public string strAnio { get; set; }

        [JsonProperty("mes")]
        public string strMes { get; set; }

        [JsonProperty("usuario")]
        public string strUsuario { get; set; }

        [JsonProperty("valores")]
        public List<ProcesoResultadoDto> LstValores { get; set; }


    }
}
