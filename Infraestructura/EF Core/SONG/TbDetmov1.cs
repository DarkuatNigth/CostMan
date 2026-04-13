using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[PrimaryKey("DetCodcia", "DetCabece", "DetTipo", "DetNumero")]
[Table("tb_detmov1")]
[Index("DetCodcia", "DetCabece", Name = "IX_tb_detmov1_1")]
[Index("DetCabece", Name = "IX_tb_detmov1_cabece")]
[Index("DetCodart", "DetCabece", Name = "IX_tb_detmov1_codart_cabece")]
[Index("DetCodart", "DetOrdpro", Name = "IX_tb_detmov1_codart_ordpro")]
[Index("DetCodcia", "DetTipo", Name = "IX_tb_detmov1_codcia_tipo")]
public partial class TbDetmov1
{
    [Key]
    [Column("det_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string DetCodcia { get; set; } = null!;

    [Key]
    [Column("det_cabece")]
    [StringLength(10)]
    [Unicode(false)]
    public string DetCabece { get; set; } = null!;

    [Key]
    [Column("det_tipo")]
    [StringLength(6)]
    [Unicode(false)]
    public string DetTipo { get; set; } = null!;

    [Key]
    [Column("det_numero")]
    [StringLength(10)]
    [Unicode(false)]
    public string DetNumero { get; set; } = null!;

    [Column("det_codart")]
    [StringLength(18)]
    [Unicode(false)]
    public string? DetCodart { get; set; }

    [Column("det_nomart")]
    [StringLength(254)]
    [Unicode(false)]
    public string? DetNomart { get; set; }

    [Column("det_canti")]
    public double? DetCanti { get; set; }

    [Column("det_iva")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DetIva { get; set; }

    [Column("det_pordes")]
    public double? DetPordes { get; set; }

    [Column("det_valdes")]
    public double? DetValdes { get; set; }

    [Column("det_preuni")]
    public double? DetPreuni { get; set; }

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
    [StringLength(25)]
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
    [StringLength(15)]
    [Unicode(false)]
    public string? DetOrdpro { get; set; }

    [Column("det_tipine")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DetTipine { get; set; }

    [Column("det_centro")]
    [StringLength(15)]
    [Unicode(false)]
    public string? DetCentro { get; set; }

    [Column("det_subcen")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DetSubcen { get; set; }

    [Column("det_cosdes")]
    public double? DetCosdes { get; set; }

    [Column("det_observ")]
    [StringLength(250)]
    [Unicode(false)]
    public string? DetObserv { get; set; }

    [Column("det_punto")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DetPunto { get; set; }

    [Column("det_clase")]
    [StringLength(3)]
    [Unicode(false)]
    public string? DetClase { get; set; }

    [Column("det_talla")]
    [StringLength(6)]
    [Unicode(false)]
    public string? DetTalla { get; set; }

    [Column("det_NroLotMat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DetNroLotMat { get; set; }

    [Column("det_codcub")]
    [StringLength(1)]
    [Unicode(false)]
    public string? DetCodcub { get; set; }

    [Column("det_codpar")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DetCodpar { get; set; }

    [Column("det_artDev", TypeName = "numeric(9, 2)")]
    public decimal? DetArtDev { get; set; }

    [Column("det_requisicion")]
    [StringLength(200)]
    [Unicode(false)]
    public string? DetRequisicion { get; set; }

    [Column("det_lbrsVendidas", TypeName = "numeric(18, 2)")]
    public decimal? DetLbrsVendidas { get; set; }

    [Column("det_lbrsEgresadas", TypeName = "numeric(18, 2)")]
    public decimal? DetLbrsEgresadas { get; set; }
}
