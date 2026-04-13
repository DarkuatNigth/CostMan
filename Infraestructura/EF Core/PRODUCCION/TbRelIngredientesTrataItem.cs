using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_relIngredientesTrata_Item")]
public partial class TbRelIngredientesTrataItem
{
    [Key]
    [Column("rt_id", TypeName = "numeric(18, 0)")]
    public decimal RtId { get; set; }

    [Column("rt_codrel", TypeName = "numeric(10, 0)")]
    public decimal? RtCodrel { get; set; }

    [Column("rt_codItem", TypeName = "numeric(10, 0)")]
    public decimal? RtCodItem { get; set; }

    [Column("rt_tipo")]
    [StringLength(25)]
    [Unicode(false)]
    public string? RtTipo { get; set; }

    [Column("rt_tara", TypeName = "numeric(10, 2)")]
    public decimal? RtTara { get; set; }

    [Column("rt_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RtEstado { get; set; }

    [Column("rt_itemSacoEnKg")]
    public double? RtItemSacoEnKg { get; set; }

    [Column("rt_codItemAlterno", TypeName = "numeric(10, 0)")]
    public decimal? RtCodItemAlterno { get; set; }

    [Column("it_CodItemPrincipalRespaldo", TypeName = "numeric(10, 0)")]
    public decimal? ItCodItemPrincipalRespaldo { get; set; }
}
