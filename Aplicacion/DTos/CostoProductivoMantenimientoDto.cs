using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CostManagement.Aplicación.DTos
{
    public sealed class CostoProductivoMantenimientoDto
    {
        // tipos activos de PRODUCCION pendientes de configurar.
        [JsonProperty("tiposProcesoDisponibles")]
        public List<TipoProcesoDisponibleDto> lstTiposProcesoDisponibles { get; set; } = new List<TipoProcesoDisponibleDto>();

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
        // identificar etapas de diarios.
        [JsonProperty("esDiario")] public bool blEsDiario { get; set; }

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
        // uso explícito; vacío en requests anteriores se infiere por etapa.
        [JsonProperty("esDiario")] public bool blEsDiario { get; set; }
        [JsonProperty("uso")] public string strUso { get; set; } = string.Empty;

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

        [JsonProperty("cantidadCuentas")]
        public int intCantidadCuentas { get; set; }

        [JsonProperty("esNuevo")]
        public bool blEsNuevo { get; set; }

        [JsonProperty("modoValor")] public string strModoValor { get; set; } = TipoValorProceso.Calculado;
        [JsonProperty("tipoProcesoCodigo")] public string strTipoProcesoCodigo { get; set; } = string.Empty;
        [JsonProperty("tipoProceso")] public string strTipoProceso { get; set; } = string.Empty;
        [JsonProperty("esTarifa")] public bool blEsTarifa { get; set; }
        // información de las celdas FIC activas.
        [JsonProperty("esFichaTecnica")] public bool blEsFichaTecnica { get; set; }
        [JsonProperty("tiposFicha")] public string strTiposFicha { get; set; } = string.Empty;
        [JsonProperty("paramId")] public int intParamId { get; set; }
        [JsonProperty("sinProceso")] public bool blSinProceso { get; set; }
        [JsonProperty("editarCodigo")] public bool blEditarCodigo { get; set; }
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

    // catálogo de tipos nuevos desde tb_tiplot.
    public sealed class TipoProcesoDisponibleDto
    {
        [JsonProperty("codigo")] public string strCodigo { get; set; } = string.Empty;
        [JsonProperty("nombre")] public string strNombre { get; set; } = string.Empty;
        [JsonProperty("tipoLoteCodigo")] public string strTipoLoteCodigo { get; set; } = string.Empty;
        [JsonProperty("tipoLote")] public string strTipoLote { get; set; } = string.Empty;
        [JsonProperty("nivelSugerido")] public int intNivelSugerido { get; set; }
        [JsonProperty("loteValido")] public bool blLoteValido { get; set; }
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

        // Nombres reales tomados de tb_maecta (SONG). Solo lectura.
        [JsonProperty("cuentaNombre")]
        public string strCuentaNombre { get; set; } = string.Empty;

        [JsonProperty("auxiliarCodigo")]
        public string strAuxiliarCodigo { get; set; } = string.Empty;

        [JsonProperty("auxiliarNombre")]
        public string strAuxiliarNombre { get; set; } = string.Empty;

        [JsonProperty("centroNombre")]
        public string strCentroNombre { get; set; } = string.Empty;

        [JsonProperty("subcentroNombre")]
        public string strSubcentroNombre { get; set; } = string.Empty;

        [JsonProperty("procesoCodigo")]
        public string strProcesoCodigo { get; set; } = string.Empty;

        [JsonProperty("procesoNombre")]
        public string strProcesoNombre { get; set; } = string.Empty;

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

    public sealed class GuardarCostoProductivoMantenimientoResponse
    {
        [JsonProperty("guardado")]
        public bool blGuardado { get; set; }

        [JsonProperty("valorizacionesAnuladas")]
        public ValorizacionesAnuladasDto objValorizacionesAnuladas { get; set; } = new ValorizacionesAnuladasDto();
    }

    // conteos de cabeceras y detalles que cambiaron de AC a AN.
    public sealed class ValorizacionesAnuladasDto
    {
        [JsonProperty("hayTransferencia")] public bool blHayTransferencia { get; set; }
        // el guardado detecta, pero no anula.
        [JsonProperty("requiereAnulacion")] public bool blRequiereAnulacion { get; set; }
        [JsonProperty("frescoCabeceras")] public int intFrescoCabeceras { get; set; }
        [JsonProperty("frescoDetalles")] public int intFrescoDetalles { get; set; }
        [JsonProperty("reproCabeceras")] public int intReproCabeceras { get; set; }
        [JsonProperty("reproDetalles")] public int intReproDetalles { get; set; }
    }

    public sealed class GuardarCostoProductivoMantenimientoApiResponse : ApiResponse<string>
    {
        [JsonProperty("valorizacionesAnuladas")]
        public ValorizacionesAnuladasDto objValorizacionesAnuladas { get; set; } = new();
    }

    // mismo mecanismo de usuario del mantenimiento.
    public sealed class AnularValorizacionesRequest
    {
        // periodo opcional para limitar la anulacion a un mes.
        [JsonProperty("anio")] public int? intAnio { get; set; }
        [JsonProperty("mes")] public int? intMes { get; set; }
        [JsonProperty("tipo")] public string strTipo { get; set; } = string.Empty;
        [JsonProperty("usuario")] public string strUsuario { get; set; } = string.Empty;
    }

    public sealed class AnularValorizacionesResultadoDto
    {
        [JsonProperty("tipo")] public string strTipo { get; set; } = string.Empty;
        [JsonProperty("mesesProcesados")] public int intMesesProcesados => lstMeses.Count;
        [JsonProperty("cabecerasAnuladas")] public int intCabecerasAnuladas => lstMeses.Sum(x => x.intCabeceras);
        [JsonProperty("detallesAnulados")] public int intDetallesAnulados => lstMeses.Sum(x => x.intDetalles);
        [JsonProperty("meses")] public List<AnularValorizacionesMesDto> lstMeses { get; set; } = new();
    }

    public sealed class AnularValorizacionesMesDto
    {
        [JsonProperty("anio")] public int intAnio { get; set; }
        [JsonProperty("mes")] public int intMes { get; set; }
        [JsonProperty("cabeceras")] public int intCabeceras { get; set; }
        [JsonProperty("detalles")] public int intDetalles { get; set; }
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

        [JsonProperty("procesos")]
        public List<ProcesoCostoMantenimientoDto> lstProcesos { get; set; } = new List<ProcesoCostoMantenimientoDto>();
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

    public sealed class CuentasDisponiblesFiltroDto
    {
        [FromQuery(Name = "empresa")] public int intEmpresa { get; set; } = 1;
        [FromQuery(Name = "texto")] public string? strTexto { get; set; }
        [FromQuery(Name = "cuenta")] public string? strCuenta { get; set; }
        [FromQuery(Name = "centro")] public string? strCentro { get; set; }
        [FromQuery(Name = "subcentro")] public string? strSubcentro { get; set; }
        [FromQuery(Name = "soloDisponibles")] public bool blSoloDisponibles { get; set; }
        [FromQuery(Name = "procesoCodigo")] public string? strProcesoCodigo { get; set; }
        [FromQuery(Name = "pagina")] public int intPagina { get; set; } = 1;
        [FromQuery(Name = "cantidad")] public int intCantidad { get; set; } = 50;
    }

    public sealed class CuentaDisponibleDto
    {
        [JsonProperty("empresa")] public int intEmpresa { get; set; } = 1;
        [JsonProperty("cuenta")] public string strCuenta { get; set; } = string.Empty;
        [JsonProperty("cuentaNombre")] public string strCuentaNombre { get; set; } = string.Empty;
        [JsonProperty("auxiliarCodigo")] public string strAuxiliarCodigo { get; set; } = string.Empty;
        [JsonProperty("auxiliarNombre")] public string strAuxiliarNombre { get; set; } = string.Empty;
        [JsonProperty("centroCodigo")] public string strCentroCodigo { get; set; } = string.Empty;
        [JsonProperty("centroNombre")] public string strCentroNombre { get; set; } = string.Empty;
        [JsonProperty("subcentroCodigo")] public string strSubcentroCodigo { get; set; } = string.Empty;
        [JsonProperty("subcentroNombre")] public string strSubcentroNombre { get; set; } = string.Empty;
        [JsonProperty("etapaCodigo")] public string strEtapaCodigo { get; set; } = string.Empty;
        [JsonProperty("etapaNombre")] public string strEtapaNombre { get; set; } = string.Empty;
        [JsonProperty("procesoActualCodigo")] public string strProcesoActualCodigo { get; set; } = string.Empty;
        [JsonProperty("procesoActualNombre")] public string strProcesoActualNombre { get; set; } = string.Empty;
        [JsonProperty("asignada")] public bool blAsignada { get; set; }
        [JsonProperty("anulada")] public bool blAnulada { get; set; }
        [JsonProperty("mensaje")] public string strMensaje { get; set; } = string.Empty;
    }

    public sealed class CuentasDisponiblesResultadoDto
    {
        [JsonProperty("total")] public int intTotal { get; set; }
        [JsonProperty("pagina")] public int intPagina { get; set; }
        [JsonProperty("cantidad")] public int intCantidad { get; set; }
        [JsonProperty("items")] public List<CuentaDisponibleDto> lstItems { get; set; } = new List<CuentaDisponibleDto>();
    }
}
