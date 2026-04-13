using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_medida")]
[Index("MedCodigo", Name = "IX_tb_medida_medCodigo_medFactor", IsUnique = true)]
public partial class TbMedida
{
    [Key]
    [Column("med_codigo", TypeName = "numeric(18, 0)")]
    public decimal MedCodigo { get; set; }

    [Column("med_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string MedDescri { get; set; } = null!;

    [Column("med_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? MedEstado { get; set; }

    [Column("med_factor")]
    public double MedFactor { get; set; }

    [Column("med_factor2")]
    public double? MedFactor2 { get; set; }

    [Column("med_kilo")]
    public double? MedKilo { get; set; }

    [Column("med_abrev")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MedAbrev { get; set; }

    [Column("med_abrev2")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MedAbrev2 { get; set; }

    [Column("med_abrev3")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MedAbrev3 { get; set; }

    [Column("med_abrev4")]
    [StringLength(4)]
    [Unicode(false)]
    public string? MedAbrev4 { get; set; }

    [Column("med_factor3")]
    public double? MedFactor3 { get; set; }

    [Column("med_abrev_ing")]
    [StringLength(4)]
    [Unicode(false)]
    public string? MedAbrevIng { get; set; }

    [Column("med_Toneladas", TypeName = "numeric(18, 9)")]
    public decimal? MedToneladas { get; set; }
}
