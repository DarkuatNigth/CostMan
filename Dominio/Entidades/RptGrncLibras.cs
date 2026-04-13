using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Dominio.Entidades
{
    public class RptGrncLibras
    {
        [Column("RloEnviad")]
        public decimal dcRloEnviad { get; set; }

        [Column("RloNetas")]
        public decimal dcRloNetas { get; set; }

        [Column("RloProcab")]
        public decimal dcRloProCab { get; set; }

        [Column("RloRomane")]
        public decimal dcRloRomane { get; set; }

        [Column("RloProcesodest")]
        public int intRloProcesodest { get; set; } // Asumido como fecha por el nombre, ajustar a str si es texto

        [Column("RloProcol")]
        public decimal dcRloProCol { get; set; }

        [Column("RloTipolote")]
        public string strRloTipolote { get; set; }

        [Column("RloRecibi")]
        public decimal dcRloRecibi { get; set; }

        [Column("RloXproco")]
        public decimal dcRloXProco { get; set; }

        [Column("PrrDescripcion")]
        public string strPrrDescripcion { get; set; }
    }


    public class LbsCongelamiento 
    {

        public string strProClas03 { get; set; }
        public string strProClas06 { get; set; }
        public string strLiqTunpla { get; set; }
        public string strBodDescri { get; set; }
        public string strBodTipo { get; set; }
        public bool   blBodEsBrine { get; set; }
        public string strBodCateg { get; set; }
        public string strBodTipo2 { get; set; }
        public int    intProCongel { get; set; }
        public string strDprDescri { get; set; }
        public decimal dcLibras  { get; set; }
    }

    public class RptCongInd
    {
        [Column("lot_numero")]
        public decimal dcLotNumero { get; set; }

        [Column("lot_tipo")]
        public string strLotTipo { get; set; }

        [Column("lot_tiplot")]
        public string strLotTipLot { get; set; }

        [Column("tip_descri")]
        public string strTipDescri { get; set; }

        [Column("lot_planta")]
        public decimal intLotPlanta { get; set; }

        [Column("lot_codbod")]
        public string strLotCodBod { get; set; }

        [Column("lot_fecha")]
        public DateTime dtLotFecha { get; set; }

        [Column("lot_brutas")]
        public decimal dcLotBrutas { get; set; }

        [Column("lot_recibi")]
        public decimal dcLotRecibi { get; set; }

        [Column("lot_shello")]
        public decimal dcLotShello { get; set; }

        [Column("lot_proces")]
        public decimal dcLotProces { get; set; }

        [Column("lot_valagr")]
        public decimal dcLotValAgr { get; set; }

        [Column("lot_rechaz")]
        public decimal dcLotRechaz { get; set; }

        [Column("lot_numdoc2")]
        public int intLotNumDoc { get; set; }

        [Column("lot_estado")]
        public string strLotEstado { get; set; }

        [Column("tip_rdmto")]
        public decimal dcTipRdmto { get; set; }

        [Column("LOT_VALAGRVL")]
        public decimal dcLotValAgrVl { get; set; }

        [Column("LOT_VIGENCIA")]
        public DateTime dtLotVigencia { get; set; }

        [Column("lot_observ")]
        public string strLotObserv { get; set; }

        [Column("dias")]
        public int intDias { get; set; }

        [Column("MES")]
        public int intMes { get; set; }

        [Column("LOT_FREDES")]
        public string strLotFredes { get; set; }

        [Column("CONGELA")]
        public string strCongela { get; set; }

        [Column("LoteUnificado")]
        public string strLoteUnificado { get; set; }

        [Column("LibrasDestajo")]
        public decimal dcLibrasDestajo { get; set; }

        [Column("LibrasTratado")]
        public decimal dcLibrasTratado { get; set; }

        [Column("HorIniDestajo")]
        public DateTime dtHorIniDestajo { get; set; }

        [Column("HorFinDestajo")]
        public DateTime dtHorFinDestajo { get; set; }

        [Column("TallasMP")]
        public string strTallasMP { get; set; }

        [Column("lot_libexport")]
        public decimal dcLotLibExport { get; set; }

        [Column("Tratado")]
        public string strTratado { get; set; }

        [Column("lot_prodped")]
        public string strLotProdPed { get; set; }

        [Column("pro_congela")]
        public int? intProCongela { get; set; }

        [Column("lot_congiqf")]
        public decimal? dcLotCongiqf { get; set; }

        [Column("CTH_LOTE")]
        public double? dbCthLote { get; set; }

        [Column("rec_nombre")]
        public string? strRecNombre { get; set; }

        [Column("rec_tipohid")]
        public string? strRecTipohid { get; set; }

        [Column("rec_tipo")]
        public string? strRecTipo { get; set; }

        [Column("rec_porchid")]
        public decimal? dcRecPorChid { get; set; }

        [Column("rec_porcagua")]
        public decimal? dcRecPorcAgua { get; set; }

        [Column("rec_porcsal")]
        public decimal? dcRecPorcSal { get; set; }

        [Column("rec_porchielo")]
        public decimal? dcRecPorcHielo { get; set; }

        [Column("rec_estado")]
        public string? strRecEstado { get; set; }

        [Column("rec_tiempo")]
        public string? strRecTiempo { get; set; }

        [Column("rec_nombre")]
        public string? strRecNombre2 { get; set; }

        [Column("rec_tipohid")]
        public string? strRecTipohid2 { get; set; }

        [Column("rec_tipo")]
        public string? strRecTipo2 { get; set; }

        [Column("rec_porchid")]
        public decimal? dcRecPorChid2 { get; set; }

        [Column("rec_porcagua")]
        public decimal? dcRecPorcAgua2 { get; set; }

        [Column("rec_porcsal")]
        public decimal? dcRecPorcSal2 { get; set; }

        [Column("rec_porchielo")]
        public decimal? dcRecPorcHielo2 { get; set; }

        [Column("rec_estado")]
        public string? strRecEstado2 { get; set; }

        [Column("rec_tiempo")]
        public int? intRecTiempo { get; set; }

        [Column("LibrasProyectadasValorAgregado")]
        public int intLibrasProyectadasValorAgregado { get; set; }

        [Column("Tratado")]
        public string strTratado2 { get; set; }

        [Column("pro_congela")]
        public int? intProCongela2 { get; set; }

        [Column("lot_congiqf")]
        public decimal? dcLotCongiqf2 { get; set; }


    }

}
