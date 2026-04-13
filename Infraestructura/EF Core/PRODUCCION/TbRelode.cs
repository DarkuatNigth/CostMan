using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_relode")]
[Index("RldNumero", "RldTipo", Name = "IX01_tb_relode")]
[Index("RldLote", Name = "IX02_tb_relode")]
[Index("RldLote", "RldProcod", "RldCodtal", Name = "IX_tb_relode_numero_cantid")]
public partial class TbRelode
{
    [Column("rld_numero", TypeName = "numeric(18, 0)")]
    public decimal RldNumero { get; set; }

    [Column("rld_tipo")]
    [StringLength(2)]
    [Unicode(false)]
    public string RldTipo { get; set; } = null!;

    [Column("rld_lote")]
    public long RldLote { get; set; }

    [Column("rld_cabcol")]
    [StringLength(2)]
    [Unicode(false)]
    public string RldCabcol { get; set; } = null!;

    [Column("rld_procod")]
    [StringLength(50)]
    [Unicode(false)]
    public string RldProcod { get; set; } = null!;

    [Column("rld_codtal")]
    public int RldCodtal { get; set; }

    [Column("rld_cantid")]
    public double RldCantid { get; set; }

    [Column("rld_cosnor")]
    public double RldCosnor { get; set; }

    [Column("rld_cosdif")]
    public double RldCosdif { get; set; }

    [Column("rld_codcub")]
    [StringLength(1)]
    [Unicode(false)]
    public string RldCodcub { get; set; } = null!;

    [Column("rld_codsec")]
    [StringLength(1)]
    [Unicode(false)]
    public string RldCodsec { get; set; } = null!;

    [Column("rld_numeroPO")]
    [StringLength(200)]
    [Unicode(false)]
    public string? RldNumeroPo { get; set; }

    [Column("rld_turno")]
    [StringLength(2)]
    [Unicode(false)]
    public string? RldTurno { get; set; }

    [ForeignKey("RldNumero, RldTipo")]
    public virtual TbLototr TbLototr { get; set; } = null!;
}
