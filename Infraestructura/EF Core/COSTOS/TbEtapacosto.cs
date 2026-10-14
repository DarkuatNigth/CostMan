using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_etapacosto", Schema = "costos")]
[Index("EcOrden", Name = "UQ_tb_etapacosto_orden", IsUnique = true)]
public partial class TbEtapacosto
{
    [Key]
    [Column("ec_codigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string EcCodigo { get; set; } = null!;

    [Column("ec_nombre")]
    [StringLength(100)]
    [Unicode(false)]
    public string EcNombre { get; set; } = null!;

    [Column("ec_orden")]
    public byte EcOrden { get; set; }

    [Column("ec_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string EcEstado { get; set; } = null!;

    [Column("ec_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string EcUsuarioCrea { get; set; } = null!;

    [Column("ec_fechaCrea", TypeName = "datetime")]
    public DateTime EcFechaCrea { get; set; }

    [Column("ec_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string EcEquipoCrea { get; set; } = null!;

    [InverseProperty("CcEcCodigoNavigation")]
    public virtual ICollection<TbConfigCuentaCosto> TbConfigCuentaCosto { get; set; } = new List<TbConfigCuentaCosto>();

    [InverseProperty("PcEcCodigoNavigation")]
    public virtual ICollection<TbProcesocosto> TbProcesocosto { get; set; } = new List<TbProcesocosto>();
}
