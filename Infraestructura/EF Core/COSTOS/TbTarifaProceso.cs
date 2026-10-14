using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[Keyless]
[Table("tb_tarifaProceso", Schema = "costos")]
public partial class TbTarifaProceso
{
    [Column("tp_id")]
    public short TpId { get; set; }

    [Column("tp_tipoProceso")]
    [StringLength(5)]
    [Unicode(false)]
    public string TpTipoProceso { get; set; } = null!;

    [Column("tp_tipoCorte")]
    [StringLength(5)]
    [Unicode(false)]
    public string TpTipoCorte { get; set; } = null!;

    [Column("tp_talCodigo")]
    public short TpTalCodigo { get; set; }

    [Column("tp_tarifa", TypeName = "numeric(18, 5)")]
    public decimal TpTarifa { get; set; }

    [Column("tp_peso", TypeName = "numeric(18, 5)")]
    public decimal? TpPeso { get; set; }

    [Column("tp_medida")]
    [StringLength(1)]
    [Unicode(false)]
    public string? TpMedida { get; set; }

    [Column("tp_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string TpEstado { get; set; } = null!;

    [Column("tp_usuarioCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string TpUsuarioCrea { get; set; } = null!;

    [Column("tp_fechaCrea", TypeName = "datetime")]
    public DateTime TpFechaCrea { get; set; }

    [Column("tp_equipoCrea")]
    [StringLength(60)]
    [Unicode(false)]
    public string TpEquipoCrea { get; set; } = null!;

    [Column("tp_usuarioMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? TpUsuarioMod { get; set; }

    [Column("tp_fechaMod", TypeName = "datetime")]
    public DateTime? TpFechaMod { get; set; }

    [Column("tp_equipoMod")]
    [StringLength(60)]
    [Unicode(false)]
    public string? TpEquipoMod { get; set; }

    [Column("tp_usuarioEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? TpUsuarioEli { get; set; }

    [Column("tp_fechaEli", TypeName = "datetime")]
    public DateTime? TpFechaEli { get; set; }

    [Column("tp_equipoEli")]
    [StringLength(60)]
    [Unicode(false)]
    public string? TpEquipoEli { get; set; }
}
