using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Aplicación.DTos
{
    public sealed class DiarioMovimientoCuentaDto
    {
        [Column("cc_id")]
        public int intId { get; set; }

        [Column("cc_empCodigo")]
        public short intEmpresa { get; set; }

        [Column("cc_ecCodigo")]
        public string strEtapaCodigo { get; set; } = "DM";

        [Column("cc_pcCodigo")]
        public string strProcesoCodigo { get; set; } = string.Empty;

        [Column("pc_nombre")]
        public string strProceso { get; set; } = string.Empty;

        [Column("cc_rol")]
        public string strRol { get; set; } = string.Empty;

        [Column("cc_ctaNumero")]
        public string strCuenta { get; set; } = string.Empty;

        public string? strClave { get; set; }
        public string strDescripcion { get; set; } = string.Empty;
        public string? strNaturalezaCuenta { get; set; }

        public string strNaturalezaMovimiento { get; set; } = string.Empty;
        public string strTipoProducto { get; set; } = string.Empty;
        public string strClase { get; set; } = string.Empty;

        [JsonIgnore]
        public string strParticionCodigo =>
            strTipoProducto switch
            {
                "ENTERO" => "EN",
                "COLA" => "CO",
                "VALOR AGREGADO" => "VA",
                _ => string.Empty
            };

        public void ResolverRol()
        {
            string rol = (strRol ?? string.Empty).Trim().ToUpperInvariant();

            if (rol.StartsWith("D_")) strNaturalezaMovimiento = "D";
            else if (rol.StartsWith("H_")) strNaturalezaMovimiento = "H";
            else if (rol.StartsWith("W_")) strNaturalezaMovimiento = "W";
            else strNaturalezaMovimiento = string.Empty;

            string cuerpo = rol.Length > 2 ? rol.Substring(2) : rol;

            if (cuerpo.StartsWith("EN_"))
            {
                strTipoProducto = "ENTERO";
                strClase = NormalizarClase(cuerpo.Substring(3));
            }
            else if (cuerpo.StartsWith("CO_"))
            {
                strTipoProducto = "COLA";
                strClase = NormalizarClase(cuerpo.Substring(3));
            }
            else if (cuerpo.StartsWith("VA_"))
            {
                strTipoProducto = "VALOR AGREGADO";
                strClase = NormalizarClase(cuerpo.Substring(3));
            }
        }

        private static string NormalizarClase(string? clase) =>
            (clase ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "A+" => "A",
                "N" => "B",
                var x => x
            };
    }

    public sealed class DiarioMovimientoPersistenciaDto
    {
        public int intAnio { get; set; }
        public int intMes { get; set; }
        public DateOnly dtFechaCorte { get; set; }

        public string strEtapaCodigo { get; set; } = "DM";
        public string strProcesoCodigo { get; set; } = string.Empty;
        public string strCuenta { get; set; } = string.Empty;

        public string strCentroCodigo { get; set; } = string.Empty;
        public string strSubcentroCodigo { get; set; } = string.Empty;

        public decimal dcMontoEntero { get; set; }
        public decimal dcMontoCola { get; set; }
        public decimal dcMontoVag { get; set; }

        public string strAgrupacion { get; set; } = string.Empty;
        public string strGrupo { get; set; } = string.Empty;
        public string strNaturaleza { get; set; } = "D";

        [JsonIgnore]
        public decimal dcTotal =>
            dcMontoEntero + dcMontoCola + dcMontoVag;

        public void AsignarMonto(string tipoProducto, decimal monto)
        {
            string tipo = NormalizarTipo(tipoProducto);

            dcMontoEntero = 0m;
            dcMontoCola = 0m;
            dcMontoVag = 0m;

            if (tipo == "ENTERO") dcMontoEntero = monto;
            else if (tipo == "COLA") dcMontoCola = monto;
            else if (tipo == "VALOR AGREGADO") dcMontoVag = monto;
            else dcMontoEntero = monto;
        }

        public static string NormalizarTipo(string? tipo) =>
            (tipo ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "ENTERO" => "ENTERO",
                "COLA" => "COLA",
                "ENTERO VALOR AGREGADO" => "VALOR AGREGADO",
                "COLA VALOR AGREGADO" => "VALOR AGREGADO",
                "VALOR AGREGADO" => "VALOR AGREGADO",
                var x => x
            };
    }

    /// <summary>
    /// Resultado base de SPE_repfactpesoreal reducido a lo que necesita el cierre.
    /// </summary>
    public sealed class FacturaCostoSalidaDto
    {
        [Column("tcd_numero")]
        public long intMovimiento { get; set; }

        [Column("EMB_FECHAPED")]
        public DateTime? dtFechaSalida { get; set; }

        [Column("tcd_produc")]
        public string strCodProd { get; set; } = string.Empty;

        [Column("pro_desexp")]
        public string? strDescripcionProducto { get; set; }

        [Column("tcd_codtal")]
        public long intCodTalla { get; set; }

        [Column("tal_descri")]
        public string? strTalla { get; set; }

        [Column("FACT")]
        public string? strFactura { get; set; }

        [Column("EMB_FACTURA")]
        public string? strFacturaEmbarque { get; set; }

        [Column("lbs1")]
        public double? dbLibras { get; set; }

        [JsonIgnore]
        public int intCodProd =>
            int.TryParse(strCodProd?.Trim(), out int x) ? x : 0;

        [JsonIgnore]
        public string strFacturaKey =>
            DocumentoCostoKey.SecuenciaFactura(
                !string.IsNullOrWhiteSpace(strFactura)
                    ? strFactura
                    : strFacturaEmbarque);

        [JsonIgnore]
        public decimal dcLibras => (decimal)(dbLibras ?? 0d);
    }

    public sealed class CostoVentaSalidaHistoricoDto
    {
        public int intAnio { get; set; }
        public int intMes { get; set; }
        public DateOnly dtFechaSalida { get; set; }

        public string strFactura { get; set; } = string.Empty;
        public string strFacturaKey { get; set; } = string.Empty;

        public int intCodProd { get; set; }
        public int intCodTalla { get; set; }

        public string strDescripcionProducto { get; set; } = string.Empty;
        public string strTalla { get; set; } = string.Empty;

        public string strTipoProducto { get; set; } = string.Empty;
        public string strClase { get; set; } = string.Empty;

        public decimal dcLibras { get; set; }
        public decimal dcCostoUnitario { get; set; }
        public decimal dcCostoTotal { get; set; }

        public string strCuentaCostoVenta { get; set; } = string.Empty;
        public string strFuente { get; set; } = "CALCULADO";
    }

    public sealed class DiarioMovimientoCalculoResultadoDto
    {
        public DiariosCierreDto objVista { get; set; } = new();

        public List<DiarioMovimientoPersistenciaDto> lstPersistenciaPeriodo { get; set; } = new();

        /// <summary>
        /// DVS del período actual + DVS históricos reconstruidos que aún no existen.
        /// Se guardan append-only.
        /// </summary>
        public List<DiarioMovimientoPersistenciaDto> lstDvsPendiente { get; set; } = new();
    }

    public sealed class DiariosCierreDto
    {
        [JsonProperty("idGeneracion")]
        public int intIdGeneracion { get; set; }

        [JsonProperty("anio")]
        public int intAnio { get; set; }

        [JsonProperty("mes")]
        public int intMes { get; set; }

        [JsonProperty("periodoTexto")]
        public string strPeriodoTexto { get; set; } = string.Empty;

        [JsonProperty("guardado")]
        public bool blGuardado { get; set; }

        [JsonProperty("estado")]
        public string strEstado { get; set; } = "GENERADO";

        [JsonProperty("retornos")]
        public RetornosCierreDto objRetornos { get; set; } = new();

        [JsonProperty("transferenciaGif")]
        public TransferenciaGifCierreDto objTransferenciaGif { get; set; } = new();
    }

    public sealed class RetornosCierreDto
    {
        [JsonProperty("titulo")]
        public string strTitulo { get; set; } = "Retorno de Contenedores";

        [JsonProperty("mensaje")]
        public string? strMensaje { get; set; }

        [JsonProperty("advertencias")]
        public List<string> lstAdvertencias { get; set; } = new();

        [JsonProperty("diarios")]
        public List<DiarioRetornoCierreDto> lstDiarios { get; set; } = new();
    }

    public sealed class DiarioRetornoCierreDto
    {
        [JsonProperty("id")]
        public int intId { get; set; }

        [JsonProperty("titulo")]
        public string strTitulo { get; set; } = string.Empty;

        [JsonProperty("subtitulo")]
        public string? strSubtitulo { get; set; }

        [JsonProperty("numeroDocumento")]
        public string? strNumeroDocumento { get; set; }

        [JsonProperty("referencia")]
        public string? strReferencia { get; set; }

        [JsonProperty("glosa")]
        public string strGlosa { get; set; } = string.Empty;

        [JsonProperty("filas")]
        public List<DiarioRetornoFilaDto> lstFilas { get; set; } = new();
    }

    public sealed class DiarioRetornoFilaDto
    {
        [JsonProperty("id")]
        public int intId { get; set; }

        [JsonProperty("orden")]
        public int intOrden { get; set; }

        [JsonProperty("codigo")]
        public string? strCodigo { get; set; }

        [JsonProperty("cuentaContable")]
        public string strCuentaContable { get; set; } = string.Empty;

        [JsonProperty("clave")]
        public string? strClave { get; set; }

        [JsonProperty("descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [JsonProperty("moneda")]
        public string? strMoneda { get; set; }

        [JsonProperty("debe")]
        public decimal dcDebe { get; set; }

        [JsonProperty("haber")]
        public decimal dcHaber { get; set; }

        [JsonProperty("detalle")]
        public string? strDetalle { get; set; }
    }

    public sealed class TransferenciaGifCierreDto
    {
        [JsonProperty("titulo")]
        public string strTitulo { get; set; } = "Transferencias de Costos de Producción";

        [JsonProperty("periodoTexto")]
        public string strPeriodoTexto { get; set; } = string.Empty;

        [JsonProperty("baseCalculo")]
        public decimal dcBaseCalculo { get; set; }

        [JsonProperty("totalGastoIndirecto")]
        public decimal dcTotalGastoIndirecto { get; set; }

        [JsonProperty("diarios")]
        public List<DiarioTransferenciaCierreDto> lstDiarios { get; set; } = new();
    }

    public sealed class DiarioTransferenciaCierreDto
    {
        [JsonProperty("id")]
        public int intId { get; set; }

        [JsonProperty("codigo")]
        public string strCodigo { get; set; } = string.Empty;

        [JsonProperty("titulo")]
        public string strTitulo { get; set; } = string.Empty;

        [JsonProperty("glosa")]
        public string strGlosa { get; set; } = string.Empty;

        [JsonProperty("filas")]
        public List<DiarioTransferenciaFilaDto> lstFilas { get; set; } = new();
    }

    public sealed class DiarioTransferenciaFilaDto
    {
        [JsonProperty("id")]
        public int intId { get; set; }

        [JsonProperty("orden")]
        public int intOrden { get; set; }

        [JsonProperty("calculo")]
        public decimal? dcCalculo { get; set; }

        [JsonProperty("codigo")]
        public string? strCodigo { get; set; }

        [JsonProperty("clave")]
        public string? strClave { get; set; }

        [JsonProperty("nombreCuenta")]
        public string strNombreCuenta { get; set; } = string.Empty;

        [JsonProperty("debe")]
        public decimal dcDebe { get; set; }

        [JsonProperty("haber")]
        public decimal dcHaber { get; set; }
    }

    public sealed class GuardarDiariosCierreRequest
    {
        [JsonProperty("idGeneracion")]
        public int intIdGeneracion { get; set; }

        [JsonProperty("anio")]
        public int intAnio { get; set; }

        [JsonProperty("mes")]
        public int intMes { get; set; }

        [JsonProperty("usuario")]
        public string strUsuario { get; set; } = string.Empty;
    }

    public sealed class NotaCreditoRetornoContenedorDto
    {
        [Column("FECHA")]
        public DateTime dtFecha { get; set; }

        [Column("NUMERO")]
        public string strNumero { get; set; } = string.Empty;

        [Column("COD_CLIENTE")]
        public string strCodCliente { get; set; } = string.Empty;

        [Column("CLIENTE")]
        public string strCliente { get; set; } = string.Empty;

        [Column("APLICAFACTURA")]
        public string strAplicaFactura { get; set; } = string.Empty;

        [Column("FECHA_FACTURA_APLICADA")]
        public DateTime? dtFechaFacturaAplicada { get; set; }

        [Column("BASE0")]
        public decimal dcBase0 { get; set; }

        [Column("VALOR_FOB")]
        public decimal dcValorFob { get; set; }

        [Column("TOTAL")]
        public decimal dcTotal { get; set; }

        [Column("CONCEPTO")]
        public string? strConcepto { get; set; }

        [Column("OBSERVACIONRIDE")]
        public string? strObservacionRide { get; set; }

        [Column("TIPDEU")]
        public string strTipDeu { get; set; } = string.Empty;

        [Column("REFER")]
        public string? strRefer { get; set; }

        [Column("TIPO_CLIENTE")]
        public string strTipoCliente { get; set; } = string.Empty;

        [Column("REFERENCIA_EMBARQUE")]
        public string? strReferenciaEmbarque { get; set; }

        [JsonIgnore]
        public string strFacturaKey =>
            DocumentoCostoKey.SecuenciaFactura(strAplicaFactura);

        [JsonIgnore]
        public bool blEsRetornoContenedor
        {
            get
            {
                if (!string.Equals(strTipDeu?.Trim(), "NC", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.Equals(strTipoCliente?.Trim(), "EXTERIOR", StringComparison.OrdinalIgnoreCase))
                    return false;

                string texto = $"{strObservacionRide} {strConcepto}".ToUpperInvariant();

                return
                    texto.Contains("DEVOLUCION DE CONTENEDOR") ||
                    texto.Contains("DEVOLUCIÓN DE CONTENEDOR") ||
                    texto.Contains("RETORNO DE CONTENEDOR");
            }
        }
    }
}
