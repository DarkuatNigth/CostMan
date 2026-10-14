using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public sealed class CostoProductivoConfigCuentaDto
    {
        [Column("cc_id")] public short intId { get; set; }
        [Column("cc_empCodigo")] public short intEmpresa { get; set; }
        [Column("cc_ecCodigo")] public string strEcCodigo { get; set; } = string.Empty;
        [Column("ec_nombre")] public string strEtapaGeneral { get; set; } = string.Empty;
        [Column("cc_pcCodigo")] public string strPcCodigo { get; set; } = string.Empty;
        [Column("pc_nombre")] public string strProcesoCosto { get; set; } = string.Empty;

        // BASE ACTUALIZADA columna K.
        [Column("cc_tipo")] public string strTipo { get; set; } = string.Empty;

        // BASE ACTUALIZADA columna L.
        [Column("cc_tipoCosto")] public string strTipoCosto { get; set; } = string.Empty;

        // BASE ACTUALIZADA columna Q.
        [Column("cc_tipo3")] public string strTipo3 { get; set; } = string.Empty;

        // BASE ACTUALIZADA columnas R y S.
        [Column("cc_agrupacionCentro")] public string strAgrupacionCentro { get; set; } = string.Empty;

        [Column("cc_centroCosto")]
        public string strCentroCosto { get; set; } = string.Empty;

        [Column("cc_subcentroCosto")]
        public string strSubcentroCosto { get; set; } = string.Empty;
        [Column("cc_grupoCentro")] public string strGrupoCentro { get; set; } = string.Empty;

        [Column("cc_cenCodigo")] public string strCentroCodigo { get; set; } = string.Empty;
        [Column("cc_subCodigo")] public string strSubcentroCodigo { get; set; } = string.Empty;
        [Column("cc_ctaNumero")] public string strCuenta { get; set; } = string.Empty;
        [Column("cc_factorAsignacion")] public decimal dcFactorAsignacion { get; set; } = 1m;
        [Column("cc_montoBaseReferencia")] public decimal? dcMontoBaseReferencia { get; set; }
        // Clasificación original de BASE ACTUALIZADA, columna tipo2.
        [Column("cc_tipo2")]
        public string strTipo2 { get; set; } = string.Empty;

        // Driver efectivo. Puede diferir de pcCodigo sin cambiar el proceso de la fila.
        // Ejemplo: MPE común usa CIV como driver, pero se persiste como MPE.
        [Column("cc_driverCodigo")]
        public string strDriverCodigo { get; set; } = string.Empty;

        [Column("cc_modoMonto")]
        public string strModoMonto { get; set; } = "NETO_MES";

        [Column("cc_requiereCreditoRetiro")]
        public bool blRequiereCreditoRetiro { get; set; }
    }

    public sealed class CostoProductivoConfiguracionDbDto
    {
        public List<CostoProductivoConfigCuentaDto> lstCuentas { get; set; } = new();
        public List<ProcesoCostoAplicacionDto> lstAplicaciones { get; set; } = new();

        // Resultset 3 del SP. No representa una cuenta SONG.
        public CostoProductivoDerivadoConfigDto objDescongelado { get; set; } = new();
        public List<IqfConfiguracionDto> lstIqf { get; set; } = new();
    }

    public sealed class CostoProductivoExcelDto
    {
        public static readonly string[] Columnas =
        {
        "Etapa",
        "Etapa general",
        "Origen",
        "Cuenta",
        "Centro costo",
        "Subcentro costo",
        "Tipo",
        "Tipo costo",
        "Agrupación",
        "Grupo",
        "Rubro",
        "Auxiliar",
        "Entero",
        "Cola",
        "Sub-total",
        "Valor Agregado",
        "C. Directo Fijo",
        "C. Directo Variable",
        "C. Indirecto Fijo",
        "C. Indirecto Variable",
        "Reclasif. Descongelado",
        "Total",
        "Monto cuenta",
        "Factor cuenta",
        "Libras driver",
        "Peso driver",
        "Origen distribución",
        "Procesos aplicables",
        "Diferencia",
        "Cuadra"
    };

        [Column("Etapa")]
        public string strEtapa { get; set; } = string.Empty;

        [Column("Etapa general")]
        public string strGrupoEtapa { get; set; } = string.Empty;

        [Column("Origen")]
        public string strOrigen { get; set; } = string.Empty;

        [Column("Cuenta")]
        public string strCuenta { get; set; } = string.Empty;

        [Column("Centro costo")]
        public string strCentroCosto { get; set; } = string.Empty;

        [Column("Subcentro costo")]
        public string strSubcentroCosto { get; set; } = string.Empty;

        [Column("Tipo")]
        public string strTipo { get; set; } = string.Empty;

        [Column("Tipo costo")]
        public string strTipoCosto { get; set; } = string.Empty;

        [Column("Agrupación")]
        public string strAgrupacionCentro { get; set; } = string.Empty;

        [Column("Grupo")]
        public string strGrupoCentro { get; set; } = string.Empty;

        [Column("Rubro")]
        public string strRubro { get; set; } = string.Empty;

        [Column("Auxiliar")]
        public string strAuxiliar { get; set; } = string.Empty;

        [Column("Entero")]
        public decimal dcEntero { get; set; }

        [Column("Cola")]
        public decimal dcCola { get; set; }

        [Column("Sub-total")]
        public decimal dcSubtotal { get; set; }

        [Column("Valor Agregado")]
        public decimal dcValorAgregado { get; set; }

        [Column("C. Directo Fijo")]
        public decimal dcCostoDirectoFijo { get; set; }

        [Column("C. Directo Variable")]
        public decimal dcCostoDirectoVariable { get; set; }

        [Column("C. Indirecto Fijo")]
        public decimal dcCostoIndirectoFijo { get; set; }

        [Column("C. Indirecto Variable")]
        public decimal dcCostoIndirectoVariable { get; set; }

        [Column("Reclasif. Descongelado")]
        public decimal dcReclasificadoDescongelado { get; set; }

        [Column("Total")]
        public decimal dcTotal { get; set; }

        [Column("Monto cuenta")]
        public decimal dcMontoCuenta { get; set; }

        [Column("Factor cuenta")]
        public decimal dcFactorAsignacion { get; set; }

        [Column("Libras driver")]
        public decimal dcLibrasDriver { get; set; }

        [Column("Peso driver")]
        public decimal dcPesoDriver { get; set; }

        [Column("Origen distribución")]
        public string strOrigenDistribucion { get; set; } = string.Empty;

        [Column("Procesos aplicables")]
        public string strProcesosAplicables { get; set; } = string.Empty;

        [Column("Diferencia")]
        public decimal dcDiferencia { get; set; }

        [Column("Cuadra")]
        public bool blCuadra { get; set; }


        public static CostoProductivoExcelDto Crear(
            CostoProductivoCuentaDto item)
        {
            return new CostoProductivoExcelDto
            {
                strEtapa = item.strEtapa,
                strGrupoEtapa = item.strGrupoEtapa,
                strOrigen = item.strOrigen,
                strCuenta = item.strCuenta,
                strCentroCosto = item.strCentroCosto,
                strSubcentroCosto = item.strSubcentroCosto,
                strTipo = item.strTipo,
                strTipoCosto = item.strTipoCosto,
                strAgrupacionCentro = item.strAgrupacionCentro,
                strGrupoCentro = item.strGrupoCentro,
                strRubro = item.strRubro,
                strAuxiliar = item.strAuxiliar,

                dcEntero = item.dcMontoEntero,
                dcCola = item.dcMontoCola,
                dcSubtotal = item.dcSubtotal,
                dcValorAgregado = item.dcMontoVag,

                dcCostoDirectoFijo = item.dcCostoDirectoFijo,
                dcCostoDirectoVariable = item.dcCostoDirectoVariable,
                dcCostoIndirectoFijo = item.dcCostoIndirectoFijo,
                dcCostoIndirectoVariable = item.dcCostoIndirectoVariable,

                dcReclasificadoDescongelado =
                    item.dcReclasificadoDescongelado,

                dcTotal = item.dcTotal,
                dcMontoCuenta = item.dcMontoCuenta,
                dcFactorAsignacion = item.dcFactorAsignacion,

                dcLibrasDriver = item.dcLibrasDriver,
                dcPesoDriver = item.dcPesoDriver,

                strOrigenDistribucion = item.strOrigenDistribucion,
                strProcesosAplicables = item.strProcesosAplicables,

                dcDiferencia = item.dcDiferencia,
                blCuadra = item.blCuadra
            };
        }
    }

    public sealed class CostoProductivoDerivadoConfigDto
    {
        [Column("ec_codigo")] public string strEcCodigo { get; set; } = string.Empty;
        [Column("ec_nombre")] public string strEtapaGeneral { get; set; } = string.Empty;
        [Column("pc_codigo")] public string strPcCodigo { get; set; } = string.Empty;
        [Column("pc_nombre")] public string strProcesoCosto { get; set; } = string.Empty;
        [Column("tipo")] public string strTipo { get; set; } = string.Empty;
        [Column("tipoCosto")] public string strTipoCosto { get; set; } = string.Empty;
        [Column("cenCodigo")] public string strCentroCodigo { get; set; } = string.Empty;
        [Column("centroCosto")] public string strCentroCosto { get; set; } = string.Empty;
        [Column("subCodigo")] public string strSubcentroCodigo { get; set; } = string.Empty;
        [Column("subcentroCosto")] public string strSubcentroCosto { get; set; } = string.Empty;
        [Column("agrupacionCentro")] public string strAgrupacionCentro { get; set; } = string.Empty;
        [Column("grupoCentro")] public string strGrupoCentro { get; set; } = string.Empty;
        [Column("tarifa")] public decimal dcTarifa { get; set; } = 0.02m;
    }

    public sealed class ProcesoCostoAplicacionDto
    {
        [Column("pc_codigo")]
        public string strPcCodigo { get; set; } = string.Empty;

        [Column("pc_nombre")]
        public string strPcNombre { get; set; } = string.Empty;

        [Column("pc_tarifa")]
        public decimal? dcTarifa { get; set; }

        [Column("to_codigo")]
        public string strTipoProcesoCodigo { get; set; } = string.Empty;

        [Column("to_nombre")]
        public string strTipoProceso { get; set; } = string.Empty;

        [Column("cg_config")]
        public string strConfig { get; set; } = string.Empty;

        /// <summary>
        /// Configuración normalizada recibida de la matriz:
        /// SI, X2, PRO, NO, NA, TAR, etc.
        /// No es una columna adicional del procedimiento.
        /// </summary>
        [JsonIgnore]
        public string strConfigNormalizada =>
            (strConfig ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        /// <summary>
        /// Factor aplicable directamente por la lógica general.
        ///
        /// PRO intencionalmente devuelve cero, porque debe resolverse
        /// utilizando el congelamiento real del lote.
        /// </summary>
        [JsonIgnore]
        public int intFactor =>
            strConfigNormalizada switch
            {
                "X2" => 2,
                "SI" => 1,
                _ => 0
            };

        /// <summary>
        /// Aplicación tarifada, como Descongelado.
        /// </summary>
        [JsonIgnore]
        public bool blEsTarifa =>
            strConfigNormalizada == "TAR";

        /// <summary>
        /// Participación condicionada por el congelamiento real del lote.
        /// Para IQF participa solamente cuando strCongeProduc se resuelve como IQF.
        /// </summary>
        [JsonIgnore]
        public bool blEsPro =>
            strConfigNormalizada == "PRO";

        /// <summary>
        /// Participación directa sin condición adicional.
        /// </summary>
        [JsonIgnore]
        public bool blEsSi =>
            strConfigNormalizada == "SI";

        /// <summary>
        /// Configuración expresamente excluida.
        /// </summary>
        [JsonIgnore]
        public bool blEsNo =>
            strConfigNormalizada == "NO";

        /// <summary>
        /// Configuración no aplicable al proceso.
        /// </summary>
        [JsonIgnore]
        public bool blEsNoAplica =>
            strConfigNormalizada == "NA";

        /// <summary>
        /// Permite conservar compatibilidad con configuraciones X2 anteriores.
        /// </summary>
        [JsonIgnore]
        public bool blEsDoble =>
            strConfigNormalizada == "X2";
    }

    public sealed class DriverFrescoRecibidoDto
    {
        public string strPcCodigo { get; set; } = string.Empty;
        public decimal dcLibrasRecibidasEntero { get; set; }
        public decimal dcLibrasRecibidasCola { get; set; }
        public decimal dcLibrasProcesadasEntero { get; set; }
        public decimal dcLibrasProcesadasCola { get; set; }
    }

    public sealed class SaldoCuentaSongDto
    {
        [Column("Empresa")] public string strEmpresa { get; set; } = string.Empty;
        [Column("Nivel1Numero")] public string strNivel1Numero { get; set; } = string.Empty;
        [Column("Nivel1Descripcion")] public string strNivel1Descripcion { get; set; } = string.Empty;
        [Column("Nivel2Numero")] public string strNivel2Numero { get; set; } = string.Empty;
        [Column("Nivel2Descripcion")] public string strNivel2Descripcion { get; set; } = string.Empty;
        [Column("Nivel3Numero")] public string strNivel3Numero { get; set; } = string.Empty;
        [Column("Nivel3Descripcion")] public string strNivel3Descripcion { get; set; } = string.Empty;
        [Column("Nivel4Numero")] public string strNivel4Numero { get; set; } = string.Empty;
        [Column("Nivel4Descripcion")] public string strNivel4Descripcion { get; set; } = string.Empty;
        [Column("Cuenta")] public string strCuenta { get; set; } = string.Empty;
        [Column("CuentaDescripcion")] public string strCuentaDescripcion { get; set; } = string.Empty;
        [Column("Naturaleza")] public string strNaturaleza { get; set; } = string.Empty;
        [Column("MostrarSaldoCero")] public string strMostrarSaldoCero { get; set; } = string.Empty;

        [Column("IniDebe")] public decimal dcIniDebe { get; set; }
        [Column("IniCredito")] public decimal dcIniCredito { get; set; }
        [Column("ActDebe")] public decimal dcActDebe { get; set; }
        [Column("ActCredito")] public decimal dcActCredito { get; set; }
        [Column("EneDebe")] public decimal dcEneDebe { get; set; }
        [Column("EneCredito")] public decimal dcEneCredito { get; set; }
        [Column("FebDebe")] public decimal dcFebDebe { get; set; }
        [Column("FebCredito")] public decimal dcFebCredito { get; set; }
        [Column("MarDebe")] public decimal dcMarDebe { get; set; }
        [Column("MarCredito")] public decimal dcMarCredito { get; set; }
        [Column("AbrDebe")] public decimal dcAbrDebe { get; set; }
        [Column("AbrCredito")] public decimal dcAbrCredito { get; set; }
        [Column("MayDebe")] public decimal dcMayDebe { get; set; }
        [Column("MayCredito")] public decimal dcMayCredito { get; set; }
        [Column("JunDebe")] public decimal dcJunDebe { get; set; }
        [Column("JunCredito")] public decimal dcJunCredito { get; set; }
        [Column("JulDebe")] public decimal dcJulDebe { get; set; }
        [Column("JulCredito")] public decimal dcJulCredito { get; set; }
        [Column("AgoDebe")] public decimal dcAgoDebe { get; set; }
        [Column("AgoCredito")] public decimal dcAgoCredito { get; set; }
        [Column("SepDebe")] public decimal dcSepDebe { get; set; }
        [Column("SepCredito")] public decimal dcSepCredito { get; set; }
        [Column("OctDebe")] public decimal dcOctDebe { get; set; }
        [Column("OctCredito")] public decimal dcOctCredito { get; set; }
        [Column("NovDebe")] public decimal dcNovDebe { get; set; }
        [Column("NovCredito")] public decimal dcNovCredito { get; set; }
        [Column("DicDebe")] public decimal dcDicDebe { get; set; }
        [Column("DicCredito")] public decimal dcDicCredito { get; set; }

        [JsonIgnore]
        public int intEmpresa =>
            int.TryParse((strEmpresa ?? string.Empty).Trim(), out int codigo) ? codigo : 1;

        public (decimal Debe, decimal Credito) ObtenerAcumulado(int intMes)
        {
            return intMes switch
            {
                1 => (dcEneDebe, dcEneCredito),
                2 => (dcFebDebe, dcFebCredito),
                3 => (dcMarDebe, dcMarCredito),
                4 => (dcAbrDebe, dcAbrCredito),
                5 => (dcMayDebe, dcMayCredito),
                6 => (dcJunDebe, dcJunCredito),
                7 => (dcJulDebe, dcJulCredito),
                8 => (dcAgoDebe, dcAgoCredito),
                9 => (dcSepDebe, dcSepCredito),
                10 => (dcOctDebe, dcOctCredito),
                11 => (dcNovDebe, dcNovCredito),
                12 => (dcDicDebe, dcDicCredito),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(intMes),
                    "El mes debe estar entre 1 y 12.")
            };
        }

        public string ObtenerDescripcionJerarquia(string? codigo)
        {
            string c = (codigo ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(c)) return string.Empty;

            var niveles = new (string Numero, string Descripcion)[]
            {
                (strNivel1Numero,strNivel1Descripcion),
                (strNivel2Numero,strNivel2Descripcion),
                (strNivel3Numero,strNivel3Descripcion),
                (strNivel4Numero,strNivel4Descripcion),
                (strCuenta,strCuentaDescripcion)
            };
            return niveles.FirstOrDefault(x =>
                string.Equals((x.Numero ?? "").Trim(), c, StringComparison.OrdinalIgnoreCase))
                .Descripcion?.Trim() ?? string.Empty;
        }
    }

    public sealed class DriverProcesoProductoDto
    {
        public string strPcCodigo { get; set; } = string.Empty;
        public string strTipCodigo { get; set; } = string.Empty;
        public string strParticionCodigo { get; set; } = string.Empty;
        public string strParticion { get; set; } = string.Empty;
        public decimal dcLibras { get; set; }
        public int intFactor { get; set; } = 1;
        public decimal dcPeso { get; set; }
    }

    public sealed class DistribucionMontoProductoDto
    {
        public decimal dcEntero { get; set; }
        public decimal dcCola { get; set; }
        public decimal dcValorAgregado { get; set; }
        public decimal dcLibrasDriver { get; set; }
        public decimal dcPesoDriver { get; set; }
        public bool blUsoDriver { get; set; }
        public string strOrigen { get; set; } = string.Empty;
        public decimal dcLibrasRecibidasEntero { get; set; }
        public decimal dcLibrasRecibidasCola { get; set; }
        public decimal dcLibrasProcesadasEntero { get; set; }
        public decimal dcLibrasProcesadasCola { get; set; }
        public decimal dcCostoUnitarioRecibido { get; set; }
        public decimal dcCostoUnitarioEntero { get; set; }
        public decimal dcCostoUnitarioCola { get; set; }

    }

    public sealed class DescongeladoResultadoDto
    {
        public decimal dcTarifa { get; set; } = 0.02m;
        public decimal dcLibrasRecibidasPt { get; set; }
        public decimal dcMontoTotal { get; set; }
        public decimal dcMontoAsignado { get; set; }
        public decimal dcMontoSinAsignar { get; set; }
        public decimal dcEntero { get; set; }
        public decimal dcCola { get; set; }
        public decimal dcValorAgregado { get; set; }
        public int intLotesElegibles { get; set; }
        public int intLotesSinSalida { get; set; }
        public string strTiposAplicables { get; set; } = string.Empty;
    }

    public sealed class CostoProductivoDriversDto
    {
        public List<DriverProcesoProductoDto> lstDrivers { get; set; } = new();
        public DescongeladoResultadoDto objDescongelado { get; set; } = new();

        public List<DriverFrescoRecibidoDto> lstFrescoRecibido { get; set; } = new();

        public Dictionary<string, string> dicParticionFisicaUnicaProceso { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class CostoProductivoProcesoAplicableDetalleDto
    {
        [JsonProperty("codigoProceso")]
        [Column("codigoProceso")]
        public string strCodigoProceso { get; set; } = string.Empty;

        [JsonProperty("proceso")]
        [Column("proceso")]
        public string strProceso { get; set; } = string.Empty;

        [JsonProperty("dolares")]
        [Column("dolares")]
        public decimal dcDolares { get; set; }
    }

    public sealed class CostoProductivoCuentaDto
    {
        [JsonProperty("id")][Column("id")] public int intId { get; set; }

        [JsonProperty("origen")]
        [Column("origen")]
        public string strOrigen { get; set; } = "CONTABLE";

        [JsonProperty("etapaVista")]
        [Column("etapaVista")]
        public string strEtapa { get; set; } = string.Empty;

        [JsonProperty("grupoEtapa")]
        [Column("grupoEtapa")]
        public string strGrupoEtapa { get; set; } = string.Empty;

        [JsonProperty("ecCodigo")]
        [Column("ecCodigo")]
        public string strEcCodigo { get; set; } = string.Empty;

        [JsonProperty("pcCodigo")]
        [Column("pcCodigo")]
        public string strPcCodigo { get; set; } = string.Empty;

        [JsonProperty("tipo")]
        [Column("tipo")]
        public string strTipo { get; set; } = string.Empty;

        [JsonProperty("tipoCosto")]
        [Column("tipoCosto")]
        public string strTipoCosto { get; set; } = string.Empty;

        [JsonProperty("tipo3")]
        [Column("tipo3")]
        public string strTipo3 { get; set; } = string.Empty;

        // BASE ACTUALIZADA R / S. Estos son los valores que se persisten.
        [JsonProperty("agrupacionCentro")]
        [Column("agrupacionCentro")]
        public string strAgrupacionCentro { get; set; } = string.Empty;

        [JsonProperty("grupoCentro")]
        [Column("grupoCentro")]
        public string strGrupoCentro { get; set; } = string.Empty;

        [JsonProperty("clasificacionMonto")]
        [Column("clasificacionMonto")]
        public string strClasificacionMonto { get; set; } = string.Empty;

        [JsonProperty("origenDistribucion")]
        [Column("origenDistribucion")]
        public string strOrigenDistribucion { get; set; } = string.Empty;

        [JsonProperty("cuenta")]
        [Column("cuenta")]
        public string strCuenta { get; set; } = string.Empty;

        [JsonProperty("centroCodigo")]
        [Column("centroCodigo")]
        public string strCentroCodigo { get; set; } = string.Empty;

        [JsonProperty("centroCosto")]
        [Column("centroCosto")]
        public string strCentroCosto { get; set; } = string.Empty;

        [JsonProperty("subcentroCodigo")]
        [Column("subcentroCodigo")]
        public string strSubcentroCodigo { get; set; } = string.Empty;

        [JsonProperty("subcentroCosto")]
        [Column("subcentroCosto")]
        public string strSubcentroCosto { get; set; } = string.Empty;

        [JsonProperty("rubro")]
        [Column("rubro")]
        public string strRubro { get; set; } = string.Empty;

        [JsonProperty("auxiliar")]
        [Column("auxiliar")]
        public string strAuxiliar { get; set; } = string.Empty;

        [JsonProperty("naturaleza")]
        [Column("naturaleza")]
        public string strNaturaleza { get; set; } = "D";

        [JsonProperty("debe")][Column("debe")] public decimal dcDebe { get; set; }
        [JsonProperty("credito")][Column("credito")] public decimal dcCredito { get; set; }

        // Saldo completo de SONG antes de aplicar factor de configuración.
        [JsonProperty("montoFuenteSong")]
        [Column("montoFuenteSong")]
        public decimal dcMontoFuenteSong { get; set; }

        [JsonProperty("factorAsignacion")]
        [Column("factorAsignacion")]
        public decimal dcFactorAsignacion { get; set; } = 1m;

        // Monto canónico de esta configuración. Para una cuenta dividida,
        // la suma de sus configuraciones vuelve al saldo SONG original.
        [JsonProperty("montoCuenta")]
        [Column("montoCuenta")]
        public decimal dcMontoCuenta { get; set; }

        [JsonProperty("entero")][Column("entero")] public decimal dcMontoEntero { get; set; }
        [JsonProperty("cola")][Column("cola")] public decimal dcMontoCola { get; set; }
        [JsonProperty("subtotal")][Column("subtotal")] public decimal dcSubtotal { get; set; }
        [JsonProperty("valorAgregado")][Column("valorAgregado")] public decimal dcMontoVag { get; set; }

        [JsonProperty("costoDirectoFijo")]
        [Column("costoDirectoFijo")]
        public decimal dcCostoDirectoFijo { get; set; }

        [JsonProperty("costoDirectoVariable")]
        [Column("costoDirectoVariable")]
        public decimal dcCostoDirectoVariable { get; set; }

        [JsonProperty("costoIndirectoFijo")]
        [Column("costoIndirectoFijo")]
        public decimal dcCostoIndirectoFijo { get; set; }

        [JsonProperty("costoIndirectoVariable")]
        [Column("costoIndirectoVariable")]
        public decimal dcCostoIndirectoVariable { get; set; }

        // Monto retirado de la columna VAG y llevado a la fila derivada Descongelado.
        [JsonProperty("reclasificadoDescongelado")]
        [Column("reclasificadoDescongelado")]
        public decimal dcReclasificadoDescongelado { get; set; }

        [JsonProperty("librasDriver")]
        [Column("librasDriver")]
        public decimal dcLibrasDriver { get; set; }

        [JsonProperty("pesoDriver")]
        [Column("pesoDriver")]
        public decimal dcPesoDriver { get; set; }

        [JsonProperty("total")][Column("total")] public decimal dcTotal { get; set; }
        [JsonProperty("diferencia")][Column("diferencia")] public decimal dcDiferencia { get; set; }

        [JsonProperty("destinosSiTexto")]
        [Column("destinosSiTexto")]
        public string strProcesosAplicables { get; set; } = string.Empty;

        [JsonProperty("cantidadDestinosSi")]
        [Column("cantidadDestinosSi")]
        public int intCantidadDestinos { get; set; }

        [JsonProperty("encontradaSong")]
        [Column("encontradaSong")]
        public bool blEncontradaSong { get; set; }

        [JsonProperty("detalleProcesosAplicables")]
        [Column("detalleProcesosAplicables")]
        public List<CostoProductivoProcesoAplicableDetalleDto> lstDetalleProcesosAplicables { get; set; } = new();

        [JsonProperty("librasRecibidasEntero")]
        [Column("librasRecibidasEntero")]
        public decimal dcLibrasRecibidasEntero { get; set; }

        [JsonProperty("librasRecibidasCola")]
        [Column("librasRecibidasCola")]
        public decimal dcLibrasRecibidasCola { get; set; }

        [JsonProperty("librasProcesadasEntero")]
        [Column("librasProcesadasEntero")]
        public decimal dcLibrasProcesadasEntero { get; set; }

        [JsonProperty("librasProcesadasCola")]
        [Column("librasProcesadasCola")]
        public decimal dcLibrasProcesadasCola { get; set; }

        [JsonProperty("costoUnitarioRecibido")]
        [Column("costoUnitarioRecibido")]
        public decimal dcCostoUnitarioRecibido { get; set; }

        [JsonProperty("costoUnitarioEntero")]
        [Column("costoUnitarioEntero")]
        public decimal dcCostoUnitarioEntero { get; set; }

        [JsonProperty("costoUnitarioCola")]
        [Column("costoUnitarioCola")]
        public decimal dcCostoUnitarioCola { get; set; }


        [JsonProperty("cuadra")][Column("cuadra")] public bool blCuadra { get; set; }

        [JsonIgnore]
        public bool blEsDerivado =>
            string.Equals(strOrigen, "DERIVADO", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool blEsCostoEstructural =>
            strClasificacionMonto.StartsWith("COSTO_", StringComparison.OrdinalIgnoreCase);

        [JsonProperty("enteroPersistencia")]
        [Column("enteroPersistencia")]
        public decimal dcMontoEnteroPersistencia { get; set; }

        [JsonProperty("colaPersistencia")]
        [Column("colaPersistencia")]
        public decimal dcMontoColaPersistencia { get; set; }

        [JsonProperty("valorAgregadoPersistencia")]
        [Column("valorAgregadoPersistencia")]
        public decimal dcMontoVagPersistencia { get; set; }

        [JsonProperty("tipo2")]
        [Column("tipo2")]
        public string strTipo2 { get; set; } = string.Empty;

        [JsonProperty("driverCodigo")]
        [Column("driverCodigo")]
        public string strDriverCodigo { get; set; } = string.Empty;

        [JsonProperty("montoOriginalAsignado")]
        [Column("montoOriginalAsignado")]
        public decimal dcMontoOriginalAsignado { get; set; }

        [JsonProperty("recuperadoIqf")]
        [Column("recuperadoIqf")]
        public decimal dcRecuperadoIqf { get; set; }

        [JsonProperty("retiradoIqf")]
        [Column("retiradoIqf")]
        public decimal dcRetiradoIqf { get; set; }

        public void RecalcularTotales()
        {
            dcSubtotal = dcMontoEntero + dcMontoCola;
            dcTotal =
                dcMontoEntero + dcMontoCola + dcMontoVag +
                dcCostoDirectoFijo + dcCostoDirectoVariable +
                dcCostoIndirectoFijo + dcCostoIndirectoVariable;

            if (blEsDerivado)
            {
                dcDiferencia = 0m;
                blCuadra = true;
            }
            else
            {
                // El ajuste Descongelado sale de esta fila pero reaparece en una fila DERIVADO.
                dcDiferencia = Math.Round(
                    dcMontoCuenta - (dcTotal + dcReclasificadoDescongelado), 4);
                blCuadra = Math.Abs(dcDiferencia) < 0.01m;
            }
        }
    }

    public sealed class CostoProductivoResumenDto
    {
        [JsonProperty("registros")][Column("registros")] public int intRegistros { get; set; }
        [JsonProperty("cuentasContables")][Column("cuentasContables")] public int intCuentasContables { get; set; }
        [JsonProperty("cuentasSinSaldo")][Column("cuentasSinSaldo")] public int intCuentasSinSaldo { get; set; }

        [JsonProperty("debe")][Column("debe")] public decimal dcDebe { get; set; }
        [JsonProperty("credito")][Column("credito")] public decimal dcCredito { get; set; }
        [JsonProperty("montoCuenta")][Column("montoCuenta")] public decimal dcMontoCuenta { get; set; }

        [JsonProperty("entero")][Column("entero")] public decimal dcEntero { get; set; }
        [JsonProperty("cola")][Column("cola")] public decimal dcCola { get; set; }
        [JsonProperty("subtotal")][Column("subtotal")] public decimal dcSubtotal { get; set; }
        [JsonProperty("valorAgregado")][Column("valorAgregado")] public decimal dcVag { get; set; }

        [JsonProperty("valorAgregadoBruto")]
        [Column("valorAgregadoBruto")]
        public decimal dcValorAgregadoBruto { get; set; }

        [JsonProperty("valorAgregadoNeto")]
        [Column("valorAgregadoNeto")]
        public decimal dcValorAgregadoNeto { get; set; }

        [JsonProperty("reclasificadoDescongelado")]
        [Column("reclasificadoDescongelado")]
        public decimal dcReclasificadoDescongelado { get; set; }

        [JsonProperty("descongelado")]
        [Column("descongelado")]
        public decimal dcDescongelado { get; set; }

        [JsonProperty("librasDescongelado")]
        [Column("librasDescongelado")]
        public decimal dcLibrasDescongelado { get; set; }

        [JsonProperty("tarifaDescongelado")]
        [Column("tarifaDescongelado")]
        public decimal dcTarifaDescongelado { get; set; }

        [JsonProperty("descongeladoSinAsignar")]
        [Column("descongeladoSinAsignar")]
        public decimal dcDescongeladoSinAsignar { get; set; }

        [JsonProperty("lotesDescongeladoSinSalida")]
        [Column("lotesDescongeladoSinSalida")]
        public int intLotesDescongeladoSinSalida { get; set; }

        [JsonProperty("costoDirectoFijo")]
        [Column("costoDirectoFijo")]
        public decimal dcCostoDirectoFijo { get; set; }

        [JsonProperty("costoDirectoVariable")]
        [Column("costoDirectoVariable")]
        public decimal dcCostoDirectoVariable { get; set; }

        [JsonProperty("costoIndirectoFijo")]
        [Column("costoIndirectoFijo")]
        public decimal dcCostoIndirectoFijo { get; set; }

        [JsonProperty("costoIndirectoVariable")]
        [Column("costoIndirectoVariable")]
        public decimal dcCostoIndirectoVariable { get; set; }

        [JsonProperty("total")][Column("total")] public decimal dcTotal { get; set; }
        [JsonProperty("diferencia")][Column("diferencia")] public decimal dcDiferencia { get; set; }
        [JsonProperty("cuadra")][Column("cuadra")] public bool blCuadra { get; set; }

        public static CostoProductivoResumenDto Crear(
            IEnumerable<CostoProductivoCuentaDto> datos,
            DescongeladoResultadoDto? descongelado = null)
        {
            List<CostoProductivoCuentaDto> lista = datos.ToList();
            List<CostoProductivoCuentaDto> contables =
                lista.Where(x => !x.blEsDerivado).ToList();

            decimal montoCuenta = contables.Sum(x => x.dcMontoCuenta);
            decimal total = lista.Sum(x => x.dcTotal);
            decimal reclasificado = contables.Sum(x => x.dcReclasificadoDescongelado);
            decimal vagNeto = contables.Sum(x => x.dcMontoVag);

            return new CostoProductivoResumenDto
            {
                intRegistros = lista.Count,
                intCuentasContables = contables.Select(x => x.strCuenta).Distinct().Count(),
                intCuentasSinSaldo = contables.Count(x => !x.blEncontradaSong),
                dcDebe = contables.Sum(x => x.dcDebe),
                dcCredito = contables.Sum(x => x.dcCredito),
                dcMontoCuenta = montoCuenta,
                dcEntero = lista.Sum(x => x.dcMontoEntero),
                dcCola = lista.Sum(x => x.dcMontoCola),
                dcSubtotal = lista.Sum(x => x.dcSubtotal),
                dcVag = lista.Sum(x => x.dcMontoVag),
                dcValorAgregadoBruto = vagNeto + reclasificado,
                dcValorAgregadoNeto = vagNeto,
                dcReclasificadoDescongelado = reclasificado,
                dcDescongelado = descongelado?.dcMontoAsignado ?? 0m,
                dcLibrasDescongelado = descongelado?.dcLibrasRecibidasPt ?? 0m,
                dcTarifaDescongelado = descongelado?.dcTarifa ?? 0.02m,
                dcDescongeladoSinAsignar = descongelado?.dcMontoSinAsignar ?? 0m,
                intLotesDescongeladoSinSalida = descongelado?.intLotesSinSalida ?? 0,
                dcCostoDirectoFijo = lista.Sum(x => x.dcCostoDirectoFijo),
                dcCostoDirectoVariable = lista.Sum(x => x.dcCostoDirectoVariable),
                dcCostoIndirectoFijo = lista.Sum(x => x.dcCostoIndirectoFijo),
                dcCostoIndirectoVariable = lista.Sum(x => x.dcCostoIndirectoVariable),
                dcTotal = total,
                dcDiferencia = Math.Round(montoCuenta - total, 4),
                blCuadra =
                    Math.Abs(montoCuenta - total) < 0.01m &&
                    contables.All(x => x.blCuadra) &&
                    (descongelado?.dcMontoSinAsignar ?? 0m) < 0.01m
            };
        }
    }


    public sealed class CostoProductivoResultadoDto
    {
        public List<CostoProductivoCuentaDto> lstCuentas { get; set; } = new();
        public CostoProductivoResumenDto objResumen { get; set; } = new();
        public DescongeladoResultadoDto objDescongelado { get; set; } = new();
        public List<CostoProductivoProcesoOrigenDto> lstProcesosOrigen { get; set; } = new();
    }

    public sealed class GuardarCostoProductivoRequest
    {
        [JsonProperty("anio")] public string strAnio { get; set; } = string.Empty;
        [JsonProperty("mes")] public string strMes { get; set; } = string.Empty;
        [JsonProperty("usuario")] public string strUsuario { get; set; } = string.Empty;

        [JsonProperty("registros")]
        public List<CostoProductivoCuentaDto> lstRegistros { get; set; } = new();

        [JsonProperty("resumen")]
        public CostoProductivoResumenDto? objResumen { get; set; }
    }

}
