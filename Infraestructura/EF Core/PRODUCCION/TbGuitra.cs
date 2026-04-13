using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Guias de remision del Sistema de Produccion
/// </summary>
[Table("tb_guitra")]
[Index("GtrCodpro", Name = "IDX_TRAZA_LOTE")]
[Index("GtrCodtra", "GtrDatcre", Name = "IX_tb_guitra_codtra_datcre")]
[Index("GtrEstado", Name = "IX_tb_guitra_estado")]
[Index("GtrNumero", "GtrCodpro", Name = "IX_tb_guitra_numero_codpro")]
[Index("GtrEstado", "GtrDatcre", Name = "ix_guitraLogist")]
[Index("GtrNrolot", "GtrRecepFechaHoraLlegada", Name = "ixtb_guitraHoraLLegada")]
[Index("GtrCodpesca", "GtrTiporaleo", Name = "ixtb_guitraPescaTipoRaleo")]
public partial class TbGuitra
{
    /// <summary>
    /// Numero de guia
    /// </summary>
    [Key]
    [Column("gtr_numero", TypeName = "numeric(18, 0)")]
    public decimal GtrNumero { get; set; }

    [Column("gtr_planta")]
    public int GtrPlanta { get; set; }

    /// <summary>
    /// Codigo del proveedor. Codgio debe existir en tabla tb_provee.
    /// </summary>
    [Column("gtr_codpro", TypeName = "numeric(18, 0)")]
    public decimal GtrCodpro { get; set; }

    [Column("gtr_codtra")]
    [StringLength(6)]
    public string GtrCodtra { get; set; } = null!;

    [Column("gtr_codcho", TypeName = "numeric(18, 0)")]
    public decimal GtrCodcho { get; set; }

    [Column("gtr_sello")]
    [StringLength(8000)]
    [Unicode(false)]
    public string? GtrSello { get; set; }

    [Column("gtr_sachie", TypeName = "numeric(12, 2)")]
    public decimal GtrSachie { get; set; }

    [Column("gtr_sacmet", TypeName = "numeric(12, 2)")]
    public decimal GtrSacmet { get; set; }

    [Column("gtr_fecsal", TypeName = "datetime")]
    public DateTime GtrFecsal { get; set; }

    [Column("gtr_feclle", TypeName = "datetime")]
    public DateTime GtrFeclle { get; set; }

    [Column("gtr_kavcal", TypeName = "numeric(12, 2)")]
    public decimal GtrKavcal { get; set; }

    [Column("gtr_kavcon", TypeName = "numeric(12, 2)")]
    public decimal GtrKavcon { get; set; }

    [Column("gtr_tinas", TypeName = "numeric(12, 2)")]
    public decimal GtrTinas { get; set; }

