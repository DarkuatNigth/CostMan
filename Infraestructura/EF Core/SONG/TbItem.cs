using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[Table("tb_item")]
[Index("CiaCodigo", "IteStatus", Name = "IX05_tb_item")]
[Index("CiaCodigo", "IteCodigo", Name = "ix_item_codigo")]
[Index("IteCodigo", "LinCodigo", "GrpCodigo", Name = "ixtbitem2")]
public partial class TbItem
{
    [Column("cia_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string CiaCodigo { get; set; } = null!;

    [Key]
    [Column("ite_codigo")]
    [StringLength(18)]
    [Unicode(false)]
    public string IteCodigo { get; set; } = null!;

    [Column("cla_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string ClaCodigo { get; set; } = null!;

    [Column("mod_codigo")]
    [StringLength(100)]
    [Unicode(false)]
    public string ModCodigo { get; set; } = null!;

    [Column("mar_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? MarCodigo { get; set; }

    [Column("med_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string MedCodigo { get; set; } = null!;

    [Column("grp_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string GrpCodigo { get; set; } = null!;

    [Column("sgr_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string SgrCodigo { get; set; } = null!;

    [Column("lin_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string LinCodigo { get; set; } = null!;

    [Column("imp_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ImpCodigo { get; set; }

    [Column("sec_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? SecCodigo { get; set; }

    [Column("cos_codigo")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CosCodigo { get; set; }

    [Column("con_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ConCodigo { get; set; }

    [Column("tip_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? TipCodigo { get; set; }

    [Column("ite_descor")]
    [StringLength(200)]
    [Unicode(false)]
    public string? IteDescor { get; set; }

    [Column("ite_desven")]
    [StringLength(200)]
    [Unicode(false)]
    public string? IteDesven { get; set; }

    [Column("ite_descom")]
    [StringLength(200)]
    [Unicode(false)]
    public string? IteDescom { get; set; }

    [Column("ite_destec")]
    [StringLength(250)]
    [Unicode(false)]
    public string? IteDestec { get; set; }

    [Column("ite_percha")]
    [StringLength(20)]
    [Unicode(false)]
    public string ItePercha { get; set; } = null!;

    [Column("ite_status")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteStatus { get; set; }

    [Column("ite_combo")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteCombo { get; set; }

    [Column("ite_foto")]
    [StringLength(100)]
    [Unicode(false)]
    public string? IteFoto { get; set; }

    [Column("ite_numpar")]
    [StringLength(20)]
    [Unicode(false)]
    public string? IteNumpar { get; set; }

    [Column("ite_unipes")]
    public double? IteUnipes { get; set; }

    [Column("ite_pescon")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ItePescon { get; set; }

    [Column("ite_univol")]
    [StringLength(15)]
    [Unicode(false)]
    public string? IteUnivol { get; set; }

    [Column("ite_feccre")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteFeccre { get; set; }

    [Column("ite_fabric")]
    [StringLength(100)]
    [Unicode(false)]
    public string? IteFabric { get; set; }

    [Column("ite_serial")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteSerial { get; set; }

    [Column("ite_lotes")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteLotes { get; set; }

    [Column("ite_codbar")]
    [StringLength(15)]
    [Unicode(false)]
    public string? IteCodbar { get; set; }

    [Column("ite_locali")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteLocali { get; set; }

    [Column("ite_ctavta")]
    [StringLength(14)]
    [Unicode(false)]
    public string? IteCtavta { get; set; }

    [Column("ite_ctdvta")]
    [StringLength(14)]
    [Unicode(false)]
    public string? IteCtdvta { get; set; }

    [Column("ite_ctainv")]
    [StringLength(14)]
    [Unicode(false)]
    public string? IteCtainv { get; set; }

    [Column("ite_ctacvt")]
    [StringLength(14)]
    [Unicode(false)]
    public string? IteCtacvt { get; set; }

    [Column("ite_fulcon")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteFulcon { get; set; }

    [Column("ite_creuse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteCreuse { get; set; }

    [Column("ite_moduse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteModuse { get; set; }

    [Column("ite_moddat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteModdat { get; set; }

    [Column("ite_eliuse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteEliuse { get; set; }

    [Column("ite_elidat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteElidat { get; set; }

    [Column("ite_camcta")]
    public double? IteCamcta { get; set; }

    [Column("ite_ctadev")]
    [StringLength(14)]
    [Unicode(false)]
    public string? IteCtadev { get; set; }

    [Column("ite_kardex")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteKardex { get; set; }

    [Column("ite_feccad")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteFeccad { get; set; }

    [Column("ite_clave")]
    [StringLength(12)]
    [Unicode(false)]
    public string? IteClave { get; set; }

    [Column("ite_modpre")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteModpre { get; set; }

    [Column("ite_vta")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteVta { get; set; }

    [Column("ite_unimin")]
    [StringLength(3)]
    [Unicode(false)]
    public string? IteUnimin { get; set; }

    [Column("ite_tipfac")]
    [StringLength(2)]
    [Unicode(false)]
    public string? IteTipfac { get; set; }

    [Column("ite_clacli")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteClacli { get; set; }

    [Column("ite_linfac")]
    [StringLength(3)]
    [Unicode(false)]
    public string? IteLinfac { get; set; }

    [Column("ite_ult_feccom", TypeName = "datetime")]
    public DateTime? IteUltFeccom { get; set; }

    [Column("ite_minimo", TypeName = "numeric(18, 0)")]
    public decimal? IteMinimo { get; set; }

    [Column("ite_maximo", TypeName = "numeric(18, 0)")]
    public decimal? IteMaximo { get; set; }

    [Column("ite_sumdes")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteSumdes { get; set; }

    [Column("ite_MostrarHojaDiaria")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteMostrarHojaDiaria { get; set; }

    [Column("ite_ModObserv")]
    [StringLength(255)]
    [Unicode(false)]
    public string? IteModObserv { get; set; }

    [Column("ite_devoluc")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteDevoluc { get; set; }

    [Column("ite_UltPrcCompra", TypeName = "numeric(18, 2)")]
    public decimal? IteUltPrcCompra { get; set; }

    [Column("ite_PesoEnGramos")]
    public double? ItePesoEnGramos { get; set; }

    [Column("ite_canreo", TypeName = "numeric(18, 0)")]
    public decimal? IteCanreo { get; set; }

    [Column("ite_codbod")]
    [StringLength(2)]
    [Unicode(false)]
    public string? IteCodbod { get; set; }

    [Column("ite_costoCero")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteCostoCero { get; set; }

    [Column("ite_genLotAutomAprob")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteGenLotAutomAprob { get; set; }

    [Column("ite_controlLote")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteControlLote { get; set; }

    [Column("ite_controlEtiq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteControlEtiq { get; set; }

    [Column("ite_consumoEtiquetas")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteConsumoEtiquetas { get; set; }

    [Column("ite_ptoCgo")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ItePtoCgo { get; set; }

    [Column("ite_aplConsumoAut")]
    [StringLength(1)]
    [Unicode(false)]
    public string? IteAplConsumoAut { get; set; }

    [Column("ite_controlaPpto")]
    public int? IteControlaPpto { get; set; }

    [Column("ite_catPpto")]
    [StringLength(18)]
    [Unicode(false)]
    public string? IteCatPpto { get; set; }

    [Column("ite_catNombrePpto")]
    [StringLength(200)]
    [Unicode(false)]
    public string? IteCatNombrePpto { get; set; }

    [Column("ite_periodoPpto")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ItePeriodoPpto { get; set; }

    [Column("ite_clientepropietario")]
    public bool? IteClientepropietario { get; set; }

    [Column("ite_exigeDocSustento")]
    public bool? IteExigeDocSustento { get; set; }

    [Column("ite_creaLoteMater")]
    public bool? IteCreaLoteMater { get; set; }

    [Column("ite_genericLoteMater")]
    [StringLength(10)]
    [Unicode(false)]
    public string? IteGenericLoteMater { get; set; }

    /// <summary>
    /// Se crea este campo para implementacion de IVA 5%. Este codigo debe existir en dbo.tb_imppor.
    /// </summary>
    [Column("ite_prcCodigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ItePrcCodigo { get; set; }

    /// <summary>
    /// Indica si el item debe controlar presupuesto en cantiades en las transacciones de egreso.
    /// </summary>
    [Column("ite_controlaPptoEgresos")]
    public bool? IteControlaPptoEgresos { get; set; }
}
