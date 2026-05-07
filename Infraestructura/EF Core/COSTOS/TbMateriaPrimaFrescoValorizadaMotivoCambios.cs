using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

/// <summary>
/// Motivos de cambios de datos
/// </summary>
[Keyless]
[Table("tb_materiaPrimaFrescoValorizada_MotivoCambios", Schema = "costos")]
public partial class TbMateriaPrimaFrescoValorizadaMotivoCambios
{
    [Column("mc_id")]
    public int McId { get; set; }

    [Column("mc_mfId")]
    public int McMfId { get; set; }

    [Column("mc_motivo")]
    [Unicode(false)]
    public string McMotivo { get; set; } = null!;

    [Column("mc_costoTotalAnterior", TypeName = "numeric(18, 5)")]
    public decimal? McCostoTotalAnterior { get; set; }

    [Column("mc_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string McEstado { get; set; } = null!;

    [Column("mc_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string McUsuarioCrea { get; set; } = null!;

    [Column("mc_fechaCrea", TypeName = "datetime")]
    public DateTime McFechaCrea { get; set; }

    [Column("mc_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string McEquipoCrea { get; set; } = null!;

    [Column("mc_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? McUsuarioMod { get; set; }

    [Column("mc_fechaMod", TypeName = "datetime")]
    public DateTime? McFechaMod { get; set; }

    [Column("mc_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? McEquipoMod { get; set; }

    [Column("mc_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? McUsuarioEli { get; set; }

    [Column("mc_fechaEli", TypeName = "datetime")]
    public DateTime? McFechaEli { get; set; }

    [Column("mc_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? McEquipoEli { get; set; }
}