    [Column("gtr_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string GtrEstado { get; set; } = null!;

    [Column("gtr_observ")]
    [StringLength(255)]
    [Unicode(false)]
    public string GtrObserv { get; set; } = null!;

    [Column("gtr_useapr")]
    [StringLength(10)]
    [Unicode(false)]
    public string GtrUseapr { get; set; } = null!;

    [Column("gtr_datapr", TypeName = "datetime")]
    public DateTime GtrDatapr { get; set; }

    [Column("gtr_usecre")]
    [StringLength(10)]
    [Unicode(false)]
    public string GtrUsecre { get; set; } = null!;

    [Column("gtr_datcre", TypeName = "datetime")]
    public DateTime GtrDatcre { get; set; }

    [Column("gtr_usemod")]
    [StringLength(10)]
    [Unicode(false)]
    public string GtrUsemod { get; set; } = null!;

    [Column("gtr_datmod", TypeName = "datetime")]
    public DateTime GtrDatmod { get; set; }

    [Column("gtr_useeli")]
    [StringLength(10)]
    [Unicode(false)]
    public string GtrUseeli { get; set; } = null!;

    [Column("gtr_dateli", TypeName = "datetime")]
    public DateTime GtrDateli { get; set; }

    /// <summary>
    /// &quot;Libras reportadas&quot;. A la fecha no se logra validar este numero con otros reportes, y no se logra identificar como se actualiza este valor.
    /// </summary>
    [Column("gtr_librep", TypeName = "numeric(12, 2)")]
    public decimal GtrLibrep { get; set; }

    /// <summary>
    /// Numero de lote de produccion. El valor debe existir en la tabla tb_reglot.
    /// </summary>
    [Column("gtr_nrolot")]
    public long? GtrNrolot { get; set; }

    /// <summary>
    /// Numero de piscina (programada) del proveedor o finca.
    /// </summary>
    [Column("gtr_piscin")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrPiscin { get; set; }

    [Column("gtr_motivotras", TypeName = "numeric(18, 0)")]
    public decimal? GtrMotivotras { get; set; }

    [Column("gtr_origen", TypeName = "numeric(18, 0)")]
    public decimal? GtrOrigen { get; set; }

    [Column("gtr_destino", TypeName = "numeric(18, 0)")]
    public decimal? GtrDestino { get; set; }

    [Column("gtr_fecsald", TypeName = "datetime")]
    public DateTime? GtrFecsald { get; set; }

    [Column("gtr_feclled", TypeName = "datetime")]
    public DateTime? GtrFeclled { get; set; }

    [Column("gtr_horsal")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrHorsal { get; set; }

    [Column("gtr_horlle")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrHorlle { get; set; }

    [Column("gtr_horsald")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrHorsald { get; set; }

    [Column("gtr_horlled")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrHorlled { get; set; }

    [Column("gtr_motexto")]
    [StringLength(255)]
    [Unicode(false)]
    public string? GtrMotexto { get; set; }

    [Column("gtr_equipocre")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEquipocre { get; set; }

    [Column("gtr_equipomod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEquipomod { get; set; }

    [Column("gtr_equipoeli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEquipoeli { get; set; }

    [Column("gtr_equipoapr")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEquipoapr { get; set; }

    [Column("gtr_useimp")]
    [StringLength(10)]
    [Unicode(false)]
    public string? GtrUseimp { get; set; }

    [Column("gtr_datimp", TypeName = "datetime")]
    public DateTime? GtrDatimp { get; set; }

    [Column("gtr_equipoimp")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEquipoimp { get; set; }

    [Column("gtr_sellorec")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrSellorec { get; set; }

    [Column("gtr_libdesp", TypeName = "numeric(12, 2)")]
    public decimal? GtrLibdesp { get; set; }

    [Column("gtr_kavcalrec", TypeName = "numeric(12, 2)")]
    public decimal? GtrKavcalrec { get; set; }

    [Column("gtr_kavconrec", TypeName = "numeric(12, 2)")]
    public decimal? GtrKavconrec { get; set; }

    [Column("gtr_tinasrec", TypeName = "numeric(12, 2)")]
    public decimal? GtrTinasrec { get; set; }

    [Column("gtr_sachierec", TypeName = "numeric(12, 2)")]
    public decimal? GtrSachierec { get; set; }

    [Column("gtr_sacmetrec", TypeName = "numeric(12, 2)")]
    public decimal? GtrSacmetrec { get; set; }

    [Column("gtr_kmsalida", TypeName = "numeric(18, 0)")]
    public decimal? GtrKmsalida { get; set; }

    [Column("gtr_kmllegada", TypeName = "numeric(18, 0)")]
    public decimal? GtrKmllegada { get; set; }

    [Column("gtr_kmsalidad", TypeName = "numeric(18, 0)")]
    public decimal? GtrKmsalidad { get; set; }

    [Column("gtr_kmllegadad", TypeName = "numeric(18, 0)")]
    public decimal? GtrKmllegadad { get; set; }

    [Column("gtr_userec")]
    [StringLength(10)]
    [Unicode(false)]
    public string? GtrUserec { get; set; }

    [Column("gtr_datrec", TypeName = "datetime")]
    public DateTime? GtrDatrec { get; set; }

    [Column("gtr_equiporec")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEquiporec { get; set; }

    [Column("gtr_estadoalt")]
    [StringLength(2)]
    [Unicode(false)]
    public string? GtrEstadoalt { get; set; }

    [Column("gtr_codprooringal", TypeName = "numeric(18, 0)")]
    public decimal? GtrCodprooringal { get; set; }

    [Column("gtr_destinooringal", TypeName = "numeric(18, 0)")]
    public decimal? GtrDestinooringal { get; set; }

    [Column("gtr_piscinaoringal")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrPiscinaoringal { get; set; }

    [Column("gtr_useautcamb")]
    [StringLength(10)]
    [Unicode(false)]
    public string? GtrUseautcamb { get; set; }

    [Column("gtr_datautcamb", TypeName = "datetime")]
    public DateTime? GtrDatautcamb { get; set; }

    [Column("gtr_equipoutcamb")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEquipoutcamb { get; set; }

    [Column("gtr_obsautcamb")]
    [StringLength(255)]
    [Unicode(false)]
    public string? GtrObsautcamb { get; set; }

    [Column("gtr_funmeta", TypeName = "numeric(12, 2)")]
    public decimal? GtrFunmeta { get; set; }

    [Column("gtr_funmetarec", TypeName = "numeric(12, 2)")]
    public decimal? GtrFunmetarec { get; set; }

    [Column("gtr_llesal", TypeName = "numeric(18, 0)")]
    public decimal GtrLlesal { get; set; }

    [Column("gtr_difsello")]
    [StringLength(2)]
    [Unicode(false)]
    public string? GtrDifsello { get; set; }

    [Column("gtr_RecepFechaHoraLlegada", TypeName = "datetime")]
    public DateTime? GtrRecepFechaHoraLlegada { get; set; }

    [Column("gtr_RecepFechaHoraIniDescarga", TypeName = "datetime")]
    public DateTime? GtrRecepFechaHoraIniDescarga { get; set; }

    [Column("gtr_RecepFechaHoraFinDescarga", TypeName = "datetime")]
    public DateTime? GtrRecepFechaHoraFinDescarga { get; set; }

    /// <summary>
    /// Libras reportadas por la finca, o libras recibidas desde el punto de vista de logistica. No son las libras recibidas desde el punto de vista de produccion.
    /// </summary>
    [Column("gtr_RecepLibras", TypeName = "numeric(18, 0)")]
    public decimal? GtrRecepLibras { get; set; }

    [Column("gtr_RecepTotal", TypeName = "numeric(18, 0)")]
    public decimal? GtrRecepTotal { get; set; }

    [Column("gtr_RecepTipoUnidad")]
    [StringLength(1)]
    [Unicode(false)]
    public string? GtrRecepTipoUnidad { get; set; }

    [Column("gtr_RecepObservac")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? GtrRecepObservac { get; set; }

    [Column("gtr_gramesperado", TypeName = "numeric(18, 4)")]
    public decimal? GtrGramesperado { get; set; }

    [Column("gtr_fectentlleg")]
    [StringLength(23)]
    [Unicode(false)]
    public string? GtrFectentlleg { get; set; }

    [Column("gtr_claveAcceso")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrClaveAcceso { get; set; }

    [Column("gtr_claveAutorizacion")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrClaveAutorizacion { get; set; }

    [Column("gtr_claveContingencia")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrClaveContingencia { get; set; }

    [Column("gtr_ambiente")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrAmbiente { get; set; }

    [Column("gtr_emision")]
    [StringLength(2)]
    [Unicode(false)]
    public string? GtrEmision { get; set; }

    [Column("gtr_fechaHoraClaveAutorizacion", TypeName = "datetime")]
    public DateTime? GtrFechaHoraClaveAutorizacion { get; set; }

    [Column("gtr_mensaje")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? GtrMensaje { get; set; }

    [Column("gtr_estaut")]
    [StringLength(60)]
    [Unicode(false)]
    public string? GtrEstaut { get; set; }

    /// <summary>
    /// Codigo del programa de pesca. Corresponde al campo PRODUCCION.tb_ProgramaPescas.ppe_secuenc.
    /// </summary>
    [Column("gtr_codpesca", TypeName = "numeric(18, 0)")]
    public decimal? GtrCodpesca { get; set; }

    [Column("gtr_piscinaReal")]
    [StringLength(50)]
    [Unicode(false)]
    public string? GtrPiscinaReal { get; set; }

    [Column("gtr_regularizada")]
    public int GtrRegularizada { get; set; }

    [Column("gtr_serie")]
    [StringLength(7)]
    [Unicode(false)]
    public string? GtrSerie { get; set; }

    [Column("gtr_codordencarga", TypeName = "numeric(18, 0)")]
    public decimal? GtrCodordencarga { get; set; }

    [Column("gtr_obseranulacion")]
    [StringLength(255)]
    [Unicode(false)]
    public string? GtrObseranulacion { get; set; }

    [Column("gtr_obserrecepcion")]
    [StringLength(255)]
    [Unicode(false)]
    public string? GtrObserrecepcion { get; set; }

    [Column("gtr_tiporaleo")]
    [StringLength(2)]
    [Unicode(false)]
    public string? GtrTiporaleo { get; set; }

    [Column("gtr_conmetabisulfito")]
    public bool? GtrConmetabisulfito { get; set; }

    [Column("gtr_tipPis")]
    [StringLength(3)]
    [Unicode(false)]
    public string? GtrTipPis { get; set; }

    [Column("gtr_plantaproc", TypeName = "numeric(18, 0)")]
    public decimal? GtrPlantaproc { get; set; }

    [Column("gtr_facturado")]
    public bool? GtrFacturado { get; set; }

    [Column("gtr_codmotivo", TypeName = "numeric(18, 0)")]
    public decimal? GtrCodmotivo { get; set; }

    [Column("gtr_valadicional", TypeName = "numeric(18, 2)")]
    public decimal? GtrValadicional { get; set; }

    [Column("gtr_valorguia", TypeName = "numeric(18, 2)")]
    public decimal? GtrValorguia { get; set; }

    [Column("gtr_valortotalguia", TypeName = "numeric(18, 2)")]
    public decimal? GtrValortotalguia { get; set; }

    [Column("gtr_gramreal", TypeName = "numeric(6, 2)")]
    public decimal? GtrGramreal { get; set; }

    [Column("gtr_ciacopacking", TypeName = "numeric(5, 0)")]
    public decimal? GtrCiacopacking { get; set; }

    [Column("gtr_seriecopacking")]
    [StringLength(20)]
    [Unicode(false)]
    public string? GtrSeriecopacking { get; set; }

    [Column("gtr_secuencopacking", TypeName = "numeric(18, 0)")]
    public decimal? GtrSecuencopacking { get; set; }

    [Column("gtr_guiaorigen", TypeName = "numeric(18, 0)")]
    public decimal? GtrGuiaorigen { get; set; }

    [Column("gtr_motivomov", TypeName = "numeric(5, 0)")]
    public decimal? GtrMotivomov { get; set; }

    [Column("gtr_codmotivoanula", TypeName = "numeric(18, 0)")]
    public decimal? GtrCodmotivoanula { get; set; }

    [Column("gtr_novalorizado")]
    public bool? GtrNovalorizado { get; set; }

    [Column("gtr_codguiaplantaexterna", TypeName = "numeric(18, 0)")]
    public decimal? GtrCodguiaplantaexterna { get; set; }

    /// <summary>
    /// Especifica si la guia lleva o no hielo cuando el tipo de transporte es terceros.
    /// </summary>
    [Column("gtr_conHieloTerceros")]
    public bool? GtrConHieloTerceros { get; set; }
}
