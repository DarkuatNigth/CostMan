using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Detalle de premios de liquidaciones valorizadas.
/// </summary>
[Table("tb_liqvalPremiosDet")]
public partial class TbLiqvalPremiosDet
{
    [Key]
    [Column("lid_id")]
    public long LidId { get; set; }

    [Column("lid_cabId")]
    public long LidCabId { get; set; }

    [Column("lid_noliqu", TypeName = "numeric(18, 0)")]
    public decimal LidNoliqu { get; set; }

    [Column("lid_pais")]
    public int? LidPais { get; set; }

    [Column("lid_producto")]
    [StringLength(30)]
    [Unicode(false)]
    public string LidProducto { get; set; } = null!;

    [Column("lid_tipo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? LidTipo { get; set; }

    [Column("lid_clase")]
    [StringLength(3)]
    [Unicode(false)]
    public string? LidClase { get; set; }

    [Column("lid_talla")]
    public int LidTalla { get; set; }

    [Column("lid_libras", TypeName = "numeric(18, 2)")]
    public decimal LidLibras { get; set; }

    [Column("lid_kilos", TypeName = "numeric(18, 2)")]
    public decimal? LidKilos { get; set; }

    [Column("lid_listaPrecio")]
    public int LidListaPrecio { get; set; }

    [Column("lid_precio", TypeName = "numeric(18, 4)")]
    public decimal? LidPrecio { get; set; }

    [Column("lid_total", TypeName = "numeric(18, 4)")]
    public decimal? LidTotal { get; set; }

    [Column("lid_fecCre", TypeName = "datetime")]
    public DateTime LidFecCre { get; set; }

    [Column("lid_usuCre")]
    [StringLength(60)]
    [Unicode(false)]
    public string LidUsuCre { get; set; } = null!;

    [Column("lid_fecMod", TypeName = "datetime")]
    public DateTime? LidFecMod { get; set; }

    [Column("lid_usuMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? LidUsuMod { get; set; }

    [Column("lid_fecEli", TypeName = "datetime")]
    public DateTime? LidFecEli { get; set; }

    [Column("lid_usuEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? LidUsuEli { get; set; }
}
