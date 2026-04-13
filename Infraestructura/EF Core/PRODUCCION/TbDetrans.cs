using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_detrans")]
[Index("Nsecuencial", "Id780", Name = "IX_tb_detrans_Nsecuencial_id780")]
[Index("DetCodpes", "Hora", "ProCodcor", Name = "IX_tb_detrans_codpes_hora_codcor")]
[Index("ProCodcor", Name = "IX_tb_detrans_proCodcor")]
[Index("DetEstado", "Hora", Name = "idx_tb_detrans")]
[Index("Hora", "ProCodcor", "DetCodpes", Name = "idx_tbdetrans")]
public partial class TbDetrans
{
    public double? Nsecuencial { get; set; }

    public double? Ntrans780 { get; set; }

    [Column("hora", TypeName = "datetime")]
    public DateTime? Hora { get; set; }

    [Column("pro_codcor")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ProCodcor { get; set; }

    [Column("tal_codigo")]
    [StringLength(7)]
    public string? TalCodigo { get; set; }

    [Column("peso")]
    public double? Peso { get; set; }

    [Column("id_780")]
    [StringLength(1)]
    [Unicode(false)]
    public string? Id780 { get; set; }

    [Column("det_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DetEstado { get; set; }

    [Column("det_codpes")]
    [StringLength(15)]
    [Unicode(false)]
    public string? DetCodpes { get; set; }

    [Column("det_numtran", TypeName = "numeric(18, 0)")]
    public decimal? DetNumtran { get; set; }

    [Column("pesoAntesTara", TypeName = "numeric(18, 8)")]
    public decimal? PesoAntesTara { get; set; }

    [Column("tra_numsec", TypeName = "numeric(18, 0)")]
    public decimal? TraNumsec { get; set; }

    [Column("tra_coche", TypeName = "numeric(18, 0)")]
    public decimal? TraCoche { get; set; }
}
