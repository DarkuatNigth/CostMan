using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[PrimaryKey("VarCodigo", "VarTipo")]
[Table("tb_varios")]
public partial class TbVarios
{
    [Key]
    [Column("var_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string VarCodigo { get; set; } = null!;

    [Column("var_descri")]
    [StringLength(100)]
    [Unicode(false)]
    public string VarDescri { get; set; } = null!;

    [Key]
    [Column("var_tipo")]
    [StringLength(4)]
    [Unicode(false)]
    public string VarTipo { get; set; } = null!;

    [Column("var_orden", TypeName = "numeric(18, 0)")]
    public decimal? VarOrden { get; set; }

    [Column("var_grupo")]
    [StringLength(3)]
    [Unicode(false)]
    public string VarGrupo { get; set; } = null!;

    [Column("var_esCocedero")]
    public bool? VarEsCocedero { get; set; }
}
