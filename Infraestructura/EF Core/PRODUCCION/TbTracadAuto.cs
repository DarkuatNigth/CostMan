using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_tracadAuto")]
[Index("TcdProduc", Name = "IX_tb_tracadAuto_produc")]
[Index("TcdLote", Name = "ixtb_tracadAutolote")]
public partial class TbTracadAuto
{
    [Column("tcd_numero", TypeName = "numeric(18, 0)")]
    public decimal TcdNumero { get; set; }

    [Column("tcd_lote")]
    public long TcdLote { get; set; }

    [Column("tcd_produc")]
    [StringLength(30)]
    [Unicode(false)]
    public string TcdProduc { get; set; } = null!;

    [Column("tcd_codtal", TypeName = "numeric(18, 0)")]
    public decimal TcdCodtal { get; set; }

    [Column("tcd_cantid", TypeName = "numeric(12, 2)")]
    public decimal TcdCantid { get; set; }

    [Column("tcd_cosnor", TypeName = "numeric(12, 2)")]
    public decimal TcdCosnor { get; set; }

    [Column("tcd_cosdif", TypeName = "numeric(12, 4)")]
    public decimal? TcdCosdif { get; set; }

    [Column("tcd_cubori")]
    [StringLength(1)]
    [Unicode(false)]
    public string TcdCubori { get; set; } = null!;

    [Column("tcd_secori")]
    [StringLength(4)]
    [Unicode(false)]
    public string? TcdSecori { get; set; }

    [Column("tcd_cubdes")]
    [StringLength(1)]
    [Unicode(false)]
    public string TcdCubdes { get; set; } = null!;

    [Column("tcd_secdes")]
    [StringLength(4)]
    [Unicode(false)]
    public string? TcdSecdes { get; set; }

    [Column("tcd_cantajus", TypeName = "numeric(18, 0)")]
    public decimal? TcdCantajus { get; set; }

    [Column("tcd_cantinv", TypeName = "numeric(18, 0)")]
    public decimal? TcdCantinv { get; set; }

    [Column("tcd_producVta")]
    [StringLength(30)]
    [Unicode(false)]
    public string? TcdProducVta { get; set; }

    [Column("tcd_codtalVta", TypeName = "numeric(18, 0)")]
    public decimal? TcdCodtalVta { get; set; }

    [Column("tcd_embvta")]
    [StringLength(6)]
    [Unicode(false)]
    public string? TcdEmbvta { get; set; }

    [Column("tcd_codtalbce", TypeName = "numeric(18, 0)")]
    public decimal? TcdCodtalbce { get; set; }

    [Column("tcd_produc2vta")]
    [StringLength(30)]
    [Unicode(false)]
    public string? TcdProduc2vta { get; set; }

    [Column("tcd_codtal2vta", TypeName = "numeric(18, 0)")]
    public decimal? TcdCodtal2vta { get; set; }

    [Column("tcd_muestra")]
    [StringLength(5)]
    [Unicode(false)]
    public string? TcdMuestra { get; set; }

    [Column("tcd_DescriTalVta")]
    [StringLength(60)]
    [Unicode(false)]
    public string? TcdDescriTalVta { get; set; }

    [Column("tcd_numeroPO")]
    [StringLength(200)]
    [Unicode(false)]
    public string? TcdNumeroPo { get; set; }

    [Column("tcd_prodrelfact")]
    [StringLength(30)]
    [Unicode(false)]
    public string? TcdProdrelfact { get; set; }
}
