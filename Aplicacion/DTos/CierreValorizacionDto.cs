using CostManagement.Infraestructura.EF_Core;
using Newtonsoft.Json;

namespace CostManagement.Aplicación.DTos
{
    public sealed class GuardarCierreValorizacionRequest
    {
        [JsonProperty("anio")]
        public string strAnio { get; set; } = string.Empty;
        [JsonProperty("mes")]
        public string strMes { get; set; } = string.Empty;
        [JsonProperty("usuario")]
        public string strUsuario { get; set; } = string.Empty;
    }

    public sealed class CierreValorizacionResultadoDto
    {
        public CierreValorizacionResumenDto objResumen { get; set; } = new();
    }

    public sealed class CierreValorizacionContexto
    {
        public List<ProcesoResultadoDto> lstProcesoFrs { get; set; } = new();
        public List<ProcesoResultadoDto> lstProcesoRpc { get; set; } = new();
        public List<CostoProcesoParticionDto> lstCostoProcesoParticion { get; set; } = new();
        public List<ProcesoResultadoDto> lstParametrosWarren { get; set; } = new();
        public bool blIgnorarCache { get; set; } = true;
        // una configuración por cierre, compartida por fresco y reproceso.
        public ConfiguracionCostoProductivoRuntimeDto? objConfiguracion { get; set; }
        public int intFrescoEsperados { get; set; }
        public int intReproEsperados { get; set; }
        public Dictionary<string, string> dicEtapas { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> lstAdvertencias { get; set; } = new();

    }

    public sealed class CierreValorizacionDto
    {
        // identidad de los parámetros leídos; no forma parte del contrato HTTP.
        [JsonIgnore]
        public string strVersionParametros { get; set; } = string.Empty;
        public List<CierreFrescoLineaDto> lstFresco { get; set; } = new();
        public List<CierreReproLineaDto> lstRepro { get; set; } = new();
        public CierreValorizacionResumenDto objResumen { get; set; } = new();
        public List<string> lstAdvertencias { get; set; } = new();
    }

    public sealed class CierreFrescoLineaDto
    {
        public TbMateriaPrimaFrescoValorizada objCabecera { get; set; } = new();
        public List<CierreDetalleDto> lstDetalles { get; set; } = new();
    }

    public sealed class CierreReproLineaDto
    {
        public TbMateriaPrimaReproValorizada objCabecera { get; set; } = new();
        public List<CierreDetalleDto> lstDetalles { get; set; } = new();
    }

    public sealed class CierreDetalleDto
    {
        #region Identificación de tarifa
        // un código de componente nunca identifica tarifa por coincidencia con toCodigo.
        internal static string NormalizarCodigo(string? strCodigo) => (strCodigo ?? string.Empty).Trim().ToUpperInvariant();

        internal static bool EsComponente(string strCodigo) => NormalizarCodigo(strCodigo) is
            "LOG" or "REC" or "CLA" or "CAJ" or "DEC" or "RET" or "BRI" or "IQF" or "TUN" or
            "DES" or "PEL" or "HID" or "COC" or "CDV" or "CDF" or "CIV" or "CIF" or "COP" or "DSC";

        internal static bool EsTarifa(string strCodigo, string? strTipo)
        {
            // un dinámico no es tarifa mientras su código no coincida con el tipo de lote.
            // La configuración impide esa colisión; no se amplía la lista fija de componentes.
            string strPc = NormalizarCodigo(strCodigo), strTo = NormalizarCodigo(strTipo);
            return strPc == "TAR" || (strPc.Length > 0 && strPc == strTo && !EsComponente(strTo));
        }
        #endregion

        public string strEtapaCodigo { get; set; } = string.Empty;
        public string strParticion { get; set; } = string.Empty;
        public string strPcCodigo { get; set; } = string.Empty;
        public string strToCodigo { get; set; } = string.Empty;
        public decimal dcLibras { get; set; }
        public decimal dcCostoUnitario { get; set; }
        public decimal dcCostoTotal { get; set; }
    }

    public sealed class CierreValorizacionResumenDto
    {
        [JsonProperty("estado")]
        public string strEstado { get; set; } = string.Empty;

        [JsonProperty("frescoGuardados")]
        public int intFrescoGuardados { get; set; }

        [JsonProperty("frescoNuevos")]
        public int intFrescoNuevos { get; set; }

        [JsonProperty("frescoActualizados")]
        public int intFrescoActualizados { get; set; }

        [JsonProperty("frescoAnulados")]
        public int intFrescoAnulados { get; set; }

        [JsonProperty("frescoDetalles")]
        public int intFrescoDetalles { get; set; }

        [JsonProperty("reproGuardados")]
        public int intReproGuardados { get; set; }

        [JsonProperty("reproNuevos")]
        public int intReproNuevos { get; set; }

        [JsonProperty("reproActualizados")]
        public int intReproActualizados { get; set; }

        [JsonProperty("reproAnulados")]
        public int intReproAnulados { get; set; }

        [JsonProperty("reproDetalles")]
        public int intReproDetalles { get; set; }

        [JsonProperty("detallesAnulados")]
        public int intDetallesAnulados { get; set; }

        [JsonProperty("warrenGuardado")]
        public bool blWarrenGuardado { get; set; }

        [JsonProperty("advertencias")]
        public List<string> lstAdvertencias { get; set; } = new();
    }
}
