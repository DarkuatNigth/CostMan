using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_almace")]
public partial class TbAlmace
{
    [Key]
    [Column("alm_codigo", TypeName = "numeric(18, 0)")]
    public decimal AlmCodigo { get; set; }

    [Column("alm_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string? AlmDescri { get; set; }

    [Column("alm_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? AlmEstado { get; set; }
}
