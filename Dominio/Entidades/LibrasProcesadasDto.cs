using CostManagement.Dominio.Entidades;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Dominio.Entidades
{
    public class LibrasProcesadasDto
    {
        // ── IDENTIFICADORES DE LOTE ──
        [Column("TipCodigo")]
        public string TipCodigo { get; set; }

        [Column("tip_descri")]
        public string TipDescri { get; set; }

        [Column("lot_tipo")]
        public string LotTipo { get; set; }

        // CORREGIDO: string -> int (SQL retorna int por el CAST)
        [Column("LotCopack")]
        public string LotCopack { get; set; }

        [Column("TipoCopacking")]
        public string TipoCopacking { get; set; }

        [Column("lot_numero")]
        public decimal LotNumero { get; set; }

        // CORREGIDO: int -> decimal (SQL retorna numeric(18,0))
        [Column("LoteUnificado")]
        public decimal LoteUnificado { get; set; }

        [Column("lot_proces")]
        public decimal LotProces { get; set; }

        [Column("LotValagr")]
        public decimal? LotValagr { get; set; }

        [Column("LotRecibi")]
        public decimal? LotRecibi { get; set; }

        [Column("lot_fecha")]
        public DateTime LotFecha { get; set; }

        // ── PLANTA Y TIPO ──
        [Column("PlantaProceso")]
        public string PlantaProceso { get; set; }

        [Column("TipoProducto")]
        public string TipoProducto { get; set; }

        [Column("Congelamiento Producto")]
        public string CongelamientoProducto { get; set; }

        // ── PRODUCTO ──
        [Column("rld_prodcod")]
        public string LidProduc { get; set; }

        [Column("DescriProduc")]
        public string ProDesesp { get; set; }

        [Column("Clase Prod")]
        public string ClaseProd { get; set; }

        [Column("Proc Prod")]
        public string ProClas03 { get; set; }

        // ── TALLA ──
        [Column("tal_descri")]
        public string TalDescri { get; set; }

        [Column("LidCodtal")]
        public int LidCodtal { get; set; }

        // ── CANTIDADES ──
        [Column("Libras")]
        public double Procesado { get; set; }

        [Column("CantCajas")]
        public double CantCajas { get; set; }

        [Column("Peso")]
        public double Peso { get; set; }

        [NotMapped]
        [Column("LbsCajasRetra")]
        public decimal LbsCajasRetra { get; set; }

        // ── HIDRATACIÓN ──
        [Column("RecNombre")]
        public string? RecNombre { get; set; }

        [Column("RecTipo")]
        public string? RecTipo { get; set; }

        [Column("RtCodItem")]
        public int? RtCodItem { get; set; }

        [Column("CthHidlbs")]
        public decimal? CthHidlbs { get; set; }

        [Column("CthSallbs")]
        public decimal? CthSallbs { get; set; }

        [Column("recSal")]
        public decimal? RecPorSal { get; set; }

        [Column("recHid")]
        public decimal? RecPorHid { get; set; }

        // ── FLAGS ──
        [Column("blPelado")]
        public bool blPelado { get; set; }

        [Column("blDecorado")]
        public bool blDecorado { get; set; }

        [NotMapped]
        [Column("Retractilado")]
        public bool blRetractilado { get; set; }

        [Column("blDescabezado")]
        public bool BlDescabezado { get; set; }

        // ── BODEGA / EMBALAJE / MEDIDA ──
        [Column("BodCodigo")]
        public string BodCodigo { get; set; }

        // CORREGIDO: bool? -> byte (SQL retorna tinyint, EF no convierte tinyint a bool)
        [Column("BodEsBrine")]
        public bool? BodEsBrine { get; set; }

        [Column("EmbCodigo")]
        public string EmbCodigo { get; set; }

        [Column("MedCodigo")]
        public int MedCodigo { get; set; }

        // ── DECORADO / RETRACTILADO TIPO ──
        [Column("TidCodigo")]
        public int? TidCodigo { get; set; }

        [Column("RtaCodigo")]
        public int? RtaCodigo { get; set; }

        // ── CONGELAMIENTO ──
        [Column("ProCongela")]
        public decimal ProCongela { get; set; }
        // ── AGREGADO: Certificado — descripción del premio si existe ──
        [Column("Certificado")]
        public string Certificado { get; set; }

        // ── AGREGADO: LidPremio — precio del premio, null si no existe ──
        [Column("LidPremio")]
        public decimal? LidPremio { get; set; }

        [Column("lot_observacion")]
        public string? LotObservacion { get; set; }

        [NotMapped]
        public LoteFrsKey objRpckey { get; set; }

        public void ConstruirKey()
        {
            int intCodProd = int.TryParse(LidProduc, out int result) ? result : 0;
            objRpckey = new LoteFrsKey((int)LoteUnificado, intCodProd, LidCodtal);
        }
    }
}
