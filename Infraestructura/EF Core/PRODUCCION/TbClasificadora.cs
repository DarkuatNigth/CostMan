using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_Clasificadora")]
public partial class TbClasificadora
{
    [Key]
    [Column("cla_codigo")]
    [StringLength(2)]
    [Unicode(false)]
    public string ClaCodigo { get; set; } = null!;

    [Column("cla_descripcion")]
    [StringLength(100)]
    [Unicode(false)]
    public string ClaDescripcion { get; set; } = null!;

    [Column("cla_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string ClaEstado { get; set; } = null!;

    [Column("cla_abrev")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ClaAbrev { get; set; }

    [Column("cla_HorIn")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ClaHorIn { get; set; }

    [Column("cla_LibHor", TypeName = "numeric(9, 2)")]
    public decimal? ClaLibHor { get; set; }

    [Column("cla_Planifica")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClaPlanifica { get; set; }

    [Column("cla_HorDiurno")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ClaHorDiurno { get; set; }

    [Column("cla_cantMaxEtiq", TypeName = "numeric(18, 0)")]
    public decimal? ClaCantMaxEtiq { get; set; }

    [Column("cla_libxHorN", TypeName = "numeric(18, 2)")]
    public decimal? ClaLibxHorN { get; set; }

    [Column("cla_codPlanta")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ClaCodPlanta { get; set; }

    [Column("cla_visibeMaquinaProceso")]
    public bool? ClaVisibeMaquinaProceso { get; set; }

    [Column("cla_manejalote")]
    public bool? ClaManejalote { get; set; }

    /// <summary>
    /// Indica si se debe crear un registro de limpieza al inicio de turno.
    /// </summary>
    [Column("cla_manchekclistcal", TypeName = "numeric(1, 0)")]
    public decimal? ClaManchekclistcal { get; set; }

    [Column("vertextbox")]
    [StringLength(2)]
    [Unicode(false)]
    public string? Vertextbox { get; set; }

    [Column("EsVAG")]
    public bool? EsVag { get; set; }

    public bool? EsDescabezado { get; set; }

    [Column("alc_clasifOrdenAsig")]
    public int? AlcClasifOrdenAsig { get; set; }

    [Column("productoproceso")]
    public bool? Productoproceso { get; set; }

    [Column("cla_ProcesadoPorMaquina")]
    public bool? ClaProcesadoPorMaquina { get; set; }

    [Column("cla_habilitacombotipo")]
    public bool? ClaHabilitacombotipo { get; set; }

    [Column("cla_esBrine")]
    public bool? ClaEsBrine { get; set; }

    [Column("cla_imprimeticket")]
    public bool? ClaImprimeticket { get; set; }

    [Column("cla_colormaquina")]
    [StringLength(100)]
    [Unicode(false)]
    public string? ClaColormaquina { get; set; }

    [Column("cla_planta", TypeName = "numeric(3, 0)")]
    public decimal? ClaPlanta { get; set; }

    [Column("cla_maqCalculaEficiencia")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClaMaqCalculaEficiencia { get; set; }

    [Column("cla_mostrarpopup")]
    public bool? ClaMostrarpopup { get; set; }

    [Column("cla_esMaquinaria")]
    public bool? ClaEsMaquinaria { get; set; }

    [Column("cla_popup")]
    [StringLength(5000)]
    [Unicode(false)]
    public string? ClaPopup { get; set; }

    [Column("cla_mostrararrmaq")]
    public bool? ClaMostrararrmaq { get; set; }

    [Column("cla_areaVAG")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ClaAreaVag { get; set; }

    /// <summary>
    /// Indicador para identificar clasificadoras que generan paradas auotmaticas.
    /// </summary>
    [Column("cla_paradaAuto")]
    public bool? ClaParadaAuto { get; set; }

    /// <summary>
    /// Indicador si la clasificadora debe considerarse para liquidaciones.
    /// </summary>
    [Column("cla_aplicaLiquidacion")]
    public bool? ClaAplicaLiquidacion { get; set; }

    /// <summary>
    /// Variación de gramaje máxima permitida para unificación de lotes
    /// </summary>
    [Column("cla_variacionMaxGramaje", TypeName = "numeric(18, 5)")]
    public decimal? ClaVariacionMaxGramaje { get; set; }
}
