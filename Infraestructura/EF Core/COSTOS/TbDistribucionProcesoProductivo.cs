using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_distribucionProcesoProductivo", Schema = "costos")]
public partial class TbDistribucionProcesoProductivo
{
    [Key]
    [Column("dp_id")]
    public int DpId { get; set; }

    [Column("dp_anio")]
    public short DpAnio { get; set; }

    [Column("dp_mes")]
    public short DpMes { get; set; }

    [Column("dp_fechaCorte")]
    public DateOnly DpFechaCorte { get; set; }

    [Column("dp_ecCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string DpEcCodigo { get; set; } = null!;

    [Column("dp_cenCodigo")]
    [StringLength(10)]
    [Unicode(false)]
    public string DpCenCodigo { get; set; } = null!;

    [Column("dp_subCodigo")]
    [StringLength(10)]
    [Unicode(false)]
    public string DpSubCodigo { get; set; } = null!;

    [Column("dp_montoEntero", TypeName = "numeric(18,4)")]
    public decimal DpMontoEntero { get; set; }

    [Column("dp_montoCola", TypeName = "numeric(18,4)")]
    public decimal DpMontoCola { get; set; }

    [Column("dp_montoVag", TypeName = "numeric(18,4)")]
    public decimal DpMontoVag { get; set; }

    [Column("dp_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string DpEstado { get; set; } = null!;

    [Column("dp_agrupacionCentro")]
    [StringLength(250)]
    [Unicode(false)]
    public string DpAgrupacionCentro { get; set; } = null!;

    [Column("dp_ctaNatura")]
    [StringLength(1)]
    [Unicode(false)]
    public string DpCtaNatura { get; set; } = null!;

    [Column("dp_grupoCentro")]
    [StringLength(150)]
    [Unicode(false)]
    public string DpGrupoCentro { get; set; } = null!;

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

    [Column("dp_pcCodigo")]
    [StringLength(5)]
    public string? DpPcCodigo { get; set; }

    [Column("dp_ctaNumero")]
    [StringLength(13)]
    public string? DpCtaNumero { get; set; }
}