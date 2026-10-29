using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagementService.Infraestructura.EF_Core;

[PrimaryKey("CtaCodcia", "CtaNumero")]
[Table("tb_maecta")]
public partial class TbMaecta
{
    [Key]
    [Column("cta_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string CtaCodcia { get; set; } = null!;

    [Key]
    [Column("cta_numero")]
    [StringLength(13)]
    [Unicode(false)]
    public string CtaNumero { get; set; } = null!;

    [Column("cta_clave")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CtaClave { get; set; }

    [Column("cta_nivel")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CtaNivel { get; set; }

    [Column("cta_descor")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CtaDescor { get; set; }

    [Column("cta_deslar")]
    [StringLength(60)]
    [Unicode(false)]
    public string? CtaDeslar { get; set; }

    [Column("cta_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? CtaEstado { get; set; }

    [Column("cta_fluefe")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CtaFluefe { get; set; }

    [Column("cta_tipact")]
    [StringLength(4)]
    [Unicode(false)]
    public string? CtaTipact { get; set; }

    [Column("cta_tipcta")]
    [StringLength(4)]
    [Unicode(false)]
    public string? CtaTipcta { get; set; }

    [Column("cta_indfin")]
    [StringLength(4)]
    [Unicode(false)]
    public string? CtaIndfin { get; set; }

    [Column("cta_termin")]
    [StringLength(4)]
    [Unicode(false)]
    public string? CtaTermin { get; set; }

    [Column("cta_codaut")]
    [StringLength(4)]
    [Unicode(false)]
    public string? CtaCodaut { get; set; }

    [Column("cta_poraut")]
    public double? CtaPoraut { get; set; }

    [Column("cta_relaci")]
    [StringLength(13)]
    [Unicode(false)]
    public string? CtaRelaci { get; set; }

    [Column("cta_natura")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CtaNatura { get; set; }

    [Column("cta_saluni")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CtaSaluni { get; set; }

    [Column("cta_anio")]
    public double? CtaAnio { get; set; }

    [Column("cta_valrea")]
    public double? CtaValrea { get; set; }

    [Column("cta_valpre")]
    public double? CtaValpre { get; set; }

    [Column("cta_acurea")]
    public double? CtaAcurea { get; set; }

    [Column("cta_acupre")]
    public double? CtaAcupre { get; set; }

    [Column("cta_aniopre")]
    public double? CtaAniopre { get; set; }

    [Column("cta_usucre")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CtaUsucre { get; set; }

    [Column("cta_usumod")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CtaUsumod { get; set; }

    [Column("cta_usueli")]
    [StringLength(15)]
    [Unicode(false)]
    public string? CtaUsueli { get; set; }

    [Column("cta_feccre")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CtaFeccre { get; set; }

    [Column("cta_fecmod")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CtaFecmod { get; set; }

    [Column("cta_feceli")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CtaFeceli { get; set; }

    [Column("cta_grupo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CtaGrupo { get; set; }

    [Column("cta_subgrupo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CtaSubgrupo { get; set; }

    [Column("cta_fecini")]
    [StringLength(10)]
    [Unicode(false)]
    public string? CtaFecini { get; set; }

    [Column("cta_mostrarSaldoCero")]
    [StringLength(1)]
    [Unicode(false)]
    public string? CtaMostrarSaldoCero { get; set; }

    [Column("cta_cajaChica")]
    public bool? CtaCajaChica { get; set; }
}
