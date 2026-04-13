using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_marca")]
public partial class TbMarca
{
    [Key]
    [Column("mar_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string MarCodigo { get; set; } = null!;

    [Column("mar_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MarDescri { get; set; }

    [Column("mar_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? MarEstado { get; set; }

    [Column("mar_clase")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MarClase { get; set; }

    [Column("mar_cocede")]
    [StringLength(50)]
    [Unicode(false)]
    public string? MarCocede { get; set; }

    [Column("mar_orden", TypeName = "numeric(3, 0)")]
    public decimal? MarOrden { get; set; }

    [Column("mar_DesVta")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MarDesVta { get; set; }

    [Column("mar_codimg")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MarCodimg { get; set; }
}
