using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_distribucionCosto", Schema = "costos")]
[Index("DpAnio", "DpMes", Name = "IX_tb_distribucionCosto")]
//[Index("DpTipo", "DpAnio", "DpMes", "DpCtaNumero", Name = "UK_tb_distribucionCosto", IsUnique = true)]
public partial class TbDistribucionCosto
{
    [Key]
    [Column("dp_id")]
    public int DpId { get; set; }

    [Column("dp_tipo")]
    [StringLength(5)]
    [Unicode(false)]
    public string DpTipo { get; set; } = null!;

    [Column("dp_fechaCorte")]
    public DateOnly DpFechaCorte { get; set; }

    [Column("dp_anio")]
    public short DpAnio { get; set; }

    [Column("dp_mes")]
    public short DpMes { get; set; }

    [Column("dp_porcentaje", TypeName = "numeric(18, 6)")]
    public decimal DpPorcentaje { get; set; }

    [Column("dp_monto", TypeName = "numeric(18, 6)")]
    public decimal DpMonto { get; set; }

    [Column("dp_valorKW", TypeName = "numeric(18, 5)")]
    public decimal DpValorKw { get; set; }

    [Column("dp_ctaNumero")]
    [StringLength(13)]
    [Unicode(false)]
    public string DpCtaNumero { get; set; } = null!;

    [Column("dp_ctaNatura")]
    [StringLength(1)]
    [Unicode(false)]
    public string DpCtaNatura { get; set; } = null!;

    [Column("dp_codigoMedidor")]
    [StringLength(8)]
    [Unicode(false)]
    public string DpCodigoMedidor { get; set; } = null!;

    [Column("dp_paCodigo")]
    public byte? DpPaCodigo { get; set; }

    [Column("dp_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string DpEstado { get; set; } = null!;

    [Column("dp_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string DpUsuarioCrea { get; set; } = null!;

    [Column("dp_fechaCrea", TypeName = "datetime")]
    public DateTime DpFechaCrea { get; set; }

    [Column("dp_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string DpEquipoCrea { get; set; } = null!;

    [Column("dp_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DpUsuarioMod { get; set; }

    [Column("dp_fechaMod", TypeName = "datetime")]
    public DateTime? DpFechaMod { get; set; }

    [Column("dp_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DpEquipoMod { get; set; }

    [Column("dp_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DpUsuarioEli { get; set; }

    [Column("dp_fechaEli", TypeName = "datetime")]
    public DateTime? DpFechaEli { get; set; }

    [Column("dp_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DpEquipoEli { get; set; }
}
