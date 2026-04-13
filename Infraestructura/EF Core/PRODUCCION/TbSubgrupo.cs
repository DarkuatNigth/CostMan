using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_subgrupo")]
public partial class TbSubgrupo
{
    [Key]
    [Column("sgr_codigo")]
    [StringLength(2)]
    [Unicode(false)]
    public string SgrCodigo { get; set; } = null!;

    [Column("sgr_descri")]
    [StringLength(60)]
    [Unicode(false)]
    public string? SgrDescri { get; set; }

    [Column("sgr_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? SgrEstado { get; set; }
}
