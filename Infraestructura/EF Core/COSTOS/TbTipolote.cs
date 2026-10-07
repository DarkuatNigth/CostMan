using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_tipolote", Schema = "costos")]
public partial class TbTipolote
{
    [Key]
    [Column("tl_codigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string TlCodigo { get; set; } = null!;

    [Column("tl_nombre")]
    [StringLength(100)]
    public string TlNombre { get; set; } = null!;

    [Column("tl_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string TlEstado { get; set; } = null!;

    [Column("tl_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string TlUsuarioCrea { get; set; } = null!;

    [Column("tl_fechaCrea", TypeName = "datetime")]
    public DateTime TlFechaCrea { get; set; }

    [Column("tl_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string TlEquipoCrea { get; set; } = null!;

    [InverseProperty("ToTlCodigoNavigation")]
    public virtual ICollection<TbTipoOtrosProcesos> TbTipoOtrosProcesos { get; set; } = new List<TbTipoOtrosProcesos>();
}
