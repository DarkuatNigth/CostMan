using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tbl_ctrltrathid")]
[Index("CthSeclote", "CthFecha", Name = "ix_ctrltrathidsec")]
public partial class TblCtrltrathid
{
    [Key]
    [Column("cth_codigo", TypeName = "numeric(18, 0)")]
    public decimal CthCodigo { get; set; }

    [Column("cth_lote")]
    public double? CthLote { get; set; }

    [Column("cth_fecha")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CthFecha { get; set; }

    [Column("cth_hidratante")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CthHidratante { get; set; }

    [Column("cth_modprod")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CthModprod { get; set; }

    [Column("cth_talla")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CthTalla { get; set; }

    [Column("cth_lbcamxtrat", TypeName = "numeric(18, 2)")]
    public decimal? CthLbcamxtrat { get; set; }

    [Column("cth_lbsentrega", TypeName = "numeric(18, 2)")]
    public decimal? CthLbsentrega { get; set; }

    [Column("cth_notumbler")]
    public double? CthNotumbler { get; set; }

    [Column("cth_hidlbs", TypeName = "numeric(18, 2)")]
    public decimal? CthHidlbs { get; set; }

    [Column("cth_agualbs", TypeName = "numeric(18, 2)")]
    public decimal? CthAgualbs { get; set; }

    [Column("cth_hielolbs", TypeName = "numeric(18, 2)")]
    public decimal? CthHielolbs { get; set; }

    [Column("cth_sallbs", TypeName = "numeric(18, 2)")]
    public decimal? CthSallbs { get; set; }

    [Column("cth_horai")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CthHorai { get; set; }

    [Column("cth_conteo1", TypeName = "numeric(18, 2)")]
    public decimal? CthConteo1 { get; set; }

    [Column("cth_destino")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CthDestino { get; set; }

    [Column("cth_horaf")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CthHoraf { get; set; }

    [Column("cth_conteo2", TypeName = "numeric(18, 2)")]
    public decimal? CthConteo2 { get; set; }

    [Column("cth_usrresp")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CthUsrresp { get; set; }

    [Column("cth_turno")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CthTurno { get; set; }

    [Column("cth_usucre")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CthUsucre { get; set; }

    [Column("cth_horacre", TypeName = "datetime")]
    public DateTime? CthHoracre { get; set; }

    [Column("cth_usumod")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CthUsumod { get; set; }

    [Column("cth_horamod", TypeName = "datetime")]
    public DateTime? CthHoramod { get; set; }

    [Column("cth_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CthEstado { get; set; }

    [Column("cth_Observacion")]
    [StringLength(8000)]
    [Unicode(false)]
    public string? CthObservacion { get; set; }

    [Column("cth_secuencial", TypeName = "numeric(18, 0)")]
    public decimal? CthSecuencial { get; set; }

    [Column("cth_TempEntrada", TypeName = "numeric(18, 2)")]
    public decimal? CthTempEntrada { get; set; }

    [Column("cth_TempSalida", TypeName = "numeric(18, 2)")]
    public decimal? CthTempSalida { get; set; }

    [Column("cth_tempSol")]
    public long? CthTempSol { get; set; }

    [Column("cth_tempEnj")]
    public long? CthTempEnj { get; set; }

    [Column("cth_UnifGrd", TypeName = "numeric(18, 2)")]
    public decimal? CthUnifGrd { get; set; }

    [Column("cth_UnifPeq", TypeName = "numeric(18, 2)")]
    public decimal? CthUnifPeq { get; set; }

    [Column("cth_UnifPorc", TypeName = "numeric(18, 2)")]
    public decimal? CthUnifPorc { get; set; }

    [Column("cth_obsctrlcal")]
    [StringLength(8000)]
    [Unicode(false)]
    public string? CthObsctrlcal { get; set; }

    [Column("cth_crucocido")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CthCrucocido { get; set; }

    [Column("cth_seclote")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CthSeclote { get; set; }
}
