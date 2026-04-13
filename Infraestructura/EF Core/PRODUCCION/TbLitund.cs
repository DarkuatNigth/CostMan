using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_litund")]
[Index("LidNumero", Name = "IX_tb_litund")]
[Index("LidLote", Name = "ix_tb_litund_lid_lote")]
[Index("LidProduc", Name = "ixlitundlid_produc")]
public partial class TbLitund
{
    [Column("lid_numero", TypeName = "numeric(18, 0)")]
    public decimal LidNumero { get; set; }

    [Column("lid_coche", TypeName = "numeric(18, 0)")]
    public decimal LidCoche { get; set; }

    [Column("lid_lote")]
    public long LidLote { get; set; }

    [Column("lid_produc")]
    [StringLength(30)]
    [Unicode(false)]
    public string LidProduc { get; set; } = null!;

    [Column("lid_codtal")]
    public int LidCodtal { get; set; }

    [Column("lid_canenv", TypeName = "numeric(12, 2)")]
    public decimal LidCanenv { get; set; }

    [Column("lid_totnor", TypeName = "numeric(12, 2)")]
    public decimal LidTotnor { get; set; }

    [Column("lid_totdif", TypeName = "numeric(12, 2)")]
    public decimal LidTotdif { get; set; }

    [Column("lid_canpro", TypeName = "numeric(9, 2)")]
    public decimal LidCanpro { get; set; }

    [Column("lid_porcen", TypeName = "numeric(5, 2)")]
    public decimal LidPorcen { get; set; }

    [Column("lid_codcub")]
    [StringLength(1)]
    [Unicode(false)]
    public string LidCodcub { get; set; } = null!;

    [Column("lid_codsec")]
    [StringLength(1)]
    [Unicode(false)]
    public string LidCodsec { get; set; } = null!;

    [Column("lid_locero")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LidLocero { get; set; }

    [Column("lid_canlbs", TypeName = "numeric(12, 2)")]
    public decimal? LidCanlbs { get; set; }

    [Column("lid_canuni", TypeName = "numeric(12, 2)")]
    public decimal? LidCanuni { get; set; }

    [Column("lid_numsec", TypeName = "numeric(8, 0)")]
    public decimal? LidNumsec { get; set; }

    [Column("lid_lbscuadre", TypeName = "numeric(12, 2)")]
    public decimal? LidLbscuadre { get; set; }

    /// <summary>
    /// Abreviatura de clasificadora (tb_Clasificadora.cla_abrev)
    /// </summary>
    [Column("lid_clasificadora")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LidClasificadora { get; set; }

    [Column("lid_fechaEtiqueta", TypeName = "datetime")]
    public DateTime? LidFechaEtiqueta { get; set; }

    [Column("lid_numeroPO")]
    [StringLength(200)]
    [Unicode(false)]
    public string? LidNumeroPo { get; set; }

    [Column("lid_fecRegistro", TypeName = "datetime")]
    public DateTime? LidFecRegistro { get; set; }

    [ForeignKey("LidNumero")]
    public virtual TbLiqtun LidNumeroNavigation { get; set; } = null!;
}
