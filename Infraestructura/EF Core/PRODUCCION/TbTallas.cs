using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_tallas")]
public partial class TbTallas
{
    [Key]
    [Column("tal_codigo", TypeName = "numeric(18, 0)")]
    public decimal TalCodigo { get; set; }

    [Column("tal_descri")]
    [StringLength(15)]
    [Unicode(false)]
    public string? TalDescri { get; set; }

    [Column("tal_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string TalEstado { get; set; } = null!;

    [Column("tal_tipo")]
    [StringLength(2)]
    [Unicode(false)]
    public string TalTipo { get; set; } = null!;

    [Column("tal_protal")]
    public double TalProtal { get; set; }

    /// <summary>
    /// Ordenamiento en reportes
    /// </summary>
    [Column("tal_ordvis")]
    public double TalOrdvis { get; set; }

    [Column("tal_broken")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TalBroken { get; set; }

    [Column("Tal_GrRgoIni", TypeName = "money")]
    public decimal TalGrRgoIni { get; set; }

    [Column("Tal_GrRgoFin", TypeName = "money")]
    public decimal TalGrRgoFin { get; set; }

    [Column("tal_ocultexp")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TalOcultexp { get; set; }

    [Column("tal_feccre", TypeName = "datetime")]
    public DateTime? TalFeccre { get; set; }

    [Column("tal_predet")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TalPredet { get; set; }

    [Column("tal_IngesaLimite")]
    public int? TalIngesaLimite { get; set; }

    [Column("tal_unimed", TypeName = "numeric(2, 0)")]
    public decimal? TalUnimed { get; set; }

    [Column("tal_permiteUsoLoteClon")]
    public bool? TalPermiteUsoLoteClon { get; set; }

    [Column("tal_vagcategoria")]
    [StringLength(6)]
    [Unicode(false)]
    public string? TalVagcategoria { get; set; }

    [Column("tal_vagcategoriatablaid")]
    public int? TalVagcategoriatablaid { get; set; }

    [Column("tal_tamanoLiq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TalTamanoLiq { get; set; }

    [Column("tal_merLocal")]
    public int? TalMerLocal { get; set; }

    [Column("tal_contMin", TypeName = "numeric(3, 0)")]
    public decimal? TalContMin { get; set; }

    [Column("tal_contMax", TypeName = "numeric(3, 0)")]
    public decimal? TalContMax { get; set; }
}
