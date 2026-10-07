using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_procesocosto", Schema = "costos")]
[Index("PcCodigo", "PcOrden", Name = "UQ_tb_procesocosto_etapa_orden", IsUnique = true)]
public partial class TbProcesocosto
{
    [Key]
    [Column("pc_codigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string PcCodigo { get; set; } = null!;

    [Column("pc_nombre")]
    [StringLength(100)]
    [Unicode(false)]
    public string PcNombre { get; set; } = null!;

    [Column("pc_ecCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string PcEcCodigo { get; set; } = null!;

    [Column("pc_orden")]
    public short PcOrden { get; set; }

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

    [ForeignKey("PcEcCodigo")]
    [InverseProperty("TbProcesocosto")]
    public virtual TbEtapacosto PcEcCodigoNavigation { get; set; } = null!;

    [InverseProperty("CgPcCodigoNavigation")]
    public virtual ICollection<TbConfigTipoLiquidacionProcesoCosto> TbConfigTipoLiquidacionProcesoCosto { get; set; } = new List<TbConfigTipoLiquidacionProcesoCosto>();
}
