using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_tipdec")]
public partial class TbTipdec
{
    [Column("tid_codigo", TypeName = "numeric(18, 0)")]
    public decimal TidCodigo { get; set; }

    [Column("tid_descri")]
    [StringLength(100)]
    [Unicode(false)]
    public string TidDescri { get; set; } = null!;

    [Column("tid_decoalm", TypeName = "numeric(18, 0)")]
    public decimal TidDecoalm { get; set; }

    [Column("tid_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TidEstado { get; set; }
}
