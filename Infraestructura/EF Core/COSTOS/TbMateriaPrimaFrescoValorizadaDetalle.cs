using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_materiaPrimaFrescoValorizadaDetalle", Schema = "costos")]
public partial class TbMateriaPrimaFrescoValorizadaDetalle
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("mfd_id")]
    public long MfdId { get; set; }

    [Column("mfd_mfId")]
    public int MfdMfId { get; set; }

    [Column("mfd_etapaCodigo")]
    [StringLength(30)]
    [Unicode(false)]
    public string MfdEtapaCodigo { get; set; } = null!;

    [Column("mfd_particion")]
    [StringLength(2)]
    [Unicode(false)]
    public string MfdParticion { get; set; } = null!;

    [Column("mfd_pcCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string MfdPcCodigo { get; set; } = null!;

    [Column("mfd_toCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string MfdToCodigo { get; set; } = null!;

    [Column("mfd_libras", TypeName = "numeric(18, 5)")]
    public decimal MfdLibras { get; set; }

    [Column("mfd_costoUnitario", TypeName = "numeric(18, 5)")]
    public decimal MfdCostoUnitario { get; set; }

    [Column("mfd_costoTotal", TypeName = "numeric(18, 5)")]
    public decimal MfdCostoTotal { get; set; }

    [Column("mfd_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string MfdEstado { get; set; } = null!;

    [Column("mfd_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MfdUsuarioCrea { get; set; } = null!;

    [Column("mfd_fechaCrea", TypeName = "datetime")]
    public DateTime MfdFechaCrea { get; set; }

    [Column("mfd_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MfdEquipoCrea { get; set; } = null!;

    [Column("mfd_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MfdUsuarioMod { get; set; }

    [Column("mfd_fechaMod", TypeName = "datetime")]
    public DateTime? MfdFechaMod { get; set; }

    [Column("mfd_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MfdEquipoMod { get; set; }

    public virtual TbMateriaPrimaFrescoValorizada MfdMf { get; set; } = null!;
}
