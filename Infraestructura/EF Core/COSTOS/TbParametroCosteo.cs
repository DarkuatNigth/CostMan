using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CostManagementService.Infraestructura.EF_Core;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Parametrización de costeo de produccion por periodo
/// </summary>
[Table("tb_parametroCosteo", Schema = "costos")]
public partial class TbParametroCosteo
{
    [Key]
    [Column("pc_id")]
    public short PcId { get; set; }

    [Column("pc_prId")]
    public byte PcPrId { get; set; }

    [Column("pc_fecha")]
    public DateOnly PcFecha { get; set; }

    [Column("pc_tipoLote")]
    [StringLength(3)]
    [Unicode(false)]
    public string PcTipoLote { get; set; } = null!;

    [Column("pc_libras", TypeName = "numeric(18, 5)")]
    public decimal PcLibras { get; set; }

    [Column("pc_cotoUnitario", TypeName = "numeric(18, 5)")]
    public decimal PcCotoUnitario { get; set; }

    [Column("pc_monto", TypeName = "numeric(18, 5)")]
    public decimal PcMonto { get; set; }

    [Column("pc_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string PcEstado { get; set; } = null!;

    [Column("pc_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string PcUsuarioCrea { get; set; } = null!;

    [Column("pc_fechaCrea", TypeName = "datetime")]
    public DateTime PcFechaCrea { get; set; }

    [Column("pc_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string PcEquipoCrea { get; set; } = null!;

    [Column("pc_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PcUsuarioMod { get; set; }

    [Column("pc_fechaMod", TypeName = "datetime")]
    public DateTime? PcFechaMod { get; set; }

    [Column("pc_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PcEquipoMod { get; set; }

    [Column("pc_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PcUsuarioEli { get; set; }

    [Column("pc_fechaEli", TypeName = "datetime")]
    public DateTime? PcFechaEli { get; set; }

    [Column("pc_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PcEquipoEli { get; set; }

    [ForeignKey("PcPrId")]
    [InverseProperty("TbParametroCosteo")]
    public virtual TbProcesoCosteo PcPr { get; set; } = null!;
}
