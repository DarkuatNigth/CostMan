using CostManagement.Aplicación.DTos;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Dominio.Reglas
{
    /// <summary>
    /// PRUEBA CONTROLADA DEL DISPONIBLE ANTES DE VENTAS (experimental).
    ///
    /// Reproduce el agrupado manual hecho en Excel: TODOS los movimientos
    /// (INVENTARIO, AJUSTES, LIQ_PFR, 1. RECIBIDO, 2. PROCESADO, LOTE PISO,
    /// movimientos de cámara) agrupan por su propio intLote + intCodProd +
    /// intCodTalla. RECIBIDO conserva sus libras/costo negativos (ya vienen
    /// firmados desde CostVentUni), pero NO se traslada a intLoteOrigen.
    /// intLoteUni no participa en la clave.
    ///
    /// El motor NO recalcula costos de los movimientos.
    /// El saldo es exclusivamente SUM(dcLibras) y SUM(dcCostoTot) del grupo.
    /// </summary>
    public class MotorDisponibleInventario
    {
        private const decimal dcTolerancia = 0.0001m;

        // Tolerancia de NEGOCIO para saldos residuales (distinta de dcTolerancia,
        // que es técnica y se usa para comparaciones de cuadre). Un saldo dentro
        // de este rango se considera agotado aunque no sea exactamente cero.
        private const decimal dcToleranciaSaldoLibras = 0.09m;

        // Umbral para decidir si un residuo monetario dentro del rango de
        // tolerancia de libras merece registrarse como excepción de auditoría.
        private const decimal dcToleranciaCostoResidual = 0.01m;

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
                // PRUEBA CONTROLADA COSTO VENTA UNITARIO: todos los movimientos
                // agrupan por su propio intLote. No se traslada RECIBIDO a
                // intLoteOrigen y no participa intLoteUni. Esto reproduce el
                // agrupado manual de Excel (Lote + CodProd + Talla).
                int? intLoteDisponible = objRegistro.intLote;

                // PRUEBA CONTROLADA COSTO VENTA UNITARIO: bloque LOTE_ORIGEN_INVALIDO
                // comentado — ya no aplica porque intLoteDisponible sale directo de
                // objRegistro.intLote (int no nullable), nunca de ResolverLoteOrigen.
                //bool blEsRecibido = EsRecibido(objRegistro);
                //if (blEsRecibido && !intLoteDisponible.HasValue)
                //{
                //    objResultado.objAuditoria.lstExcepciones.Add(
                //        CrearExcepcion(
                //            objRegistro,
                //            null,
                //            "LOTE_ORIGEN_INVALIDO",
                //            "RECIBIDO de reproceso sin intLoteOrigen válido. " +
                //            "No se usa intLote como fallback y no se inventa un lote."));
                //}

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
                .Where(x => EsIngreso(x.objRegistro))
                .Select(x => x.objClave)
                .ToHashSet();

            HashSet<ClaveLote> hshEgresosSinIngreso = lstMovimientos
                .Where(x => EsEgreso(x.objRegistro))
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
            // BLOQUE TEMPORAL DE DEBUG (PRUEBA CONTROLADA) — mantener
            // comentado. Reemplazar TU_LOTE, TU_CODPROD y "TU_TALLA" por los
            // valores reales solo para inspección manual puntual; no dejar
            // descomentado en ningún commit ni ejecución normal.
            // ============================================================
            //var lstDebugDetalleClave = lstMovimientos
            //    .Where(x =>
            //        x.objClave.intLote == TU_LOTE &&
            //        x.objClave.intCodProd == TU_CODPROD &&
            //        x.objClave.intCodTalla == TU_TALLA)
            //    .Select(x => new
            //    {
            //        x.objRegistro.strTipoLiq,
            //        x.objRegistro.strAgrupacion,
            //        x.objRegistro.intLote,
            //        x.objRegistro.intLoteOrigen,
            //        x.objRegistro.intLoteUni,
            //        x.objRegistro.dcLibras,
            //        x.objRegistro.dcCostoTot
            //    })
            //    .ToList();
            //
            //var lstDebugResumenPorTipo = lstMovimientos
            //    .Where(x =>
            //        x.objClave.intLote == TU_LOTE &&
            //        x.objClave.intCodProd == TU_CODPROD &&
            //        x.objClave.intCodTalla == TU_TALLA)
            //    .GroupBy(x => x.objRegistro.strTipoLiq)
            //    .Select(g => new
            //    {
            //        strTipoLiq = g.Key,
            //        intCantidad = g.Count(),
            //        dcLibras = g.Sum(x => x.objRegistro.dcLibras),
            //        dcCostoTot = g.Sum(x => x.objRegistro.dcCostoTot)
            //    })
            //    .ToList();

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

                    // Columnas explicativas: clasificadas por CostVentUni.strTipo
                    // (I = Ingreso, E = Egreso), NUNCA por el signo de dcLibras.
                    decimal dcLibrasIngreso = lstGrupo.Where(EsIngreso).Sum(x => x.dcLibras);
                    decimal dcLibrasEgresoFirmado = lstGrupo.Where(EsEgreso).Sum(x => x.dcLibras);
                    decimal dcCostoIngreso = lstGrupo.Where(EsIngreso).Sum(x => x.dcCostoTot);
                    decimal dcCostoEgresoFirmado = lstGrupo.Where(EsEgreso).Sum(x => x.dcCostoTot);

                    // Saldo real: suma algebraica firmada. NUNCA Ingreso - Egreso.
                    decimal dcLibrasDisponible = lstGrupo.Sum(x => x.dcLibras);
                    decimal dcCostoTotalDisponible = lstGrupo.Sum(x => x.dcCostoTot);

                    // BLOQUE TEMPORAL DE DEBUG (PRUEBA CONTROLADA) — mantener comentado.
                    //var lstDebugResumenGrupo = lstGrupo
                    //    .GroupBy(x => new { x.strTipo, x.strTipoLiq, x.strAgrupacion, x.strDescriMov })
                    //    .Select(gg => new { gg.Key.strTipo, gg.Key.strTipoLiq, gg.Key.strAgrupacion, gg.Key.strDescriMov, intCantidad = gg.Count(), dcLibras = gg.Sum(x => x.dcLibras), dcCostoTot = gg.Sum(x => x.dcCostoTot) })
                    //    .OrderBy(x => x.strTipo)
                    //    .ThenByDescending(x => Math.Abs(x.dcCostoTot))
                    //    .ToList();
                    //var lstDebugEgresos = lstGrupo.Where(EsEgreso).ToList();
                    //var dcDebugTotalCostoEgreso = lstDebugEgresos.Sum(x => x.dcCostoTot);

                    // Presentación
                    decimal dcCostoXLibra = CalcularCostoXLibra(dcLibrasDisponible, dcCostoTotalDisponible);
                    string strEstadoSaldo = ObtenerEstadoSaldo(g.Key, dcLibrasDisponible, hshEgresosSinIngreso);

                    return new CostoVentaDisponibleLoteDto
                    {
                        strTipoProceso = objReferencia.strTipoLiq ?? string.Empty,
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
                objResultado.lstDisponibleLote.Where(x => x.dcLibrasDisponible < -dcToleranciaSaldoLibras))
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

            // Registrar residuo de costo sin libras suficientes como excepción
            // SIN modificar dcLibrasDisponible/dcCostoTotalDisponible.
            foreach (CostoVentaDisponibleLoteDto objLote in
                objResultado.lstDisponibleLote.Where(x =>
                    Math.Abs(x.dcLibrasDisponible) <= dcToleranciaSaldoLibras &&
                    Math.Abs(x.dcCostoTotalDisponible) > dcToleranciaCostoResidual))
            {
                objResultado.objAuditoria.lstExcepciones.Add(
                    new CostoVentaDisponibleExcepcionDto
                    {
                        strTipoExcepcion = "COSTO_RESIDUAL_SIN_LIBRAS",
                        intLoteDisponible = objLote.intLote,
                        intCodProd = objLote.intCodProd,
                        intCodTalla = objLote.intCodTalla,
                        strTalla = objLote.strTalla,
                        dcLibras = objLote.dcLibrasDisponible,
                        dcCostoTotal = objLote.dcCostoTotalDisponible,
                        strDetalle =
                            "El lote está dentro del rango de tolerancia -0.09 a 0.09 lb, " +
                            "pero conserva un costo monetario residual."
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

                    decimal dcCostoVentaUnitario = CalcularCostoXLibra(
                        dcLibrasDisponible,
                        dcCostoTotalDisponible);

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
                    x => x.dcLibrasDisponible < -dcToleranciaSaldoLibras);
            objResultado.objAuditoria.dcLibrasClavesNegativas =
                objResultado.lstDisponibleLote
                    .Where(x => x.dcLibrasDisponible < -dcToleranciaSaldoLibras)
                    .Sum(x => x.dcLibrasDisponible);
            objResultado.objAuditoria.intTotalRecibidosSinLoteOrigen =
                lstRecibidosSinOrigen.Count;
            objResultado.objAuditoria.dcLibrasRecibidosSinLoteOrigen =
                lstRecibidosSinOrigen.Sum(x => x.dcLibras);
            objResultado.objAuditoria.intTotalEgresosSinIngreso =
                hshEgresosSinIngreso.Count;
            objResultado.objAuditoria.intTotalSignosInconsistentes = lstMovimientos.Count(x =>
                (EsIngreso(x.objRegistro) && (x.objRegistro.dcLibras < 0m || x.objRegistro.dcCostoTot < 0m)) ||
                (EsEgreso(x.objRegistro) && (x.objRegistro.dcLibras > 0m || x.objRegistro.dcCostoTot > 0m)));
            objResultado.objAuditoria.intTotalTipoSaldoInvalido = lstMovimientos.Count(x => !EsIngreso(x.objRegistro) && !EsEgreso(x.objRegistro));
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

        private static string NormalizarTipoSaldo(CostVentUni objRegistro) => (objRegistro.strTipo ?? string.Empty).Trim().ToUpperInvariant();
        private static bool EsIngreso(CostVentUni objRegistro) => NormalizarTipoSaldo(objRegistro) == "I";
        private static bool EsEgreso(CostVentUni objRegistro) => NormalizarTipoSaldo(objRegistro) == "E";

        // Único punto de cálculo del costo por libra para lote y consolidado.
        // Un saldo dentro de +-dcToleranciaSaldoLibras (residuo operativo) o
        // negativo más allá de esa tolerancia no divide: devuelve 0. Nunca
        // modifica dcLibrasDisponible/dcCostoTotalDisponible, solo lee.
        private static decimal CalcularCostoXLibra(
            decimal dcLibrasDisponible,
            decimal dcCostoTotalDisponible)
        {
            if (Math.Abs(dcLibrasDisponible) <= dcToleranciaSaldoLibras)
                return 0m;

            if (dcLibrasDisponible < 0m)
                return 0m;

            return dcCostoTotalDisponible / dcLibrasDisponible;
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

            if (Math.Abs(dcLibrasDisponible) <= dcToleranciaSaldoLibras)
                return "AGOTADO";

            if (dcLibrasDisponible < -dcToleranciaSaldoLibras)
                return "NEGATIVO";

            return "DISPONIBLE";
        }

        // PRUEBA CONTROLADA COSTO VENTA UNITARIO: comentado porque queda sin
        // caller — su único uso era el bloque LOTE_ORIGEN_INVALIDO comentado
        // en 2.2. No se elimina.
        //private static CostoVentaDisponibleExcepcionDto CrearExcepcion(
        //    CostVentUni objRegistro,
        //    int? intLoteDisponible,
        //    string strTipoExcepcion,
        //    string strDetalle)
        //{
        //    return new CostoVentaDisponibleExcepcionDto
        //    {
        //        strTipoExcepcion = strTipoExcepcion,
        //        intLoteOriginal = objRegistro.intLote,
        //        intLoteOrigen = objRegistro.intLoteOrigen,
        //        intLoteDisponible = intLoteDisponible,
        //        intCodProd = objRegistro.intCodProd,
        //        intCodTalla = objRegistro.intCodTalla,
        //        strTalla = objRegistro.strTalla ?? string.Empty,
        //        dcLibras = objRegistro.dcLibras,
        //        dcCostoTotal = objRegistro.dcCostoTot,
        //        strDetalle = strDetalle
        //    };
        //}
    }
}
