using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tbl_recetahidrat")]
public partial class TblRecetahidrat
{
    [Key]
    [Column("rec_codigo", TypeName = "numeric(18, 0)")]
    public decimal RecCodigo { get; set; }

    [Column("rec_nombre")]
    [StringLength(50)]
    [Unicode(false)]
    public string? RecNombre { get; set; }

    [Column("rec_tipohid")]
    [StringLength(3)]
    [Unicode(false)]
    public string? RecTipohid { get; set; }

    [Column("rec_tipo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? RecTipo { get; set; }

    [Column("rec_porchid", TypeName = "numeric(18, 2)")]
    public decimal? RecPorchid { get; set; }

    [Column("rec_porcagua", TypeName = "numeric(18, 1)")]
    public decimal? RecPorcagua { get; set; }

    [Column("rec_porcsal", TypeName = "numeric(18, 1)")]
    public decimal? RecPorcsal { get; set; }

    [Column("rec_porchielo", TypeName = "numeric(18, 1)")]
    public decimal? RecPorchielo { get; set; }

    [Column("rec_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RecEstado { get; set; }

    [Column("rec_tiempo")]
    [StringLength(20)]
    [Unicode(false)]
    public string? RecTiempo { get; set; }

    [Column("rec_codrelingr", TypeName = "numeric(10, 0)")]
    public decimal? RecCodrelingr { get; set; }
}
