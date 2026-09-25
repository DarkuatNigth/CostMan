using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_tarifaDecoradosRetractilado", Schema = "costos")]
public partial class TbTarifaDecoradosRetractilado
{
    [Key]
    [Column("tr_id")]
    public int TrId { get; set; }

    /// <summary>
    /// Codigo de embalaje. Ref SVRSNG04.PRODUCCION.dbo.tb_embala
    /// </summary>
    [Column("tr_embCodigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string TrEmbCodigo { get; set; } = null!;

    /// <summary>
    /// Peso del embalaje. Ref SVRSNG04.PRODUCCION.dbo.tb_embala
    /// </summary>
    [Column("tr_embPeso", TypeName = "numeric(18, 5)")]
    public decimal TrEmbPeso { get; set; }

    /// <summary>
    /// Codigo de la unidad de medida. Ref SVRSNG04.PRODUCCION.dbo.tb_medida
    /// </summary>
    [Column("tr_medCodigo")]
    public short TrMedCodigo { get; set; }

    [Column("tr_retractilado", TypeName = "numeric(18, 5)")]
    public decimal TrRetractilado { get; set; }

    [Column("tr_decorado", TypeName = "numeric(18, 5)")]
    public decimal TrDecorado { get; set; }
}
