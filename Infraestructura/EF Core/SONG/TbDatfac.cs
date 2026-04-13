using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core.SONG;

[Table("tb_datfac")]
[Index("DfaFecha", Name = "idx_datfac_fecha")]
public partial class TbDatfac
{
    [Key]
    [Column("dfa_numpag")]
    [StringLength(10)]
    [Unicode(false)]
    public string DfaNumpag { get; set; } = null!;

    [Column("dfa_numfac")]
    [StringLength(15)]
    [Unicode(false)]
    public string DfaNumfac { get; set; } = null!;

    [Column("dfa_fecha")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaFecha { get; set; }

    [Column("dfa_feccad")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaFeccad { get; set; }

    [Column("dfa_numaut")]
    [StringLength(60)]
    [Unicode(false)]
    public string? DfaNumaut { get; set; }

    [Column("dfa_numimp")]
    [StringLength(15)]
    [Unicode(false)]
    public string? DfaNumimp { get; set; }

    [Column("dfa_provee")]
    [StringLength(50)]
    [Unicode(false)]
    public string? DfaProvee { get; set; }

    [Column("dfa_provin")]
    [StringLength(20)]
    [Unicode(false)]
    public string? DfaProvin { get; set; }

    [Column("dfa_ciudad")]
    [StringLength(20)]
    [Unicode(false)]
    public string? DfaCiudad { get; set; }

    [Column("dfa_fecpag")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaFecpag { get; set; }

    [Column("dfa_banco")]
    [StringLength(200)]
    [Unicode(false)]
    public string? DfaBanco { get; set; }

    [Column("dfa_cheque")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaCheque { get; set; }

    [Column("dfa_fecent")]
    [StringLength(15)]
    [Unicode(false)]
    public string? DfaFecent { get; set; }

    [Column("dfa_observ", TypeName = "text")]
    public string? DfaObserv { get; set; }

    [Column("dfa_campo1")]
    [StringLength(18)]
    [Unicode(false)]
    public string? DfaCampo1 { get; set; }

    [Column("dfa_campo2")]
    [StringLength(255)]
    [Unicode(false)]
    public string? DfaCampo2 { get; set; }

    [Column("dfa_campo3")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaCampo3 { get; set; }

    [Column("dfa_campo4")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaCampo4 { get; set; }

    [Column("dfa_nroser")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaNroser { get; set; }

    [Column("dfa_tipo")]
    [StringLength(20)]
    [Unicode(false)]
    public string? DfaTipo { get; set; }

    [Column("dfa_nroser2")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DfaNroser2 { get; set; }

    [Column("dfa_fiscal")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DfaFiscal { get; set; }

    [Column("dfa_estado")]
    [StringLength(2)]
    [Unicode(false)]
    public string? DfaEstado { get; set; }

    [Column("dfa_codcia")]
    [StringLength(6)]
    [Unicode(false)]
    public string? DfaCodcia { get; set; }
}
