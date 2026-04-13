using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[PrimaryKey("LinCodigo", "GrpCodigo")]
[Table("tb_grupo")]
[Index("LinCodigo", "GrpEstado", Name = "IX_tb_grupo")]
[Index("GrpCodigo", "LinCodigo", "GrpStatus", Name = "IX_tb_grupo_1")]
public partial class TbGrupo
{
    [Key]
    [Column("lin_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string LinCodigo { get; set; } = null!;

    [Key]
    [Column("grp_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string GrpCodigo { get; set; } = null!;

    [Column("grp_nombre")]
    [StringLength(30)]
    [Unicode(false)]
    public string? GrpNombre { get; set; }

    [Column("grp_observ")]
    [StringLength(254)]
    [Unicode(false)]
    public string? GrpObserv { get; set; }

    [Column("grp_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? GrpEstado { get; set; }

    [Column("crea_user")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CreaUser { get; set; }

    [Column("crea_date", TypeName = "datetime")]
    public DateTime? CreaDate { get; set; }

    [Column("mod_user")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ModUser { get; set; }

    [Column("mod_date", TypeName = "datetime")]
    public DateTime? ModDate { get; set; }

    [Column("eli_user")]
    [StringLength(10)]
    [Unicode(false)]
    public string? EliUser { get; set; }

    [Column("eli_date", TypeName = "datetime")]
    public DateTime? EliDate { get; set; }

    [Column("grp_status")]
    [StringLength(2)]
    [Unicode(false)]
    public string? GrpStatus { get; set; }

    [Column("grp_ReqRef")]
    [StringLength(1)]
    [Unicode(false)]
    public string? GrpReqRef { get; set; }
}
