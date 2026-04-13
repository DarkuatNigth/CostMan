using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_grupo")]
public partial class TbGrupo
{
    [Key]
    [Column("grp_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string GrpCodigo { get; set; } = null!;

    [Column("grp_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string? GrpDescri { get; set; }

    [Column("grp_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? GrpEstado { get; set; }

    [Column("grp_ciarealcionada")]
    [StringLength(1)]
    [Unicode(false)]
    public string? GrpCiarealcionada { get; set; }

    [Column("grp_origen")]
    public int? GrpOrigen { get; set; }

    [Column("GRP_ORDEN", TypeName = "numeric(2, 0)")]
    public decimal? GrpOrden { get; set; }

    [Column("grp_validapiscina")]
    public bool? GrpValidapiscina { get; set; }

    [Column("grp_diaspago", TypeName = "numeric(2, 0)")]
    public decimal? GrpDiaspago { get; set; }
}
