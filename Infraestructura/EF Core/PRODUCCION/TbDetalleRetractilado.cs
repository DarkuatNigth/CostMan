using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_DetalleRetractilado")]
[Index("Cajas", Name = "IX_tb_DetalleRetractilado_cajas")]
[Index("CodProd", "FecCrea", Name = "IX_tb_DetalleRetractilado_codprod_feccrea")]
public partial class TbDetalleRetractilado
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("cod_Retra")]
    public long CodRetra { get; set; }

    [Column("cod_prod")]
    [StringLength(25)]
    [Unicode(false)]
    public string? CodProd { get; set; }

    [Column("cod_tal", TypeName = "numeric(18, 0)")]
    public decimal? CodTal { get; set; }

    [Column("lote")]
    [StringLength(8)]
    [Unicode(false)]
    public string? Lote { get; set; }

    [Column("masters", TypeName = "numeric(18, 2)")]
    public decimal? Masters { get; set; }

    [Column("cajas", TypeName = "numeric(18, 2)")]
    public decimal? Cajas { get; set; }

    [Column("masters_retra", TypeName = "numeric(18, 2)")]
    public decimal? MastersRetra { get; set; }

    [Column("cajas_retra", TypeName = "numeric(18, 2)")]
    public decimal? CajasRetra { get; set; }

    [Column("usr_crea")]
    [StringLength(50)]
    [Unicode(false)]
    public string? UsrCrea { get; set; }

    [Column("fec_crea", TypeName = "datetime")]
    public DateTime? FecCrea { get; set; }
}
