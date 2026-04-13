using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_plantaProc_OE")]
public partial class TbPlantaProcOe
{
    [Key]
    [Column("pa_codigo")]
    [StringLength(50)]
    [Unicode(false)]
    public string PaCodigo { get; set; } = null!;

    [Column("pa_descri")]
    [StringLength(250)]
    [Unicode(false)]
    public string? PaDescri { get; set; }

    [Column("pa_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? PaEstado { get; set; }

    [Column("pa_dirimprecep")]
    [StringLength(50)]
    [Unicode(false)]
    public string? PaDirimprecep { get; set; }

    [Column("pa_codBodPlanta")]
    [StringLength(5)]
    [Unicode(false)]
    public string? PaCodBodPlanta { get; set; }

    [Column("pa_codPlantaLote", TypeName = "numeric(10, 0)")]
    public decimal? PaCodPlantaLote { get; set; }

    [Column("pa_codRelacionPlanta")]
    [StringLength(50)]
    [Unicode(false)]
    public string? PaCodRelacionPlanta { get; set; }

    /// <summary>
    /// Codigo/secuencia de la empresa exportadora. Es el valor de la columna eex_secuen de la tabla dbo.tb_empresExport.
    /// </summary>
    [Column("pa_secuenEmpresExport", TypeName = "numeric(18, 0)")]
    public decimal? PaSecuenEmpresExport { get; set; }

    /// <summary>
    /// Indica si la bodega es de coopacking o no
    /// </summary>
    [Column("pa_esCopaking")]
    public bool? PaEsCopaking { get; set; }
}
