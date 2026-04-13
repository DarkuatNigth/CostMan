using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Keyless]
[Table("tb_boditesaldMes")]
[Index("BitFecha", "BitProduc", Name = "IX01_tb_boditesaldMes")]
[Index("BitFecha", "BitSdocaja", Name = "ixtb_boditesaldMesfecsdocaj")]
[Index("BitFecha", Name = "pk_boditesaldmes")]
public partial class TbBoditesaldMes
{
    [Column("bit_fecha")]
    [StringLength(10)]
    [Unicode(false)]
    public string BitFecha { get; set; } = null!;

    [Column("bit_planta", TypeName = "numeric(18, 0)")]
    public decimal BitPlanta { get; set; }

    [Column("bit_codbod")]
    [StringLength(2)]
    [Unicode(false)]
    public string BitCodbod { get; set; } = null!;

    [Column("bit_bodega")]
    [StringLength(2)]
    [Unicode(false)]
    public string BitBodega { get; set; } = null!;

    [Column("bit_codcub")]
    [StringLength(1)]
    [Unicode(false)]
    public string BitCodcub { get; set; } = null!;

    [Column("bit_codsec")]
    [StringLength(4)]
    [Unicode(false)]
    public string? BitCodsec { get; set; }

    [Column("bit_produc")]
    [StringLength(30)]
    [Unicode(false)]
    public string BitProduc { get; set; } = null!;

    [Column("bit_codtal", TypeName = "numeric(18, 0)")]
    public decimal BitCodtal { get; set; }

    [Column("bit_lote")]
    public long BitLote { get; set; }

    [Column("bit_sdocaja", TypeName = "numeric(12, 2)")]
    public decimal BitSdocaja { get; set; }

    [Column("bit_costot", TypeName = "numeric(12, 2)")]
    public decimal BitCostot { get; set; }

    [Column("bit_cosprm", TypeName = "numeric(12, 2)")]
    public decimal BitCosprm { get; set; }

    [Column("bit_sscc")]
    [StringLength(25)]
    [Unicode(false)]
    public string? BitSscc { get; set; }

    [Column("bit_ubicbarra")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BitUbicbarra { get; set; }

    [Column("bit_numeroPO")]
    [StringLength(200)]
    [Unicode(false)]
    public string? BitNumeroPo { get; set; }

    [Column("bit_libras")]
    public double? BitLibras { get; set; }
}
