using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[PrimaryKey("BodCodigo", "SucCodigo", "CiaCodigo", "IteCodigo", "BodLote", "BodCodcub", "BodCodpar")]
[Table("tb_bodite")]
public partial class TbBodite
{
    [Key]
    [Column("bod_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string BodCodigo { get; set; } = null!;

    [Key]
    [Column("suc_codigo")]
    [StringLength(10)]
    [Unicode(false)]
    public string SucCodigo { get; set; } = null!;

    [Key]
    [Column("cia_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string CiaCodigo { get; set; } = null!;

    [Key]
    [Column("ite_codigo")]
    [StringLength(18)]
    [Unicode(false)]
    public string IteCodigo { get; set; } = null!;

    [Column("ite_descri")]
    [StringLength(160)]
    [Unicode(false)]
    public string? IteDescri { get; set; }

    [Column("bod_stock")]
    public double? BodStock { get; set; }

    [Column("bod_canmin")]
    public double? BodCanmin { get; set; }

    [Column("bod_canmax")]
    public double? BodCanmax { get; set; }

    [Column("bod_canreo")]
    public double? BodCanreo { get; set; }

    [Column("bod_tireor")]
    public double? BodTireor { get; set; }

    [Column("bod_prorot")]
    public double? BodProrot { get; set; }

    [Column("bod_creuse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodCreuse { get; set; }

    [Column("bod_credat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodCredat { get; set; }

    [Column("bod_moduse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodModuse { get; set; }

    [Column("bod_moddat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodModdat { get; set; }

    [Column("bod_eliuse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodEliuse { get; set; }

    [Column("bod_elidat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodElidat { get; set; }

    [Column("bod_status")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodStatus { get; set; }

    [Column("bod_unidad")]
    public double? BodUnidad { get; set; }

    [Column("bod_cospro", TypeName = "numeric(20, 12)")]
    public decimal? BodCospro { get; set; }

    [Column("bod_total", TypeName = "numeric(18, 2)")]
    public decimal? BodTotal { get; set; }

    [Key]
    [Column("bod_lote")]
    [StringLength(10)]
    [Unicode(false)]
    public string BodLote { get; set; } = null!;

    [Key]
    [Column("bod_codcub")]
    [StringLength(2)]
    [Unicode(false)]
    public string BodCodcub { get; set; } = null!;

    [Key]
    [Column("bod_codpar")]
    [StringLength(2)]
    [Unicode(false)]
    public string BodCodpar { get; set; } = null!;

    [Column("bod_ultCosCompra")]
    public double? BodUltCosCompra { get; set; }

    [Column("bod_ultCosConsum")]
    public double? BodUltCosConsum { get; set; }

    [Column("bod_loteEstado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodLoteEstado { get; set; }

    [Column("bod_loteFechaL")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodLoteFechaL { get; set; }

    [Column("bod_loteFecCad")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodLoteFecCad { get; set; }

    [Column("bod_fecUltConsumo")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodFecUltConsumo { get; set; }

    [Column("bod_fecUltCompra")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodFecUltCompra { get; set; }
}
