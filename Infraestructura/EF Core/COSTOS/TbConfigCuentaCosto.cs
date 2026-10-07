using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_configCuentaCosto", Schema = "costos")]
public partial class TbConfigCuentaCosto
{
    [Key]
    [Column("cc_id")]
    public short CcId { get; set; }

    [Column("cc_empCodigo")]
    public short CcEmpCodigo { get; set; }

    [Column("cc_ecCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string CcEcCodigo { get; set; } = null!;

    [Column("cc_cenCodigo")]
    [StringLength(10)]
    [Unicode(false)]
    public string CcCenCodigo { get; set; } = null!;

    [Column("cc_subCodigo")]
    [StringLength(10)]
    [Unicode(false)]
    public string CcSubCodigo { get; set; } = null!;

    [Column("cc_ctaNumero")]
    [StringLength(13)]
    [Unicode(false)]
    public string CcCtaNumero { get; set; } = null!;

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

    [Column("ec_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? EcUsuarioMod { get; set; }

    [Column("ec_fechaMod", TypeName = "datetime")]
    public DateTime? EcFechaMod { get; set; }

    [Column("ec_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? EcEquipoMod { get; set; }

    [Column("ec_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? EcUsuarioEli { get; set; }

    [Column("ec_fechaEli", TypeName = "datetime")]
    public DateTime? EcFechaEli { get; set; }

    [Column("ec_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? EcEquipoEli { get; set; }

    [ForeignKey("CcEcCodigo")]
    [InverseProperty("TbConfigCuentaCosto")]
    public virtual TbEtapacosto CcEcCodigoNavigation { get; set; } = null!;
}
