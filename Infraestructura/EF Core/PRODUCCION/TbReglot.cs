using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Maestro de lotes de produccion.
/// </summary>
[Table("tb_reglot")]
[Index("RloNumero", "RloPlantaproceso", Name = "IX_tb_reglot_numero_plantaProceso")]
[Index("RloTipolote", "RloFecha", "RloEstado", Name = "IXtb_reglotTIPOFECHAESTADO")]
[Index("RloEstado", Name = "ixreglotEstado")]
[Index("RloFecha", "RloEstado", "RloLoteAdi", Name = "ixreglotfechaestadoadi")]
[Index("RloLoteEmpExt", Name = "ixtb_reglotLoteEmpExt")]
[Index("RloEstado", "RloLoteAdi", Name = "ixtb_reglotestadoAdi")]
[Index("RloLoteAdi", Name = "reglotloteadi")]
public partial class TbReglot
{
    [Key]
    [Column("rlo_numero", TypeName = "numeric(18, 0)")]
    public decimal RloNumero { get; set; }

    /// <summary>
    /// Numero de guia. Actualmente un lote puede estar relacionada a varias guias, relacion que se guarda en la tabla tb_guitra.
    /// </summary>
    [Column("rlo_guitra", TypeName = "numeric(18, 0)")]
    public decimal? RloGuitra { get; set; }

    [Column("rlo_noguia")]
    [StringLength(12)]
    [Unicode(false)]
    public string? RloNoguia { get; set; }

    /// <summary>
    /// Tipo de lote. Se encuentra definido en tb_general campo gen_tipo = TLO. Tambien se encuentra definido en vista general.vw_catalogo campo cat_codigo = TLO
    /// </summary>
    [Column("rlo_tipolote")]
    [StringLength(3)]
    [Unicode(false)]
    public string RloTipolote { get; set; } = null!;

    /// <summary>
    /// Fecha de creacion del lote.
    /// </summary>
    [Column("rlo_fecha", TypeName = "datetime")]
    public DateTime? RloFecha { get; set; }

    [Column("rlo_planta", TypeName = "numeric(18, 0)")]
    public decimal? RloPlanta { get; set; }

    [Column("rlo_piscin")]
    [StringLength(60)]
    [Unicode(false)]
    public string? RloPiscin { get; set; }

    [Column("rlo_enviad", TypeName = "numeric(12, 2)")]
    public decimal? RloEnviad { get; set; }

    [Column("rlo_retira", TypeName = "numeric(12, 2)")]
    public decimal? RloRetira { get; set; }

    [Column("rlo_recibi", TypeName = "numeric(12, 2)")]
    public decimal? RloRecibi { get; set; }

    [Column("rlo_romane", TypeName = "numeric(12, 2)")]
    public decimal? RloRomane { get; set; }

    [Column("rlo_basura", TypeName = "numeric(12, 2)")]
    public decimal? RloBasura { get; set; }

    [Column("rlo_codlot")]
    [StringLength(15)]
    [Unicode(false)]
    public string? RloCodlot { get; set; }

