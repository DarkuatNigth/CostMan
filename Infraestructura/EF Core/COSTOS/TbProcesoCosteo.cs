using CostManagement.Infraestructura.EF_Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Infraestructura.EF_Core;

/// <summary>
/// Procesos para costeo de produccion (primarios: recepcion, codificacion, descabezado); congelacion (brine, iqf, tunel), etc.
/// </summary>
[Table("tb_procesoCosteo", Schema = "costos")]
public partial class TbProcesoCosteo
{
    [Key]
    [Column("pr_id")]
    public byte PrId { get; set; }

    [Column("pr_empCodigo")]
    public short PrEmpCodigo { get; set; }

    [Column("pr_descri")]
    [StringLength(60)]
    [Unicode(false)]
    public string PrDescri { get; set; } = null!;

    [Column("pr_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string PrEstado { get; set; } = null!;

    [Column("pr_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string PrUsuarioCrea { get; set; } = null!;

    [Column("pr_fechaCrea", TypeName = "datetime")]
    public DateTime PrFechaCrea { get; set; }

    [Column("pr_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string PrEquipoCrea { get; set; } = null!;

    [Column("pr_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PrUsuarioMod { get; set; }

    [Column("pr_fechaMod", TypeName = "datetime")]
    public DateTime? PrFechaMod { get; set; }

    [Column("pr_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PrEquipoMod { get; set; }

    [Column("pr_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PrUsuarioEli { get; set; }

    [Column("pr_fechaEli", TypeName = "datetime")]
    public DateTime? PrFechaEli { get; set; }

    [Column("pr_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? PrEquipoEli { get; set; }

    [Column("pr_tipCodigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string? PrTipCodigo { get; set; }

    [Column("pr_editable")]
    public bool? PrEditable { get; set; }
    [InverseProperty("PcPr")]
    public virtual ICollection<TbParametroCosteo> TbParametroCosteo { get; set; } = new List<TbParametroCosteo>();
}
