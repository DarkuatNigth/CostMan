using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_bodega")]
public partial class TbBodega
{
    [Key]
    [Column("bod_codigo")]
    [StringLength(2)]
    [Unicode(false)]
    public string BodCodigo { get; set; } = null!;

    [Column("bod_planta")]
    public int BodPlanta { get; set; }

    [Column("bod_descri")]
    [StringLength(50)]
    [Unicode(false)]
    public string BodDescri { get; set; } = null!;

    [Column("bod_capaci")]
    public double? BodCapaci { get; set; }

    [Column("bod_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodEstado { get; set; }

    [Column("bod_tipo")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodTipo { get; set; }

    [Column("bod_invent")]
    [StringLength(50)]
    [Unicode(false)]
    public string? BodInvent { get; set; }

    [Column("bod_grupo")]
    public int BodGrupo { get; set; }

    [Column("bod_categ")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodCateg { get; set; }

    [Column("bod_rangCocheIni")]
    public int? BodRangCocheIni { get; set; }

    [Column("bod_rangCocheFin")]
    public int? BodRangCocheFin { get; set; }

    [Column("bod_MovBodega")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodMovBodega { get; set; }

    [Column("bod_sello")]
    public bool BodSello { get; set; }

    [Column("bod_direccion")]
    [StringLength(200)]
    [Unicode(false)]
    public string? BodDireccion { get; set; }

    [Column("bod_valmast")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodValmast { get; set; }

    [Column("bod_tiempoCong", TypeName = "numeric(2, 0)")]
    public decimal? BodTiempoCong { get; set; }

    [Column("bod_prioridad", TypeName = "numeric(2, 0)")]
    public decimal? BodPrioridad { get; set; }

    [Column("bod_tipo2")]
    [StringLength(5)]
    [Unicode(false)]
    public string? BodTipo2 { get; set; }

    [Column("bod_movCuarent")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodMovCuarent { get; set; }

    [Column("bod_direccionCorta")]
    [StringLength(100)]
    [Unicode(false)]
    public string? BodDireccionCorta { get; set; }

    [Column("bod_ptocgo")]
    [StringLength(100)]
    [Unicode(false)]
    public string? BodPtocgo { get; set; }

    [Column("bod_plantaProc")]
    public int? BodPlantaProc { get; set; }

    [Column("bod_pallet")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodPallet { get; set; }

    [Column("bod_ListCuarent")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodListCuarent { get; set; }

    [Column("bod_movStatus")]
    [StringLength(50)]
    [Unicode(false)]
    public string? BodMovStatus { get; set; }

    [Column("bod_movUnifLotes")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodMovUnifLotes { get; set; }

    [Column("bod_EsTransito")]
    public bool? BodEsTransito { get; set; }

    [Column("bod_descriExtendida")]
    [StringLength(100)]
    public string? BodDescriExtendida { get; set; }

    [Column("bod_embarque")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodEmbarque { get; set; }

    [Column("bod_requiererevision")]
    public bool? BodRequiererevision { get; set; }

    [Column("bod_codEstabDestino")]
    [StringLength(6)]
    [Unicode(false)]
    public string? BodCodEstabDestino { get; set; }

    [Column("bod_patio")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodPatio { get; set; }

    [Column("bod_plantaProcesoOE")]
    [StringLength(5)]
    [Unicode(false)]
    public string? BodPlantaProcesoOe { get; set; }

    [Column("bod_EsCola")]
    public bool? BodEsCola { get; set; }

    [Column("bod_EsPlanta")]
    public bool? BodEsPlanta { get; set; }

    [Column("bod_EsTransitoPT")]
    public bool? BodEsTransitoPt { get; set; }

    [Column("bod_movBalanzasAuto")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodMovBalanzasAuto { get; set; }

    /// <summary>
    /// Indica si la bodega debe incluirse en los egresos por consummo de etiquetas
    /// </summary>
    [Column("bod_darDeBajaEtiquetas")]
    public bool? BodDarDeBajaEtiquetas { get; set; }

    /// <summary>
    /// Indica si la bodega es utilizada para liquidacion directa.
    /// </summary>
    [Column("bod_esDeLiquidacion")]
    public bool? BodEsDeLiquidacion { get; set; }

    [Column("bod_EsRechazo")]
    public bool? BodEsRechazo { get; set; }

    /// <summary>
    /// Identificar las bodegas que pasan por el proceso de descongelado para pelar o para descabezar
    /// </summary>
    [Column("bod_esDecongelado")]
    public bool? BodEsDecongelado { get; set; }

    /// <summary>
    /// Se usará siempre que las bodegas sean de tipo transito, para que el sistema, al crear los secuenciales, sepa en que bodega se debe de extraer el saldo
    /// </summary>
    [Column("bod_bodFinal")]
    [StringLength(3)]
    [Unicode(false)]
    public string? BodBodFinal { get; set; }

    /// <summary>
    /// Indica si la bodega valida requerimiento de producto
    /// </summary>
    [Column("bod_validaRequerimiento")]
    public bool? BodValidaRequerimiento { get; set; }

    [Column("bod_esBrine")]
    public bool? BodEsBrine { get; set; }

    /// <summary>
    /// Codigo de bodega fisica o principal a la que pertenece la bodega logica.
    /// </summary>
    [Column("bod_codigoPadre")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodCodigoPadre { get; set; }

    /// <summary>
    /// Indicador si la bodega tiene configurado codigo QR pare restriccion de lectura manual.
    /// </summary>
    [Column("bod_tieneQR")]
    public bool? BodTieneQr { get; set; }
}
