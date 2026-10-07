using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_configTipoLiquidacion_procesoCosto", Schema = "costos")]
public partial class TbConfigTipoLiquidacionProcesoCosto
{
    [Key]
    [Column("cg_id")]
    public short CgId { get; set; }

    [Column("cg_pcCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string CgPcCodigo { get; set; } = null!;

    [Column("cg_toCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string CgToCodigo { get; set; } = null!;

    [Column("cg_config")]
    [StringLength(3)]
    [Unicode(false)]
    public string CgConfig { get; set; } = null!;

    [Column("cg_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string CgEstado { get; set; } = null!;

    [Column("cg_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CgUsuarioCrea { get; set; } = null!;

    [Column("cg_fechaCrea", TypeName = "datetime")]
    public DateTime CgFechaCrea { get; set; }

    [Column("cg_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CgEquipoCrea { get; set; } = null!;

    [ForeignKey("CgPcCodigo")]
    [InverseProperty("TbConfigTipoLiquidacionProcesoCosto")]
    public virtual TbProcesocosto CgPcCodigoNavigation { get; set; } = null!;

    [ForeignKey("CgToCodigo")]
    [InverseProperty("TbConfigTipoLiquidacionProcesoCosto")]
    public virtual TbTipoOtrosProcesos CgToCodigoNavigation { get; set; } = null!;
}
