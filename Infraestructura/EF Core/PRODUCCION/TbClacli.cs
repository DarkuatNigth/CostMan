using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_clacli")]
public partial class TbClacli
{
    [Column("cla_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string ClaCodcia { get; set; } = null!;

    [Key]
    [Column("cla_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string ClaCodigo { get; set; } = null!;

    [Column("cla_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string ClaDescri { get; set; } = null!;

    [Column("cla_ctacon")]
    [StringLength(13)]
    [Unicode(false)]
    public string? ClaCtacon { get; set; }

    [Column("cla_estado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClaEstado { get; set; }

    [Column("cla_user")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ClaUser { get; set; }

    [Column("cla_ListaBase")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClaListaBase { get; set; }

    /// <summary>
    /// Correos de usuarios y proveedores a quienes se notificara la aprobacion de una lista de precios.
    /// </summary>
    [Column("cla_listaPrecioEmails")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? ClaListaPrecioEmails { get; set; }
}
