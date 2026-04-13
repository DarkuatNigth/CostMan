using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

/// <summary>
/// Cabecera del cálculo de costos de materiales de empaque por lote y producto terminado
/// </summary>
[Table("tb_costoEmpaqueCab", Schema = "costos")]
public partial class TbCostoEmpaqueCab
{
    [Key]
    [Column("ce_id")]
    public int CeId { get; set; }

    [Column("ce_empCodigo")]
    public short CeEmpCodigo { get; set; }

    [Column("ce_fecha")]
    public DateOnly CeFecha { get; set; }

    [Column("ce_rloNumero")]
    public int CeRloNumero { get; set; }

    [Column("ce_proCodcor")]
    [StringLength(30)]
    [Unicode(false)]
    public string CeProCodcor { get; set; } = null!;

    [Column("ce_lbsMaster", TypeName = "numeric(18, 5)")]
    public decimal CeLbsMaster { get; set; }

    [Column("cc_costo", TypeName = "numeric(18, 5)")]
    public decimal CcCosto { get; set; }

    [Column("cc_medCodigo")]
    public byte CcMedCodigo { get; set; }

    [Column("cc_embCodigo")]
    [StringLength(8)]
    [Unicode(false)]
    public string CcEmbCodigo { get; set; } = null!;

    [Column("cc_embPeso", TypeName = "numeric(18, 5)")]
    public decimal CcEmbPeso { get; set; }

    [Column("cc_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string CcEstado { get; set; } = null!;

    [Column("cc_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CcUsuarioCrea { get; set; } = null!;

    [Column("cc_fechaCrea", TypeName = "datetime")]
    public DateTime CcFechaCrea { get; set; }

    [Column("cc_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string CcEquipoCrea { get; set; } = null!;

    [Column("cc_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CcUsuarioMod { get; set; }

    [Column("cc_fechaMod", TypeName = "datetime")]
    public DateTime? CcFechaMod { get; set; }

    [Column("cc_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CcEquipoMod { get; set; }

    [InverseProperty("CdCc")]
    public virtual ICollection<TbCostoEmpaqueDet> TbCostoEmpaqueDet { get; set; } = new List<TbCostoEmpaqueDet>();
}
