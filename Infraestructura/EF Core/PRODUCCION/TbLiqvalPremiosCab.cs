using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Cabecera de premios de liquidaciones valorizadas.
/// </summary>
[Table("tb_liqvalPremiosCab")]
public partial class TbLiqvalPremiosCab
{
    [Key]
    [Column("lip_id")]
    public long LipId { get; set; }

    [Column("lip_noliqu", TypeName = "numeric(18, 0)")]
    public decimal LipNoliqu { get; set; }

    [Column("lip_tipolote")]
    [StringLength(3)]
    [Unicode(false)]
    public string LipTipolote { get; set; } = null!;

    [Column("lip_fecha", TypeName = "datetime")]
    public DateTime? LipFecha { get; set; }

    [Column("lip_tipoPremio")]
    public int? LipTipoPremio { get; set; }

    [Column("lip_estado")]
    public bool LipEstado { get; set; }

    [Column("lip_fecCre", TypeName = "datetime")]
    public DateTime LipFecCre { get; set; }

    [Column("lip_usuCre")]
    [StringLength(60)]
    [Unicode(false)]
    public string LipUsuCre { get; set; } = null!;

    [Column("lip_fecMod", TypeName = "datetime")]
    public DateTime? LipFecMod { get; set; }

    [Column("lip_usuMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? LipUsuMod { get; set; }

    [Column("lip_fecEli", TypeName = "datetime")]
    public DateTime? LipFecEli { get; set; }

    [Column("lip_usuEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? LipUsuEli { get; set; }
}
