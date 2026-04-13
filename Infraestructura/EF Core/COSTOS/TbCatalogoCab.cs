using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Cabecera de catalogo
/// </summary>
[Table("tb_catalogoCab", Schema = "general")]
[Index("CatCodigo", Name = "UK_tb_catalogoCab", IsUnique = true)]
public partial class TbCatalogoCab
{
    /// <summary>
    /// Id automatico.
    /// </summary>
    [Key]
    [Column("cat_id")]
    public int CatId { get; set; }

    /// <summary>
    /// Codigo de cabecera
    /// </summary>
    [Column("cat_codigo")]
    [StringLength(15)]
    [Unicode(false)]
    public string CatCodigo { get; set; } = null!;

    /// <summary>
    /// Descripcion
    /// </summary>
    [Column("cat_descripcion")]
    [StringLength(500)]
    [Unicode(false)]
    public string? CatDescripcion { get; set; }

    /// <summary>
    /// Activo / inactivo
    /// </summary>
    [Column("cat_estado")]
    public bool CatEstado { get; set; }

    [Column("cat_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CatUsuarioCrea { get; set; } = null!;

    [Column("cat_fechaCrea", TypeName = "datetime")]
    public DateTime CatFechaCrea { get; set; }

    [Column("cat_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CatEquipoCrea { get; set; } = null!;

    [Column("cat_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CatUsuarioMod { get; set; }

    [Column("cat_fechaMod", TypeName = "datetime")]
    public DateTime? CatFechaMod { get; set; }

    [Column("cat_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CatEquipoMod { get; set; }

    [Column("cat_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CatUsuarioEli { get; set; }

    [Column("cat_fechaEli", TypeName = "datetime")]
    public DateTime? CatFechaEli { get; set; }

    [Column("cat_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CatEquipoEli { get; set; }

    [InverseProperty("DetIdCabNavigation")]
    public virtual ICollection<TbCatalogoDet> TbCatalogoDet { get; set; } = new List<TbCatalogoDet>();
}
