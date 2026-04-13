using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_pais")]
public partial class TbPais
{
    [Key]
    [Column("pai_codigo", TypeName = "numeric(18, 0)")]
    public decimal PaiCodigo { get; set; }

    [Column("pai_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string PaiDescri { get; set; } = null!;

    [Column("pai_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string PaiEstado { get; set; } = null!;

    [Column("pai_continente", TypeName = "decimal(18, 0)")]
    public decimal? PaiContinente { get; set; }

    [Column("pai_mercado", TypeName = "decimal(18, 0)")]
    public decimal? PaiMercado { get; set; }

    [Column("pai_abrev")]
    [StringLength(30)]
    [Unicode(false)]
    public string? PaiAbrev { get; set; }

    [Column("pai_descriIng")]
    [StringLength(30)]
    [Unicode(false)]
    public string? PaiDescriIng { get; set; }

    [Column("pai_codsri")]
    [StringLength(5)]
    [Unicode(false)]
    public string? PaiCodsri { get; set; }

    [Column("pai_diasretraso", TypeName = "numeric(18, 0)")]
    public decimal? PaiDiasretraso { get; set; }

    [Column("pai_MostrarFechaExpiracion")]
    [StringLength(1)]
    [Unicode(false)]
    public string? PaiMostrarFechaExpiracion { get; set; }

    [Column("pai_valFactor", TypeName = "numeric(5, 2)")]
    public decimal? PaiValFactor { get; set; }

    [Column("pai_agrupa")]
    [StringLength(3)]
    [Unicode(false)]
    public string? PaiAgrupa { get; set; }

    [Column("pai_regla")]
    [StringLength(5)]
    [Unicode(false)]
    public string? PaiRegla { get; set; }

    [Column("pai_incoterm")]
    [StringLength(5)]
    [Unicode(false)]
    public string? PaiIncoterm { get; set; }

    [Column("pai_reffletexLib", TypeName = "numeric(18, 2)")]
    public decimal? PaiReffletexLib { get; set; }

    [Column("pai_temp_block")]
    public int? PaiTempBlock { get; set; }

    [Column("pai_temp_brine")]
    public int? PaiTempBrine { get; set; }

    [Column("pai_usucrea")]
    [StringLength(20)]
    [Unicode(false)]
    public string? PaiUsucrea { get; set; }

    [Column("pai_feccrea", TypeName = "datetime")]
    public DateTime? PaiFeccrea { get; set; }

    [Column("pai_usumod")]
    [StringLength(20)]
    [Unicode(false)]
    public string? PaiUsumod { get; set; }

    [Column("pai_fecmod", TypeName = "datetime")]
    public DateTime? PaiFecmod { get; set; }

    [Column("pai_usueli")]
    [StringLength(20)]
    [Unicode(false)]
    public string? PaiUsueli { get; set; }

    [Column("pai_feceli", TypeName = "datetime")]
    public DateTime? PaiFeceli { get; set; }

    [Column("pai_requiereAnalisis")]
    public bool PaiRequiereAnalisis { get; set; }

    [Column("pai_requiereCarta")]
    public bool? PaiRequiereCarta { get; set; }

    [Column("pai_nombreReporteCarta")]
    [StringLength(400)]
    [Unicode(false)]
    public string? PaiNombreReporteCarta { get; set; }

    [Column("pai_ClonacionPorVirus")]
    public bool? PaiClonacionPorVirus { get; set; }

    [Column("pai_ClonacionPorVigencia")]
    public bool? PaiClonacionPorVigencia { get; set; }

    [Column("pai_tipoSecuencialClonacion")]
    [StringLength(50)]
    [Unicode(false)]
    public string? PaiTipoSecuencialClonacion { get; set; }

    [Column("pai_reglaClonacion")]
    public short? PaiReglaClonacion { get; set; }
}
