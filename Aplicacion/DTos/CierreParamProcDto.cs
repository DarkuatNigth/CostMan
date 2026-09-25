using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    /// <summary>
    /// Request del cierre único de ParamProc.
    /// El backend recalcula el snapshot canónico; el front no envía importes editables.
    /// </summary>
    public sealed class GuardarCierreParamProcRequest
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

    /// <summary>
    /// Dólares y libras de un proceso separados por origen PFR/RPC.
    /// Se construye desde los drivers del costo productivo, no desde el front.
    /// </summary>
    public sealed class CostoProductivoProcesoOrigenDto
    {
        public string strPcCodigo { get; set; } = string.Empty;
        public string strProceso { get; set; } = string.Empty;
        public string strOrigen { get; set; } = string.Empty;

        public decimal dcLibrasEntero { get; set; }
        public decimal dcLibrasCola { get; set; }
        public decimal dcLibrasValorAgregado { get; set; }

        public decimal dcDolaresEntero { get; set; }
        public decimal dcDolaresCola { get; set; }
        public decimal dcDolaresValorAgregado { get; set; }

        public decimal dcTotalLibras { get; set; }
        public decimal dcTotalDolares { get; set; }
        public decimal dcCostoUnitario { get; set; }

        public void Recalcular()
        {
            dcTotalLibras = Math.Round(
                dcLibrasEntero + dcLibrasCola + dcLibrasValorAgregado,
                5);

            dcTotalDolares = Math.Round(
                dcDolaresEntero + dcDolaresCola + dcDolaresValorAgregado,
                5);

            dcCostoUnitario = dcTotalLibras > 0m
                ? Math.Round(dcTotalDolares / dcTotalLibras, 5)
                : 0m;
        }
    }

    /// <summary>
    /// Snapshot resumido que permite aplicar unitarios diferentes por partición
    /// a LiquidacionResultado para LOG/REC/CLA.
    /// </summary>
    public sealed class CostoProcesoParticionDto
    {
        [Column("ProcesoCodigo")]
        public string strPcCodigo { get; set; } = string.Empty;

        [Column("Proceso")]
        public string strProceso { get; set; } = string.Empty;

        [Column("DolaresEntero")]
        public decimal dcDolaresEntero { get; set; }

        [Column("DolaresCola")]
        public decimal dcDolaresCola { get; set; }

        [Column("DolaresValorAgregado")]
        public decimal dcDolaresValorAgregado { get; set; }
    }

    /// <summary>Table7 del endpoint param-proc.</summary>
    public sealed class WarrenPeriodoResumenDto
    {
        public bool blExisteWarren { get; set; }
        public decimal dcObjetivoWarren { get; set; }
        public decimal dcCostoColaActual { get; set; }
        public decimal dcDiferenciaWarren { get; set; }
        public decimal dcAjusteTotal { get; set; }
        public decimal dcBaseAbsorcionEntero { get; set; }
        public decimal dcLibrasCola { get; set; }

        public static WarrenPeriodoResumenDto Desde(
            WarrenResultadoDto? resultado)
        {
            if (resultado == null)
                return new WarrenPeriodoResumenDto { blExisteWarren = false };

            return new WarrenPeriodoResumenDto
            {
                blExisteWarren = true,
                dcObjetivoWarren = resultado.dcObjetivoWarren,
                dcCostoColaActual = resultado.dcCostoColaActual,
                dcDiferenciaWarren = resultado.dcDiferenciaWarren,
                dcAjusteTotal = resultado.dcAjusteTotal,
                dcBaseAbsorcionEntero = resultado.dcBaseAbsorcionEntero,
                dcLibrasCola = resultado.dcLibrasCola
            };
        }
    }

    public sealed class CierreParamProcResultadoDto
    {
        public CostoProductivoResultadoDto objCostoProductivo { get; set; } = new();
        public List<ProcesoResultadoDto> lstParametrosPfr { get; set; } = new();
        public List<ProcesoResultadoDto> lstParametrosRpc { get; set; } = new();
        public WarrenResultadoDto objWarren { get; set; } = new();
    }
}
