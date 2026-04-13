using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Table("tb_materiaPrimaSaldo", Schema = "costos")]
public partial class TbMateriaPrimaSaldo
{
    [Key]
    [Column("mps_id")]
    public int MpsId { get; set; }

    [Column("mps_empCodigo")]
    public short MpsEmpCodigo { get; set; }

    [Column("mps_tipo")]
    [StringLength(1)]
    [Unicode(false)]
    public string MpsTipo { get; set; } = null!;

    [Column("mps_tipoLote")]
    [StringLength(3)]
    [Unicode(false)]
    public string MpsTipoLote { get; set; } = null!;

    [Column("mps_fechaCorte")]
    public DateOnly MpsFechaCorte { get; set; }

    [Column("mps_bodCodigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string MpsBodCodigo { get; set; } = null!;

    [Column("mps_rloNumero")]
    public int MpsRloNumero { get; set; }

    [Column("mps_talCodigo")]
    public short MpsTalCodigo { get; set; }

    [Column("mps_medCodigo")]
    public byte MpsMedCodigo { get; set; }

    [Column("mps_embCodigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string MpsEmbCodigo { get; set; } = null!;

    [Column("mps_proCodcor")]
    [StringLength(30)]
    [Unicode(false)]
    public string MpsProCodcor { get; set; } = null!;

    [Column("mps_masters", TypeName = "numeric(18, 5)")]
    public decimal MpsMasters { get; set; }

    [Column("mps_libras", TypeName = "numeric(18, 5)")]
    public decimal MpsLibras { get; set; }

    [Column("mps_costoUnitario", TypeName = "numeric(18, 5)")]
    public decimal MpsCostoUnitario { get; set; }

    [Column("mps_costoTotal", TypeName = "numeric(18, 5)")]
    public decimal MpsCostoTotal { get; set; }

    [Column("mps_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string MpsEstado { get; set; } = null!;

    [Column("mps_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MpsUsuarioCrea { get; set; } = null!;

    [Column("mps_fechaCrea", TypeName = "datetime")]
    public DateTime MpsFechaCrea { get; set; }

    [Column("mps_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MpsEquipoCrea { get; set; } = null!;

    [Column("mps_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MpsUsuarioMod { get; set; }

    [Column("mps_fechaMod", TypeName = "datetime")]
    public DateTime? MpsFechaMod { get; set; }

    [Column("mps_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MpsEquipoMod { get; set; }

    [Column("mps_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MpsUsuarioEli { get; set; }

    [Column("mps_fechaEli", TypeName = "datetime")]
    public DateTime? MpsFechaEli { get; set; }

    [Column("mps_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MpsEquipoEli { get; set; }

    [Column("mps_trsCodigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? MpsTrsCodigo { get; set; }

    [Column("mps_fecha", TypeName = "datetime")]
    public DateTime? MpsFecha { get; set; }
}
