using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Infraestructura.EF_Core.SONG
{
    public class TracamAutoResult
    {// --- NÚMEROS SIN DECIMAL (int) ---
     // Usamos long para numeric(18,0) y bigint para asegurar capacidad

        [Column("trc_numsec")]
        public long intTrcNumsec { get; set; } // numeric(18,0), null: 0

        [Column("trc_numtra")]
        public long intTrcNumtra { get; set; } // numeric(18,0), null: 0

        [Column("trc_codpla")]
        public long intTrcCodpla { get; set; } // numeric(18,0), null: 0

        [Column("trc_plades")]
        public long? intTrcPlades { get; set; } // numeric(18,0), null: 1

        [Column("tcd_lote")]
        public long intTcdLote { get; set; } // bigint, null: 0

        [Column("tcd_codtal")]
        public long intTcdCodtal { get; set; } // numeric(18,0), null: 0

        [Column("pallets")]
        public int? intPallets { get; set; } // numeric(2,0), null: 1

        [Column("trc_numerFactLocal")]
        public long? intTrcNumerFactLocal { get; set; } // numeric(18,0), null: 1


        // --- NÚMEROS CON DECIMAL (dc) ---

        [Column("tcd_cantid")]
        public decimal dcTcdCantid { get; set; } // numeric(12,2), null: 0

        [Column("tal_ordvis")]
        public double dcTalOrdvis { get; set; } // float, null: 0

        [Column("libras")]
        public double? dcLibras { get; set; } // float, null: 1

        [Column("master")]
        public double? dcMaster { get; set; } // float, null: 1

        [Column("kilos")]
        public double? dcKilos { get; set; } // float, null: 1


        // --- STRINGS (str) ---

        [Column("trc_ingegr")]
        public string strTrcIngegr { get; set; } = string.Empty; // varchar(1), null: 0

        [Column("trc_tipo")]
        public string strTrcTipo { get; set; } = string.Empty; // varchar(6), null: 0

        [Column("trc_codcam")]
        public string strTrcCodcam { get; set; } = string.Empty; // varchar(2), null: 0

        [Column("trc_camdes")]
        public string? strTrcCamdes { get; set; } // varchar(2), null: 1

        [Column("trc_compro")]
        public string? strTrcCompro { get; set; } // varchar(10), null: 1

        [Column("trc_estado")]
        public string strTrcEstado { get; set; } = string.Empty; // varchar(2), null: 0

        [Column("tcd_produc")]
        public string strTcdProduc { get; set; } = string.Empty; // varchar(30), null: 0

        [Column("pro_desesp")]
        public string strProDesesp { get; set; } = string.Empty; // varchar(100), null: 0

        [Column("pro_clas01")]
        public string strProClas01 { get; set; } = string.Empty; // varchar(3), null: 0

        [Column("pro_proesp")]
        public string? strProProesp { get; set; } // char(1), null: 1

        [Column("tal_descri")]
        public string? strTalDescri { get; set; } // varchar(15), null: 1

        [Column("trs_descri")]
        public string strTrsDescri { get; set; } = string.Empty; // char(30), null: 0

        [Column("bod_ori")]
        public string? strBodOri { get; set; } // varchar(50), null: 1

        [Column("bod_des")]
        public string? strBodDes { get; set; } // varchar(50), null: 1

        [Column("pla_ori")]
        public string? strPlaOri { get; set; } // varchar(20), null: 1

        [Column("pla_des")]
        public string? strPlaDes { get; set; } // varchar(20), null: 1

        [Column("pro_clas02")]
        public string strProClas02 { get; set; } = string.Empty; // varchar(3), null: 0

        //[Column("pro_clas01")]
        //public string strProClas01_Dup { get; set; } = string.Empty; // varchar(3), null: 0 (Duplicado)

        [Column("pro_clas05")]
        public string strProClas05 { get; set; } = string.Empty; // varchar(3), null: 0

        [Column("Ubicacion")]
        public string? strUbicacion { get; set; } // varchar(5), null: 1

        [Column("trc_embfactura")]
        public string? strTrcEmbfactura { get; set; } // varchar(15), null: 1

        [Column("trc_observ")]
        public string? strTrcObserv { get; set; } // varchar(255), null: 1

        [Column("trc_resp")]
        public string? strTrcResp { get; set; } // varchar(50), null: 1

        [Column("UniMedInv")]
        public string strUniMedInv { get; set; } = string.Empty; // varchar(6), null: 0

        [Column("trc_serieFactLocal")]
        public string? strTrcSerieFactLocal { get; set; } // varchar(7), null: 1

        [Column("mov_numdoc")]
        public string? strMovNumdoc { get; set; } // varchar(7), null: 1


        // --- FECHAS (dt) ---

        [Column("trc_fecha")]
        public DateTime dtTrcFecha { get; set; } // datetime, null: 0


        public static ConcurrentDictionary<string, TracamAutoResult> CrearDicMovimiento(
                List<TracamAutoResult> lstPesosReales)
        {
            return new ConcurrentDictionary<string, TracamAutoResult>(
                lstPesosReales  
                    .Where(x => !string.IsNullOrEmpty(x.strMovNumdoc) && !string.IsNullOrWhiteSpace(x.strMovNumdoc))
                    .GroupBy(x => x.strMovNumdoc)
                    .ToDictionary(
                        grp => grp.Key!,
                        grp => grp.First()
                    )
            );
        }
    }
}
