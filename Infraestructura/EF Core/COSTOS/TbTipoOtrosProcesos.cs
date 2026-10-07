using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_tipoOtrosProcesos", Schema = "costos")]
public partial class TbTipoOtrosProcesos
{
    [Key]
    [Column("to_codigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string ToCodigo { get; set; } = null!;

    [Column("to_nombre")]
    [StringLength(100)]
    [Unicode(false)]
    public string? ToNombre { get; set; }

    [Column("to_tlCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string ToTlCodigo { get; set; } = null!;

    [Column("to_nivel")]
    public byte ToNivel { get; set; }

    [Column("to_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string ToEstado { get; set; } = null!;

    [Column("to_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string ToUsuarioCrea { get; set; } = null!;

    [Column("to_fechaCrea", TypeName = "datetime")]
    public DateTime ToFechaCrea { get; set; }

    [Column("to_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string ToEquipoCrea { get; set; } = null!;

    [InverseProperty("CgToCodigoNavigation")]
    public virtual ICollection<TbConfigTipoLiquidacionProcesoCosto> TbConfigTipoLiquidacionProcesoCosto { get; set; } = new List<TbConfigTipoLiquidacionProcesoCosto>();

    [ForeignKey("ToTlCodigo")]
    [InverseProperty("TbTipoOtrosProcesos")]
    public virtual TbTipolote ToTlCodigoNavigation { get; set; } = null!;
}
