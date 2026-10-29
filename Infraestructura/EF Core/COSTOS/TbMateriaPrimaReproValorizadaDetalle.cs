using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_materiaPrimaReproValorizadaDetalle", Schema = "costos")]
public partial class TbMateriaPrimaReproValorizadaDetalle
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("mrd_id")]
    public long MrdId { get; set; }

    [Column("mrd_mrId")]
    public int MrdMrId { get; set; }

    [Column("mrd_etapaCodigo")]
    [StringLength(30)]
    [Unicode(false)]
    public string MrdEtapaCodigo { get; set; } = null!;

    [Column("mrd_particion")]
    [StringLength(2)]
    [Unicode(false)]
    public string MrdParticion { get; set; } = null!;

    [Column("mrd_pcCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string MrdPcCodigo { get; set; } = null!;

    [Column("mrd_toCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string MrdToCodigo { get; set; } = null!;

    [Column("mrd_libras", TypeName = "numeric(18, 5)")]
    public decimal MrdLibras { get; set; }

    [Column("mrd_costoUnitario", TypeName = "numeric(18, 5)")]
    public decimal MrdCostoUnitario { get; set; }

    [Column("mrd_costoTotal", TypeName = "numeric(18, 5)")]
    public decimal MrdCostoTotal { get; set; }

    [Column("mrd_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string MrdEstado { get; set; } = null!;

    [Column("mrd_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MrdUsuarioCrea { get; set; } = null!;

    [Column("mrd_fechaCrea", TypeName = "datetime")]
    public DateTime MrdFechaCrea { get; set; }

    [Column("mrd_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MrdEquipoCrea { get; set; } = null!;

    [Column("mrd_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MrdUsuarioMod { get; set; }

    [Column("mrd_fechaMod", TypeName = "datetime")]
    public DateTime? MrdFechaMod { get; set; }

    [Column("mrd_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MrdEquipoMod { get; set; }

    public virtual TbMateriaPrimaReproValorizada MrdMr { get; set; } = null!;
}
