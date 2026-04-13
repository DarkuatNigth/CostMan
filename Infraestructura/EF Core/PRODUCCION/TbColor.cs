using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_color")]
public partial class TbColor
{
    [Key]
    [Column("col_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string ColCodigo { get; set; } = null!;

    [Column("col_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string? ColDescri { get; set; }

    [Column("col_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ColEstado { get; set; }
}
