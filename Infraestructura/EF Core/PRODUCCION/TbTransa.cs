using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Motivos de movimiento de inventario
/// </summary>
[Table("tb_transa")]
public partial class TbTransa
{
    [Key]
    [Column("trs_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string TrsCodigo { get; set; } = null!;

    [Column("trs_tipo")]
    [StringLength(1)]
    [Unicode(false)]
    public string TrsTipo { get; set; } = null!;

    [Column("trs_descri")]
    [StringLength(30)]
    [Unicode(false)]
    public string TrsDescri { get; set; } = null!;

    [Column("trs_autoriza")]
    public bool TrsAutoriza { get; set; }

    [Column("trs_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string TrsEstado { get; set; } = null!;

    [Column("trs_numsec")]
    public int TrsNumsec { get; set; }

    [Column("trs_tumbada")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TrsTumbada { get; set; }

    [Column("trs_tomainv")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TrsTomainv { get; set; }

    [Column("trs_NoValidaEgresoTinas")]
    public bool? TrsNoValidaEgresoTinas { get; set; }

    [Column("trs_validaPermisosEgresoUsuarios")]
    public bool? TrsValidaPermisosEgresoUsuarios { get; set; }
}
