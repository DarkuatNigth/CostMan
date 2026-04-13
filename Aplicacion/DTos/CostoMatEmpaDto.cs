using DocumentFormat.OpenXml.Drawing;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class CostoMatEmpaDto
    {
        [Column("pro_codcor")]
        public string? strProCodCor { get; set; }

        [Column("pro_desesp")]
        public string? strProDesEsp { get; set; }

        [Column("mar_descri")]
        public string? strMarDescri { get; set; }

        [Column("emb_cantid")]
        public float? dcEmbCantid { get; set; }

        [Column("alm_descri")]
        public string? strAlmDescri { get; set; }

        [Column("emb_peso")]
        public float? dcEmbPeso { get; set; }

        [Column("med_descri")]
        public string? strMedDescri { get; set; }

        [Column("LibxMaster")]
        public float? dcLibxMaster { get; set; }

        [Column("Tipo Proc Prod")]
        public string? strProClas03 { get; set; }

        [Column("eft_item")]
        public int intEftItem { get; set; }

        [Column("ITE_DESCOR")]
        public string strIteDesCor { get; set; }

        [Column("linea")]
        public string? strLinea { get; set; }

        [Column("grupo")]
        public string? strGrupo { get; set; }

        [Column("eft_cantidad")]
        public float? dcEftCantidad { get; set; }

        [Column("cospro")]
        public decimal? dcCosPro { get; set; }

        [Column("CostoXMaster")]
        public float? ftCostoXMaster { get; set; }

        [Column("CostoXLibra")]
        public float? ftCostoXLibra { get; set; }

        [Column("total")]
        public double intTotal { get; set; }

        [Column("CostUltConsumo")]
        public float? dcCostUltConsumo { get; set; }

        [Column("SUBCLASE")]
        public string? strSubClase { get; set; }

        [Column("EMB_DESCRI")]
        public string? strEmbDescri { get; set; }

        [Column("pro_costoDesperdicioBobina")]
        public decimal? dcProCostoDesperdicioBobina { get; set; }

        [NotMapped]
        public string strEstadoEmpaque { get; set; }

        [NotMapped]
        public int intLiqLote { get; set; }

        [Column("Fecha Egreso/Lote")]

        [NotMapped]
        public DateOnly dtFechaLote { get; set; }

        [NotMapped]

        [JsonIgnore]
        public string strEmbCodigo { get; set; }

        [NotMapped]

        [JsonIgnore]
        public decimal dcMedCodigo { get; set; }



        public CostoMatEmpaDto() { }

        public CostoMatEmpaDto(
            string? proCodCor, string? proDesEsp, string? marDescri,
            float? embCantid, string? almDescri, float? embPeso,
            string? medDescri, float? libxMaster, int eftItem,
            string? iteDesCor, string? linea, string? grupo,
            float? eftCantidad, double? cosPro, double total,
            float? costUltConsumo, string? subClase, string? embDescri,
            double? proCostoDesperdicioBobina)
        {
            strProCodCor = proCodCor?.Trim();
            strProDesEsp = proDesEsp?.Trim();
            strMarDescri = marDescri?.Trim();
            dcEmbCantid = embCantid;
            strAlmDescri = almDescri?.Trim();
            dcEmbPeso = embPeso;
            strMedDescri = medDescri?.Trim();
            dcLibxMaster = libxMaster;
            intEftItem = eftItem;
            strIteDesCor = iteDesCor.Trim();
            strLinea = linea?.Trim();
            strGrupo = grupo?.Trim();
            dcEftCantidad = eftCantidad;
            dcCosPro = cosPro != null ? Math.Round((decimal)cosPro, 8) : null;
            intTotal = total;
            dcCostUltConsumo = costUltConsumo != null ? (float?)Math.Round((double)costUltConsumo, 8) : null;
            strSubClase = subClase?.Trim();
            strEmbDescri = embDescri?.Trim();
            dcProCostoDesperdicioBobina = proCostoDesperdicioBobina != null ? Math.Round((decimal)proCostoDesperdicioBobina, 4) : null;
        }
    }

    public class TratamientoProdDto
    {
        public int intProdCod { get; set; }
        public string strTratamiento { get; set; }
        public decimal dcRpvPorcen { get; set; }
        public decimal dcRpvMin { get; set; }

        public TratamientoProdDto(string ProdCod, string Tratamiento, decimal? RpvPorcen, decimal? RpvMin, string RecTiempo)
        {
            intProdCod = Convert.ToInt32(ProdCod);
            strTratamiento = Tratamiento;
            dcRpvPorcen = RpvPorcen ?? 0;
            dcRpvMin = RecTiempo == null ? RpvMin ?? 0 : Convert.ToDecimal(RecTiempo);
        }
    }
}
