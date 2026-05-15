using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Infraestructura.EF_Core.SONG
{
    public class FacturaResult
    {
        // --- STRINGS (str) ---

        [Column("mov_tipo")]
        public string? strMovTipo { get; set; } // varchar(15), nullable: 1

        [Column("gcl_codigo")]
        public string? strGclCodigo { get; set; } // varchar(5), nullable: 1

        [Column("gcl_descri")]
        public string? strGclDescri { get; set; } // varchar(25), nullable: 1

        [Column("cla_codigo")]
        public string strClaCodigo { get; set; } = string.Empty; // varchar(3), nullable: 0

        [Column("cla_descri")]
        public string strClaDescri { get; set; } = string.Empty; // varchar(255), nullable: 0

        [Column("cli_codigo")]
        public string strCliCodigo { get; set; } = string.Empty; // char(6), nullable: 0

        [Column("cli_nomcom")]
        public string strCliNomcom { get; set; } = string.Empty; // varchar(255), nullable: 0

        [Column("cli_ruccli")]
        public string strCliRuccli { get; set; } = string.Empty; // varchar(20), nullable: 0

        [Column("cli_direcc")]
        public string? strCliDirecc { get; set; } // char(200), nullable: 1

        [Column("det_codart")]
        public string? strDetCodart { get; set; } // char(18), nullable: 1

        [Column("pro_desesp")]
        public string? strProDesesp { get; set; } // char(254), nullable: 1

        [Column("tal_descri")]
        public string? strTalDescri { get; set; } // varchar(15), nullable: 1

        [Column("alm_descri")]
        public string? strAlmDescri { get; set; } // varchar(30), nullable: 1

        [Column("mov_numdoc")]
        public string? strMovNumdoc { get; set; } // varchar(17), nullable: 1

        [Column("mov_produc")]
        public string? strMovProduc { get; set; } // varchar(6), nullable: 1

        [Column("tip_descripcion")]
        public string? strTipDescripcion { get; set; } // varchar(50), nullable: 1

        [Column("mar_codigo")]
        public string? strMarCodigo { get; set; } // varchar(3), nullable: 1

        [Column("mar_descri")]
        public string? strMarDescri { get; set; } // varchar(30), nullable: 1

        [Column("gru_codigo")]
        public string? strGruCodigo { get; set; } // varchar(5), nullable: 1

        [Column("gru_descri")]
        public string? strGruDescri { get; set; } // varchar(25), nullable: 1

        [Column("cla_ctacon")]
        public string strClaCtacon { get; set; } = string.Empty; // char(13), nullable: 0

        [Column("cla_abrev")]
        public string? strClaAbrev { get; set; } // varchar(20), nullable: 1

        [Column("mov_exporta")]
        public string? strMovExporta { get; set; } // char(1), nullable: 1

        [Column("cta_clave")]
        public string? strCtaClave { get; set; } // varchar(50), nullable: 1

        [Column("mov_estado")]
        public string? strMovEstado { get; set; } // char(2), nullable: 1

        [Column("med_codigo")]
        public string? strMedCodigo { get; set; } // varchar(31), nullable: 1

        [Column("med_descri")]
        public string? strMedDescri { get; set; } // varchar(30), nullable: 1

        [Column("det_unimed")]
        public string? strDetUnimed { get; set; } // char(10), nullable: 1

        [Column("claprod_codigo")]
        public string? strClaprodCodigo { get; set; } // varchar(2), nullable: 1

        [Column("claprod_descri")]
        public string? strClaprodDescri { get; set; } // varchar(60), nullable: 1

        [Column("medvar_codigo")]
        public string? strMedvarCodigo { get; set; } // char(3), nullable: 1

        [Column("medvar_descri")]
        public string? strMedvarDescri { get; set; } // char(30), nullable: 1

        [Column("tip_descripcion2")]
        public string? strTipDescripcion2 { get; set; } // varchar(50), nullable: 1

        [Column("mov_codcia")]
        public string strMovCodcia { get; set; } = string.Empty; // char(6), nullable: 0

        [Column("pro_clas05")]
        public string? strProClas05 { get; set; } // varchar(3), nullable: 1

        [Column("det_nomart")]
        public string? strDetNomart { get; set; } // char(254), nullable: 1

        [Column("pro_clas02")]
        public string? strProClas02 { get; set; } // varchar(5), nullable: 1

        [Column("det_nomart2")]
        public string? strDetNomart2 { get; set; } // char(254), nullable: 1

        [Column("col_codigo")]
        public string? strColCodigo { get; set; } // varchar(3), nullable: 1

        [Column("col_descri")]
        public string? strColDescri { get; set; } // varchar(30), nullable: 1

        [Column("col_descri2")]
        public string? strColDescri2 { get; set; } // varchar(30), nullable: 1

        [Column("pro_proesp")]
        public string? strProProesp { get; set; } // char(1), nullable: 1

        [Column("pro_clas01")]
        public string? strProClas01 { get; set; } // varchar(3), nullable: 1

        [Column("mov_tipfac")]
        public string strMovTipfac { get; set; } = string.Empty; // char(6), nullable: 0

        [Column("mov_nrobl")]
        public string? strMovNrobl { get; set; } // varchar(25), nullable: 1

        [Column("mov_nrofue")]
        public string? strMovNrofue { get; set; } // varchar(25), nullable: 1

        [Column("mov_nroord")]
        public string? strMovNroord { get; set; } // varchar(25), nullable: 1

        [Column("pro_clas02b")]
        public string? strProClas02b { get; set; } // varchar(5), nullable: 1

        [Column("mov_nroref")]
        public string? strMovNroref { get; set; } // varchar(25), nullable: 1

        [Column("mov_nrodau")]
        public string? strMovNrodau { get; set; } // varchar(25), nullable: 1

        [Column("cpg_referendo")]
        public string strCpgReferendo { get; set; } = string.Empty; // char(30), nullable: 0

        [Column("pro_clas02c")]
        public string? strProClas02c { get; set; } // varchar(5), nullable: 1

        [Column("emp_logo")]
        public string? strEmpLogo { get; set; } // varchar(1), nullable: 1

        [Column("det_iva")]
        public string? strDetIva { get; set; } // char(1), nullable: 1

        [Column("a")]
        public string? strA { get; set; } // varchar(1), nullable: 1

        [Column("Expo_Pais")]
        public string? strExpoPais { get; set; } // varchar(200), nullable: 1

        [Column("Expo_Ciudad")]
        public string? strExpoCiudad { get; set; } // varchar(200), nullable: 1

        [Column("AD")]
        public string? strAd { get; set; } // varchar(12), nullable: 1

        [Column("Telefono")]
        public string? strTelefono { get; set; } // varchar(50), nullable: 1

        [Column("ForPag")]
        public string? strForPag { get; set; } // varchar(20), nullable: 1

        [Column("cta_numero")]
        public string? strCtaNumero { get; set; } // char(13), nullable: 1

        [Column("cta_deslar")]
        public string? strCtaDeslar { get; set; } // varchar(60), nullable: 1

        [Column("mov_claveAutorizacion")]
        public string? strMovClaveAutorizacion { get; set; } // varchar(64), nullable: 1

        [Column("mov_nummov")]
        public string strMovNummov { get; set; } = string.Empty; // char(10), nullable: 0

        [Column("MOV_FORPAG")]
        public string? strMovForpag { get; set; } // char(3), nullable: 1


        // --- NÚMEROS CON DECIMAL (dc) ---

        [Column("det_canti")]
        public double? dcDetCanti { get; set; } // float, nullable: 1

        [Column("det_libras")]
        public double? dcDetLibras { get; set; } // float, nullable: 1

        [Column("det_preuni")]
        public double? dcDetPreuni { get; set; } // float, nullable: 1

        [Column("det_subtot")]
        public double? dcDetSubtot { get; set; } // float, nullable: 1

        [Column("det_poriva")]
        public double? dcDetPoriva { get; set; } // float, nullable: 1

        [Column("det_valiva")]
        public double? dcDetValiva { get; set; } // float, nullable: 1

        [Column("gru_cupo")]
        public decimal? dcGruCupo { get; set; } // decimal(18,2), nullable: 1

        [Column("det_libfun")]
        public double? dcDetLibfun { get; set; } // float, nullable: 1

        [Column("det_libcaj")]
        public double? dcDetLibcaj { get; set; } // float, nullable: 1

        [Column("gcl_cupodl")]
        public decimal? dcGclCupodl { get; set; } // decimal(18,2), nullable: 1

        [Column("RecargoDescuento")]
        public decimal? dcRecargoDescuento { get; set; } // numeric(18,2), nullable: 1

        [Column("RecargoSeguro")]
        public decimal? dcRecargoSeguro { get; set; } // numeric(18,2), nullable: 1


        // --- NÚMEROS SIN DECIMAL (int) ---

        [Column("tal_codigo")]
        public long? intTalCodigo { get; set; } // numeric(18,0), nullable: 1

        [Column("alm_codigo")]
        public long? intAlmCodigo { get; set; } // numeric(18,0), nullable: 1

        [Column("tip_codigo")]
        public byte intTipCodigo { get; set; } // tinyint, nullable: 0


        // --- FECHAS (dt) ---
        // Nota: Se mapean como string porque en SQL son char/varchar, pero se nombran dt por estándar solicitado

        [Column("mov_fecha")]
        public string? dtMovFecha { get; set; } // char(10), nullable: 1

        [Column("mov_fecbl")]
        public string? dtMovFecbl { get; set; } // varchar(8), nullable: 1

        [Column("mov_fecemb")]
        public string? dtMovFecemb { get; set; } // varchar(8), nullable: 1

        [Column("mov_fechab")]
        public string? dtMovFechab { get; set; } // char(10), nullable: 1
    }
}
