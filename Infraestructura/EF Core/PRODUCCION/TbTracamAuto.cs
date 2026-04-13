using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_tracamAuto")]
[Index("TrcIngegr", "TrcFecha", "TrcCamdes", "TrcEstado", Name = "IX01_tb_tracamAuto")]
[Index("TrcFecha", Name = "IX_tb_tracamAuto_Fecha")]
[Index("TrcNumtra", Name = "IX_tb_tracamAuto_Numtra")]
[Index("TrcIngegr", "TrcTipo", "TrcEmbfactura", "TrcEstado", Name = "ixTracamAutoInggr")]
[Index("TrcCodcam", Name = "ixTracamautoCodCam")]
[Index("TrcTipo", Name = "ixTracamautoTipo")]
[Index("TrcIngegr", "TrcFecha", "TrcCamdes", "TrcEstado", Name = "ixtb_tracamAutoIngegrfechacamdeesta")]
[Index("TrcTipo", "TrcEstado", Name = "ixtb_tracamAutoTipoEstado")]
[Index("TrcIngegr", "TrcFecha", "TrcEstado", Name = "ixtb_tracamAutoingegfecesta")]
[Index("TrcCamdes", "TrcFecha", "TrcTipo", Name = "ixtracamautocamdesfechatipo")]
[Index("TrcIngegr", "TrcEmbfactura", Name = "ixtracamautoingegr2")]
public partial class TbTracamAuto
{
    [Key]
    [Column("trc_numsec", TypeName = "numeric(18, 0)")]
    public decimal TrcNumsec { get; set; }

    [Column("trc_numtra", TypeName = "numeric(18, 0)")]
    public decimal TrcNumtra { get; set; }

    [Column("trc_fecha", TypeName = "datetime")]
    public DateTime TrcFecha { get; set; }

    [Column("trc_ingegr")]
    [StringLength(1)]
    [Unicode(false)]
    public string TrcIngegr { get; set; } = null!;

    [Column("trc_cabcol")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TrcCabcol { get; set; }

    [Column("trc_tipo")]
    [StringLength(6)]
    [Unicode(false)]
    public string TrcTipo { get; set; } = null!;

    [Column("trc_codpla", TypeName = "numeric(18, 0)")]
    public decimal TrcCodpla { get; set; }

    [Column("trc_codcam")]
    [StringLength(2)]
    [Unicode(false)]
    public string TrcCodcam { get; set; } = null!;

    [Column("trc_plades", TypeName = "numeric(18, 0)")]
    public decimal? TrcPlades { get; set; }

    [Column("trc_camdes")]
    [StringLength(2)]
    [Unicode(false)]
    public string? TrcCamdes { get; set; }

    [Column("trc_compro")]
    [StringLength(10)]
    [Unicode(false)]
    public string? TrcCompro { get; set; }

    [Column("trc_ordemb", TypeName = "numeric(18, 0)")]
    public decimal? TrcOrdemb { get; set; }

    [Column("trc_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string TrcEstado { get; set; } = null!;

    [Column("trc_observ")]
    [StringLength(255)]
    [Unicode(false)]
    public string? TrcObserv { get; set; }

    [Column("trc_proces")]
    public double? TrcProces { get; set; }

    [Column("trc_lote", TypeName = "numeric(18, 0)")]
    public decimal? TrcLote { get; set; }

    [Column("trc_autori")]
    [StringLength(2)]
    [Unicode(false)]
    public string TrcAutori { get; set; } = null!;

    [Column("trc_resp")]
    [StringLength(50)]
    [Unicode(false)]
    public string? TrcResp { get; set; }

    [Column("trc_datecrea", TypeName = "datetime")]
    public DateTime? TrcDatecrea { get; set; }

    [Column("trc_datemod", TypeName = "datetime")]
    public DateTime? TrcDatemod { get; set; }

    [Column("trc_embfecha")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TrcEmbfecha { get; set; }

    [Column("trc_embvapor")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TrcEmbvapor { get; set; }

    [Column("trc_embconten")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TrcEmbconten { get; set; }

    [Column("trc_embagencia")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TrcEmbagencia { get; set; }

    [Column("trc_embfactura")]
    [StringLength(15)]
    [Unicode(false)]
    public string? TrcEmbfactura { get; set; }

    [Column("trc_embcliente")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TrcEmbcliente { get; set; }

    [Column("trc_embmarcas")]
    [StringLength(50)]
    [Unicode(false)]
    public string? TrcEmbmarcas { get; set; }

    [Column("trc_embdestino")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TrcEmbdestino { get; set; }

    [Column("trc_cantajus", TypeName = "numeric(18, 0)")]
    public decimal? TrcCantajus { get; set; }

    [Column("trc_resprec")]
    [StringLength(10)]
    [Unicode(false)]
    public string? TrcResprec { get; set; }

    [Column("trc_movil")]
    [StringLength(10)]
    [Unicode(false)]
    public string? TrcMovil { get; set; }

    [Column("trc_sello")]
    [StringLength(20)]
    [Unicode(false)]
    public string? TrcSello { get; set; }

    [Column("trc_chofer", TypeName = "numeric(4, 0)")]
    public decimal? TrcChofer { get; set; }

    [Column("trc_embcia")]
    [StringLength(10)]
    [Unicode(false)]
    public string? TrcEmbcia { get; set; }

    [Column("trc_numpallet", TypeName = "numeric(2, 0)")]
    public decimal? TrcNumpallet { get; set; }

    [Column("trc_claveAcceso")]
    [StringLength(70)]
    [Unicode(false)]
    public string? TrcClaveAcceso { get; set; }

    [Column("trc_ambiente")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TrcAmbiente { get; set; }

    [Column("trc_emision")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TrcEmision { get; set; }

    [Column("trc_ptoser")]
    [StringLength(7)]
    [Unicode(false)]
    public string? TrcPtoser { get; set; }

    [Column("trc_secuen")]
    [StringLength(9)]
    [Unicode(false)]
    public string? TrcSecuen { get; set; }

    [Column("trc_autorizacion")]
    [StringLength(60)]
    [Unicode(false)]
    public string? TrcAutorizacion { get; set; }

    [Column("trc_mensaje")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? TrcMensaje { get; set; }

    [Column("trc_transmitido")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TrcTransmitido { get; set; }

    [Column("trc_FechaHoraAutorizacion", TypeName = "datetime")]
    public DateTime? TrcFechaHoraAutorizacion { get; set; }

    [Column("trc_maquina")]
    [StringLength(25)]
    [Unicode(false)]
    public string? TrcMaquina { get; set; }

    [Column("trc_serieFactLocal")]
    [StringLength(7)]
    [Unicode(false)]
    public string? TrcSerieFactLocal { get; set; }

    [Column("trc_numerFactLocal", TypeName = "numeric(18, 0)")]
    public decimal? TrcNumerFactLocal { get; set; }

    [Column("trc_requnif")]
    [StringLength(50)]
    [Unicode(false)]
    public string? TrcRequnif { get; set; }
}
