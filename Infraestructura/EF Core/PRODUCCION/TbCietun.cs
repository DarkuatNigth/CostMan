using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_cietun")]
[Index("CtuTunpla", "CtuNroEgreso", "CtuNroEgresoCaja", "CtuFechor", Name = "IX02_tb_cietun")]
[Index("CtuTunpla", "CtuNumero", "CtuFechor", "CtuEstado", Name = "IX03_tb_cietun")]
[Index("CtuDatlibera", Name = "IX_tb_cietun_datlibera")]
public partial class TbCietun
{
    [Key]
    [Column("ctu_numero")]
    public int CtuNumero { get; set; }

    [Column("ctu_fechor", TypeName = "datetime")]
    public DateTime CtuFechor { get; set; }

    [Column("ctu_planta", TypeName = "numeric(18, 0)")]
    public decimal CtuPlanta { get; set; }

    [Column("ctu_tunpla")]
    [StringLength(2)]
    [Unicode(false)]
    public string CtuTunpla { get; set; } = null!;

    [Column("ctu_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string CtuEstado { get; set; } = null!;

    [Column("ctu_fecdes", TypeName = "datetime")]
    public DateTime CtuFecdes { get; set; }

    [Column("ctu_fechas", TypeName = "datetime")]
    public DateTime CtuFechas { get; set; }

    [Column("ctu_tiplot")]
    [StringLength(2)]
    [Unicode(false)]
    public string CtuTiplot { get; set; } = null!;

    [Column("ctu_observ")]
    [StringLength(255)]
    [Unicode(false)]
    public string CtuObserv { get; set; } = null!;

    [Column("ctu_consid")]
    [StringLength(1)]
    [Unicode(false)]
    public string CtuConsid { get; set; } = null!;

    [Column("ctu_fecini", TypeName = "datetime")]
    public DateTime CtuFecini { get; set; }

    [Column("ctu_usuario")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CtuUsuario { get; set; }

    [Column("ctu_equipo")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CtuEquipo { get; set; }

    [Column("ctu_estProceso")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CtuEstProceso { get; set; }

    [Column("ctu_datcre", TypeName = "datetime")]
    public DateTime? CtuDatcre { get; set; }

    [Column("ctu_datmod", TypeName = "datetime")]
    public DateTime? CtuDatmod { get; set; }

    [Column("ctu_revetiq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CtuRevetiq { get; set; }

    [Column("ctu_fecrev", TypeName = "datetime")]
    public DateTime? CtuFecrev { get; set; }

    [Column("ctu_revusr")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CtuRevusr { get; set; }

    [Column("ctu_revobs")]
    [StringLength(200)]
    [Unicode(false)]
    public string? CtuRevobs { get; set; }

    [Column("ctu_NroEgreso")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CtuNroEgreso { get; set; }

    [Column("ctu_datlibera", TypeName = "datetime")]
    public DateTime? CtuDatlibera { get; set; }

    [Column("ctu_NroEgresoCaja")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CtuNroEgresoCaja { get; set; }

    [Column("ctu_revisado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CtuRevisado { get; set; }

    [Column("ctu_mensajeError")]
    [StringLength(800)]
    [Unicode(false)]
    public string? CtuMensajeError { get; set; }

    [Column("ctu_fecFinTumb", TypeName = "datetime")]
    public DateTime? CtuFecFinTumb { get; set; }

    [Column("ctu_NroEgresoEtiqCaja")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CtuNroEgresoEtiqCaja { get; set; }

    [Column("ctu_NroEgresoEtiqMast")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CtuNroEgresoEtiqMast { get; set; }
}
