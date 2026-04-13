using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Maestros de productos del sistema de Produccion
/// </summary>
[Table("tb_produc")]
[Index("ProUnimed", Name = "ixProductounimed")]
[Index("ProChinaEspecial", Name = "ixtb_producChinaEsp")]
[Index("ProClas01", Name = "ixtb_producproclas01")]
public partial class TbProduc
{
    [Key]
    [Column("pro_codcor")]
    [StringLength(30)]
    [Unicode(false)]
    public string ProCodcor { get; set; } = null!;

    [Column("pro_codigo")]
    [StringLength(30)]
    [Unicode(false)]
    public string ProCodigo { get; set; } = null!;

    [Column("pro_clas01")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas01 { get; set; } = null!;

    [Column("pro_clas02")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas02 { get; set; } = null!;

    [Column("pro_clas03")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas03 { get; set; } = null!;

    [Column("pro_clas04")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas04 { get; set; } = null!;

    [Column("pro_clas05")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas05 { get; set; } = null!;

    [Column("pro_clas06")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas06 { get; set; } = null!;

    [Column("pro_clas07")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ProClas07 { get; set; }

    [Column("pro_clas08")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas08 { get; set; } = null!;

    [Column("pro_clas09")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas09 { get; set; } = null!;

    [Column("pro_clas10")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProClas10 { get; set; } = null!;

    [Column("pro_color")]
    [StringLength(2)]
    [Unicode(false)]
    public string ProColor { get; set; } = null!;

    [Column("pro_desesp")]
    [StringLength(100)]
    [Unicode(false)]
    public string ProDesesp { get; set; } = null!;

    [Column("pro_desing")]
    [StringLength(100)]
    [Unicode(false)]
    public string ProDesing { get; set; } = null!;

    [Column("pro_desliq")]
    [StringLength(100)]
    [Unicode(false)]
    public string ProDesliq { get; set; } = null!;

    [Column("pro_desexp")]
    [StringLength(150)]
    [Unicode(false)]
    public string? ProDesexp { get; set; }

    [Column("pro_unimed", TypeName = "decimal(18, 0)")]
    public decimal ProUnimed { get; set; }

    [Column("pro_unialm", TypeName = "decimal(18, 0)")]
    public decimal ProUnialm { get; set; }

    [Column("pro_embala")]
    [StringLength(6)]
    [Unicode(false)]
    public string ProEmbala { get; set; } = null!;

    [Column("pro_capenv")]
    public double ProCapenv { get; set; }

    [Column("pro_envcar")]
    public double ProEnvcar { get; set; }

    [Column("pro_pesnet")]
    public double ProPesnet { get; set; }

    [Column("pro_pesbru")]
    public double ProPesbru { get; set; }

    [Column("pro_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string ProEstado { get; set; } = null!;

    [Column("pro_ordvis")]
    public double ProOrdvis { get; set; }

    [Column("pro_proesp")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProProesp { get; set; }

    [Column("pro_esptec")]
    [StringLength(4000)]
    [Unicode(false)]
    public string? ProEsptec { get; set; }

    [Column("pro_diacadu", TypeName = "decimal(18, 0)")]
    public decimal? ProDiacadu { get; set; }

    [Column("pro_usucre")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ProUsucre { get; set; }

    [Column("pro_usumod")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ProUsumod { get; set; }

    [Column("pro_usueli")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ProUsueli { get; set; }

    [Column("pro_feccre")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProFeccre { get; set; }

    [Column("pro_fecmod")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProFecmod { get; set; }

    [Column("pro_feceli")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProFeceli { get; set; }

    [Column("pro_pesobrutoLb", TypeName = "decimal(12, 4)")]
    public decimal? ProPesobrutoLb { get; set; }

    [Column("pro_import", TypeName = "decimal(18, 0)")]
    public decimal? ProImport { get; set; }

    [Column("pro_glaseo")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProGlaseo { get; set; }

    [Column("pro_equipo")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ProEquipo { get; set; }

    [Column("pro_linpro", TypeName = "numeric(18, 0)")]
    public decimal? ProLinpro { get; set; }

    [Column("pro_origen", TypeName = "numeric(18, 0)")]
    public decimal? ProOrigen { get; set; }

    [Column("pro_especi", TypeName = "numeric(18, 0)")]
    public decimal? ProEspeci { get; set; }

    [Column("pro_tratam", TypeName = "numeric(18, 0)")]
    public decimal? ProTratam { get; set; }

    [Column("pro_etique")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProEtique { get; set; }

    [Column("pro_muestr")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProMuestr { get; set; }

    [Column("pro_generi")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProGeneri { get; set; }

    [Column("pro_receta", TypeName = "numeric(18, 0)")]
    public decimal? ProReceta { get; set; }

    [Column("pro_qtsinpall", TypeName = "numeric(18, 2)")]
    public decimal? ProQtsinpall { get; set; }

    [Column("pro_qtpallet", TypeName = "numeric(18, 2)")]
    public decimal? ProQtpallet { get; set; }

    [Column("pro_ctsxpallet", TypeName = "numeric(18, 2)")]
    public decimal? ProCtsxpallet { get; set; }

    [Column("pro_pesonetoLb", TypeName = "numeric(18, 2)")]
    public decimal? ProPesonetoLb { get; set; }

    [Column("pro_etqcaja", TypeName = "numeric(18, 0)")]
    public decimal? ProEtqcaja { get; set; }

    [Column("pro_iva")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProIva { get; set; }

    [Column("pro_etqImpMast", TypeName = "numeric(8, 0)")]
    public decimal? ProEtqImpMast { get; set; }

    [Column("pro_etqImpcaja", TypeName = "numeric(8, 0)")]
    public decimal? ProEtqImpcaja { get; set; }

    [Column("pro_snretract")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProSnretract { get; set; }

    [Column("pro_snInstrucc")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProSnInstrucc { get; set; }

    [Column("pro_clasdump")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ProClasdump { get; set; }

    [Column("pro_codbaresp")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProCodbaresp { get; set; }

    [Column("pro_MezclaColorxpallet")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProMezclaColorxpallet { get; set; }

    [Column("pro_destino", TypeName = "numeric(18, 0)")]
    public decimal? ProDestino { get; set; }

    [Column("pro_embalaEmp")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ProEmbalaEmp { get; set; }

    [Column("pro_ExcepEtiq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProExcepEtiq { get; set; }

    [Column("pro_ClasePago")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ProClasePago { get; set; }

    [Column("pro_pesnet_Pro")]
    public double ProPesnetPro { get; set; }

    [Column("pro_pesnet_Dec")]
    public double ProPesnetDec { get; set; }

    [Column("pro_ConsidGenerico")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProConsidGenerico { get; set; }

    [Column("pro_modali", TypeName = "numeric(18, 0)")]
    public decimal ProModali { get; set; }

    [Column("pro_grmgla", TypeName = "numeric(12, 2)")]
    public decimal ProGrmgla { get; set; }

    [Column("pro_chkpro")]
    [StringLength(1)]
    [Unicode(false)]
    public string ProChkpro { get; set; } = null!;

    [Column("pro_chkdec")]
    [StringLength(1)]
    [Unicode(false)]
    public string ProChkdec { get; set; } = null!;

    [Column("pro_chkcli")]
    [StringLength(1)]
    [Unicode(false)]
    public string ProChkcli { get; set; } = null!;

    [Column("pro_tipalt", TypeName = "numeric(18, 0)")]
    public decimal ProTipalt { get; set; }

    [Column("pro_tipalm", TypeName = "numeric(18, 0)")]
    public decimal ProTipalm { get; set; }

    [Column("pro_caralm", TypeName = "numeric(18, 0)")]
    public decimal ProCaralm { get; set; }

    [Column("pro_comple", TypeName = "numeric(18, 0)")]
    public decimal ProComple { get; set; }

    [Column("pro_decora", TypeName = "numeric(18, 0)")]
    public decimal ProDecora { get; set; }

    [Column("pro_tipdec", TypeName = "numeric(18, 0)")]
    public decimal ProTipdec { get; set; }

    [Column("pro_retrac", TypeName = "numeric(18, 0)")]
    public decimal ProRetrac { get; set; }

    [Column("pro_dimast", TypeName = "numeric(18, 0)")]
    public decimal ProDimast { get; set; }

    [Column("pro_merca")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProMerca { get; set; } = null!;

    [Column("pro_chknomcorto", TypeName = "numeric(18, 0)")]
    public decimal ProChknomcorto { get; set; }

    [Column("pro_unimedficcli", TypeName = "decimal(18, 0)")]
    public decimal? ProUnimedficcli { get; set; }

    [Column("pro_unimedproducto", TypeName = "decimal(18, 0)")]
    public decimal? ProUnimedproducto { get; set; }

    [Column("pro_unimeddeclarado", TypeName = "decimal(18, 0)")]
    public decimal? ProUnimeddeclarado { get; set; }

    [Column("pro_preliminar")]
    public bool ProPreliminar { get; set; }

    [Column("chkgranel")]
    [StringLength(1)]
    [Unicode(false)]
    public string Chkgranel { get; set; } = null!;

    [Column("chkEanImp")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ChkEanImp { get; set; }

    [Column("pro_colorcocido")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProColorcocido { get; set; }

    [Column("pro_liquidado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProLiquidado { get; set; }

    [Column("pro_InstruccOE")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProInstruccOe { get; set; }

    [Column("pro_OcultaTituloEtiq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProOcultaTituloEtiq { get; set; }

    [Column("pro_CodPallet")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ProCodPallet { get; set; }

    [Column("pro_etqMaster", TypeName = "numeric(18, 0)")]
    public decimal? ProEtqMaster { get; set; }

    [Column("pro_congela", TypeName = "numeric(18, 0)")]
    public decimal? ProCongela { get; set; }

    [Column("pro_coccion", TypeName = "numeric(18, 0)")]
    public decimal? ProCoccion { get; set; }

    [Column("pro_chkpesovta")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProChkpesovta { get; set; }

    [Column("pro_pesovta")]
    public double? ProPesovta { get; set; }

    [Column("pro_unimedpvta", TypeName = "decimal(18, 0)")]
    public decimal? ProUnimedpvta { get; set; }

    [Column("pro_pesoempaque")]
    public double? ProPesoempaque { get; set; }

    [Column("pro_brutoproduccion")]
    public double? ProBrutoproduccion { get; set; }

    [Column("pro_unimedbprod", TypeName = "decimal(18, 0)")]
    public decimal? ProUnimedbprod { get; set; }

    [Column("pro_ImprimeEtiqProv")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProImprimeEtiqProv { get; set; }

    [Column("pro_preglaseado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProPreglaseado { get; set; }

    [Column("pro_unimedglaseo", TypeName = "numeric(18, 0)")]
    public decimal? ProUnimedglaseo { get; set; }

    [Column("pro_pesoGanadoGr", TypeName = "numeric(18, 2)")]
    public decimal? ProPesoGanadoGr { get; set; }

    [Column("pro_origenganadogr")]
    [StringLength(100)]
    [Unicode(false)]
    public string? ProOrigenganadogr { get; set; }

    [Column("pro_numsolicitud")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ProNumsolicitud { get; set; }

    [Column("pro_detSolicitud")]
    [StringLength(5000)]
    [Unicode(false)]
    public string? ProDetSolicitud { get; set; }

    [Column("pro_tipcaj", TypeName = "numeric(18, 0)")]
    public decimal? ProTipcaj { get; set; }

    [Column("pro_revisionmetal")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProRevisionmetal { get; set; }

    [Column("pro_costoDesperdicioBobinaKg", TypeName = "numeric(4, 4)")]
    public decimal? ProCostoDesperdicioBobinaKg { get; set; }

    [Column("pro_certif")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ProCertif { get; set; }

    [Column("pro_tiempoEsp", TypeName = "numeric(18, 0)")]
    public decimal? ProTiempoEsp { get; set; }

    [Column("pro_rendim", TypeName = "numeric(9, 2)")]
    public decimal? ProRendim { get; set; }

    [Column("pro_embalaVtaAut")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ProEmbalaVtaAut { get; set; }

    [Column("pro_claseCotizacion")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ProClaseCotizacion { get; set; }

    [Column("pro_numCartFilas", TypeName = "numeric(10, 2)")]
    public decimal? ProNumCartFilas { get; set; }

    [Column("pro_tipGlaseo")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ProTipGlaseo { get; set; }

    [Column("pro_residualMin", TypeName = "numeric(4, 0)")]
    public decimal? ProResidualMin { get; set; }

    [Column("pro_residualMax", TypeName = "numeric(4, 0)")]
    public decimal? ProResidualMax { get; set; }

    [Column("pro_loteAdi")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProLoteAdi { get; set; }

    [Column("pro_tipLotOtr")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ProTipLotOtr { get; set; }

    [Column("pro_tipTalla", TypeName = "numeric(10, 0)")]
    public decimal? ProTipTalla { get; set; }

    [Column("pro_rectOnline")]
    public int? ProRectOnline { get; set; }

    [Column("pro_decoraObservacion")]
    [StringLength(200)]
    [Unicode(false)]
    public string? ProDecoraObservacion { get; set; }

    [Column("pro_poclte")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProPoclte { get; set; }

    [Column("pro_LoteEmpExt")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProLoteEmpExt { get; set; }

    [Column("pro_CodOtras")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProCodOtras { get; set; }

    [Column("pro_ChinaEspecial")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ProChinaEspecial { get; set; }

    [Column("pro_glasagua", TypeName = "numeric(18, 2)")]
    public decimal? ProGlasagua { get; set; }

    [Column("pro_glashielo", TypeName = "numeric(18, 2)")]
    public decimal? ProGlashielo { get; set; }

    /// <summary>
    /// Codigo de la planta al que corresponde el producto. El valor del campo corresponde a la columna pla_abrev de la tabla dbo.tb_planta, donde el estado es activo.
    /// </summary>
    [Column("pro_ciaconsigcodigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ProCiaconsigcodigo { get; set; }

    [Column("pro_poInterno")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProPoInterno { get; set; }

    [Column("pro_esdescabezado")]
    public bool? ProEsdescabezado { get; set; }

    [Column("pro_escoladescabezado")]
    public bool? ProEscoladescabezado { get; set; }

    /// <summary>
    /// Indica si el producto requiere analisis de microbiologia
    /// </summary>
    [Column("pro_reqAnalisisMicrobiologia")]
    public bool? ProReqAnalisisMicrobiologia { get; set; }

    /// <summary>
    /// Codigo de planta exportadora. Codigo debe existir en tb_plantaProc_OE. Se crea para exportaciones de Songa 2. Este valor tambien puede existir en la tabla tb_NumSecFacExport.
    /// </summary>
    [Column("pro_paCodigoExportadora")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProPaCodigoExportadora { get; set; }
}
