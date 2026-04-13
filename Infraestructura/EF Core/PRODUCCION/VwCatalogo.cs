using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
public partial class VwCatalogo
{
    [Column("det_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string DetCodigo { get; set; } = null!;

    [Column("det_descripcion")]
    [StringLength(500)]
    [Unicode(false)]
    public string? DetDescripcion { get; set; }

    [Column("det_estado")]
    public bool DetEstado { get; set; }

    [Column("cat_id")]
    public int CatId { get; set; }

    [Column("cat_descripcion")]
    [StringLength(500)]
    [Unicode(false)]
    public string? CatDescripcion { get; set; }

    [Column("cat_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string CatCodigo { get; set; } = null!;

    [Column("cat_estado")]
    public bool CatEstado { get; set; }

    [Column("det_referenciaAdi1")]
    [Unicode(false)]
    public string? DetReferenciaAdi1 { get; set; }

    [Column("det_referenciaAdi2")]
    [Unicode(false)]
    public string? DetReferenciaAdi2 { get; set; }

    [Column("det_referenciaAdi3")]
    [Unicode(false)]
    public string? DetReferenciaAdi3 { get; set; }
}
