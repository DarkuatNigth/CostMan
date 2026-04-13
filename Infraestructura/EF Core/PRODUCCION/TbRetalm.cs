using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_retalm")]
public partial class TbRetalm
{
    [Column("rta_codigo", TypeName = "numeric(18, 0)")]
    public decimal RtaCodigo { get; set; }

    [Column("rta_descri")]
    [StringLength(100)]
    [Unicode(false)]
    public string RtaDescri { get; set; } = null!;

    [Column("rta_almace", TypeName = "numeric(18, 0)")]
    public decimal RtaAlmace { get; set; }

    [Column("rta_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RtaEstado { get; set; }
}
