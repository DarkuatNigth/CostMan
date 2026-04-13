using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[PrimaryKey("DxlNumpag", "DxlNrolot", "DxlTiplot")]
[Table("tb_cobpag1_dxl")]
[Index("DxlNrolot", "DxlTiplot", Name = "IX_tb_cobpag1_dxl_nrolot_tiplot")]
public partial class TbCobpag1Dxl
{
    [Key]
    [Column("dxl_numpag")]
    [StringLength(10)]
    [Unicode(false)]
    public string DxlNumpag { get; set; } = null!;

    [Key]
    [Column("dxl_nrolot", TypeName = "numeric(18, 0)")]
    public decimal DxlNrolot { get; set; }

    [Key]
    [Column("dxl_tiplot")]
    [StringLength(3)]
    [Unicode(false)]
    public string DxlTiplot { get; set; } = null!;

    [Column("dxl_pagado", TypeName = "numeric(18, 2)")]
    public decimal? DxlPagado { get; set; }
}
