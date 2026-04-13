using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_proces")]
public partial class TbProces
{
    [Key]
    [Column("pro_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string ProCodigo { get; set; } = null!;

    [Column("pro_descri")]
    [StringLength(60)]
    [Unicode(false)]
    public string? ProDescri { get; set; }

    [Column("pro_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ProEstado { get; set; }

    [Column("pro_rendim", TypeName = "numeric(18, 0)")]
    public decimal? ProRendim { get; set; }

    [Column("pro_orden", TypeName = "numeric(4, 0)")]
    public decimal? ProOrden { get; set; }

    [Column("pro_congel", TypeName = "numeric(18, 0)")]
    public decimal? ProCongel { get; set; }

    [Column("pro_cocido", TypeName = "numeric(18, 0)")]
    public decimal? ProCocido { get; set; }

    [Column("pro_modali", TypeName = "numeric(18, 0)")]
    public decimal? ProModali { get; set; }

    [Column("pro_tipcol", TypeName = "numeric(18, 0)")]
    public decimal? ProTipcol { get; set; }

    [Column("pro_presen", TypeName = "numeric(18, 0)")]
    public decimal? ProPresen { get; set; }

    [Column("pro_hcm")]
    [StringLength(40)]
    [Unicode(false)]
    public string? ProHcm { get; set; }

    [Column("pro_tiplot")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ProTiplot { get; set; }

    [Column("pro_proctiplot")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ProProctiplot { get; set; }

    [Column("pro_desing")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProDesing { get; set; }

    [Column("pro_desexpo")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProDesexpo { get; set; }

    [Column("pro_desexpi")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ProDesexpi { get; set; }

    [Column("pro_subcls")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ProSubcls { get; set; }

    [Column("pro_ConsidNugget")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProConsidNugget { get; set; }

    [Column("pro_RendimOrigen", TypeName = "numeric(18, 0)")]
    public decimal? ProRendimOrigen { get; set; }

    [Column("pro_SoloVAOE")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProSoloVaoe { get; set; }

    [Column("pro_rendimEntero", TypeName = "numeric(18, 2)")]
    public decimal? ProRendimEntero { get; set; }

    [Column("pro_rendimCola", TypeName = "numeric(8, 2)")]
    public decimal? ProRendimCola { get; set; }

    [Column("pro_pelado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ProPelado { get; set; }

    [Column("pro_marrent", TypeName = "numeric(5, 2)")]
    public decimal? ProMarrent { get; set; }

    [Column("pro_clasep")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ProClasep { get; set; }
}
