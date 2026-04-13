using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_certificadoFT")]
public partial class TbCertificadoFt
{
    [Key]
    [Column("cer_id")]
    public int CerId { get; set; }

    [Column("cer_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CerCodigo { get; set; }

    [Column("cer_descri")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CerDescri { get; set; }

    [Column("cer_estado")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CerEstado { get; set; }

    [Column("descripcion2")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Descripcion2 { get; set; }

    [Column("esPorPiscina")]
    public bool? EsPorPiscina { get; set; }

    [Column("cer_esPorProvInsumo")]
    public bool? CerEsPorProvInsumo { get; set; }
}
