using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[PrimaryKey("CiaCodigo", "CpgNumero", "CpgTipo")]
[Table("tb_cobpag1")]
[Index("CpgTipo", "CpgFecinv", "CpgSaldo", Name = "IDX_1")]
[Index("CpgNumero", Name = "IX_tb_cobpag1")]
[Index("CpgProvee", Name = "IX_tb_cobpag1_1")]
[Index("CpgTipo2", Name = "IX_tb_cobpag1_2")]
[Index("CpgTipdeu", Name = "IX_tb_cobpag1_3")]
[Index("CpgMovimi", Name = "IX_tb_cobpag1_5")]
[Index("CpgClaveAcceso", Name = "IX_tb_cobpag1_6")]
[Index("CiaCodigo", "CpgTipdeu", "CpgFecinv", "CpgSaldo", Name = "IX_tb_cobpag1_ciaCodigo_cpgTipdeu_cpgFecinv_cpgSaldo")]
[Index("CpgTipo", "CpgTipdeu", "CpgEstado", "CpgRefere", Name = "IX_tb_cobpag1_refere_tipo_tipdeu_estado_refere")]
[Index("CpgTipo", "CpgEstado", "CpgSaldo", Name = "IX_tb_cobpag1_tipo_estado_saldo")]
[Index("CpgTipo", "CpgSaldoAnticipo", Name = "IX_tb_cobpag1_tipo_saldoAnticipo")]
[Index("CpgTipo", "CpgTipdeu", "CpgSaldo", Name = "IX_tb_cobpag1_tipo_tipdeu_saldo")]
[Index("CpgTipo", "CpgNroord", Name = "idx2_tb_cobpag1")]
[Index("CpgTipo", "CpgEstado", "CpgEstdeu", Name = "idx_tb_cobpag1")]
[Index("CiaCodigo", "CpgTipdeu", "CpgSaldo", Name = "idx_tb_cobpag1_1")]
[Index("CpgFecinv", Name = "idx_tb_cobpag1_3")]
public partial class TbCobpag1
{
    [Key]
    [Column("cia_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string CiaCodigo { get; set; } = null!;

    [Column("cpg_refere")]
    [StringLength(15)]
    [Unicode(false)]
    public string CpgRefere { get; set; } = null!;

    [Key]
    [Column("cpg_numero")]
    [StringLength(10)]
    [Unicode(false)]
    public string CpgNumero { get; set; } = null!;

    [Key]
    [Column("cpg_tipo")]
    [StringLength(2)]
    [Unicode(false)]
    public string CpgTipo { get; set; } = null!;

    [Column("cpg_provee")]
    [StringLength(6)]
    [Unicode(false)]
    public string? CpgProvee { get; set; }

    [Column("cpg_fecapr")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFecapr { get; set; }

    [Column("cpg_fecinv")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFecinv { get; set; }

    [Column("cpg_feccad")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFeccad { get; set; }

    [Column("cpg_valdes")]
    public double? CpgValdes { get; set; }

    [Column("cpg_total")]
    public double? CpgTotal { get; set; }

    [Column("cpg_ctades")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CpgCtades { get; set; }

    [Column("cpg_ctaban")]
    [StringLength(14)]
    [Unicode(false)]
    public string? CpgCtaban { get; set; }

    [Column("cpg_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CpgEstado { get; set; }

    [Column("cpg_saldo")]
    public double? CpgSaldo { get; set; }

    [Column("cpg_fecint")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFecint { get; set; }

    [Column("cpg_forpag")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CpgForpag { get; set; }

    [Column("cpg_valiva")]
    public double? CpgValiva { get; set; }

    [Column("cpg_intmor")]
    public double? CpgIntmor { get; set; }

    [Column("cpg_intval")]
    public double? CpgIntval { get; set; }

    [Column("cpg_tipdeu")]
    [StringLength(5)]
    [Unicode(false)]
    public string? CpgTipdeu { get; set; }

    [Column("cpg_retiva")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgRetiva { get; set; }

    [Column("cpg_retfue")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgRetfue { get; set; }

    [Column("cpg_movimi")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgMovimi { get; set; }

    [Column("cpg_feclle")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFeclle { get; set; }

    [Column("cpg_observ")]
    [Unicode(false)]
    public string? CpgObserv { get; set; }

    [Column("cpg_estdeu")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CpgEstdeu { get; set; }

    [Column("cpg_totcan")]
    public double? CpgTotcan { get; set; }

    [Column("cpg_ordpag")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgOrdpag { get; set; }

    [Column("cpg_aplret")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgAplret { get; set; }

    [Column("cpg_tipo2")]
    [StringLength(4)]
    [Unicode(false)]
    public string? CpgTipo2 { get; set; }

    [Column("cpg_baseimp")]
    public double? CpgBaseimp { get; set; }

    [Column("cpg_valice")]
    public double? CpgValice { get; set; }

    [Column("cpg_basecer")]
    public double? CpgBasecer { get; set; }

    [Column("cpg_fiscal")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgFiscal { get; set; }

    [Column("cpg_ultfec")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CpgUltfec { get; set; }

    [Column("cpg_npag")]
    public int CpgNpag { get; set; }

    [Column("cpg_cpag")]
    public int? CpgCpag { get; set; }

    [Column("cpg_ndias")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CpgNdias { get; set; }

    [Column("cpg_flujocaja")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CpgFlujocaja { get; set; }

    [Column("cpg_ndnc")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgNdnc { get; set; }

    [Column("cpg_actu")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgActu { get; set; }

    [Column("cpg_crdtrib")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CpgCrdtrib { get; set; }

    [Column("cpg_deviva")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgDeviva { get; set; }

    [Column("cpg_nrocompexp")]
    [StringLength(30)]
    [Unicode(false)]
    public string? CpgNrocompexp { get; set; }

    [Column("cpg_referendo")]
    [StringLength(30)]
    [Unicode(false)]
    public string? CpgReferendo { get; set; }

    [Column("cpg_naviera")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CpgNaviera { get; set; }

    [Column("cpg_tipodocexp")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CpgTipodocexp { get; set; }

    [Column("cpg_bl")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CpgBl { get; set; }

    [Column("cpg_feccompexp")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFeccompexp { get; set; }

    [Column("cpg_aplretiva")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgAplretiva { get; set; }

    [Column("cpg_clasecli")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgClasecli { get; set; }

    [Column("cpg_solpag")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgSolpag { get; set; }

    [Column("cpg_fecdig", TypeName = "datetime")]
    public DateTime? CpgFecdig { get; set; }

    [Column("cpg_nrotra")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgNrotra { get; set; }

    [Column("cpg_nrobl")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CpgNrobl { get; set; }

    [Column("cpg_fecbl")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFecbl { get; set; }

    [Column("cpg_nrofue")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CpgNrofue { get; set; }

    [Column("cpg_nroord")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CpgNroord { get; set; }

    [Column("cpg_locext")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgLocext { get; set; }

    [Column("cpg_SaldoAnticipo", TypeName = "numeric(18, 2)")]
    public decimal? CpgSaldoAnticipo { get; set; }

    [Column("cpg_RetSer1")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgRetSer1 { get; set; }

    [Column("cpg_RetSer2")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgRetSer2 { get; set; }

    [Column("cpg_RetSec")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgRetSec { get; set; }

    [Column("cpg_RetAutori")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgRetAutori { get; set; }

    [Column("cpg_valret", TypeName = "numeric(18, 2)")]
    public decimal? CpgValret { get; set; }

    [Column("cpg_NroCobro", TypeName = "numeric(18, 0)")]
    public decimal? CpgNroCobro { get; set; }

    [Column("cpg_cancelaA")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgCancelaA { get; set; }

    [Column("cpg_ncomp")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CpgNcomp { get; set; }

    [Column("cpg_nroser")]
    [StringLength(5)]
    [Unicode(false)]
    public string? CpgNroser { get; set; }

    [Column("cpg_nroser2")]
    [StringLength(5)]
    [Unicode(false)]
    public string? CpgNroser2 { get; set; }

    [Column("cpg_fecliqant")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgFecliqant { get; set; }

    [Column("cpg_solicitadopor")]
    public double? CpgSolicitadopor { get; set; }

    [Column("cpg_claveAcceso")]
    [StringLength(64)]
    [Unicode(false)]
    public string? CpgClaveAcceso { get; set; }

    [Column("cpg_claveAutorizacion")]
    [StringLength(64)]
    [Unicode(false)]
    public string? CpgClaveAutorizacion { get; set; }

    [Column("cpg_claveContingencia")]
    [StringLength(64)]
    [Unicode(false)]
    public string? CpgClaveContingencia { get; set; }

    [Column("cpg_ambiente")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CpgAmbiente { get; set; }

    [Column("cpg_emision")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CpgEmision { get; set; }

    [Column("cpg_fechaHoraClaveAutorizacion", TypeName = "datetime")]
    public DateTime? CpgFechaHoraClaveAutorizacion { get; set; }

    [Column("cpg_mensaje")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? CpgMensaje { get; set; }

    [Column("cpg_estaut")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CpgEstaut { get; set; }

    [Column("cpg_docAfecta")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgDocAfecta { get; set; }

    [Column("cpg_ObservaDetalle")]
    [StringLength(200)]
    [Unicode(false)]
    public string? CpgObservaDetalle { get; set; }

    [Column("cpg_AsientoDiario")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgAsientoDiario { get; set; }

    [Column("cpg_transmitido")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CpgTransmitido { get; set; }

    [Column("cpg_aresol")]
    [StringLength(6)]
    [Unicode(false)]
    public string? CpgAresol { get; set; }

    [Column("cpg_tipdocAfecta")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CpgTipdocAfecta { get; set; }

    [Column("cpg_cantidadEnLetras")]
    [StringLength(500)]
    [Unicode(false)]
    public string? CpgCantidadEnLetras { get; set; }

    [Column("cpg_poriva", TypeName = "numeric(18, 0)")]
    public decimal? CpgPoriva { get; set; }

    [Column("cpg_obsInfoAdi")]
    [StringLength(100)]
    [Unicode(false)]
    public string? CpgObsInfoAdi { get; set; }

    [Column("cpg_formaPago", TypeName = "numeric(18, 0)")]
    public decimal? CpgFormaPago { get; set; }

    [Column("CPG_XML", TypeName = "xml")]
    public string? CpgXml { get; set; }

    [Column("cpg_observPago")]
    [StringLength(4000)]
    [Unicode(false)]
    public string? CpgObservPago { get; set; }

    [Column("cpg_egresoVarios")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgEgresoVarios { get; set; }

    [Column("cpg_codBco")]
    [StringLength(6)]
    [Unicode(false)]
    public string? CpgCodBco { get; set; }

    [Column("cpg_codCta")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CpgCodCta { get; set; }

    [Column("cpg_codIteLiqCom")]
    [StringLength(18)]
    [Unicode(false)]
    public string? CpgCodIteLiqCom { get; set; }

    [Column("cpg_ncvalFob", TypeName = "numeric(18, 2)")]
    public decimal? CpgNcvalFob { get; set; }

    [Column("cpg_ncvalFle", TypeName = "numeric(18, 2)")]
    public decimal? CpgNcvalFle { get; set; }

    [Column("cpg_ncvalSeg", TypeName = "numeric(18, 2)")]
    public decimal? CpgNcvalSeg { get; set; }

    [Column("cpg_nroInternRet")]
    public int? CpgNroInternRet { get; set; }

    [Column("cpg_nroCompteRet")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgNroCompteRet { get; set; }

    [Column("cpg_valPagado", TypeName = "numeric(18, 2)")]
    public decimal? CpgValPagado { get; set; }

    [Column("cpg_valConciliad", TypeName = "numeric(18, 2)")]
    public decimal? CpgValConciliad { get; set; }

    [Column("cpg_nroAD")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CpgNroAd { get; set; }

    [Column("cpg_movId")]
    public int? CpgMovId { get; set; }

    [Column("cpg_usuario")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CpgUsuario { get; set; }

    [Column("cpg_codDocReembolso")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CpgCodDocReembolso { get; set; }

    [Column("cpg_nroDocInterno")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CpgNroDocInterno { get; set; }

    [Column("cpg_baseNoObjIva", TypeName = "numeric(18, 2)")]
    public decimal? CpgBaseNoObjIva { get; set; }

    [Column("cpg_codPrcIvaCer")]
    public int? CpgCodPrcIvaCer { get; set; }

    [Column("cpg_codPrcIvaImp")]
    public int? CpgCodPrcIvaImp { get; set; }

    [Column("cpg_aplRetAutom")]
    public bool? CpgAplRetAutom { get; set; }

    [Column("cpg_aplOPAutom")]
    public bool? CpgAplOpautom { get; set; }

    [Column("cpg_codBcoPago")]
    [StringLength(14)]
    [Unicode(false)]
    public string? CpgCodBcoPago { get; set; }

    [Column("cpg_codCtaPago")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CpgCodCtaPago { get; set; }

    /// <summary>
    /// Indicador si las compras relacionadas a la deuda fueron generadas con o sin presupuesto de compras. El campo es actualizado al ejecutar lista de pagos pendientes (spr_OrdxApr2).
    /// </summary>
    [Column("cpg_bajoPresupuesto")]
    public bool? CpgBajoPresupuesto { get; set; }
}
