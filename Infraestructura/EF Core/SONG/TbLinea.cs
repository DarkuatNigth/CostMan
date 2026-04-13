using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[Table("tb_linea")]
public partial class TbLinea
{
    [Key]
    [Column("lin_codigo")]
    [StringLength(50)]
    [Unicode(false)]
    public string LinCodigo { get; set; } = null!;

    [Column("lin_nombre")]
    [StringLength(50)]
    [Unicode(false)]
    public string? LinNombre { get; set; }

    [Column("lin_comisi")]
    public double? LinComisi { get; set; }

    [Column("lin_observ", TypeName = "text")]
    public string? LinObserv { get; set; }

    [Column("lin_cta1")]
    [StringLength(13)]
    [Unicode(false)]
    public string? LinCta1 { get; set; }

    [Column("lin_cta2")]
    [StringLength(13)]
    [Unicode(false)]
    public string? LinCta2 { get; set; }

    [Column("lin_cta3")]
    [StringLength(13)]
    [Unicode(false)]
    public string? LinCta3 { get; set; }

    [Column("lin_cta4")]
    [StringLength(13)]
    [Unicode(false)]
    public string? LinCta4 { get; set; }

    [Column("lin_cta5")]
    [StringLength(13)]
    [Unicode(false)]
    public string? LinCta5 { get; set; }

    [Column("lin_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? LinEstado { get; set; }

    [Column("crea_user")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CreaUser { get; set; }

    [Column("crea_date")]
    [StringLength(20)]
    public string? CreaDate { get; set; }

    [Column("mod_user")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ModUser { get; set; }

    [Column("mod_date")]
    [StringLength(20)]
    public string? ModDate { get; set; }

    [Column("eli_user")]
    [StringLength(10)]
    [Unicode(false)]
    public string? EliUser { get; set; }

    [Column("eli_date")]
    [StringLength(20)]
    public string? EliDate { get; set; }

    [Column("lin_ClaveMat")]
    [StringLength(2)]
    [Unicode(false)]
    public string? LinClaveMat { get; set; }

    [Column("lin_Abrev")]
    [StringLength(50)]
    [Unicode(false)]
    public string? LinAbrev { get; set; }

    [Column("lin_StoLot")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LinStoLot { get; set; }

    [Column("lin_ReqRef")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LinReqRef { get; set; }

    [Column("lin_OrdTra")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LinOrdTra { get; set; }

    [Column("lin_genReqManual")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LinGenReqManual { get; set; }

    [Column("lin_NoConsidAprReq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LinNoConsidAprReq { get; set; }

    [Column("lin_genLotAutomAprob")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LinGenLotAutomAprob { get; set; }

    [Column("lin_validaUsuxDpto")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LinValidaUsuxDpto { get; set; }
}
