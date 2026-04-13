using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[Table("tb_cabmov2")]
[Index("MovCodcia", "MovTipo", "MovFecha", "MovEstado", Name = "IX_tb_cabmov2")]
[Index("MovCodcia", "MovTipo", "MovNumdoc", Name = "IX_tb_cabmov2_1")]
[Index("MovFecha", Name = "IX_tb_cabmov2_Fecha")]
[Index("MovFecha", "MovFhasta", "MovNummov", Name = "IX_tb_cabmov2_fecha_fhasta")]
[Index("MovTipo", Name = "IX_tb_cabmov2_tipo")]
[Index("MovTipo", "MovEstado", "MovFecha", Name = "IX_tb_cabmov2_tipo_estado_FechaDesc", IsDescending = new[] { false, false, true })]
[Index("MovTipo", "MovFecha", "MovEstado", Name = "ix_tbCabmov2_movTipo_moFecha_movEstado")]
[Index("MovClaveAcceso", "MovTipo", Name = "tb_cabmov2_claveAcceso_tipo")]
[Index("MovNumdoc", "MovTipo", Name = "tb_cabmov2_numdoc_tipo")]
public partial class TbCabmov2
{
    [Column("mov_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string MovCodcia { get; set; } = null!;

    [Key]
    [Column("mov_nummov")]
    [StringLength(10)]
    [Unicode(false)]
    public string MovNummov { get; set; } = null!;

    [Column("mov_tipo")]
    [StringLength(6)]
    [Unicode(false)]
    public string MovTipo { get; set; } = null!;

    [Column("mov_numdoc")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovNumdoc { get; set; }

    [Column("mov_numord")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovNumord { get; set; }

    [Column("mov_fecha")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovFecha { get; set; }

    [Column("mov_fhasta")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovFhasta { get; set; }

    [Column("mov_locori")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovLocori { get; set; }

    [Column("mov_bodori")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovBodori { get; set; }

    [Column("mov_provee")]
    [StringLength(13)]
    [Unicode(false)]
    public string? MovProvee { get; set; }

    [Column("mov_forpag")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovForpag { get; set; }

    [Column("mov_intere")]
    public double? MovIntere { get; set; }

    [Column("mov_subtot")]
    public double? MovSubtot { get; set; }

    [Column("mov_poriva")]
    public double? MovPoriva { get; set; }

    [Column("mov_valiva")]
    public double? MovValiva { get; set; }

    [Column("mov_pordes")]
    public double? MovPordes { get; set; }

    [Column("mov_valdes")]
    public double? MovValdes { get; set; }

    [Column("mov_flete")]
    public double? MovFlete { get; set; }

    [Column("mov_total")]
    public double? MovTotal { get; set; }

    [Column("mov_viaent")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovViaent { get; set; }

    [Column("mov_respon")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovRespon { get; set; }

    [Column("mov_compra")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovCompra { get; set; }

    [Column("mov_dirpro")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovDirpro { get; set; }

    [Column("mov_locdes")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovLocdes { get; set; }

    [Column("mov_boddes")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovBoddes { get; set; }

    [Column("mov_cierre")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovCierre { get; set; }

    [Column("mov_confir")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovConfir { get; set; }

    [Column("mov_solici")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovSolici { get; set; }

    [Column("mov_concep")]
    [StringLength(500)]
    [Unicode(false)]
    public string? MovConcep { get; set; }

    [Column("mov_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? MovEstado { get; set; }

    [Column("mov_tipcom")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovTipcom { get; set; }

    [Column("mov_creuse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovCreuse { get; set; }

    [Column("mov_credat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovCredat { get; set; }

    [Column("mov_moduse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovModuse { get; set; }

    [Column("mov_moddat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovModdat { get; set; }

    [Column("mod_eliuse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ModEliuse { get; set; }

    [Column("mod_elidat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ModElidat { get; set; }

    [Column("mov_gastos")]
    public double? MovGastos { get; set; }

    [Column("mov_ruccli")]
    [StringLength(13)]
    [Unicode(false)]
    public string? MovRuccli { get; set; }

    [Column("mov_coment")]
    [StringLength(500)]
    [Unicode(false)]
    public string? MovComent { get; set; }

    [Column("mov_descue")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovDescue { get; set; }

    [Column("mov_cosven")]
    public double? MovCosven { get; set; }

    [Column("mov_linea")]
    [StringLength(5)]
    [Unicode(false)]
    public string? MovLinea { get; set; }

    [Column("mov_centro")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovCentro { get; set; }

    [Column("mov_subcen")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovSubcen { get; set; }

    [Column("mov_ordpro")]
    [StringLength(12)]
    [Unicode(false)]
    public string? MovOrdpro { get; set; }

    [Column("mov_tipegr")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovTipegr { get; set; }

    [Column("mov_refere")]
    [StringLength(8)]
    [Unicode(false)]
    public string? MovRefere { get; set; }

    [Column("mov_nmovfa")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovNmovfa { get; set; }

    [Column("mov_fecdig", TypeName = "datetime")]
    public DateTime? MovFecdig { get; set; }

    [Column("mov_PresupGru", TypeName = "numeric(18, 0)")]
    public decimal? MovPresupGru { get; set; }

    [Column("mov_PresupSub", TypeName = "numeric(18, 0)")]
    public decimal? MovPresupSub { get; set; }

    [Column("mov_Cietun", TypeName = "numeric(18, 0)")]
    public decimal? MovCietun { get; set; }

    [Column("mov_Autom")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovAutom { get; set; }

    [Column("mov_claveIniContingencia")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovClaveIniContingencia { get; set; }

    [Column("mov_desperdicio")]
    public bool? MovDesperdicio { get; set; }

    [Column("mov_codemplRecibe")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovCodemplRecibe { get; set; }

    [Column("mov_codchofer", TypeName = "numeric(18, 0)")]
    public decimal? MovCodchofer { get; set; }

    [Column("mov_codtransp")]
    [StringLength(12)]
    [Unicode(false)]
    public string? MovCodtransp { get; set; }

    [Column("mov_TransfConGuia")]
    public bool? MovTransfConGuia { get; set; }

    [Column("mov_secuen", TypeName = "numeric(18, 0)")]
    public decimal? MovSecuen { get; set; }

    [Column("mov_estabOri")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovEstabOri { get; set; }

    [Column("mov_estabDes")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovEstabDes { get; set; }

    [Column("mov_claveAcceso")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MovClaveAcceso { get; set; }

    [Column("mov_claveAutorizacion")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MovClaveAutorizacion { get; set; }

    [Column("mov_claveContingencia")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MovClaveContingencia { get; set; }

    [Column("mov_ambiente")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovAmbiente { get; set; }

    [Column("mov_emision")]
    [StringLength(2)]
    [Unicode(false)]
    public string? MovEmision { get; set; }

    [Column("mov_fechaHoraClaveAutorizacion", TypeName = "datetime")]
    public DateTime? MovFechaHoraClaveAutorizacion { get; set; }

    [Column("mov_mensaje")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? MovMensaje { get; set; }

    [Column("mov_estaut")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovEstaut { get; set; }

    [Column("mov_sello")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovSello { get; set; }

    [Column("mov_serie")]
    [StringLength(7)]
    [Unicode(false)]
    public string? MovSerie { get; set; }

    [Column("mov_xml", TypeName = "xml")]
    public string? MovXml { get; set; }

    [Column("mov_codbar", TypeName = "image")]
    public byte[]? MovCodbar { get; set; }

    [Column("mov_muestra")]
    public bool? MovMuestra { get; set; }
}
