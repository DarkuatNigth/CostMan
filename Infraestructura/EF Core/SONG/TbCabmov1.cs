using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[PrimaryKey("MovCodcia", "MovNummov", "MovTipo")]
[Table("tb_cabmov1")]
[Index("MovCodcia", "MovTipo", "MovProvee", "MovFactura", Name = "IX_t_cabmov1_codcia_tipo_provee_factura")]
[Index("MovFhasta", Name = "IX_tb_cabmov1_1")]
[Index("MovNumdoc", Name = "IX_tb_cabmov1_12")]
[Index("MovTipo", "MovFecha", "MovEstado", Name = "IX_tb_cabmov1_13")]
[Index("MovFecha", Name = "IX_tb_cabmov1_3")]
[Index("MovNummov", "MovEstado", Name = "IX_tb_cabmov1_4")]
[Index("MovTipo", "MovNumdoc", Name = "IX_tb_cabmov1_7")]
[Index("MovContrato", Name = "IX_tb_cabmov1_contrato")]
[Index("MovTipo", "MovEstado", "MovViaent", "MovFecha", Name = "IX_tb_cabmov1_tipo_estado_viaant_fecha")]
[Index("MovTipo", "MovProduc", "MovFecha", Name = "IX_tb_cabmov1_tipo_produc_fecha")]
public partial class TbCabmov1
{
    [Key]
    [Column("mov_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string MovCodcia { get; set; } = null!;

    [Key]
    [Column("mov_nummov")]
    [StringLength(10)]
    [Unicode(false)]
    public string MovNummov { get; set; } = null!;

    [Key]
    [Column("mov_tipo")]
    [StringLength(6)]
    [Unicode(false)]
    public string MovTipo { get; set; } = null!;

    [Column("mov_numdoc")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovNumdoc { get; set; }

    [Column("mov_numord")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovNumord { get; set; }

    [Column("mov_fecha")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovFecha { get; set; }

    [Column("mov_fhasta")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovFhasta { get; set; }

    [Column("mov_locori")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovLocori { get; set; }

    [Column("mov_bodori")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovBodori { get; set; }

    [Column("mov_provee")]
    [StringLength(13)]
    [Unicode(false)]
    public string? MovProvee { get; set; }

    [Column("mov_forpag")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovForpag { get; set; }

    [Column("mov_intere")]
    public double? MovIntere { get; set; }

    [Column("mov_subtot")]
    public double? MovSubtot { get; set; }

    [Column("mov_poriva")]
    public double? MovPoriva { get; set; }

    [Column("mov_valiva")]
    public double? MovValiva { get; set; }

    [Column("mov_pordes")]
    public double? MovPordes { get; set; }

    [Column("mov_valdes")]
    public double? MovValdes { get; set; }

    [Column("mov_flete")]
    public double? MovFlete { get; set; }

    [Column("mov_total")]
    public double? MovTotal { get; set; }

    /// <summary>
    /// Tipo de compra bienes (1), servicio (2), activo fijo (3)
    /// </summary>
    [Column("mov_viaent")]
    [StringLength(13)]
    [Unicode(false)]
    public string? MovViaent { get; set; }

    [Column("mov_respon")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovRespon { get; set; }

    [Column("mov_compra")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovCompra { get; set; }

    [Column("mov_dirpro")]
    [StringLength(255)]
    [Unicode(false)]
    public string? MovDirpro { get; set; }

    [Column("mov_locdes")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovLocdes { get; set; }

    [Column("mov_boddes")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovBoddes { get; set; }

    [Column("mov_cierre")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovCierre { get; set; }

    [Column("mov_confir")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovConfir { get; set; }

    [Column("mov_solici")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovSolici { get; set; }

    [Column("mov_concep", TypeName = "text")]
    public string? MovConcep { get; set; }

    [Column("mov_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? MovEstado { get; set; }

    [Column("mov_tipcom")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovTipcom { get; set; }

    [Column("mov_creuse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovCreuse { get; set; }

    [Column("mov_credat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovCredat { get; set; }

    [Column("mov_moduse")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovModuse { get; set; }

    [Column("mov_moddat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovModdat { get; set; }

    [Column("mod_eliuse")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ModEliuse { get; set; }

    [Column("mod_elidat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ModElidat { get; set; }

    [Column("mov_gastos")]
    public double? MovGastos { get; set; }

    [Column("mov_ruccli")]
    [StringLength(13)]
    [Unicode(false)]
    public string? MovRuccli { get; set; }

    [Column("mov_coment", TypeName = "text")]
    public string? MovComent { get; set; }

    [Column("mov_descue")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovDescue { get; set; }

    [Column("mov_cosven")]
    public double? MovCosven { get; set; }

    [Column("mov_linea")]
    [StringLength(5)]
    [Unicode(false)]
    public string? MovLinea { get; set; }

    [Column("mov_centro")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovCentro { get; set; }

    [Column("mov_subcen")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovSubcen { get; set; }

    /// <summary>
    /// Numero de cotizacion si el documento es o/c bienes
    /// </summary>
    [Column("mov_ordpro")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovOrdpro { get; set; }

    [Column("mov_tipegr")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovTipegr { get; set; }

    [Column("mov_factura")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovFactura { get; set; }

    [Column("mov_nomadi1")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovNomadi1 { get; set; }

    [Column("mov_totadi1")]
    public double? MovTotadi1 { get; set; }

    [Column("mov_tipadi1")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovTipadi1 { get; set; }

    [Column("mov_nomadi2")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovNomadi2 { get; set; }

    [Column("mov_totadi2")]
    public double? MovTotadi2 { get; set; }

    [Column("mov_tipadi2")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovTipadi2 { get; set; }

    [Column("mov_refere")]
    [StringLength(8)]
    [Unicode(false)]
    public string? MovRefere { get; set; }

    [Column("mov_serie")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovSerie { get; set; }

    [Column("mov_autimp")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovAutimp { get; set; }

    [Column("mov_autsri")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovAutsri { get; set; }

    [Column("mov_ndnc")]
    [StringLength(50)]
    [Unicode(false)]
    public string? MovNdnc { get; set; }

    [Column("mov_cargadic")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovCargadic { get; set; }

    [Column("mov_serie2")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovSerie2 { get; set; }

    [Column("mov_tipocom")]
    [StringLength(4)]
    [Unicode(false)]
    public string? MovTipocom { get; set; }

    [Column("mov_consdirec")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovConsdirec { get; set; }

    [Column("mov_numtrans", TypeName = "numeric(18, 0)")]
    public decimal? MovNumtrans { get; set; }

    [Column("mov_esttrans")]
    [StringLength(8)]
    public string? MovEsttrans { get; set; }

    [Column("mov_produc")]
    [StringLength(6)]
    [Unicode(false)]
    public string? MovProduc { get; set; }

    [Column("mov_codcaj")]
    [StringLength(3)]
    [Unicode(false)]
    public string? MovCodcaj { get; set; }

    [Column("mov_agenci")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovAgenci { get; set; }

    [Column("mov_kardex")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovKardex { get; set; }

    [Column("mov_despacho")]
    [StringLength(30)]
    [Unicode(false)]
    public string? MovDespacho { get; set; }

    /// <summary>
    /// VENTANA QUE GRABO REGISTRO
    /// </summary>
    [Column("mov_exporta")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovExporta { get; set; }

    [Column("mov_clascli")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovClascli { get; set; }

    /// <summary>
    /// TIPO DE TRANSACCION
    /// </summary>
    [Column("mov_tipotran")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovTipotran { get; set; }

    [Column("mov_grucli")]
    [StringLength(5)]
    [Unicode(false)]
    public string? MovGrucli { get; set; }

    [Column("mov_fecdig", TypeName = "datetime")]
    public DateTime? MovFecdig { get; set; }

    [Column("mov_fecemb")]
    [StringLength(20)]
    [Unicode(false)]
    public string? MovFecemb { get; set; }

    [Column("mov_nroref")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovNroref { get; set; }

    [Column("mov_nrotra")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovNrotra { get; set; }

    [Column("mov_codnav")]
    [StringLength(6)]
    [Unicode(false)]
    public string? MovCodnav { get; set; }

    [Column("mov_fecent")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovFecent { get; set; }

    [Column("mov_aresol")]
    [StringLength(6)]
    [Unicode(false)]
    public string? MovAresol { get; set; }

    [Column("mov_persol")]
    [StringLength(6)]
    [Unicode(false)]
    public string? MovPersol { get; set; }

    [Column("mov_nrobl")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovNrobl { get; set; }

    [Column("mov_fecbl")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovFecbl { get; set; }

    [Column("mov_nrofue")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovNrofue { get; set; }

    [Column("mov_nroord")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovNroord { get; set; }

    [Column("mov_nrodau")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovNrodau { get; set; }

    [Column("mov_despac")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovDespac { get; set; }

    [Column("mov_usuario")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovUsuario { get; set; }

    [Column("mov_equipo")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovEquipo { get; set; }

    [Column("mov_vistobueno")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovVistobueno { get; set; }

    [Column("mov_fecdoc")]
    [StringLength(10)]
    [Unicode(false)]
    public string? MovFecdoc { get; set; }

    [Column("mov_NroLotMat")]
    [StringLength(25)]
    [Unicode(false)]
    public string? MovNroLotMat { get; set; }

    [Column("mov_Elaborado")]
    [StringLength(50)]
    [Unicode(false)]
    public string? MovElaborado { get; set; }

    [Column("mov_TipoCompte")]
    [StringLength(2)]
    [Unicode(false)]
    public string? MovTipoCompte { get; set; }

    [Column("mov_ApFuePre")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovApFuePre { get; set; }

    [Column("mov_claveAcceso")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MovClaveAcceso { get; set; }

    [Column("mov_claveAutorizacion")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MovClaveAutorizacion { get; set; }

    [Column("mov_claveContingencia")]
    [StringLength(64)]
    [Unicode(false)]
    public string? MovClaveContingencia { get; set; }

    [Column("mov_ambiente")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovAmbiente { get; set; }

    [Column("mov_emision")]
    [StringLength(2)]
    [Unicode(false)]
    public string? MovEmision { get; set; }

    [Column("mov_fechaHoraClaveAutorizacion", TypeName = "datetime")]
    public DateTime? MovFechaHoraClaveAutorizacion { get; set; }

    [Column("mov_mensaje")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? MovMensaje { get; set; }

    [Column("mov_estaut")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovEstaut { get; set; }

    [Column("mov_CodGastoExport")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovCodGastoExport { get; set; }

    [Column("mov_DeudaReemb")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovDeudaReemb { get; set; }

    [Column("mov_FueraDePresupuesto")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovFueraDePresupuesto { get; set; }

    [Column("mov_ProveeReemb")]
    [StringLength(15)]
    [Unicode(false)]
    public string? MovProveeReemb { get; set; }

    [Column("mov_ObservaReemb")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? MovObservaReemb { get; set; }

    [Column("mov_transmitido")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovTransmitido { get; set; }

    [Column("mov_claveIniContingencia")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MovClaveIniContingencia { get; set; }

    [Column("mov_formaPago", TypeName = "numeric(18, 0)")]
    public decimal? MovFormaPago { get; set; }

    [Column("mov_tipoPer")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovTipoPer { get; set; }

    [Column("mov_cantPer", TypeName = "numeric(18, 0)")]
    public decimal? MovCantPer { get; set; }

    [Column("mov_coneptoFijo")]
    [Unicode(false)]
    public string? MovConeptoFijo { get; set; }

    [Column("mov_porcot")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovPorcot { get; set; }

    [Column("mov_xml", TypeName = "xml")]
    public string? MovXml { get; set; }

    [Column("mov_envioCorreo")]
    [StringLength(1)]
    [Unicode(false)]
    public string? MovEnvioCorreo { get; set; }

    [Column("mov_contrato")]
    public bool? MovContrato { get; set; }

    [Column("mov_lbrsVendidas", TypeName = "numeric(18, 2)")]
    public decimal? MovLbrsVendidas { get; set; }

    [Column("mov_lbrsEgresadas", TypeName = "numeric(18, 2)")]
    public decimal? MovLbrsEgresadas { get; set; }
}
