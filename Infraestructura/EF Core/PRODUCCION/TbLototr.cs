using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[PrimaryKey("LotNumero", "LotTipo")]
[Table("tb_lototr")]
[Index("LotCodbod", "LotFecha", "LotEstado", Name = "IX01_tb_lototr")]
[Index("LotEsmaquina", "LotFecha", Name = "ixLototrEsMaqFecha")]
[Index("LotTipo", "LotTiplot", "LotFecha", Name = "ixlototrtipotiplotfecha")]
[Index("LotCodbod", "LotFecha", "LotEstado", Name = "ixtb_lototrCodbodFechaestad")]
[Index("LotEstado", Name = "ixtb_lototrlot_estado")]
public partial class TbLototr
{
    [Key]
    [Column("lot_numero", TypeName = "numeric(18, 0)")]
    public decimal LotNumero { get; set; }

    [Key]
    [Column("lot_tipo")]
    [StringLength(2)]
    [Unicode(false)]
    public string LotTipo { get; set; } = null!;

    [Column("lot_tiplot")]
    [StringLength(5)]
    [Unicode(false)]
    public string LotTiplot { get; set; } = null!;

    [Column("lot_planta", TypeName = "numeric(18, 0)")]
    public decimal LotPlanta { get; set; }

    [Column("lot_codbod")]
    [StringLength(10)]
    [Unicode(false)]
    public string LotCodbod { get; set; } = null!;

    [Column("lot_fecha", TypeName = "datetime")]
    public DateTime? LotFecha { get; set; }

    [Column("lot_brutas", TypeName = "numeric(12, 2)")]
    public decimal? LotBrutas { get; set; }

    [Column("lot_recibi", TypeName = "numeric(12, 2)")]
    public decimal? LotRecibi { get; set; }

    [Column("lot_shello", TypeName = "numeric(12, 2)")]
    public decimal? LotShello { get; set; }

    [Column("lot_proces", TypeName = "numeric(12, 2)")]
    public decimal? LotProces { get; set; }

    [Column("lot_valagr", TypeName = "numeric(12, 2)")]
    public decimal? LotValagr { get; set; }

    [Column("lot_rechaz", TypeName = "numeric(12, 2)")]
    public decimal? LotRechaz { get; set; }

    [Column("lot_observ")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? LotObserv { get; set; }

    [Column("lot_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? LotEstado { get; set; }

    [Column("lot_conlot")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LotConlot { get; set; }

    [Column("lot_usecre")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LotUsecre { get; set; }

    [Column("lot_datcre", TypeName = "datetime")]
    public DateTime? LotDatcre { get; set; }

    [Column("lot_usemod")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LotUsemod { get; set; }

    [Column("lot_datmod", TypeName = "datetime")]
    public DateTime? LotDatmod { get; set; }

    [Column("lot_useeli")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LotUseeli { get; set; }

    [Column("lot_dateli", TypeName = "datetime")]
    public DateTime? LotDateli { get; set; }

    [Column("lot_numdoc")]
    public int? LotNumdoc { get; set; }

    [Column("lot_resp")]
    [StringLength(50)]
    [Unicode(false)]
    public string LotResp { get; set; } = null!;

    [Column("lot_copack")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LotCopack { get; set; }

    [Column("lot_estproceso")]
    [StringLength(2)]
    [Unicode(false)]
    public string? LotEstproceso { get; set; }

    [Column("lot_valagrvl", TypeName = "numeric(12, 2)")]
    public decimal? LotValagrvl { get; set; }

    [Column("lot_fredes")]
    [StringLength(3)]
    [Unicode(false)]
    public string? LotFredes { get; set; }

    [Column("lot_rloNumero", TypeName = "numeric(18, 0)")]
    public decimal? LotRloNumero { get; set; }

    [Column("lot_vigencia", TypeName = "datetime")]
    public DateTime? LotVigencia { get; set; }

    [Column("lot_SNModificar")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LotSnmodificar { get; set; }

    [Column("lot_AutCarDes")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? LotAutCarDes { get; set; }

    [Column("lot_CieDes")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? LotCieDes { get; set; }

    [Column("lot_factura")]
    [StringLength(25)]
    [Unicode(false)]
    public string? LotFactura { get; set; }

    [Column("lot_prodPed")]
    [StringLength(30)]
    [Unicode(false)]
    public string? LotProdPed { get; set; }

    [Column("lot_talPed", TypeName = "numeric(18, 0)")]
    public decimal? LotTalPed { get; set; }

    [Column("lot_MastPed", TypeName = "numeric(18, 2)")]
    public decimal? LotMastPed { get; set; }

    [Column("lot_LibPed", TypeName = "numeric(18, 2)")]
    public decimal? LotLibPed { get; set; }

    [Column("lot_MaxLibPed", TypeName = "numeric(18, 2)")]
    public decimal? LotMaxLibPed { get; set; }

    [Column("lot_libexport", TypeName = "numeric(12, 2)")]
    public decimal? LotLibexport { get; set; }

    [Column("lot_genReq")]
    public int? LotGenReq { get; set; }

    [Column("lot_numtra", TypeName = "numeric(18, 0)")]
    public decimal? LotNumtra { get; set; }

    [Column("lot_usuApertura")]
    [StringLength(200)]
    [Unicode(false)]
    public string? LotUsuApertura { get; set; }

    [Column("lot_equApertura")]
    [StringLength(200)]
    [Unicode(false)]
    public string? LotEquApertura { get; set; }

    [Column("lot_fecApertura", TypeName = "datetime")]
    public DateTime? LotFecApertura { get; set; }

    [Column("lot_motApertura")]
    [StringLength(200)]
    [Unicode(false)]
    public string? LotMotApertura { get; set; }

    [Column("lot_codHidratado")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LotCodHidratado { get; set; }

    [Column("lot_colorProceso", TypeName = "numeric(5, 0)")]
    public decimal? LotColorProceso { get; set; }

    [Column("lot_fecImprimeEtiq", TypeName = "datetime")]
    public DateTime? LotFecImprimeEtiq { get; set; }

    [Column("lot_esmaquina")]
    public bool? LotEsmaquina { get; set; }

    [Column("lot_congiqf", TypeName = "numeric(12, 2)")]
    public decimal? LotCongiqf { get; set; }

    [Column("lot_finproceso")]
    public bool? LotFinproceso { get; set; }

    [Column("lot_usufinproceso")]
    [StringLength(60)]
    [Unicode(false)]
    public string? LotUsufinproceso { get; set; }

    [Column("lot_fecfinproceso", TypeName = "datetime")]
    public DateTime? LotFecfinproceso { get; set; }

    [Column("lot_cierreAsignacion")]
    public bool? LotCierreAsignacion { get; set; }

    [Column("lot_codHidratado2")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LotCodHidratado2 { get; set; }

    /// <summary>
    /// Columna para controlar si permite liquidar diferentes lotes del lote unificado. 1 si permite 0 no permite
    /// </summary>
    [Column("lot_permiteLiqDifLotes")]
    public bool? LotPermiteLiqDifLotes { get; set; }

    [Column("lot_cierreUnifMetodoAntiguo")]
    public bool? LotCierreUnifMetodoAntiguo { get; set; }
}
