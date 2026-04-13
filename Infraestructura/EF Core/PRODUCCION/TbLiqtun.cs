using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_liqtun")]
[Index("LiqCietun", "LiqEstado", Name = "IDX_tb_liqtun_01")]
[Index("LiqEstado", Name = "IX02_tb_liqtun")]
[Index("LiqLote", Name = "ix_tb_liqtun_liq_lote")]
public partial class TbLiqtun
{
    [Key]
    [Column("liq_numero", TypeName = "numeric(18, 0)")]
    public decimal LiqNumero { get; set; }

    [Column("liq_piscin")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LiqPiscin { get; set; }

    [Column("liq_provee", TypeName = "numeric(18, 0)")]
    public decimal LiqProvee { get; set; }

    [Column("liq_lote", TypeName = "numeric(18, 0)")]
    public decimal LiqLote { get; set; }

    [Column("liq_fecha", TypeName = "datetime")]
    public DateTime LiqFecha { get; set; }

    [Column("liq_tunpla")]
    [StringLength(2)]
    [Unicode(false)]
    public string LiqTunpla { get; set; } = null!;

    [Column("liq_proprc")]
    [StringLength(2)]
    [Unicode(false)]
    public string? LiqProprc { get; set; }

    [Column("liq_fecini", TypeName = "datetime")]
    public DateTime LiqFecini { get; set; }

    [Column("liq_fecfin", TypeName = "datetime")]
    public DateTime LiqFecfin { get; set; }

    [Column("liq_planta", TypeName = "numeric(18, 0)")]
    public decimal LiqPlanta { get; set; }

    [Column("liq_ubica")]
    [StringLength(2)]
    [Unicode(false)]
    public string LiqUbica { get; set; } = null!;

    [Column("liq_cabcol")]
    [StringLength(1)]
    [Unicode(false)]
    public string LiqCabcol { get; set; } = null!;

    [Column("liq_total")]
    public double LiqTotal { get; set; }

    [Column("liq_observ")]
    [StringLength(255)]
    [Unicode(false)]
    public string LiqObserv { get; set; } = null!;

    [Column("liq_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string LiqEstado { get; set; } = null!;

    [Column("liq_aproba")]
    [StringLength(1)]
    [Unicode(false)]
    public string LiqAproba { get; set; } = null!;

    [Column("liq_invent")]
    [StringLength(1)]
    [Unicode(false)]
    public string LiqInvent { get; set; } = null!;

    [Column("liq_useapr")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LiqUseapr { get; set; }

    [Column("liq_datapr", TypeName = "datetime")]
    public DateTime? LiqDatapr { get; set; }

    [Column("liq_usecre")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LiqUsecre { get; set; }

    [Column("liq_datcre", TypeName = "datetime")]
    public DateTime? LiqDatcre { get; set; }

    [Column("liq_usemod")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LiqUsemod { get; set; }

    [Column("liq_datmod", TypeName = "datetime")]
    public DateTime? LiqDatmod { get; set; }

    [Column("liq_useeli")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LiqUseeli { get; set; }

    [Column("liq_dateli", TypeName = "datetime")]
    public DateTime? LiqDateli { get; set; }

    [Column("liq_cietun")]
    public int? LiqCietun { get; set; }

    [Column("liq_pppt")]
    [StringLength(2)]
    [Unicode(false)]
    public string? LiqPppt { get; set; }

    [Column("liq_lotval", TypeName = "numeric(18, 0)")]
    public decimal? LiqLotval { get; set; }

    [Column("liq_copack")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LiqCopack { get; set; }

    [Column("liq_turno")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LiqTurno { get; set; }

    [Column("liq_numcorte", TypeName = "numeric(7, 0)")]
    public decimal? LiqNumcorte { get; set; }

    [Column("liq_conspagcopk")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LiqConspagcopk { get; set; }

    [Column("liq_CodReqProdEnt")]
    [StringLength(25)]
    [Unicode(false)]
    public string? LiqCodReqProdEnt { get; set; }

    [Column("liq_CodReqProdCola")]
    [StringLength(25)]
    [Unicode(false)]
    public string? LiqCodReqProdCola { get; set; }

    [Column("liq_isUnif")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LiqIsUnif { get; set; }

    [Column("liq_isTraspEntero")]
    public bool? LiqIsTraspEntero { get; set; }
}
