using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CostManagement.Infraestructura.EF_Core;

[Table("tb_provee")]
public partial class TbProvee
{
    [Column("cia_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string CiaCodigo { get; set; } = null!;

    [Key]
    [Column("clp_codigo", TypeName = "numeric(18, 0)")]
    public decimal ClpCodigo { get; set; }

    [Column("clp_ruccli")]
    [StringLength(20)]
    [Unicode(false)]
    public string ClpRuccli { get; set; } = null!;

    [Column("clp_nomcom")]
    [StringLength(255)]
    [Unicode(false)]
    public string? ClpNomcom { get; set; }

    [Column("clp_nomcia")]
    [StringLength(255)]
    [Unicode(false)]
    public string? ClpNomcia { get; set; }

    [Column("clp_pagweb")]
    [StringLength(30)]
    [Unicode(false)]
    public string? ClpPagweb { get; set; }

    [Column("clp_emacia")]
    [StringLength(250)]
    [Unicode(false)]
    public string? ClpEmacia { get; set; }

    [Column("clp_direcc")]
    [StringLength(255)]
    [Unicode(false)]
    public string? ClpDirecc { get; set; }

    [Column("clp_dirdes")]
    [StringLength(255)]
    [Unicode(false)]
    public string? ClpDirdes { get; set; }

    [Column("clp_fecalt", TypeName = "datetime")]
    public DateTime? ClpFecalt { get; set; }

    [Column("clp_tipocl")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ClpTipocl { get; set; }

    [Column("pai_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? PaiCodigo { get; set; }

    [Column("est_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? EstCodigo { get; set; }

    [Column("ciu_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? CiuCodigo { get; set; }

    [Column("clp_codpos")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ClpCodpos { get; set; }

    [Column("reg_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? RegCodigo { get; set; }

    [Column("zon_codigo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ZonCodigo { get; set; }

    [Column("clp_natjur")]
    [StringLength(12)]
    [Unicode(false)]
    public string? ClpNatjur { get; set; }

    [Column("clp_clasif")]
    [StringLength(12)]
    [Unicode(false)]
    public string? ClpClasif { get; set; }

    [Column("clp_telef1")]
    [StringLength(16)]
    [Unicode(false)]
    public string? ClpTelef1 { get; set; }

    [Column("clp_telef2")]
    [StringLength(16)]
    [Unicode(false)]
    public string? ClpTelef2 { get; set; }

    [Column("clp_clifax")]
    [StringLength(16)]
    [Unicode(false)]
    public string? ClpClifax { get; set; }

    [Column("clp_terpag")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ClpTerpag { get; set; }

    [Column("clp_metenv")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ClpMetenv { get; set; }

    [Column("clp_lispre")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ClpLispre { get; set; }

    [Column("clp_banco")]
    [StringLength(30)]
    [Unicode(false)]
    public string? ClpBanco { get; set; }

    [Column("clp_impcli")]
    [StringLength(30)]
    [Unicode(false)]
    public string? ClpImpcli { get; set; }

    [Column("clp_priori")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ClpPriori { get; set; }

    [Column("clp_ctaban")]
    [StringLength(12)]
    [Unicode(false)]
    public string? ClpCtaban { get; set; }

    [Column("clp_estado")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClpEstado { get; set; }

    [Column("clp_limcre")]
    public double? ClpLimcre { get; set; }

    [Column("clp_ivapag")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClpIvapag { get; set; }

    [Column("clp_coment")]
    [StringLength(255)]
    [Unicode(false)]
    public string? ClpComent { get; set; }

    [Column("clp_contri")]
    [StringLength(60)]
    [Unicode(false)]
    public string? ClpContri { get; set; }

    [Column("clp_afilia")]
    [StringLength(60)]
    [Unicode(false)]
    public string? ClpAfilia { get; set; }

    [Column("com_codigo")]
    [StringLength(6)]
    [Unicode(false)]
    public string? ComCodigo { get; set; }

    [Column("clp_ultven")]
    [StringLength(20)]
    [Unicode(false)]
    public string? ClpUltven { get; set; }

    [Column("clp_numdev", TypeName = "money")]
    public decimal? ClpNumdev { get; set; }

    [Column("clp_numfac", TypeName = "money")]
    public decimal? ClpNumfac { get; set; }

    [Column("clp_dolfac", TypeName = "money")]
    public decimal? ClpDolfac { get; set; }

    [Column("clp_doldev", TypeName = "money")]
    public decimal? ClpDoldev { get; set; }

    [Column("clp_saldo", TypeName = "money")]
    public decimal? ClpSaldo { get; set; }

    [Column("clp_grupo")]
    [StringLength(3)]
    [Unicode(false)]
    public string? ClpGrupo { get; set; }

    [Column("clp_garant", TypeName = "decimal(18, 2)")]
    public decimal? ClpGarant { get; set; }

    [Column("clp_numacuerdo")]
    [StringLength(200)]
    [Unicode(false)]
    public string? ClpNumacuerdo { get; set; }

    [Column("clp_provacuerdo", TypeName = "numeric(18, 0)")]
    public decimal? ClpProvacuerdo { get; set; }

    [Column("clp_claprov")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClpClaprov { get; set; }

    [Column("clp_usucre")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ClpUsucre { get; set; }

    [Column("clp_feccre", TypeName = "datetime")]
    public DateTime? ClpFeccre { get; set; }

    [Column("clp_usumod")]
    [StringLength(15)]
    [Unicode(false)]
    public string? ClpUsumod { get; set; }

    [Column("clp_fecmod", TypeName = "datetime")]
    public DateTime? ClpFecmod { get; set; }

    [Column("clp_mostrarUti")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClpMostrarUti { get; set; }

    [Column("clp_NomAbrev")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ClpNomAbrev { get; set; }

    [Column("clp_CertificINP")]
    [StringLength(50)]
    [Unicode(false)]
    public string? ClpCertificInp { get; set; }

    [Column("clp_ArchivoAcuerdoMinisterial", TypeName = "image")]
    public byte[]? ClpArchivoAcuerdoMinisterial { get; set; }

    [Column("clp_NombreArchivoAcuerdoMinisterial")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? ClpNombreArchivoAcuerdoMinisterial { get; set; }

    [Column("clp_FecVigAcMi")]
    [StringLength(10)]
    [Unicode(false)]
    public string? ClpFecVigAcMi { get; set; }

    [Column("clp_inp")]
    public int? ClpInp { get; set; }

    [Column("clp_observacion")]
    [StringLength(1000)]
    [Unicode(false)]
    public string? ClpObservacion { get; set; }

    [Column("clp_fechainp", TypeName = "datetime")]
    public DateTime? ClpFechainp { get; set; }

    [Column("clp_certAsc")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClpCertAsc { get; set; }

    [Column("clp_emaciain")]
    [StringLength(250)]
    [Unicode(false)]
    public string? ClpEmaciain { get; set; }

    [Column("clp_afiliain")]
    [StringLength(60)]
    [Unicode(false)]
    public string? ClpAfiliain { get; set; }

    [Column("clp_telin")]
    [StringLength(25)]
    [Unicode(false)]
    public string? ClpTelin { get; set; }

    [Column("clp_simid", TypeName = "numeric(18, 0)")]
    public decimal? ClpSimid { get; set; }

    [Column("clp_grupoProc")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ClpGrupoProc { get; set; }

    [Column("clp_aplCostFlu")]
    public bool? ClpAplCostFlu { get; set; }

    [Column("clp_campamento")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClpCampamento { get; set; }

    [Column("clp_nomFinca")]
    [StringLength(100)]
    [Unicode(false)]
    public string? ClpNomFinca { get; set; }

    [Column("clp_correoFechSiem")]
    [StringLength(800)]
    [Unicode(false)]
    public string? ClpCorreoFechSiem { get; set; }

    [Column("clp_nomLarFin")]
    [StringLength(800)]
    [Unicode(false)]
    public string? ClpNomLarFin { get; set; }

    [Column("clp_subgru")]
    [StringLength(2)]
    [Unicode(false)]
    public string? ClpSubgru { get; set; }

    [Column("clp_cantHieloLog", TypeName = "numeric(18, 2)")]
    public decimal? ClpCantHieloLog { get; set; }

    [Column("clp_horatope")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ClpHoratope { get; set; }

    [Column("clp_factorInvierno", TypeName = "numeric(10, 2)")]
    public decimal? ClpFactorInvierno { get; set; }

    [Column("clp_factorVerano", TypeName = "numeric(10, 2)")]
    public decimal? ClpFactorVerano { get; set; }

    [Column("clp_certAscPorPiscina")]
    [StringLength(1)]
    [Unicode(false)]
    public string? ClpCertAscPorPiscina { get; set; }

    /// <summary>
    /// Lista de correos para envio de correos de organoleptico
    /// </summary>
    [Column("clp_correoEnvioOrganoleptico")]
    [Unicode(false)]
    public string? ClpCorreoEnvioOrganoleptico { get; set; }
}
