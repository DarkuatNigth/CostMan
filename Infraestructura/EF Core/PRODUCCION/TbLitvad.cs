using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_litvad")]
[Index("LidNumero", Name = "IX01_tb_litvad")]
[Index("LidLote", "LidProduc", "LidCodtal", Name = "ixlitvadlote")]
public partial class TbLitvad
{
    [Column("lid_numero", TypeName = "numeric(18, 0)")]
    public decimal LidNumero { get; set; }

    [Column("lid_lote")]
    public long LidLote { get; set; }

    [Column("lid_coche")]
    public int LidCoche { get; set; }

    [Column("lid_produc")]
    [StringLength(30)]
    [Unicode(false)]
    public string LidProduc { get; set; } = null!;

    [Column("lid_codtal")]
    public int LidCodtal { get; set; }

    [Column("lid_canenv")]
    public double LidCanenv { get; set; }

    [Column("lid_totnor")]
    public double LidTotnor { get; set; }

    [Column("lid_totdif")]
    public double LidTotdif { get; set; }

    [Column("lid_canpro")]
    public double LidCanpro { get; set; }

    [Column("lid_porcen")]
    public double LidPorcen { get; set; }

    [Column("lid_codcub")]
    [StringLength(1)]
    [Unicode(false)]
    public string LidCodcub { get; set; } = null!;

    [Column("lid_codsec")]
    [StringLength(1)]
    [Unicode(false)]
    public string LidCodsec { get; set; } = null!;

    [Column("lid_broken")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LidBroken { get; set; }

    [Column("lid_shello")]
    [StringLength(1)]
    [Unicode(false)]
    public string? LidShello { get; set; }

    [Column("lid_canuni", TypeName = "numeric(18, 0)")]
    public decimal? LidCanuni { get; set; }

    [Column("lid_canlbs", TypeName = "numeric(18, 0)")]
    public decimal? LidCanlbs { get; set; }

    [Column("lid_numsec", TypeName = "numeric(8, 0)")]
    public decimal? LidNumsec { get; set; }

    [Column("lid_pesopag", TypeName = "numeric(12, 2)")]
    public decimal? LidPesopag { get; set; }

    /// <summary>
    /// Abreviatura de clasificadora (tb_Clasificadora.cla_abrev)
    /// </summary>
    [Column("lid_clasificadora")]
    [StringLength(10)]
    [Unicode(false)]
    public string? LidClasificadora { get; set; }

    [Column("lid_numeroPO")]
    [StringLength(200)]
    [Unicode(false)]
    public string? LidNumeroPo { get; set; }

    [Column("lid_fecRegistro", TypeName = "datetime")]
    public DateTime? LidFecRegistro { get; set; }

    [ForeignKey("LidNumero")]
    public virtual TbLiqvag LidNumeroNavigation { get; set; } = null!;
}
