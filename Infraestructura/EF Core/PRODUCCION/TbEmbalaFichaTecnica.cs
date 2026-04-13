using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[PrimaryKey("EftId", "EftItem", "EftIdioma", "EftTipo", "EftGrupo")]
[Table("tb_EmbalaFichaTecnica")]
public partial class TbEmbalaFichaTecnica
{
    [Key]
    [Column("eft_id", TypeName = "numeric(8, 0)")]
    public decimal EftId { get; set; }

    [Key]
    [Column("eft_item")]
    public int EftItem { get; set; }

    [Column("eft_cantidad")]
    public double EftCantidad { get; set; }

    [Column("eft_observa")]
    [StringLength(250)]
    [Unicode(false)]
    public string? EftObserva { get; set; }

    [Key]
    [Column("eft_idioma")]
    [StringLength(3)]
    [Unicode(false)]
    public string EftIdioma { get; set; } = null!;

    [Column("eft_requerido")]
    [StringLength(1)]
    [Unicode(false)]
    public string? EftRequerido { get; set; }

    [Key]
    [Column("eft_tipo")]
    [StringLength(3)]
    [Unicode(false)]
    public string EftTipo { get; set; } = null!;

    [Key]
    [Column("eft_grupo")]
    [StringLength(3)]
    [Unicode(false)]
    public string EftGrupo { get; set; } = null!;

    [Column("eft_equipo")]
    [StringLength(100)]
    [Unicode(false)]
    public string? EftEquipo { get; set; }

    [Column("eft_fecdig", TypeName = "datetime")]
    public DateTime? EftFecdig { get; set; }
}
