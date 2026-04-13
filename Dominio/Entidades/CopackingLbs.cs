using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Dominio.Entidades
{
    public class CopackingLbs
    {
        [Column("pla_nombre")]
        public string strPlaNombre { get; set; }

        [Column("lot_brutas")]
        public int intLotBrutas { get; set; }

        [Column("lot_recibi")]
        public int intLotRecibi { get; set; }

        [Column("lot_shello")]
        public int intLotShello { get; set; }

        [Column("lot_proces")]
        public decimal dcLotProces { get; set; }

        [Column("lot_valagr")]
        public int intLotValagr { get; set; }

        [Column("lot_rechaz")]
        public int intLotRechaz { get; set; }

        [Column("tiplot")]
        public string? strTipLot { get; set; }

        [Column("lot_tipo")]
        public string strLotTipo { get; set; }

        [Column("tippro")]
        public string strTipPro { get; set; }

        [Column("TipLot")]
        public string? strTipLotAlt { get; set; }

        [Column("lot_presupuesto")]
        public int intLotPresupuesto { get; set; }
    }
}
