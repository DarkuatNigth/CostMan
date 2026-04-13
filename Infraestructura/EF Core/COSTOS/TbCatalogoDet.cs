using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Detalle de catalogo
/// </summary>
[PrimaryKey("DetIdCab", "DetCodigo")]
[Table("tb_catalogoDet", Schema = "general")]
public partial class TbCatalogoDet
{
    /// <summary>
    /// Id automatico.
    /// </summary>
    [Key]
    [Column("det_idCab")]
    public int DetIdCab { get; set; }

    /// <summary>
    /// Codigo del detalle
    /// </summary>
    [Key]
    [Column("det_codigo")]
    [StringLength(15)]
    [Unicode(false)]
    public string DetCodigo { get; set; } = null!;

    /// <summary>
    /// Descripcion
    /// </summary>
    [Column("det_descripcion")]
    [StringLength(1000)]
    [Unicode(false)]
    public string DetDescripcion { get; set; } = null!;

    /// <summary>
    /// Descripcion
    /// </summary>
    [Column("det_descripcionCorta")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DetDescripcionCorta { get; set; }

    /// <summary>
    /// Activo / inactivo
    /// </summary>
    [Column("det_estado")]
    public bool DetEstado { get; set; }

    [Column("det_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string DetUsuarioCrea { get; set; } = null!;

    [Column("det_fechaCrea", TypeName = "datetime")]
    public DateTime DetFechaCrea { get; set; }

    [Column("det_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string DetEquipoCrea { get; set; } = null!;

    [Column("det_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DetUsuarioMod { get; set; }

    [Column("dat_fechaMod", TypeName = "datetime")]
    public DateTime? DatFechaMod { get; set; }

    [Column("det_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DetEquipoMod { get; set; }

    [Column("det_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DetUsuarioEli { get; set; }

    [Column("det_fechaEli", TypeName = "datetime")]
    public DateTime? DetFechaEli { get; set; }

    [Column("det_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DetEquipoEli { get; set; }

    [ForeignKey("DetIdCab")]
    [InverseProperty("TbCatalogoDet")]
    public virtual TbCatalogoCab DetIdCabNavigation { get; set; } = null!;
}
