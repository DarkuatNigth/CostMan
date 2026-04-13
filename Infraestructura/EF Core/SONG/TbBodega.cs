using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[PrimaryKey("SucCodigo", "BodCodigo")]
[Table("tb_bodega")]
[Index("BodCodigo", Name = "IX_tb_bodega_codigo")]
public partial class TbBodega
{
    [Column("cia_codigo")]
    [StringLength(5)]
    [Unicode(false)]
    public string CiaCodigo { get; set; } = null!;

    [Key]
    [Column("suc_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string SucCodigo { get; set; } = null!;

    [Key]
    [Column("bod_codigo")]
    [StringLength(2)]
    [Unicode(false)]
    public string BodCodigo { get; set; } = null!;

    [Column("bod_descri")]
    [StringLength(50)]
    [Unicode(false)]
    public string? BodDescri { get; set; }

    [Column("bod_estado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodEstado { get; set; }

    [Column("bod_cuenta")]
    [StringLength(13)]
    [Unicode(false)]
    public string? BodCuenta { get; set; }

    [Column("bod_creuse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodCreuse { get; set; }

    [Column("bod_credat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodCredat { get; set; }

    [Column("bod_moduse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodModuse { get; set; }

    [Column("bod_moddat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodModdat { get; set; }

    [Column("bod_eliuse")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodEliuse { get; set; }

    [Column("bod_elidat")]
    [StringLength(10)]
    [Unicode(false)]
    public string? BodElidat { get; set; }

    [Column("bod_venta")]
    public byte? BodVenta { get; set; }

    [Column("bod_platra")]
    [StringLength(3)]
    [Unicode(false)]
    public string? BodPlatra { get; set; }

    [Column("bod_considerarCostoMaterial")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodConsiderarCostoMaterial { get; set; }

    [Column("bod_NoConsidAprReq")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodNoConsidAprReq { get; set; }

    [Column("bod_codBodProduccion")]
    [StringLength(2)]
    [Unicode(false)]
    public string? BodCodBodProduccion { get; set; }

    [Column("bod_egrAut")]
    [StringLength(1)]
    [Unicode(false)]
    public string? BodEgrAut { get; set; }

    [Column("bod_Direccion")]
    [StringLength(800)]
    [Unicode(false)]
    public string? BodDireccion { get; set; }

    [Column("bod_direccioncorta")]
    [StringLength(200)]
    [Unicode(false)]
    public string? BodDireccioncorta { get; set; }

    [Column("bod_relProveedor")]
    [StringLength(6)]
    [Unicode(false)]
    public string? BodRelProveedor { get; set; }

    [Column("bod_codEstablecimiento")]
    [StringLength(3)]
    [Unicode(false)]
    public string? BodCodEstablecimiento { get; set; }

    [Column("bod_codPuntoEmision")]
    [StringLength(3)]
    [Unicode(false)]
    public string? BodCodPuntoEmision { get; set; }

    [Column("bod_plaCodigo", TypeName = "numeric(18, 0)")]
    public decimal? BodPlaCodigo { get; set; }

    [Column("bod_usomantenimiento")]
    public bool? BodUsomantenimiento { get; set; }
}
