using System;
using System.ComponentModel.DataAnnotations.Schema;
using CostManagement.Dominio.Reglas.Iqf;

namespace CostManagement.Aplicación.DTos;

/// <summary>
/// Cuarto resultset de costos.sp_costoProductivo_configuracion.
/// Una fila por empresa + cuenta fuente IQF.
/// </summary>
public sealed class IqfConfiguracionDto
{
    [Column("iqf_empresa")]
    public int intEmpresa { get; set; }

    [Column("iqf_cuentaFuente")]
    public string strCuentaFuente { get; set; } = string.Empty;

    [Column("iqf_cuentaEntero")]
    public string strCuentaEntero { get; set; } = string.Empty;

    [Column("iqf_cuentaCola")]
    public string strCuentaCola { get; set; } = string.Empty;

    [Column("iqf_cuentaVag")]
    public string strCuentaVag { get; set; } = string.Empty;

    [Column("iqf_modoMonto")]
    public string strModoMonto { get; set; } = string.Empty;

    [Column("iqf_requiereCreditoRetiro")]
    public bool blRequiereCreditoRetiro { get; set; }

    [Column("iqf_tipo")]
    public string strTipo { get; set; } = string.Empty;

    [Column("iqf_rubro")]
    public string strRubro { get; set; } = string.Empty;

    [Column("iqf_filaHoja0")]
    public short intFilaHoja0 { get; set; }

    public IqfFuenteConfiguracion Convertir() => new(
        intEmpresa,
        (strCuentaFuente ?? string.Empty).Trim(),
        (strCuentaEntero ?? string.Empty).Trim(),
        (strCuentaCola ?? string.Empty).Trim(),
        (strCuentaVag ?? string.Empty).Trim(),
        (strModoMonto ?? string.Empty).Trim(),
        blRequiereCreditoRetiro);
}
