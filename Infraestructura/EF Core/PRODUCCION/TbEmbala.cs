using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_embala")]
[Index("EmbCodigo", Name = "IX_tb_embala_codigo_peso_cantid")]
public partial class TbEmbala
{
    [Key]
    [Column("emb_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string EmbCodigo { get; set; } = null!;

    [Column("emb_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string EmbDescri { get; set; } = null!;

    [Column("emb_peso")]
    public double EmbPeso { get; set; }

    [Column("emb_cantid")]
    public double EmbCantid { get; set; }

    [Column("emb_tipo")]
    [StringLength(2)]
    [Unicode(false)]
    public string? EmbTipo { get; set; }

    [Column("emb_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string EmbEstado { get; set; } = null!;

    [Column("emb_clasif1")]
    public int? EmbClasif1 { get; set; }

    [Column("emb_clasif2")]
    public int? EmbClasif2 { get; set; }

    [Column("emb_empadi", TypeName = "numeric(18, 0)")]
    public decimal? EmbEmpadi { get; set; }

    [Column("emb_decora", TypeName = "numeric(18, 0)")]
    public decimal? EmbDecora { get; set; }

    [Column("emb_retrac", TypeName = "numeric(18, 0)")]
    public decimal? EmbRetrac { get; set; }

    [Column("emb_glaseo", TypeName = "numeric(18, 2)")]
    public decimal? EmbGlaseo { get; set; }

    [Column("emb_pesnet", TypeName = "numeric(14, 4)")]
    public decimal? EmbPesnet { get; set; }

    [Column("emb_unipes", TypeName = "numeric(18, 0)")]
    public decimal? EmbUnipes { get; set; }

    [Column("emb_descvta")]
    [StringLength(30)]
    [Unicode(false)]
    public string? EmbDescvta { get; set; }

    [Column("emb_usucre")]
    [StringLength(15)]
    [Unicode(false)]
    public string? EmbUsucre { get; set; }

    [Column("emb_feccre", TypeName = "datetime")]
    public DateTime? EmbFeccre { get; set; }

    [Column("emb_usumod")]
    [StringLength(15)]
    [Unicode(false)]
    public string? EmbUsumod { get; set; }

    [Column("emb_fecmod", TypeName = "datetime")]
    public DateTime? EmbFecmod { get; set; }

    [Column("emb_ay0101", TypeName = "numeric(12, 4)")]
    public decimal? EmbAy0101 { get; set; }

    [Column("emb_ay0102", TypeName = "numeric(12, 4)")]
    public decimal? EmbAy0102 { get; set; }

    [Column("emb_ay0103", TypeName = "numeric(12, 4)")]
    public decimal? EmbAy0103 { get; set; }

    [Column("emb_ay0104", TypeName = "numeric(12, 4)")]
    public decimal? EmbAy0104 { get; set; }

    [Column("emb_aj0105", TypeName = "numeric(12, 4)")]
    public decimal? EmbAj0105 { get; set; }
}
