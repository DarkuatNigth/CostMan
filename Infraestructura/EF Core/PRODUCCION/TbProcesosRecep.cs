using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_procesosRecep")]
public partial class TbProcesosRecep
{
    [Key]
    [Column("prr_codigo", TypeName = "numeric(18, 0)")]
    public decimal PrrCodigo { get; set; }

    [Column("prr_descripcion")]
    [StringLength(50)]
    [Unicode(false)]
    public string PrrDescripcion { get; set; } = null!;

    [Column("prr_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string PrrEstado { get; set; } = null!;

    [Column("prr_mostrar")]
    [StringLength(1)]
    [Unicode(false)]
    public string? PrrMostrar { get; set; }

    [Column("prr_iniciales")]
    [StringLength(2)]
    [Unicode(false)]
    public string? PrrIniciales { get; set; }
}
