using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_materiaPrimaReproValorizada", Schema = "costos")]
public partial class TbMateriaPrimaReproValorizada
{
    [Key]
    [Column("mr_id")]
    public int MrId { get; set; }

    [Column("mr_empCodigo")]
    public short MrEmpCodigo { get; set; }

    [Column("mr_paCodigo")]
    public byte MrPaCodigo { get; set; }

    [Column("mr_fecha")]
    public DateOnly MrFecha { get; set; }

    [Column("mr_lotNumero")]
    public int MrLotNumero { get; set; }

    [Column("mr_rloNumeroUnificado")]
    public int MrRloNumeroUnificado { get; set; }

    [Column("mr_proCodcor")]
    [StringLength(30)]
    [Unicode(false)]
    public string MrProCodcor { get; set; } = null!;

    [Column("mr_talCodigo")]
    public short MrTalCodigo { get; set; }

    [Column("mr_bodCodigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string MrBodCodigo { get; set; } = null!;

    [Column("mr_medCodigo")]
    public byte MrMedCodigo { get; set; }

    [Column("mr_embCodigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string MrEmbCodigo { get; set; } = null!;

    [Column("mr_masters", TypeName = "numeric(18, 5)")]
    public decimal MrMasters { get; set; }

    [Column("mr_libras", TypeName = "numeric(18, 5)")]
    public decimal MrLibras { get; set; }

    [Column("mr_costoUnitario", TypeName = "numeric(18, 5)")]
    public decimal MrCostoUnitario { get; set; }

    [Column("mr_costoTotal", TypeName = "numeric(18, 5)")]
    public decimal MrCostoTotal { get; set; }

    [Column("mr_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string MrEstado { get; set; } = null!;

    [Column("mr_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MrUsuarioCrea { get; set; } = null!;

    [Column("mr_fechaCrea", TypeName = "datetime")]
    public DateTime MrFechaCrea { get; set; }

    [Column("mr_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string MrEquipoCrea { get; set; } = null!;

    [Column("mr_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MrUsuarioMod { get; set; }

    [Column("mr_fechaMod", TypeName = "datetime")]
    public DateTime? MrFechaMod { get; set; }

    [Column("mr_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MrEquipoMod { get; set; }

    [Column("mr_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MrUsuarioEli { get; set; }

    [Column("mr_fechaEli", TypeName = "datetime")]
    public DateTime? MrFechaEli { get; set; }

    [Column("mr_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? MrEquipoEli { get; set; }
}
