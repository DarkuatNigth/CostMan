using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_relprodvar")]
[Index("RpvGrupo", Name = "IX_tb_relprodvar_grupo")]
[Index("RpvPorcen", Name = "IX_tb_relprodvar_porcen")]
[Index("RpvProduc", "RpvGrupo", Name = "IX_tb_relprodvar_produc_grupo")]
[Index("RpvCodigo", "RpvPorcen", Name = "ixrelprodvarcodigoPorcen")]
public partial class TbRelprodvar
{
    [Column("rpv_produc")]
    [StringLength(30)]
    [Unicode(false)]
    public string? RpvProduc { get; set; }

    [Column("rpv_grupo")]
    [StringLength(5)]
    [Unicode(false)]
    public string? RpvGrupo { get; set; }

    [Column("rpv_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? RpvCodigo { get; set; }

    [Column("rpv_idioma")]
    [StringLength(3)]
    [Unicode(false)]
    public string? RpvIdioma { get; set; }

    [Column("rpv_porcen", TypeName = "numeric(8, 4)")]
    public decimal? RpvPorcen { get; set; }

    [Column("rpv_min", TypeName = "numeric(18, 0)")]
    public decimal? RpvMin { get; set; }

    [Key]
    [Column("rpv_id")]
    public int RpvId { get; set; }
}
