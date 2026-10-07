using Newtonsoft.Json;

namespace CostManagement.Aplicación.DTos
{
    public class GuardarWarrenRequest
    {
        [JsonProperty("anio")]
        public string strAnio { get; set; } = string.Empty;

        [JsonProperty("mes")]
        public string strMes { get; set; } = string.Empty;

        [JsonProperty("objetivoWarren")]
        public decimal dcObjetivoWarren { get; set; }

        [JsonProperty("usuario")]
        public string strUsuario { get; set; } = string.Empty;
    }

    public class WarrenProcesoDto
    {
        public int intCodigo { get; set; }
        public string strDescripcion { get; set; } = string.Empty;

        public decimal dcLibrasEntero { get; set; }
        public decimal dcLibrasCola { get; set; }

        public decimal dcMontoEnteroOriginal { get; set; }
        public decimal dcMontoColaOriginal { get; set; }

        public decimal dcPorcentajeAbsorcion { get; set; }
        public decimal dcAjusteWarren { get; set; }

        public decimal dcMontoEnteroWarren { get; set; }
        public decimal dcMontoColaWarren { get; set; }

        /// <summary>Costo unitario PFR original promedio de Entero para la etapa.</summary>
        public decimal dcCostoUnitarioEnteroOriginal { get; set; }
        /// <summary>Costo unitario PFR original promedio de Cola para la etapa.</summary>
        public decimal dcCostoUnitarioColaOriginal { get; set; }

        /// <summary>Valor unitario que Warren extrae de Entero en esta etapa.</summary>
        public decimal dcUnitarioExtraidoEntero { get; set; }
        /// <summary>Valor unitario que Warren agrega a Cola en esta etapa.</summary>
        public decimal dcUnitarioAgregadoCola { get; set; }

        public decimal dcCostoUnitarioEnteroWarren { get; set; }
        public decimal dcCostoUnitarioColaWarren { get; set; }

        /// <summary>
        /// Si el proceso no tenía base física de Cola pero recibe ajuste Warren
        /// (por ejemplo Retractilado en algunos períodos), la base de aplicación
        /// para Cola pasa a ser las libras totales PFR Cola del período.
        /// </summary>
        public bool blUsaBaseGlobalCola { get; set; }
    }

    public class WarrenResultadoDto
    {
        public decimal dcObjetivoWarren { get; set; }
        public decimal dcCostoColaActual { get; set; }
        public decimal dcDiferenciaWarren { get; set; }
        public decimal dcAjusteTotal { get; set; }
        public decimal dcBaseAbsorcionEntero { get; set; }
        public decimal dcLibrasCola { get; set; }
        public List<WarrenProcesoDto> lstDetalle { get; set; } = new();
    }
}
