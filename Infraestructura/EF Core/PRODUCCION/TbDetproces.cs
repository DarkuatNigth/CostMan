using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_detproces")]
public partial class TbDetproces
{
    [Key]
    [Column("dpr_codigo", TypeName = "numeric(18, 0)")]
    public decimal DprCodigo { get; set; }

    [Column("dpr_descri")]
    [StringLength(100)]
    [Unicode(false)]
    public string DprDescri { get; set; } = null!;

    [Column("dpr_descriExpo")]
    [StringLength(100)]
    [Unicode(false)]
    public string? DprDescriExpo { get; set; }

    [Column("dpr_GruDetProces", TypeName = "numeric(18, 0)")]
    public decimal DprGruDetProces { get; set; }

    [Column("dpr_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DprEstado { get; set; }

    [Column("dpr_subcls")]
    [StringLength(3)]
    [Unicode(false)]
    public string? DprSubcls { get; set; }

    [Column("dpr_descri2")]
    [StringLength(100)]
    [Unicode(false)]
    public string DprDescri2 { get; set; } = null!;

    [Column("dpr_rdmtoCorte", TypeName = "decimal(5, 2)")]
    public decimal? DprRdmtoCorte { get; set; }
}
