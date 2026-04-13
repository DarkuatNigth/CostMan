using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

/// <summary>
/// Cabecera de requisiciones de inventario
/// </summary>
[PrimaryKey("ReqCodcia", "ReqNumreq", "ReqTipo")]
[Table("tb_cabreq")]
[Index("ReqFecha", Name = "IX_tb_cabreq")]
[Index("ReqNumreq", Name = "IX_tb_cabreq_2")]
public partial class TbCabreq
{
    [Key]
    [Column("req_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string ReqCodcia { get; set; } = null!;

    [Key]
    [Column("req_numreq")]
    public double ReqNumreq { get; set; }

    /// <summary>
    /// Tipo de requisicion, siempre es RQ.
    /// </summary>
    [Key]
    [Column("req_tipo")]
    [StringLength(3)]
    [Unicode(false)]
    public string ReqTipo { get; set; } = null!;

    [Column("req_fecha")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqFecha { get; set; }

    [Column("req_hora")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqHora { get; set; }

    [Column("req_solici")]
    [StringLength(11)]
    [Unicode(false)]
    public string? ReqSolici { get; set; }

    [Column("req_observ")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? ReqObserv { get; set; }

    [Column("req_sucurs")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqSucurs { get; set; }

    [Column("req_bodega")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqBodega { get; set; }

    [Column("req_centro")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqCentro { get; set; }

    [Column("req_subcen")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqSubcen { get; set; }

    [Column("req_tipegr")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ReqTipegr { get; set; }

    [Column("req_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ReqEstado { get; set; }

    [Column("req_aprob")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ReqAprob { get; set; }

    [Column("req_fechap")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqFechap { get; set; }

    [Column("req_horapr")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqHorapr { get; set; }

    [Column("req_usuapr")]
    [StringLength(30)]
    [Unicode(false)]
    public string? ReqUsuapr { get; set; }

    [Column("req_ordpro")]
    [StringLength(12)]
    [Unicode(false)]
    public string? ReqOrdpro { get; set; }

    [Column("req_total")]
    public double ReqTotal { get; set; }

    [Column("req_confir")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqConfir { get; set; }

    [Column("req_nomsol")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ReqNomsol { get; set; }

    [Column("req_compcont")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqCompcont { get; set; }

    [Column("creuse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Creuse { get; set; }

    [Column("credat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? Credat { get; set; }

    [Column("moduse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Moduse { get; set; }

    [Column("moddat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? Moddat { get; set; }

    [Column("modhora")]
    [StringLength(15)]
    [Unicode(false)]
    public string? Modhora { get; set; }

    [Column("eliuse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Eliuse { get; set; }

    [Column("elidat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? Elidat { get; set; }

    [Column("elihor")]
    [StringLength(15)]
    [Unicode(false)]
    public string? Elihor { get; set; }

    [Column("req_priori")]
    public double? ReqPriori { get; set; }

    [Column("req_tipcot")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ReqTipcot { get; set; }

    [Column("req_numdoc")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqNumdoc { get; set; }

    [Column("req_usuario")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqUsuario { get; set; }

    [Column("req_equipo")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqEquipo { get; set; }

    [Column("req_PresupGru", TypeName = "numeric(18, 0)")]
    public decimal? ReqPresupGru { get; set; }

    [Column("req_PresupSub", TypeName = "numeric(18, 0)")]
    public decimal? ReqPresupSub { get; set; }

    [Column("req_PresupTip")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ReqPresupTip { get; set; }

    [Column("req_NroOT", TypeName = "numeric(18, 0)")]
    public decimal? ReqNroOt { get; set; }

    [Column("req_EstAnt")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ReqEstAnt { get; set; }

    [Column("req_UsuAper")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqUsuAper { get; set; }

    [Column("req_FecHorAper", TypeName = "datetime")]
    public DateTime? ReqFecHorAper { get; set; }

    [Column("req_EquAper")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ReqEquAper { get; set; }

    [Column("req_MotivAper")]
    [StringLength(255)]
    [Unicode(false)]
    public string? ReqMotivAper { get; set; }

    [Column("req_Cietun", TypeName = "numeric(18, 0)")]
    public decimal? ReqCietun { get; set; }

    [Column("req_Autom")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ReqAutom { get; set; }

    /// <summary>
    /// Subtipo o 2do tipo de requisicion. ST = requisicion de compra. EG = requisicion de egreso.
    /// </summary>
    [Column("req_TipReq")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ReqTipReq { get; set; }

    [Column("req_FueraDePresupuesto")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ReqFueraDePresupuesto { get; set; }

    [Column("req_GenerAuto")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ReqGenerAuto { get; set; }

    [Column("req_numord")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ReqNumord { get; set; }

    [Column("req_emplsol")]
    public double? ReqEmplsol { get; set; }

    /// <summary>
    /// Toma el valor A cuando el registro representa una requisicion de egreso automatico. Toma el valor vacio cuando el registro representa una requisicion de egreso general.
    /// </summary>
    [Column("req_flag")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ReqFlag { get; set; }

    [Column("req_CajMas")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ReqCajMas { get; set; }

    [Column("req_desperdicio")]
    public bool? ReqDesperdicio { get; set; }

    [Column("req_fecprod")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ReqFecprod { get; set; }

    [Column("req_turno")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ReqTurno { get; set; }

    [Column("req_codJefeLinea", TypeName = "numeric(18, 0)")]
    public decimal? ReqCodJefeLinea { get; set; }

    [Column("req_CostoEstimado", TypeName = "numeric(18, 2)")]
    public decimal? ReqCostoEstimado { get; set; }

    [Column("req_muestra")]
    public bool? ReqMuestra { get; set; }

    [Column("req_afCenCodigo")]
    [StringLength(8)]
    [Unicode(false)]
    public string? ReqAfCenCodigo { get; set; }

    [Column("req_codciaRel")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ReqCodciaRel { get; set; }

    [Column("req_numreqRel")]
    public double? ReqNumreqRel { get; set; }

    [Column("req_tipoRel")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ReqTipoRel { get; set; }
}
