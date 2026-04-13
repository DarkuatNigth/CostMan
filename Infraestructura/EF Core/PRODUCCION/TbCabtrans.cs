using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_cabtrans")]
[Index("Ntrans", "Id780", Name = "IX_tb_cabtrans")]
[Index("Fecha", Name = "IX_tb_cabtrans_fecha")]
[Index("TraSecuencial", Name = "IX_tb_cabtrans_traSecuencial")]
[Index("Lote", Name = "idx_loteva")]
[Index("Id780", "Nsecuencial", Name = "idx_tbcabtrans")]
public partial class TbCabtrans
{
    public double? Ntrans { get; set; }

    [StringLength(2)]
    [Unicode(false)]
    public string? Modulo { get; set; }

    [Column("Id_780")]
    [StringLength(1)]
    [Unicode(false)]
    public string? Id780 { get; set; }

    public double? Nsecuencial { get; set; }

    [StringLength(8)]
    [Unicode(false)]
    public string? Lote { get; set; }

    [Column("Tro_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? TroCodigo { get; set; }

    [Column("Cod_destino")]
    [StringLength(8)]
    [Unicode(false)]
    public string? CodDestino { get; set; }

    [Column("Cod_origen")]
    [StringLength(8)]
    [Unicode(false)]
    public string? CodOrigen { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? Identifica { get; set; }

    [Column("Resp_recibe")]
    [StringLength(10)]
    [Unicode(false)]
    public string? RespRecibe { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Fecha { get; set; }

    [Column("cab_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CabEstado { get; set; }

    [Column("tip_proc1")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TipProc1 { get; set; }

    [Column("num_proc1", TypeName = "numeric(18, 0)")]
    public decimal? NumProc1 { get; set; }

    [Column("tip_proc2")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TipProc2 { get; set; }

    [Column("num_proc2", TypeName = "numeric(18, 0)")]
    public decimal? NumProc2 { get; set; }

    [Column("cab_maqCrea")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CabMaqCrea { get; set; }

    [Column("tra_secuencial", TypeName = "numeric(18, 0)")]
    public decimal? TraSecuencial { get; set; }

    [Column("tra_codHidrat", TypeName = "numeric(18, 0)")]
    public decimal? TraCodHidrat { get; set; }

    [Column("tra_sscc")]
    [StringLength(50)]
    [Unicode(false)]
    public string? TraSscc { get; set; }

    [Column("tra_ssccEstado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TraSsccEstado { get; set; }

    [Column("tra_numreq")]
    [StringLength(25)]
    [Unicode(false)]
    public string? TraNumreq { get; set; }

    [Column("tra_codIngredientes", TypeName = "numeric(18, 0)")]
    public decimal? TraCodIngredientes { get; set; }

    [Column("tra_codrecepEtiq", TypeName = "numeric(18, 0)")]
    public decimal? TraCodrecepEtiq { get; set; }

    [Column("tra_numtumbaj", TypeName = "numeric(10, 0)")]
    public decimal? TraNumtumbaj { get; set; }

    [Column("tra_codTallaMP", TypeName = "numeric(10, 0)")]
    public decimal? TraCodTallaMp { get; set; }

    [Column("tra_codguid")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TraCodguid { get; set; }

    [Column("tra_plantaproceso", TypeName = "numeric(10, 0)")]
    public decimal? TraPlantaproceso { get; set; }

    [Column("tra_estadoProcPeso")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TraEstadoProcPeso { get; set; }

    [Column("tra_observacionProcPeso")]
    [StringLength(250)]
    [Unicode(false)]
    public string? TraObservacionProcPeso { get; set; }

    [Column("tra_reqMP", TypeName = "numeric(18, 0)")]
    public decimal? TraReqMp { get; set; }
}
