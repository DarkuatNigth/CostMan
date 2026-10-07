using Newtonsoft.Json;

namespace CostManagement.Aplicación.DTos
{
    public sealed class CostoProductivoMantenimientoDto
    {
        [JsonProperty("etapas")]
        public List<EtapaCostoMantenimientoDto> lstEtapas { get; set; } = new List<EtapaCostoMantenimientoDto>();

        [JsonProperty("procesos")]
        public List<ProcesoCostoMantenimientoDto> lstProcesos { get; set; } = new List<ProcesoCostoMantenimientoDto>();

        [JsonProperty("tiposLote")]
        public List<TipoLoteMantenimientoDto> lstTiposLote { get; set; } = new List<TipoLoteMantenimientoDto>();

        [JsonProperty("tiposProceso")]
        public List<TipoProcesoMantenimientoDto> lstTiposProceso { get; set; } = new List<TipoProcesoMantenimientoDto>();

        [JsonProperty("matriz")]
        public List<AplicacionCostoMantenimientoDto> lstMatriz { get; set; } = new List<AplicacionCostoMantenimientoDto>();

        [JsonProperty("cuentas")]
        public List<CuentaCostoMantenimientoDto> lstCuentas { get; set; } = new List<CuentaCostoMantenimientoDto>();

        [JsonProperty("reglasDisponibles")]
        public List<ReglaCostoMantenimientoDto> lstReglasDisponibles { get; set; } =
            ReglaCostoMantenimientoDto.Catalogo();
    }

    public sealed class EtapaCostoMantenimientoDto
    {
        [JsonProperty("codigo")]
        public string strCodigo { get; set; } = string.Empty;

        [JsonProperty("nombre")]
        public string strNombre { get; set; } = string.Empty;

        [JsonProperty("orden")]
        public int intOrden { get; set; }

        [JsonProperty("activo")]
        public bool blActivo { get; set; }
    }

    public sealed class ProcesoCostoMantenimientoDto
    {
        [JsonProperty("codigo")]
        public string strCodigo { get; set; } = string.Empty;

        [JsonProperty("nombre")]
        public string strNombre { get; set; } = string.Empty;

        [JsonProperty("etapaCodigo")]
        public string strEtapaCodigo { get; set; } = string.Empty;

        [JsonProperty("etapa")]
        public string strEtapa { get; set; } = string.Empty;

        [JsonProperty("orden")]
        public int intOrden { get; set; }

        [JsonProperty("activo")]
        public bool blActivo { get; set; }

        [JsonProperty("enMatriz")]
        public bool blEnMatriz { get; set; }
    }

    public sealed class TipoLoteMantenimientoDto
    {
        [JsonProperty("codigo")]
        public string strCodigo { get; set; } = string.Empty;

        [JsonProperty("nombre")]
        public string strNombre { get; set; } = string.Empty;

        [JsonProperty("activo")]
        public bool blActivo { get; set; }
    }

    public sealed class TipoProcesoMantenimientoDto
    {
        [JsonProperty("codigo")]
        public string strCodigo { get; set; } = string.Empty;

        [JsonProperty("nombre")]
        public string strNombre { get; set; } = string.Empty;

        [JsonProperty("tipoLoteCodigo")]
        public string strTipoLoteCodigo { get; set; } = string.Empty;

        [JsonProperty("tipoLote")]
        public string strTipoLote { get; set; } = string.Empty;

        [JsonProperty("nivel")]
        public int intNivel { get; set; }

        [JsonProperty("activo")]
        public bool blActivo { get; set; }

        [JsonProperty("esNuevo")]
        public bool blEsNuevo { get; set; }
    }

    public sealed class AplicacionCostoMantenimientoDto
    {
        [JsonProperty("procesoCodigo")]
        public string strProcesoCodigo { get; set; } = string.Empty;

        [JsonProperty("tipoCodigo")]
        public string strTipoCodigo { get; set; } = string.Empty;

        [JsonProperty("regla")]
        public string strRegla { get; set; } = "NA";

        [JsonProperty("activo")]
        public bool blActivo { get; set; } = true;

        [JsonProperty("aplica")]
        public bool blAplica => ReglaCostoMantenimientoDto.EsAplicable(strRegla);

        [JsonProperty("esEspecial")]
        public bool blEsEspecial => ReglaCostoMantenimientoDto.EsEspecial(strRegla);
    }

    public sealed class CuentaCostoMantenimientoDto
    {
        [JsonProperty("id")]
        public int intId { get; set; }

        [JsonProperty("empresa")]
        public int intEmpresa { get; set; } = 1;

        [JsonProperty("etapaCodigo")]
        public string strEtapaCodigo { get; set; } = string.Empty;

        [JsonProperty("etapa")]
        public string strEtapa { get; set; } = string.Empty;

        [JsonProperty("centroCodigo")]
        public string strCentroCodigo { get; set; } = string.Empty;

        [JsonProperty("subcentroCodigo")]
        public string strSubcentroCodigo { get; set; } = string.Empty;

