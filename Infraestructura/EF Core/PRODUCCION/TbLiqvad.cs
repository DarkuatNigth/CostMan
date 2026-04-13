using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Detalle de liquidaciones valorizadas.
/// </summary>
[Keyless]
[Table("tb_liqvad")]
[Index("LidCodigo", Name = "IDX_tb_liqvad")]
public partial class TbLiqvad
{
    [Column("lid_tipolote")]
    [StringLength(3)]
    [Unicode(false)]
    public string? LidTipolote { get; set; }

    [Column("lid_noliqu", TypeName = "numeric(18, 0)")]
    public decimal LidNoliqu { get; set; }

    [Column("lid_tunel", TypeName = "numeric(18, 0)")]
    public decimal? LidTunel { get; set; }

    [Column("lid_codigo")]
    [StringLength(30)]
    [Unicode(false)]
    public string LidCodigo { get; set; } = null!;

    [Column("lid_codtal", TypeName = "numeric(18, 0)")]
    public decimal LidCodtal { get; set; }

    [Column("lid_cantid", TypeName = "numeric(12, 2)")]
    public decimal LidCantid { get; set; }

    [Column("lid_unidad", TypeName = "numeric(12, 2)")]
    public decimal LidUnidad { get; set; }

    [Column("lid_precio", TypeName = "numeric(18, 4)")]
    public decimal LidPrecio { get; set; }

    [Column("lid_total", TypeName = "numeric(12, 2)")]
    public decimal LidTotal { get; set; }

    [Column("lid_preci2", TypeName = "numeric(18, 4)")]
    public decimal? LidPreci2 { get; set; }

    [Column("lid_total2", TypeName = "numeric(12, 2)")]
    public decimal? LidTotal2 { get; set; }

    [Column("lid_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string LidEstado { get; set; } = null!;

    [Column("lid_clase")]
    [StringLength(3)]
    [Unicode(false)]
    public string? LidClase { get; set; }

    [Column("lid_lbscuadre", TypeName = "numeric(12, 2)")]
    public decimal? LidLbscuadre { get; set; }

    [Column("lid_pesopag", TypeName = "numeric(18, 4)")]
    public decimal? LidPesopag { get; set; }
}
