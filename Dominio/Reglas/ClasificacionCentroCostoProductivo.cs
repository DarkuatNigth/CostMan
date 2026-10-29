using CostManagement.Aplicación.DTos;

namespace CostManagement.Dominio.Reglas;

// clasificación común al construir las filas contables desde configuración.
internal static class ClasificacionCentroCostoProductivo
{
    internal static string ObtenerAgrupacion(CostoProductivoConfigCuentaDto objConfig) =>
        !string.IsNullOrWhiteSpace(objConfig.strAgrupacionCentro)
            ? objConfig.strAgrupacionCentro
            : (string.IsNullOrWhiteSpace(objConfig.strProcesoCosto)
                ? objConfig.strPcCodigo ?? string.Empty
                : objConfig.strProcesoCosto).Trim().ToUpperInvariant();

    internal static string ObtenerGrupo(CostoProductivoConfigCuentaDto objConfig) =>
        !string.IsNullOrWhiteSpace(objConfig.strGrupoCentro)
            ? objConfig.strGrupoCentro
            : "OTROS";
}