        [JsonProperty("cuenta")]
        public string strCuenta { get; set; } = string.Empty;

        [JsonProperty("activo")]
        public bool blActivo { get; set; }

        // Estos campos son de previsualización. No existen físicamente en
        // tb_configCuentaCosto; el C# los resuelve con la misma regla del SP.
        [JsonProperty("procesoResuelto")]
        public string strProcesoResuelto { get; set; } = string.Empty;

        [JsonProperty("driverResuelto")]
        public string strDriverResuelto { get; set; } = string.Empty;

        [JsonProperty("modoMonto")]
        public string strModoMonto { get; set; } = string.Empty;

        [JsonProperty("requiereCreditoRetiro")]
        public bool blRequiereCreditoRetiro { get; set; }

        [JsonProperty("esNuevo")]
        public bool blEsNuevo { get; set; }
    }

    public sealed class ReglaCostoMantenimientoDto
    {
        [JsonProperty("codigo")]
        public string strCodigo { get; set; } = string.Empty;

        [JsonProperty("descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [JsonProperty("aplica")]
        public bool blAplica { get; set; }

        [JsonProperty("especial")]
        public bool blEspecial { get; set; }

        public static List<ReglaCostoMantenimientoDto> Catalogo()
        {
            return new List<ReglaCostoMantenimientoDto>
            {
                new ReglaCostoMantenimientoDto { strCodigo = "SI",  strDescripcion = "Aplica normalmente", blAplica = true,  blEspecial = false },
                new ReglaCostoMantenimientoDto { strCodigo = "NO",  strDescripcion = "No aplica", blAplica = false, blEspecial = false },
                new ReglaCostoMantenimientoDto { strCodigo = "NA",  strDescripcion = "No aplica / no disponible", blAplica = false, blEspecial = true },
                new ReglaCostoMantenimientoDto { strCodigo = "TAR", strDescripcion = "Aplica por tarifa", blAplica = true, blEspecial = true },
                new ReglaCostoMantenimientoDto { strCodigo = "DEP", strDescripcion = "Depende de condición física", blAplica = true, blEspecial = true },
                new ReglaCostoMantenimientoDto { strCodigo = "PRO", strDescripcion = "Depende del proceso/congelamiento real", blAplica = true, blEspecial = true },
                new ReglaCostoMantenimientoDto { strCodigo = "FIC", strDescripcion = "Depende de ficha técnica", blAplica = true, blEspecial = true },
                new ReglaCostoMantenimientoDto { strCodigo = "D",   strDescripcion = "Regla especial heredada", blAplica = true, blEspecial = true },
                // Compatibilidad defensiva. El front no la propone, pero si existe
                // en la base no se destruye.
                new ReglaCostoMantenimientoDto { strCodigo = "X2",  strDescripcion = "Aplica con factor 2", blAplica = true, blEspecial = true }
            };
        }

        public static bool EsAplicable(string valor)
        {
            string normalizado = Normalizar(valor);
            return normalizado != "NO" && normalizado != "NA" && normalizado.Length > 0;
        }

        public static bool EsEspecial(string valor)
        {
            string normalizado = Normalizar(valor);
            return normalizado != "SI" && normalizado != "NO";
        }

        public static bool EsValida(string valor)
        {
            string normalizado = Normalizar(valor);
            return Catalogo().Any(x => x.strCodigo == normalizado);
        }

        public static string Normalizar(string valor)
        {
            return (valor ?? string.Empty).Trim().ToUpperInvariant();
        }
    }

    public sealed class GuardarCostoProductivoMantenimientoRequest
    {
        [JsonProperty("usuario")]
        public string strUsuario { get; set; } = string.Empty;

        [JsonProperty("tiposProceso")]
        public List<TipoProcesoMantenimientoDto> lstTiposProceso { get; set; } = new List<TipoProcesoMantenimientoDto>();

        [JsonProperty("matriz")]
        public List<AplicacionCostoMantenimientoDto> lstMatriz { get; set; } = new List<AplicacionCostoMantenimientoDto>();

        [JsonProperty("cuentas")]
        public List<CuentaCostoMantenimientoDto> lstCuentas { get; set; } = new List<CuentaCostoMantenimientoDto>();
    }

    /// <summary>
    /// Snapshot de configuración que se carga una sola vez por ejecución de
    /// costeo y se entrega a MotorProcesoParametro.
    /// </summary>
    public sealed class ConfiguracionCostoProductivoRuntimeDto
    {
        public List<ProcesoCostoMantenimientoDto> lstProcesos { get; set; } = new List<ProcesoCostoMantenimientoDto>();
        public List<ProcesoCostoAplicacionDto> lstAplicaciones { get; set; } = new List<ProcesoCostoAplicacionDto>();
        public List<TipoProcesoNivelRuntimeDto> lstNiveles { get; set; } = new List<TipoProcesoNivelRuntimeDto>();
    }

    public sealed class TipoProcesoNivelRuntimeDto
    {
        public string strCodigo { get; set; } = string.Empty;
        public int intNivel { get; set; }
    }
}
