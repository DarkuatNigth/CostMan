using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_materiaPrimaFrescoValorizada", Schema = "costos")]
public partial class TbMateriaPrimaFrescoValorizada
{
    [Key]
    [Column("mf_id")]
    public int MfId { get; set; }

    [Column("mf_empCodigo")]
    public short MfEmpCodigo { get; set; }

    [Column("mf_paCodigo")]
    public byte MfPaCodigo { get; set; }

    [Column("mf_fecha")]
    public DateOnly MfFecha { get; set; }

    [Column("mf_rloNumero")]
    public int MfRloNumero { get; set; }

    [Column("mf_proCodcor")]
    [StringLength(30)]
    [Unicode(false)]
    public string MfProCodcor { get; set; } = null!;

    [Column("mf_talCodigo")]
    public short MfTalCodigo { get; set; }

    [Column("mf_bodCodigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string MfBodCodigo { get; set; } = null!;

    [Column("mf_medCodigo")]
    public byte MfMedCodigo { get; set; }

    [Column("mf_embCodigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string MfEmbCodigo { get; set; } = null!;

    [Column("mf_masters", TypeName = "numeric(18, 5)")]
    public decimal MfMasters { get; set; }

    [Column("mf_libras", TypeName = "numeric(18, 5)")]
    public decimal MfLibras { get; set; }

    [Column("mf_costoUnitario", TypeName = "numeric(18, 5)")]
    public decimal MfCostoUnitario { get; set; }

    [Column("mf_costoTotal", TypeName = "numeric(18, 5)")]
    public decimal MfCostoTotal { get; set; }

    [Column("mf_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string MfEstado { get; set; } = null!;

    [Column("mf_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MfUsuarioCrea { get; set; } = null!;

    [Column("mf_fechaCrea", TypeName = "datetime")]
    public DateTime MfFechaCrea { get; set; }

    [Column("mf_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MfEquipoCrea { get; set; } = null!;

    [Column("mf_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MfUsuarioMod { get; set; }

    [Column("mf_fechaMod", TypeName = "datetime")]
    public DateTime? MfFechaMod { get; set; }

    [Column("mf_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MfEquipoMod { get; set; }

    [Column("mf_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MfUsuarioEli { get; set; }

    [Column("mf_fechaEli", TypeName = "datetime")]
    public DateTime? MfFechaEli { get; set; }

    [Column("mf_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MfEquipoEli { get; set; }
}
