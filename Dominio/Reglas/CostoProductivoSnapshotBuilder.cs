using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Construye los snapshots que consume ParamProc sin volver a consultar
    /// Producción/SONG al guardar.
    ///
    /// Fuente de verdad:
    /// - lstCuentas: importes ya distribuidos y preparados para persistencia.
    /// - lstProcesosOrigen: resumen monetario por proceso/origen.
    /// - DataProcesoParam: libras y parámetros técnicos del mismo GET.
    /// </summary>
    public static class CostoProductivoSnapshotBuilder
    {
        private const decimal dcTolerancia = 0.0000001m;

        public static List<CostoProductivoDetalleModalDto> ConstruirDetalleActual(
            IEnumerable<CostoProductivoCuentaDto> lstCuentas)
        {
            var lstResultado = new List<CostoProductivoDetalleModalDto>();

            foreach (CostoProductivoCuentaDto objFila in
                lstCuentas ?? Array.Empty<CostoProductivoCuentaDto>())
            {
                AgregarParticion(
                    lstResultado,
                    objFila,
                    "EN",
                    "Entero",
                    objFila.dcMontoEnteroPersistencia);

                AgregarParticion(
                    lstResultado,
                    objFila,
                    "CO",
                    "Cola",
                    objFila.dcMontoColaPersistencia);

                AgregarParticion(
                    lstResultado,
                    objFila,
                    "VA",
                    "Valor Agregado",
                    objFila.dcMontoVagPersistencia);
            }

            return lstResultado
                .OrderBy(x => x.strProceso)
                .ThenBy(x => x.strParticionCodigo)
                .ThenBy(x => x.strCuenta)
                .ToList();
        }

        /// <summary>
        /// Hidratación tiene dos fuentes:
        /// 1) contable/mano de obra = lstProcesosOrigen RPC/Hidratación;
        /// 2) químicos = DataProcesoParam.dcCostoHidraReproceso.
        ///
        /// Table1 se sincroniza con el total que verá/guardará el usuario.
        /// No vuelve a consultar la BD y no duplica el químico en posteriores GET.
        /// </summary>
        public static void SincronizarHidratacion(
            DataProcesoParam objDataProceso,
            IEnumerable<CostoProductivoProcesoOrigenDto> lstProcesosOrigen)
        {
            ArgumentNullException.ThrowIfNull(objDataProceso);

            ProcesoResultadoDto? objHidratacion =
                (objDataProceso.lstProcesoRpc ?? new List<ProcesoResultadoDto>())
                .FirstOrDefault(x =>
                    Normalizar(x.strDescripcion) == "HIDRATACION");

            if (objHidratacion == null)
                return;

            decimal dcMontoContable =
                (lstProcesosOrigen ?? Array.Empty<CostoProductivoProcesoOrigenDto>())
                .Where(x => Normalizar(x.strOrigen) == "RPC")
                .Where(x => Normalizar(x.strProceso) == "HIDRATACION")
                .Sum(x => x.dcTotalDolares != 0m
                    ? x.dcTotalDolares
                    : x.dcDolaresEntero + x.dcDolaresCola + x.dcDolaresValorAgregado);

            decimal dcMontoQuimicos = Math.Max(0m, Convert.ToDecimal(objDataProceso.dcCostoHidraReproceso));
            decimal dcMontoTotal = Math.Round(dcMontoContable + dcMontoQuimicos, 5);

            objHidratacion.dcValor = dcMontoTotal;
            objHidratacion.dcCostUnitario = objHidratacion.dcLibras > 0m
                ? dcMontoTotal / objHidratacion.dcLibras
                : 0m;
        }

        private static void AgregarParticion(
            List<CostoProductivoDetalleModalDto> lstResultado,
            CostoProductivoCuentaDto objFila,
            string strParticionCodigo,
            string strParticion,
            decimal dcDolares)
        {
            if (Math.Abs(dcDolares) <= dcTolerancia)
                return;

            lstResultado.Add(new CostoProductivoDetalleModalDto
            {
                strEtapaCodigo = (objFila.strEcCodigo ?? string.Empty).Trim(),
                strEtapa = (objFila.strGrupoEtapa ?? string.Empty).Trim(),
                strProcesoCodigo = (objFila.strPcCodigo ?? string.Empty).Trim(),
                strProceso = (objFila.strEtapa ?? string.Empty).Trim(),

                strCuenta = (objFila.strCuenta ?? string.Empty).Trim(),
                strDescripcionCuenta = (objFila.strAuxiliar ?? string.Empty).Trim(),
                strCentroCodigo = (objFila.strCentroCodigo ?? string.Empty).Trim(),
                strSubcentroCodigo = (objFila.strSubcentroCodigo ?? string.Empty).Trim(),
                strAgrupacionCentro = (objFila.strAgrupacionCentro ?? string.Empty).Trim(),
                strGrupoCentro = (objFila.strGrupoCentro ?? string.Empty).Trim(),
                strNaturaleza = string.IsNullOrWhiteSpace(objFila.strNaturaleza)
                    ? "D"
                    : objFila.strNaturaleza.Trim().ToUpperInvariant(),

                strParticionCodigo = strParticionCodigo,
                strParticion = strParticion,
                dcDolares = Math.Round(dcDolares, 5)
            });
        }

        private static string Normalizar(string? strValor)
        {
            return (strValor ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U");
        }
    }
}
