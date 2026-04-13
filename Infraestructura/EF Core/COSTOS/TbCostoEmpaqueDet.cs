using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Detalle de materiales de empaque utilizados y sus costos por producto y lote
/// </summary>
[Table("tb_costoEmpaqueDet", Schema = "costos")]
public partial class TbCostoEmpaqueDet
{
    [Key]
    [Column("cd_id")]
    public int CdId { get; set; }

    [Column("cd_ccId")]
    public int CdCcId { get; set; }

    [Column("cd_eftId", TypeName = "numeric(18, 0)")]
    public decimal CdEftId { get; set; }

    [Column("cd_eftItem")]
    [StringLength(18)]
    [Unicode(false)]
    public string CdEftItem { get; set; } = null!;

    [Column("cd_eftCantidad", TypeName = "numeric(23, 18)")]
    public decimal CdEftCantidad { get; set; }

    [Column("cd_costoPromedio", TypeName = "numeric(18, 5)")]
    public decimal CdCostoPromedio { get; set; }

    [Column("cd_costoUltimoConsumo", TypeName = "numeric(18, 5)")]
    public decimal CdCostoUltimoConsumo { get; set; }

    [Column("cd_costoDespericioBobina", TypeName = "numeric(18, 5)")]
    public decimal CdCostoDespericioBobina { get; set; }

    [Column("cd_origenCosto")]
    [StringLength(1)]
    [Unicode(false)]
    public string CdOrigenCosto { get; set; } = null!;

    [Column("cd_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string CdEstado { get; set; } = null!;

    [Column("cd_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CdUsuarioCrea { get; set; } = null!;

    [Column("cd_fechaCrea", TypeName = "datetime")]
    public DateTime CdFechaCrea { get; set; }

    [Column("cd_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CdEquipoCrea { get; set; } = null!;

    [Column("cd_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CdUsuarioMod { get; set; }

    [Column("cd_fechaMod", TypeName = "datetime")]
    public DateTime? CdFechaMod { get; set; }

    [Column("cd_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CdEquipoMod { get; set; }

    [ForeignKey("CdCcId")]
    [InverseProperty("TbCostoEmpaqueDet")]
    public virtual TbCostoEmpaqueCab CdCc { get; set; } = null!;
}
