using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[Keyless]
[Table("tb_detmov2")]
[Index("DetCabece", Name = "IX_tb_detmov2_1")]
[Index("DetBodega", Name = "IX_tb_detmov2_bodega")]
[Index("DetCodcia", "DetCodart", Name = "IX_tb_detmov2_codcia_codart")]
[Index("DetNrolot", Name = "IX_tb_detmov2_nrolot")]
public partial class TbDetmov2
{
    [Column("det_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string DetCodcia { get; set; } = null!;

    [Column("det_cabece")]
    [StringLength(10)]
    [Unicode(false)]
    public string DetCabece { get; set; } = null!;

    [Column("det_tipo")]
    [StringLength(6)]
    [Unicode(false)]
    public string DetTipo { get; set; } = null!;

    [Column("det_numero")]
    [StringLength(10)]
    [Unicode(false)]
    public string DetNumero { get; set; } = null!;

    [Column("det_codart")]
    [StringLength(18)]
    [Unicode(false)]
    public string DetCodart { get; set; } = null!;

    [Column("det_nomart")]
    [StringLength(254)]
    [Unicode(false)]
    public string? DetNomart { get; set; }

    [Column("det_canti")]
    public double DetCanti { get; set; }

    [Column("det_iva")]
    [StringLength(1)]
    [Unicode(false)]
    public string DetIva { get; set; } = null!;

    [Column("det_pordes")]
    public double DetPordes { get; set; }

    [Column("det_valdes")]
    public double DetValdes { get; set; }

    [Column("det_preuni")]
    public double DetPreuni { get; set; }

    [Column("det_subtot")]
    public double? DetSubtot { get; set; }

    [Column("det_bodega")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DetBodega { get; set; }

    [Column("det_unimed")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DetUnimed { get; set; }

    [Column("det_artrec")]
    public double? DetArtrec { get; set; }

    [Column("det_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DetEstado { get; set; }

    [Column("det_poriva")]
    public double? DetPoriva { get; set; }

    [Column("det_notfac")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DetNotfac { get; set; }

    [Column("det_coscif")]
    public double? DetCoscif { get; set; }

    [Column("det_des2")]
    public double? DetDes2 { get; set; }

    [Column("det_des3")]
    public double? DetDes3 { get; set; }

    [Column("det_peso")]
    public double? DetPeso { get; set; }

    [Column("det_utilid")]
    public double? DetUtilid { get; set; }

    [Column("det_cabrec")]
    public double? DetCabrec { get; set; }

    [Column("det_canent")]
    public double? DetCanent { get; set; }

    [Column("det_ordpro")]
    [StringLength(12)]
    [Unicode(false)]
    public string? DetOrdpro { get; set; }

    [Column("det_tipineg")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DetTipineg { get; set; }

    [Column("det_centro")]
    [StringLength(15)]
    [Unicode(false)]
    public string? DetCentro { get; set; }

    [Column("det_subcen")]
    [StringLength(15)]
    [Unicode(false)]
    public string? DetSubcen { get; set; }

    [Column("det_nrolot")]
    [StringLength(50)]
    [Unicode(false)]
    public string? DetNrolot { get; set; }

    [Column("det_stock", TypeName = "numeric(18, 2)")]
    public decimal? DetStock { get; set; }

    [Column("det_cantDev")]
    public double? DetCantDev { get; set; }

    [Column("det_codcubbori")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DetCodcubbori { get; set; }

    [Column("det_codparbori")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DetCodparbori { get; set; }

    [Column("det_codcubbdes")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DetCodcubbdes { get; set; }

    [Column("det_codparbdes")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DetCodparbdes { get; set; }
}
