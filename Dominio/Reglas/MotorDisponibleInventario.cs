using CostManagement.Aplicación.DTos;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// Construye el disponible valorizado ANTES DE VENTAS.
    ///
    /// Regla principal:
    /// - RECIBIDO de reproceso (Agrupacion = "1. RECIBIDO") descuenta del intLoteOrigen.
    /// - Cualquier otro movimiento conserva intLote.
    ///
    /// El motor NO recalcula costos de los movimientos.
    /// Conserva dcLibras y dcCostoTot exactamente como vienen desde CostVentUni.
    /// </summary>
    public class MotorDisponibleInventario
    {
        private const decimal dcTolerancia = 0.0001m;

        private readonly record struct ClaveLote(
            int? intLote,
            int intCodProd,
            int intCodTalla);

        private readonly record struct ClaveProdTalla(
            int intCodProd,
            int intCodTalla);

        private sealed class MovimientoDisponibleInterno
        {
            public CostVentUni objRegistro { get; init; } = null!;
            public ClaveLote objClave { get; init; }
        }

        public CostoVentaUnitarioResultadoDto Calcular(
            List<CostVentUni> lstDetalleActual)
        {
            if (lstDetalleActual == null)
                throw new ArgumentNullException(nameof(lstDetalleActual));

            var objResultado = new CostoVentaUnitarioResultadoDto();
            var lstMovimientos = new List<MovimientoDisponibleInterno>(lstDetalleActual.Count);

            // ============================================================
            // 1. RESOLVER EL LOTE REAL DEL INVENTARIO
            // ============================================================
            foreach (CostVentUni objRegistro in lstDetalleActual)
            {
                bool blEsRecibido = EsRecibido(objRegistro);

                int? intLoteDisponible = blEsRecibido
                    ? ResolverLoteOrigen(objRegistro)
                    : objRegistro.intLote;

                if (blEsRecibido && !intLoteDisponible.HasValue)
                {
                    objResultado.objAuditoria.lstExcepciones.Add(
                        CrearExcepcion(
                            objRegistro,
                            null,
                            "LOTE_ORIGEN_INVALIDO",
                            "RECIBIDO de reproceso sin intLoteOrigen válido. " +
                            "No se usa intLote como fallback y no se inventa un lote."));
                }

                lstMovimientos.Add(new MovimientoDisponibleInterno
                {
                    objRegistro = objRegistro,
                    objClave = new ClaveLote(
                        intLoteDisponible,
                        objRegistro.intCodProd,
                        objRegistro.intCodTalla)
                });
            }

            // ============================================================
            // 2. IDENTIFICAR CLAVES CON INGRESO REAL
            // ============================================================
            HashSet<ClaveLote> hshClavesConIngreso = lstMovimientos
                .Where(x => x.objRegistro.dcLibras > 0m)
                .Select(x => x.objClave)
                .ToHashSet();

            HashSet<ClaveLote> hshEgresosSinIngreso = lstMovimientos
                .Where(x => x.objRegistro.dcLibras < 0m)
                .Select(x => x.objClave)
                .Where(x => x.intLote.HasValue)
                .Where(x => !hshClavesConIngreso.Contains(x))
                .ToHashSet();

            foreach (ClaveLote objClave in hshEgresosSinIngreso)
            {
                CostVentUni objReferencia = lstMovimientos
                    .First(x => x.objClave.Equals(objClave))
                    .objRegistro;

                decimal dcLibras = lstMovimientos
                    .Where(x => x.objClave.Equals(objClave))
                    .Sum(x => x.objRegistro.dcLibras);

                decimal dcCosto = lstMovimientos
                    .Where(x => x.objClave.Equals(objClave))
                    .Sum(x => x.objRegistro.dcCostoTot);

                objResultado.objAuditoria.lstExcepciones.Add(
                    new CostoVentaDisponibleExcepcionDto
                    {
                        strTipoExcepcion = "EGRESO_SIN_INGRESO_LOTE",
                        intLoteOriginal = objReferencia.intLote,
                        intLoteOrigen = objReferencia.intLoteOrigen,
                        intLoteDisponible = objClave.intLote,
                        intCodProd = objClave.intCodProd,
                        intCodTalla = objClave.intCodTalla,
                        strTalla = objReferencia.strTalla ?? string.Empty,
                        dcLibras = dcLibras,
                        dcCostoTotal = dcCosto,
                        strDetalle =
                            "Existe egreso para Lote + CodProd + Talla, pero no existe " +
                            "ninguna fila positiva de ingreso para la misma clave resuelta."
                    });
            }

            // ============================================================
            // 3. DISPONIBLE POR LOTE + PRODUCTO + TALLA
            // ============================================================
            objResultado.lstDisponibleLote = lstMovimientos
                .GroupBy(x => x.objClave)
                .Select(g =>
                {
                    CostVentUni objReferencia = g
                        .Select(x => x.objRegistro)
                        .First();

                    List<CostVentUni> lstGrupo = g
                        .Select(x => x.objRegistro)
                        .ToList();

                    decimal dcLibrasIngreso = lstGrupo
                        .Where(x => x.dcLibras > 0m)
                        .Sum(x => x.dcLibras);

                    decimal dcLibrasEgresoFirmado = lstGrupo
                        .Where(x => x.dcLibras < 0m)
                        .Sum(x => x.dcLibras);

                    decimal dcCostoIngreso = lstGrupo
                        .Where(x => x.dcLibras > 0m)
                        .Sum(x => x.dcCostoTot);

                    decimal dcCostoEgresoFirmado = lstGrupo
                        .Where(x => x.dcLibras < 0m)
                        .Sum(x => x.dcCostoTot);

                    // IMPORTANTE:
                    // El disponible conserva exactamente el signo y el costo propio
                    // de cada movimiento. No se vuelve a valorizar el egreso.
                    decimal dcLibrasDisponible = lstGrupo.Sum(x => x.dcLibras);
                    decimal dcCostoTotalDisponible = lstGrupo.Sum(x => x.dcCostoTot);

                    decimal dcCostoXLibra = dcLibrasDisponible > 0m
                        ? dcCostoTotalDisponible / dcLibrasDisponible
                        : 0m;

                    string strEstadoSaldo = ObtenerEstadoSaldo(
                        g.Key,
                        dcLibrasDisponible,
                        hshEgresosSinIngreso);

                    return new CostoVentaDisponibleLoteDto
                    {
                        intLote = g.Key.intLote,
                        intCodProd = g.Key.intCodProd,
                        intCodTalla = g.Key.intCodTalla,
                        strTalla = objReferencia.strTalla ?? string.Empty,
                        strDescripcion = objReferencia.strDescripcion ?? string.Empty,
                        strTipoProd = objReferencia.strTipoProd ?? string.Empty,
                        strCongelamiento = objReferencia.strCongelamiento ?? string.Empty,
                        strClase = objReferencia.strClase ?? string.Empty,

                        dcLibrasIngreso = dcLibrasIngreso,
                        dcLibrasEgreso = Math.Abs(dcLibrasEgresoFirmado),
                        dcLibrasDisponible = dcLibrasDisponible,

                        dcCostoIngreso = dcCostoIngreso,
                        dcCostoEgreso = Math.Abs(dcCostoEgresoFirmado),
                        dcCostoTotalDisponible = dcCostoTotalDisponible,

                        dcCostoXLibra = dcCostoXLibra,
                        intCantidadMovimientos = lstGrupo.Count,
                        strEstadoSaldo = strEstadoSaldo
                    };
                })
                .OrderBy(x => x.intCodProd)
                .ThenBy(x => x.intCodTalla)
                .ThenBy(x => x.intLote)
                .ToList();

            // Registrar saldos negativos como excepción SIN modificarlos.
            foreach (CostoVentaDisponibleLoteDto objLote in
                objResultado.lstDisponibleLote.Where(x => x.dcLibrasDisponible < -dcTolerancia))
            {
                objResultado.objAuditoria.lstExcepciones.Add(
                    new CostoVentaDisponibleExcepcionDto
                    {
                        strTipoExcepcion = "SALDO_LOTE_NEGATIVO",
                        intLoteDisponible = objLote.intLote,
                        intCodProd = objLote.intCodProd,
                        intCodTalla = objLote.intCodTalla,
                        strTalla = objLote.strTalla,
                        dcLibras = objLote.dcLibrasDisponible,
                        dcCostoTotal = objLote.dcCostoTotalDisponible,
                        strDetalle =
                            "El saldo por Lote + CodProd + Talla es negativo. " +
                            "Se reporta tal como está; no se topa en cero."
                    });
            }

            // ============================================================
            // 4. COSTO DE VENTA UNITARIO = DISPONIBLE CONSOLIDADO
            //    POR PRODUCTO + TALLA, TODAVÍA SIN RESTAR VENTAS.
            // ============================================================
            objResultado.lstCostoVentaUnitario = objResultado.lstDisponibleLote
                .GroupBy(x => new ClaveProdTalla(x.intCodProd, x.intCodTalla))
                .Select(g =>
                {
                    CostoVentaDisponibleLoteDto objReferencia = g.First();

                    decimal dcLibrasDisponible = g.Sum(x => x.dcLibrasDisponible);
                    decimal dcCostoTotalDisponible = g.Sum(x => x.dcCostoTotalDisponible);

                    decimal dcCostoVentaUnitario = dcLibrasDisponible > 0m
                        ? dcCostoTotalDisponible / dcLibrasDisponible
                        : 0m;

                    return new CostoVentaUnitarioDto
                    {
                        intCodProd = g.Key.intCodProd,
                        intCodTalla = g.Key.intCodTalla,
                        strTalla = objReferencia.strTalla,
                        strDescripcion = objReferencia.strDescripcion,
                        strTipoProd = objReferencia.strTipoProd,
                        strCongelamiento = objReferencia.strCongelamiento,
                        strClase = objReferencia.strClase,
                        dcLibrasDisponible = dcLibrasDisponible,
                        dcCostoTotalDisponible = dcCostoTotalDisponible,
                        dcCostoVentaUnitario = dcCostoVentaUnitario,
                        intCantidadLotes = g.Count(x => x.intLote.HasValue)
                    };
                })
                .OrderBy(x => x.intCodProd)
                .ThenBy(x => x.intCodTalla)
                .ToList();

            // ============================================================
            // 5. CUADRE CONTRA EL UNIVERSO ORIGINAL CostVentUni
            // ============================================================
            Dictionary<ClaveProdTalla, (decimal dcLibras, decimal dcCosto, string strTalla)>
                dicBase = lstDetalleActual
                    .GroupBy(x => new ClaveProdTalla(x.intCodProd, x.intCodTalla))
                    .ToDictionary(
                        g => g.Key,
                        g => (
                            g.Sum(x => x.dcLibras),
                            g.Sum(x => x.dcCostoTot),
                            g.First().strTalla ?? string.Empty));

            Dictionary<ClaveProdTalla, CostoVentaUnitarioDto> dicDisponible =
                objResultado.lstCostoVentaUnitario
                    .ToDictionary(
                        x => new ClaveProdTalla(x.intCodProd, x.intCodTalla),
                        x => x);

            HashSet<ClaveProdTalla> hshClavesCuadre = dicBase.Keys.ToHashSet();
            hshClavesCuadre.UnionWith(dicDisponible.Keys);

            foreach (ClaveProdTalla objClave in hshClavesCuadre)
            {
                dicBase.TryGetValue(
                    objClave,
                    out (decimal dcLibras, decimal dcCosto, string strTalla) objBase);

                dicDisponible.TryGetValue(
                    objClave,
                    out CostoVentaUnitarioDto? objDisponible);

                decimal dcLibrasDisponible = objDisponible?.dcLibrasDisponible ?? 0m;
                decimal dcCostoDisponible = objDisponible?.dcCostoTotalDisponible ?? 0m;

                decimal dcDiferenciaLibras = dcLibrasDisponible - objBase.dcLibras;
                decimal dcDiferenciaCosto = dcCostoDisponible - objBase.dcCosto;

                if (Math.Abs(dcDiferenciaLibras) <= dcTolerancia &&
                    Math.Abs(dcDiferenciaCosto) <= dcTolerancia)
                {
                    continue;
                }

                objResultado.objAuditoria.lstDiferenciasCuadre.Add(
                    new CostoVentaDisponibleCuadreDto
                    {
                        intCodProd = objClave.intCodProd,
                        intCodTalla = objClave.intCodTalla,
                        strTalla =
                            objDisponible?.strTalla ??
                            objBase.strTalla ??
                            string.Empty,
                        dcLibrasBase = objBase.dcLibras,
                        dcLibrasDisponible = dcLibrasDisponible,
                        dcDiferenciaLibras = dcDiferenciaLibras,
                        dcCostoBase = objBase.dcCosto,
                        dcCostoDisponible = dcCostoDisponible,
                        dcDiferenciaCosto = dcDiferenciaCosto
                    });
            }

            // ============================================================
            // 6. RESUMEN INTERNO DE AUDITORÍA
            // ============================================================
            List<CostVentUni> lstRecibidosSinOrigen = lstDetalleActual
                .Where(EsRecibido)
                .Where(x => !x.intLoteOrigen.HasValue || x.intLoteOrigen.Value <= 0)
                .ToList();

            objResultado.objAuditoria.intTotalFilasBase = lstDetalleActual.Count;
            objResultado.objAuditoria.intTotalProdTalla =
                objResultado.lstCostoVentaUnitario.Count;
            objResultado.objAuditoria.intTotalClavesLote =
                objResultado.lstDisponibleLote.Count;
            objResultado.objAuditoria.intTotalClavesNegativas =
                objResultado.lstDisponibleLote.Count(
                    x => x.dcLibrasDisponible < -dcTolerancia);
            objResultado.objAuditoria.dcLibrasClavesNegativas =
                objResultado.lstDisponibleLote
                    .Where(x => x.dcLibrasDisponible < -dcTolerancia)
                    .Sum(x => x.dcLibrasDisponible);
            objResultado.objAuditoria.intTotalRecibidosSinLoteOrigen =
                lstRecibidosSinOrigen.Count;
            objResultado.objAuditoria.dcLibrasRecibidosSinLoteOrigen =
                lstRecibidosSinOrigen.Sum(x => x.dcLibras);
            objResultado.objAuditoria.intTotalEgresosSinIngreso =
                hshEgresosSinIngreso.Count;
            objResultado.objAuditoria.intTotalDiferenciasCuadre =
                objResultado.objAuditoria.lstDiferenciasCuadre.Count;
            objResultado.objAuditoria.blCuadra =
                objResultado.objAuditoria.intTotalDiferenciasCuadre == 0;

            return objResultado;
        }

        private static bool EsRecibido(CostVentUni objRegistro)
        {
            return string.Equals(
                (objRegistro.strAgrupacion ?? string.Empty).Trim(),
                "1. RECIBIDO",
                StringComparison.OrdinalIgnoreCase);
        }

        private static int? ResolverLoteOrigen(CostVentUni objRegistro)
        {
            return objRegistro.intLoteOrigen.HasValue &&
                   objRegistro.intLoteOrigen.Value > 0
                ? objRegistro.intLoteOrigen.Value
                : null;
        }

        private static string ObtenerEstadoSaldo(
            ClaveLote objClave,
            decimal dcLibrasDisponible,
            HashSet<ClaveLote> hshEgresosSinIngreso)
        {
            if (!objClave.intLote.HasValue)
                return "LOTE_ORIGEN_INVALIDO";

            if (hshEgresosSinIngreso.Contains(objClave))
                return "EGRESO_SIN_INGRESO";

            if (dcLibrasDisponible < -dcTolerancia)
                return "NEGATIVO";

            if (Math.Abs(dcLibrasDisponible) <= dcTolerancia)
                return "AGOTADO";

            return "DISPONIBLE";
        }

        private static CostoVentaDisponibleExcepcionDto CrearExcepcion(
            CostVentUni objRegistro,
            int? intLoteDisponible,
            string strTipoExcepcion,
            string strDetalle)
        {
            return new CostoVentaDisponibleExcepcionDto
            {
                strTipoExcepcion = strTipoExcepcion,
                intLoteOriginal = objRegistro.intLote,
                intLoteOrigen = objRegistro.intLoteOrigen,
                intLoteDisponible = intLoteDisponible,
                intCodProd = objRegistro.intCodProd,
                intCodTalla = objRegistro.intCodTalla,
                strTalla = objRegistro.strTalla ?? string.Empty,
                dcLibras = objRegistro.dcLibras,
                dcCostoTotal = objRegistro.dcCostoTot,
                strDetalle = strDetalle
            };
        }
    }
}
