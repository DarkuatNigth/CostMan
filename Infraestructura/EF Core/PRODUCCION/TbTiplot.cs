using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_tiplot")]
[Index("TipCodigo", "TipLote", Name = "IX_tiplot")]
public partial class TbTiplot
{
    [Key]
    [Column("tip_codigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string TipCodigo { get; set; } = null!;

    [Column("tip_lote")]
    [StringLength(2)]
    [Unicode(false)]
    public string TipLote { get; set; } = null!;

    [Column("tip_descri")]
    [StringLength(50)]
    [Unicode(false)]
    public string TipDescri { get; set; } = null!;

    [Column("tip_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string TipEstado { get; set; } = null!;

    [Column("tip_rdmto", TypeName = "decimal(5, 2)")]
    public decimal TipRdmto { get; set; }

    [Column("tip_valida")]
    public bool? TipValida { get; set; }

    [Column("tip_autrecNetas")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TipAutrecNetas { get; set; }

    [Column("tip_valRendLiq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TipValRendLiq { get; set; }

    [Column("tip_ValIngCOdProd")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TipValIngCodProd { get; set; }

    [Column("tip_Secxturno")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TipSecxturno { get; set; }

    [Column("tip_ConsidFresco")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TipConsidFresco { get; set; }

    [Column("tip_congela")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TipCongela { get; set; }

    [Column("tip_coccion")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TipCoccion { get; set; }

    [Column("tip_snTratado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TipSnTratado { get; set; }

    [Column("TIP_CORTE")]
    [StringLength(10)]
    [Unicode(false)]
    public string? TipCorte { get; set; }

    [Column("tip_ExigeEsMaquina")]
    public bool? TipExigeEsMaquina { get; set; }

    /// <summary>
    /// Columna para controlar si un tipo de secuencial debe o no validar el cierre de lotes produccion 1 valida, 0 o null no valida
    /// </summary>
    [Column("tip_validaCierreLoteProduccion")]
    public bool? TipValidaCierreLoteProduccion { get; set; }
}