    [Column("rlo_liquid")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RloLiquid { get; set; }

    [Column("rlo_netas", TypeName = "numeric(12, 2)")]
    public decimal? RloNetas { get; set; }

    [Column("rlo_packin")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloPackin { get; set; }

    [Column("rlo_procab", TypeName = "numeric(12, 2)")]
    public decimal RloProcab { get; set; }

    [Column("rlo_xproco", TypeName = "numeric(12, 2)")]
    public decimal RloXproco { get; set; }

    [Column("rlo_procol", TypeName = "numeric(12, 2)")]
    public decimal RloProcol { get; set; }

    [Column("rlo_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RloEstado { get; set; }

    [Column("rlo_selecc")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloSelecc { get; set; }

    [Column("rlo_observ")]
    [StringLength(255)]
    [Unicode(false)]
    public string? RloObserv { get; set; }

    [Column("rlo_nomresp")]
    [StringLength(50)]
    [Unicode(false)]
    public string? RloNomresp { get; set; }

    [Column("pte_fecprod", TypeName = "datetime")]
    public DateTime? PteFecprod { get; set; }

    [Column("pte_numlote")]
    [StringLength(10)]
    [Unicode(false)]
    public string? PteNumlote { get; set; }

    [Column("pte_piscina")]
    [StringLength(5)]
    [Unicode(false)]
    public string? PtePiscina { get; set; }

    [Column("pte_provee")]
    [StringLength(30)]
    [Unicode(false)]
    public string? PteProvee { get; set; }

    [Column("rlo_usecre")]
    [StringLength(10)]
    [Unicode(false)]
    public string? RloUsecre { get; set; }

    [Column("rlo_datcre", TypeName = "datetime")]
    public DateTime? RloDatcre { get; set; }

    [Column("rlo_usemod")]
    [StringLength(10)]
    [Unicode(false)]
    public string? RloUsemod { get; set; }

    [Column("rlo_datmod", TypeName = "datetime")]
    public DateTime? RloDatmod { get; set; }

    [Column("rlo_useeli")]
    [StringLength(10)]
    [Unicode(false)]
    public string? RloUseeli { get; set; }

    [Column("rlo_dateli", TypeName = "datetime")]
    public DateTime? RloDateli { get; set; }

    [Column("rlo_foto")]
    [StringLength(250)]
    [Unicode(false)]
    public string? RloFoto { get; set; }

    [Column("rlo_fotoapb")]
    public bool RloFotoapb { get; set; }

    [Column("rlo_fotoapbref", TypeName = "text")]
    public string? RloFotoapbref { get; set; }

    [Column("rlo_okTrazabilidad")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloOkTrazabilidad { get; set; }

    [Column("rlo_permitecerrarLote")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloPermitecerrarLote { get; set; }

    [Column("rlo_procesodest", TypeName = "numeric(18, 0)")]
    public decimal? RloProcesodest { get; set; }

    [Column("rlo_complocal")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloComplocal { get; set; }

    [Column("rlo_romCola", TypeName = "numeric(12, 2)")]
    public decimal? RloRomCola { get; set; }

    [Column("rlo_plantaproceso")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RloPlantaproceso { get; set; }

    [Column("rlo_fechallegaguia", TypeName = "datetime")]
    public DateTime? RloFechallegaguia { get; set; }

    [Column("rlo_TermEntero")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloTermEntero { get; set; }

    [Column("rlo_FecTermEntero", TypeName = "datetime")]
    public DateTime? RloFecTermEntero { get; set; }

    [Column("rlo_TermCol")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloTermCol { get; set; }

    [Column("rlo_FecTermCol", TypeName = "datetime")]
    public DateTime? RloFecTermCol { get; set; }

    [Column("rlo_UsrFinlote")]
    [StringLength(20)]
    [Unicode(false)]
    public string? RloUsrFinlote { get; set; }

    [Column("rlo_listaPrc", TypeName = "numeric(18, 0)")]
    public decimal? RloListaPrc { get; set; }

    [Column("rlo_usucie")]
    [StringLength(60)]
    [Unicode(false)]
    public string? RloUsucie { get; set; }

    [Column("rlo_feccie", TypeName = "datetime")]
    public DateTime? RloFeccie { get; set; }

    [Column("rlo_equcie")]
    [StringLength(60)]
    [Unicode(false)]
    public string? RloEqucie { get; set; }

    [Column("rlo_tlote")]
    [StringLength(3)]
    [Unicode(false)]
    public string? RloTlote { get; set; }

    [Column("rlo_tproce")]
    [StringLength(6)]
    [Unicode(false)]
    public string? RloTproce { get; set; }

    [Column("rlo_fecLotEtq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloFecLotEtq { get; set; }

    [Column("rlo_loteAdi")]
    [StringLength(10)]
    [Unicode(false)]
    public string? RloLoteAdi { get; set; }

    [Column("rlo_fecLotAdi", TypeName = "datetime")]
    public DateTime? RloFecLotAdi { get; set; }

    [Column("rlo_gmo")]
    public bool? RloGmo { get; set; }

    [Column("rlo_excepcionasc")]
    public bool? RloExcepcionasc { get; set; }

    [Column("rlo_observexcepcion")]
    [StringLength(255)]
    [Unicode(false)]
    public string? RloObservexcepcion { get; set; }

    [Column("rlo_unif")]
    public int? RloUnif { get; set; }

    [Column("rlo_loteunif")]
    [StringLength(50)]
    [Unicode(false)]
    public string? RloLoteunif { get; set; }

    [Column("rlo_certificadoId")]
    public int? RloCertificadoId { get; set; }

    [Column("rlo_loteEmpExt")]
    [StringLength(40)]
    [Unicode(false)]
    public string? RloLoteEmpExt { get; set; }

    [Column("rlo_tipoPesca")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RloTipoPesca { get; set; }

    [Column("rlo_guitraOrigen", TypeName = "numeric(18, 0)")]
    public decimal? RloGuitraOrigen { get; set; }

    [Column("rlo_promEntero", TypeName = "numeric(5, 2)")]
    public decimal? RloPromEntero { get; set; }

    [Column("rlo_animEnt", TypeName = "numeric(12, 2)")]
    public decimal? RloAnimEnt { get; set; }

    [Column("rlo_promCola", TypeName = "numeric(5, 2)")]
    public decimal? RloPromCola { get; set; }

    [Column("rlo_animCola", TypeName = "numeric(12, 2)")]
    public decimal? RloAnimCola { get; set; }

    [Column("rlo_promOrganol", TypeName = "numeric(5, 2)")]
    public decimal? RloPromOrganol { get; set; }

    [Column("rlo_promGeneral", TypeName = "numeric(5, 2)")]
    public decimal? RloPromGeneral { get; set; }

    [Column("rlo_observNC")]
    [StringLength(5000)]
    [Unicode(false)]
    public string? RloObservNc { get; set; }

    [Column("rlo_loteExtPlanta")]
    [StringLength(4)]
    [Unicode(false)]
    public string? RloLoteExtPlanta { get; set; }

    [Column("rlo_aplNC")]
    public bool? RloAplNc { get; set; }

    [Column("rlo_estadoApr")]
    [StringLength(3)]
    [Unicode(false)]
    public string? RloEstadoApr { get; set; }

    [Column("rlo_observaApr")]
    [StringLength(5000)]
    [Unicode(false)]
    public string? RloObservaApr { get; set; }

    [Column("rlo_usuPreApr")]
    [StringLength(50)]
    [Unicode(false)]
    public string? RloUsuPreApr { get; set; }

    [Column("rlo_fecPreApr", TypeName = "datetime")]
    public DateTime? RloFecPreApr { get; set; }

    [Column("rlo_envioXmlNaturisa")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloEnvioXmlNaturisa { get; set; }

    [Column("rlo_fecEnvioXmlNatu", TypeName = "datetime")]
    public DateTime? RloFecEnvioXmlNatu { get; set; }

    [Column("rlo_envioXmlPrecio")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloEnvioXmlPrecio { get; set; }

    [Column("rlo_fecEnvioXmlPrecio", TypeName = "datetime")]
    public DateTime? RloFecEnvioXmlPrecio { get; set; }

    [Column("rlo_envioXmlProceso")]
    [StringLength(1)]
    [Unicode(false)]
    public string? RloEnvioXmlProceso { get; set; }

    [Column("rlo_fecEnvioXmlProceso", TypeName = "datetime")]
    public DateTime? RloFecEnvioXmlProceso { get; set; }

    [Column("rlo_cierreAsignacion")]
    public bool? RloCierreAsignacion { get; set; }

    [Column("rlo_esBap4")]
    public bool? RloEsBap4 { get; set; }

    [Column("rlo_pesoPromedioRechazo", TypeName = "numeric(12, 2)")]
    public decimal? RloPesoPromedioRechazo { get; set; }
}
