// Consolidado de: DiarioMovimientoCodigos, CuentaDiarioResolver, DiarioMovimientoPersistencia, MotorRetornoContenedor, MotorDiarioCierreConfigurado, MotorCostoVentaSalidaHistorico, DiarioCierrePresentacionBuilder.
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagementService.Aplicacion.DTos;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CostManagement.Dominio.Reglas
{
    public static class DiarioMovimientoCodigos
    {
        public const string Etapa = "DM";

        public const string RetornoContenedor = "DRC";
        public const string RecepcionRetorno = "DRI";

        public const string Gif = "DGF";
        public const string Warren503 = "DWG";
        public const string WarrenGfd = "DWF";
        public const string WarrenPt = "DWP";

        public const string CostoProduccion = "DCP";
        public const string ProductoTerminado = "DPT";
        public const string MateriaPrima = "DMP";

        public const string CostoVentaExportacion = "DCE";
        public const string CostoVentaLocal = "DCL";
        public const string CostoVentaSalidaHistorico = "DVS";

        public static readonly string[] ProcesosCierreAutomatico =
        {
            RetornoContenedor,
            Gif,
            Warren503,
            WarrenGfd,
            WarrenPt,
            CostoProduccion,
            ProductoTerminado,
            MateriaPrima
        };
    }

    public sealed class CuentaDiarioResolver
    {
        private readonly IReadOnlyList<DiarioMovimientoCuentaDto> _cuentas;

        public CuentaDiarioResolver(IEnumerable<DiarioMovimientoCuentaDto> cuentas)
        {
            _cuentas = (cuentas ?? Array.Empty<DiarioMovimientoCuentaDto>()).ToList();

            foreach (DiarioMovimientoCuentaDto cuenta in _cuentas)
                cuenta.ResolverRol();
        }

        public DiarioMovimientoCuentaDto Obtener(string proceso, string rol)
        {
            DiarioMovimientoCuentaDto? cuenta = _cuentas.FirstOrDefault(x =>
                string.Equals(x.strProcesoCodigo, proceso, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.strRol, rol, StringComparison.OrdinalIgnoreCase));

            return cuenta ?? throw new InvalidOperationException(
                $"No existe configuración DM para Proceso={proceso}, Rol={rol}.");
        }

        public DiarioMovimientoCuentaDto Obtener(
            string proceso,
            string naturaleza,
            string tipoProducto,
            string clase)
        {
            string tipo = DiarioMovimientoPersistenciaDto.NormalizarTipo(tipoProducto) switch
            {
                "ENTERO" => "EN",
                "COLA" => "CO",
                "VALOR AGREGADO" => "VA",
                var x => x
            };

            string cls = (clase ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "A+" => "A",
                "N" => "B",
                var x => x
            };

            return Obtener(
                proceso,
                $"{naturaleza.Trim().ToUpperInvariant()}_{tipo}_{cls}");
        }

        public List<DiarioMovimientoCuentaDto> PorProceso(string proceso) =>
            _cuentas.Where(x =>
                string.Equals(x.strProcesoCodigo, proceso, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    public static class DiarioMovimientoPersistencia
    {
        public static DiarioMovimientoPersistenciaDto CrearLinea(
            int anio,
            int mes,
            string proceso,
            DiarioMovimientoCuentaDto cuenta,
            decimal monto,
            string documento,
            string metadatos,
            string? tipoProductoOverride = null)
        {
            string tipo = string.IsNullOrWhiteSpace(tipoProductoOverride)
                ? cuenta.strTipoProducto
                : tipoProductoOverride;

            var fila = new DiarioMovimientoPersistenciaDto
            {
                intAnio = anio,
                intMes = mes,
                dtFechaCorte = new DateOnly(
                    anio, mes, DateTime.DaysInMonth(anio, mes)),

                strEtapaCodigo = "DM",
                strProcesoCodigo = proceso,
                strCuenta = cuenta.strCuenta,

                strCentroCodigo = proceso,
                strSubcentroCodigo = cuenta.strRol,

                strAgrupacion = Limitar(documento, 250),
                strGrupo = Limitar(metadatos, 150),

                strNaturaleza =
                    cuenta.strNaturalezaMovimiento == "H" ? "H" : "D"
            };

            fila.AsignarMonto(tipo, monto);
            return fila;
        }

        public static DiarioMovimientoPersistenciaDto CrearCostoVentaSalida(
            int anio,
            int mes,
            string cuenta,
            string tipoProducto,
            int codProd,
            int codTalla,
            string facturaKey,
            string metadatos,
            decimal costoTotal)
        {
            var fila = new DiarioMovimientoPersistenciaDto
            {
                intAnio = anio,
                intMes = mes,
                dtFechaCorte = new DateOnly(
                    anio, mes, DateTime.DaysInMonth(anio, mes)),

                strEtapaCodigo = "DM",
                strProcesoCodigo = "DVS",
                strCuenta = cuenta,

                strCentroCodigo = codProd.ToString(),
                strSubcentroCodigo = codTalla.ToString(),

                strAgrupacion = Limitar($"FACT:{facturaKey}", 250),
                strGrupo = Limitar(metadatos, 150),
                strNaturaleza = "H"
            };

            fila.AsignarMonto(tipoProducto, costoTotal);
            return fila;
        }

        public static string Limitar(string? valor, int longitud)
        {
            string limpio = (valor ?? string.Empty).Trim();
            return limpio.Length <= longitud
                ? limpio
                : limpio.Substring(0, longitud);
        }
    }

    public sealed class MotorRetornoContenedor
    {
        public List<DiarioMovimientoPersistenciaDto> ConstruirPersistencia(
            int anio,
            int mes,
            NotaCreditoRetornoContenedorDto nota,
            IEnumerable<CostoVentaSalidaHistoricoDto> costoOriginal,
            IEnumerable<DiarioMovimientoCuentaDto> configuracion)
        {
            var resolver = new CuentaDiarioResolver(configuracion);

            List<CostoVentaSalidaHistoricoDto> detalle =
                (costoOriginal ?? Array.Empty<CostoVentaSalidaHistoricoDto>())
                .ToList();

            if (detalle.Count == 0)
                throw new InvalidOperationException(
                    $"NC {nota.strNumero}: no existe costo histórico de salida.");

            var resultado = new List<DiarioMovimientoPersistenciaDto>();

            string documento = $"NC:{nota.strNumero.Trim()}";

            string meta =
                $"FACT:{nota.strAplicaFactura.Trim()};" +
                $"REF:{nota.strReferenciaEmbarque ?? nota.strRefer};" +
                $"CLI:{nota.strCodCliente}";

            DiarioMovimientoCuentaDto transito =
                resolver.Obtener("DRC", "D_TRN");

            foreach (var grupo in detalle.GroupBy(x =>
                DiarioMovimientoPersistenciaDto.NormalizarTipo(x.strTipoProducto)))
            {
                decimal monto = Math.Round(
                    grupo.Sum(x => x.dcCostoTotal),
                    4,
                    MidpointRounding.AwayFromZero);

                resultado.Add(
                    DiarioMovimientoPersistencia.CrearLinea(
                        anio, mes, "DRC",
                        transito,
                        monto,
                        documento,
                        meta,
                        grupo.Key));
            }

            foreach (var grupo in detalle.GroupBy(x => new
            {
                Tipo = DiarioMovimientoPersistenciaDto.NormalizarTipo(x.strTipoProducto),
                Clase = NormalizarClase(x.strClase)
            }))
            {
                DiarioMovimientoCuentaDto cuenta = resolver.Obtener(
                    "DRC",
                    "H",
                    grupo.Key.Tipo,
                    grupo.Key.Clase);

                decimal monto = Math.Round(
                    grupo.Sum(x => x.dcCostoTotal),
                    4,
                    MidpointRounding.AwayFromZero);

                resultado.Add(
                    DiarioMovimientoPersistencia.CrearLinea(
                        anio, mes, "DRC",
                        cuenta,
                        monto,
                        documento,
                        meta));
            }

            return resultado;
        }

        private static string NormalizarClase(string? clase) =>
            (clase ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "A+" => "A",
                "N" => "B",
                var x => x
            };
    }

    public sealed class MotorDiarioCierreConfigurado
    {
        private readonly ILogger _logger;

        private readonly record struct Key(string Tipo, string Clase);

        private sealed class Acumulado
        {
            public decimal MateriaPrima { get; set; }
            public decimal CostoProduccion { get; set; }
            public decimal GastoIndirecto { get; set; }
            public decimal DeltaWarren { get; set; }
        }

        public MotorDiarioCierreConfigurado(ILogger logger)
        {
            _logger = logger;
        }

        public List<DiarioMovimientoPersistenciaDto> Construir(
            IEnumerable<LiquidacionResultado> liquidaciones,
            IEnumerable<DiarioMovimientoCuentaDto> cuentas,
            int anio,
            int mes)
        {
            // ANTERIOR: Dictionary<Key, Acumulado> bolsas = Agrupar(liquidaciones); ahora pasa el logger para el diagnóstico.
            Dictionary<Key, Acumulado> bolsas = Agrupar(liquidaciones, _logger);
            var resolver = new CuentaDiarioResolver(cuentas);
            var filas = new List<DiarioMovimientoPersistenciaDto>();

            filas.AddRange(CrearGif(anio, mes, bolsas, resolver));
            filas.AddRange(CrearWarren(anio, mes, "DWG", "DIARIO:-1A-", bolsas, resolver));
            filas.AddRange(CrearWarren(anio, mes, "DWF", "DIARIO:-1B-", bolsas, resolver));
            filas.AddRange(CrearWarren(anio, mes, "DWP", "DIARIO:-1C-", bolsas, resolver));

            filas.AddRange(CrearDoble(
                anio, mes, "DCP", "DIARIO:-2-",
                bolsas, resolver,
                x => x.CostoProduccion,
                incluirVa: true));

            filas.AddRange(CrearDoble(
                anio, mes, "DPT", "DIARIO:-3-",
                bolsas, resolver,
                x => x.CostoProduccion,
                incluirVa: true));

            filas.AddRange(CrearDoble(
                anio, mes, "DMP", "DIARIO:-4-",
                bolsas, resolver,
                x => x.MateriaPrima,
                incluirVa: false));

            _logger.LogInformation(
                "[MotorDiarioCierreConfigurado] Periodo {Anio}-{Mes:00}; Bolsas={Bolsas}; Filas={Filas}.",
                anio, mes, bolsas.Count, filas.Count);

            return filas;
        }

        private static IEnumerable<DiarioMovimientoPersistenciaDto> CrearGif(
            int anio,
            int mes,
            IReadOnlyDictionary<Key, Acumulado> bolsas,
            CuentaDiarioResolver resolver)
        {
            var filas = new List<DiarioMovimientoPersistenciaDto>();
            decimal total = 0m;

            foreach (var par in bolsas.Where(x => x.Key.Tipo != "VALOR AGREGADO"))
            {
                decimal monto = Math.Round(
                    par.Value.GastoIndirecto,
                    4,
                    MidpointRounding.AwayFromZero);

                if (monto == 0m) continue;

                DiarioMovimientoCuentaDto cuenta = resolver.Obtener(
                    "DGF",
                    "D",
                    par.Key.Tipo,
                    par.Key.Clase);

                filas.Add(
                    DiarioMovimientoPersistencia.CrearLinea(
                        anio, mes, "DGF",
                        cuenta,
                        monto,
                        "DIARIO:-1-",
                        $"{par.Key.Tipo}|{par.Key.Clase}"));

                total += monto;
            }

            if (total != 0m)
            {
                DiarioMovimientoCuentaDto contrapartida =
                    resolver.Obtener("DGF", "H_TOTAL");

                contrapartida.strNaturalezaMovimiento = "H";

                filas.Add(
                    DiarioMovimientoPersistencia.CrearLinea(
                        anio, mes, "DGF",
                        contrapartida,
                        Math.Round(total, 4, MidpointRounding.AwayFromZero),
                        "DIARIO:-1-",
                        "TOTAL GIF",
                        "ENTERO"));
            }

            return filas;
        }

        private static IEnumerable<DiarioMovimientoPersistenciaDto> CrearWarren(
            int anio,
            int mes,
            string proceso,
            string documento,
            IReadOnlyDictionary<Key, Acumulado> bolsas,
            CuentaDiarioResolver resolver)
        {
            var filas = new List<DiarioMovimientoPersistenciaDto>();

            foreach (var par in bolsas.Where(x => x.Key.Tipo != "VALOR AGREGADO"))
            {
                decimal delta = Math.Round(
                    par.Value.DeltaWarren,
                    4,
                    MidpointRounding.AwayFromZero);

                if (delta == 0m) continue;

                DiarioMovimientoCuentaDto cuenta = resolver.Obtener(
                    proceso,
                    "W",
                    par.Key.Tipo,
                    par.Key.Clase);

                cuenta.strNaturalezaMovimiento = delta > 0m ? "D" : "H";

                filas.Add(
                    DiarioMovimientoPersistencia.CrearLinea(
                        anio, mes, proceso,
                        cuenta,
                        Math.Abs(delta),
                        documento,
                        $"{par.Key.Tipo}|{par.Key.Clase}|DELTA WARREN"));
            }

            return filas;
        }

        private static IEnumerable<DiarioMovimientoPersistenciaDto> CrearDoble(
            int anio,
            int mes,
            string proceso,
            string documento,
            IReadOnlyDictionary<Key, Acumulado> bolsas,
            CuentaDiarioResolver resolver,
            Func<Acumulado, decimal> selector,
            bool incluirVa)
        {
            var filas = new List<DiarioMovimientoPersistenciaDto>();

            foreach (var par in bolsas.Where(x =>
                incluirVa || x.Key.Tipo != "VALOR AGREGADO"))
            {
                decimal monto = Math.Round(
                    selector(par.Value),
                    4,
                    MidpointRounding.AwayFromZero);

                if (monto == 0m) continue;

                foreach (string naturaleza in new[] { "D", "H" })
                {
                    DiarioMovimientoCuentaDto cuenta = resolver.Obtener(
                        proceso,
                        naturaleza,
                        par.Key.Tipo,
                        par.Key.Clase);

                    filas.Add(
                        DiarioMovimientoPersistencia.CrearLinea(
                            anio, mes, proceso,
                            cuenta,
                            Math.Abs(monto),
                            documento,
                            $"{par.Key.Tipo}|{par.Key.Clase}"));
                }
            }

            return filas;
        }

        // ANTERIOR: private static Dictionary<Key, Acumulado> Agrupar(IEnumerable<LiquidacionResultado> liquidaciones); ahora recibe el logger.
        private static Dictionary<Key, Acumulado> Agrupar(IEnumerable<LiquidacionResultado> liquidaciones, ILogger logger)
        {
            var dic = new Dictionary<Key, Acumulado>();
            int intOmitidas = 0; decimal dcDeltaOmitidoTotal = 0m;
            var lstMuestraOmitidas = new List<string>();

            foreach (LiquidacionResultado liq in
                liquidaciones ?? Array.Empty<LiquidacionResultado>())
            {
                string? particion = ParticionCosteo.ClasificarFrs(
                    liq.strProClas01,
                    liq.strProClas05);

                string tipo = particion switch
                {
                    ParticionCosteo.ENTERO => "ENTERO",
                    ParticionCosteo.COLA => "COLA",
                    ParticionCosteo.ENTERO_VA => "VALOR AGREGADO",
                    ParticionCosteo.COLA_VA => "VALOR AGREGADO",
                    _ => string.Empty
                };

                string clase = NormalizarClase(liq.strProClas02);

                // ANTERIOR (descartaba en silencio), reemplazado por conteo y una sola advertencia al final: la línea se sigue omitiendo, pero queda visible.
                //if (string.IsNullOrWhiteSpace(tipo) || clase is not ("A" or "B" or "C"))
                //    continue;
                if (string.IsNullOrWhiteSpace(tipo) || clase is not ("A" or "B" or "C"))
                {
                    decimal dcDeltaOmitido = (liq.dcTotalWarren ?? (liq.dcCostTotalProc ?? 0m)) - (liq.dcCostTotalProc ?? 0m);
                    intOmitidas++;
                    dcDeltaOmitidoTotal += dcDeltaOmitido;
                    if (dcDeltaOmitido != 0m && lstMuestraOmitidas.Count < 20) lstMuestraOmitidas.Add($"Prod={liq.intCodProd} Lote={liq.intLote} Clas01={liq.strProClas01} Clas05={liq.strProClas05} Clase={clase} DeltaWarren={dcDeltaOmitido}");
                    continue;
                }

                var key = new Key(tipo, clase);

                if (!dic.TryGetValue(key, out Acumulado? acc))
                {
                    acc = new Acumulado();
                    dic[key] = acc;
                }

                decimal materiaPrima = (decimal)(liq.dcTotalDol ?? 0d);

                decimal totalCierre =
                    liq.dcTotalCostoWarren ??
                    liq.dcTotalDolSum;

                decimal costoProduccion =
                    totalCierre - materiaPrima;

                decimal indirecto =
                    (liq.ProcesoCostIndirecto?.dcCostoFijo ?? 0m) +
                    (liq.ProcesoCostIndirecto?.dcCostoVariable ?? 0m);

                decimal normal = liq.dcCostTotalProc ?? 0m;
                decimal warren = liq.dcTotalWarren ?? normal;

                acc.MateriaPrima += materiaPrima;
                acc.CostoProduccion += costoProduccion;
                acc.GastoIndirecto += indirecto;
                acc.DeltaWarren += warren - normal;
            }

            if (intOmitidas > 0) logger.LogWarning("[DiagWarrenDiario] MotorDiarioCierre.Agrupar omitió {Omitidas} línea(s) PFR por partición vacía o clase no A/B/C; DeltaWarren omitido={Delta}. Muestra con delta (máx. 20): {Muestra}", intOmitidas, dcDeltaOmitidoTotal, string.Join(" | ", lstMuestraOmitidas));
            // ACTIVAR DESPUÉS DE REVISIÓN DE LOGS: if (Math.Abs(dcDeltaOmitidoTotal) > 0.01m) throw new InvalidOperationException($"MotorDiarioCierre omitió delta Warren sin clasificar: {dcDeltaOmitidoTotal}.");
            return dic;
        }

        private static string NormalizarClase(string? clase) =>
            (clase ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "A+" => "A",
                "N" => "B",
                var x => x
            };
    }

    /// <summary>
    /// Congela por Factura + Producto + Talla el mismo costo unitario
    /// que usa CostVentUni para valorizar la salida.
    /// </summary>
    public sealed class MotorCostoVentaSalidaHistorico
    {
        private const int DECIMALES_CU = 4;
        private const int DECIMALES_CT = 2;

        public List<CostoVentaSalidaHistoricoDto> Construir(
            IEnumerable<CostVentUni> costos,
            IEnumerable<FacturaCostoSalidaDto> facturas,
            IEnumerable<InfoProd> productos,
            IEnumerable<DiarioMovimientoCuentaDto> configuracion)
        {
            List<CostVentUni> lstCostos =
                (costos ?? Array.Empty<CostVentUni>()).ToList();

            List<FacturaCostoSalidaDto> lstFacturas =
                (facturas ?? Array.Empty<FacturaCostoSalidaDto>())
                .Where(x =>
                    x.intCodProd > 0 &&
                    x.intCodTalla > 0 &&
                    x.dcLibras != 0m &&
                    !string.IsNullOrWhiteSpace(x.strFacturaKey))
                .ToList();

            var dicCostoVenta = CostVentUni.ConstruirDictPromCostUni(lstCostos);

            Dictionary<int, InfoProd> dicProducto =
                (productos ?? Array.Empty<InfoProd>())
                .Where(x => int.TryParse(x.strProCodcor, out _))
                .GroupBy(x => Convert.ToInt32(x.strProCodcor))
                .ToDictionary(g => g.Key, g => g.First());

            var resolver = new CuentaDiarioResolver(configuracion);
            var resultado = new List<CostoVentaSalidaHistoricoDto>();

            foreach (var grupo in lstFacturas.GroupBy(x => new
            {
                x.strFacturaKey,
                x.intCodProd,
                CodTalla = (int)x.intCodTalla
            }))
            {
                var keyCosto = new PromXProdTal(
                    grupo.Key.intCodProd.ToString(),
                    grupo.Key.CodTalla);

                decimal costoBase = dicCostoVenta.GetValueOrDefault(keyCosto, 0m);

                if (costoBase == 0m)
                    continue;

                decimal costoUnitario = Truncar(costoBase, DECIMALES_CU);
                decimal libras = grupo.Sum(x => Math.Abs(x.dcLibras));
                decimal costoTotal = Math.Round(
                    libras * costoUnitario,
                    DECIMALES_CT,
                    MidpointRounding.AwayFromZero);

                FacturaCostoSalidaDto primera = grupo.First();

                dicProducto.TryGetValue(
                    grupo.Key.intCodProd,
                    out InfoProd? info);

                string tipoProducto = ResolverTipoProducto(
                    info?.strProClas01,
                    info?.strProClas05);

                string clase = NormalizarClase(info?.strProClas02);

                if (tipoProducto == "OTRO" ||
                    clase is not ("A" or "B" or "C"))
                {
                    continue;
                }

                DiarioMovimientoCuentaDto cuenta = resolver.Obtener(
                    "DCE",
                    "D",
                    tipoProducto,
                    clase);

                DateOnly fechaSalida =
                    DateOnly.FromDateTime(
                        primera.dtFechaSalida ?? DateTime.Now);

                resultado.Add(new CostoVentaSalidaHistoricoDto
                {
                    intAnio = fechaSalida.Year,
                    intMes = fechaSalida.Month,
                    dtFechaSalida = fechaSalida,

                    strFactura =
                        primera.strFactura ??
                        primera.strFacturaEmbarque ??
                        grupo.Key.strFacturaKey,

                    strFacturaKey = grupo.Key.strFacturaKey,

                    intCodProd = grupo.Key.intCodProd,
                    intCodTalla = grupo.Key.CodTalla,

                    strDescripcionProducto =
                        primera.strDescripcionProducto ?? string.Empty,
                    strTalla = primera.strTalla ?? string.Empty,

                    strTipoProducto = tipoProducto,
                    strClase = clase,

                    dcLibras = libras,
                    dcCostoUnitario = costoUnitario,
                    dcCostoTotal = costoTotal,

                    strCuentaCostoVenta = cuenta.strCuenta,
                    strFuente = "COSTVENTUNI"
                });
            }

            return resultado;
        }

        public List<DiarioMovimientoPersistenciaDto> APersistencia(
            IEnumerable<CostoVentaSalidaHistoricoDto> costos)
        {
            return (costos ?? Array.Empty<CostoVentaSalidaHistoricoDto>())
                .Where(x =>
                    x.intAnio > 0 &&
                    x.intMes > 0 &&
                    !string.IsNullOrWhiteSpace(x.strFacturaKey) &&
                    x.intCodProd > 0 &&
                    x.intCodTalla > 0 &&
                    x.dcCostoTotal != 0m)
                .Select(x =>
                    DiarioMovimientoPersistencia.CrearCostoVentaSalida(
                        x.intAnio,
                        x.intMes,
                        x.strCuentaCostoVenta,
                        x.strTipoProducto,
                        x.intCodProd,
                        x.intCodTalla,
                        x.strFacturaKey,
                        $"TIPO:{DiarioMovimientoPersistenciaDto.NormalizarTipo(x.strTipoProducto)};" +
                        $"CLASE:{NormalizarClase(x.strClase)};" +
                        $"TALLA:{x.strTalla}",
                        x.dcCostoTotal))
                .ToList();
        }

        public List<CostoVentaSalidaHistoricoDto> DesdePersistencia(
            IEnumerable<DiarioMovimientoPersistenciaDto> guardado,
            IEnumerable<FacturaCostoSalidaDto> facturas,
            IEnumerable<InfoProd> productos)
        {
            List<FacturaCostoSalidaDto> lstFacturas =
                (facturas ?? Array.Empty<FacturaCostoSalidaDto>()).ToList();

            Dictionary<int, InfoProd> dicProducto =
                (productos ?? Array.Empty<InfoProd>())
                .Where(x => int.TryParse(x.strProCodcor, out _))
                .GroupBy(x => Convert.ToInt32(x.strProCodcor))
                .ToDictionary(g => g.Key, g => g.First());

            var resultado = new List<CostoVentaSalidaHistoricoDto>();

            foreach (DiarioMovimientoPersistenciaDto row in
                guardado ?? Array.Empty<DiarioMovimientoPersistenciaDto>())
            {
                if (!string.Equals(
                    row.strProcesoCodigo,
                    "DVS",
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                string facturaKey =
                    (row.strAgrupacion ?? string.Empty)
                    .Replace("FACT:", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (!int.TryParse(row.strCentroCodigo, out int codProd) ||
                    !int.TryParse(row.strSubcentroCodigo, out int codTalla))
                    continue;

                List<FacturaCostoSalidaDto> detalleFactura =
                    lstFacturas
                    .Where(x =>
                        x.strFacturaKey == facturaKey &&
                        x.intCodProd == codProd &&
                        x.intCodTalla == codTalla)
                    .ToList();

                decimal libras = detalleFactura.Sum(x => Math.Abs(x.dcLibras));

                if (libras <= 0m)
                    continue;

                decimal costoTotal = Math.Abs(row.dcTotal);

                dicProducto.TryGetValue(codProd, out InfoProd? info);

                resultado.Add(new CostoVentaSalidaHistoricoDto
                {
                    intAnio = row.intAnio,
                    intMes = row.intMes,
                    dtFechaSalida =
                        detalleFactura.First().dtFechaSalida.HasValue
                            ? DateOnly.FromDateTime(detalleFactura.First().dtFechaSalida!.Value)
                            : row.dtFechaCorte,

                    strFactura =
                        detalleFactura.First().strFactura ??
                        detalleFactura.First().strFacturaEmbarque ??
                        facturaKey,

                    strFacturaKey = facturaKey,
                    intCodProd = codProd,
                    intCodTalla = codTalla,

                    strDescripcionProducto =
                        detalleFactura.First().strDescripcionProducto ?? string.Empty,
                    strTalla =
                        detalleFactura.First().strTalla ?? string.Empty,

                    strTipoProducto =
                        ResolverTipoProducto(
                            info?.strProClas01,
                            info?.strProClas05),

                    strClase = NormalizarClase(info?.strProClas02),

                    dcLibras = libras,
                    dcCostoUnitario = costoTotal / libras,
                    dcCostoTotal = costoTotal,

                    strCuentaCostoVenta = row.strCuenta,
                    strFuente = "DVS_GUARDADO"
                });
            }

            return resultado;
        }

        private static decimal Truncar(decimal valor, int decimales)
        {
            decimal factor = (decimal)Math.Pow(10, decimales);
            return Math.Truncate(valor * factor) / factor;
        }

        private static string ResolverTipoProducto(
            string? proClas01,
            string? proClas05) =>
            (proClas01?.Trim().ToUpperInvariant(),
             proClas05?.Trim().ToUpperInvariant()) switch
            {
                ("CC", "EN") => "ENTERO",
                ("SC", "SH") => "COLA",
                ("CC", "VA") => "VALOR AGREGADO",
                ("SC", "VA") => "VALOR AGREGADO",
                _ => "OTRO"
            };

        private static string NormalizarClase(string? clase) =>
            (clase ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "A+" => "A",
                "N" => "B",
                var x => x
            };
    }

    public static class DiarioCierrePresentacionBuilder
    {
        private static int OrdenNaturaleza(string? naturaleza)
        {
            return (naturaleza ?? string.Empty)
                .Trim()
                .ToUpperInvariant() switch
            {
                "D" => 0, // DEBE primero
                "H" => 1, // HABER después
                _ => 2
            };
        }

        private static int OrdenTipoProducto(string? rol)
        {
            string valor = (rol ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            // Ejemplos:
            // D_EN_A
            // H_EN_B
            // W_CO_A
            // W_CO_B

            if (valor.Contains("_EN_"))
                return 0;

            if (valor.Contains("_CO_"))
                return 1;

            if (valor.Contains("_VA_"))
                return 2;

            if (valor.Contains("TOTAL"))
                return 9;

            return 8;
        }

        private static int OrdenClase(string? rol)
        {
            string valor = (rol ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (valor.EndsWith("_A"))
                return 0;

            if (valor.EndsWith("_B"))
                return 1;

            if (valor.EndsWith("_C"))
                return 2;

            return 9;
        }

        private static readonly IReadOnlyDictionary<string, (string Codigo, string Titulo)> _map =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["DGF"] = ("- 1 -", "Transferencia Gastos Indirectos de Fabricación"),
                ["DWG"] = ("- 1 a -", "Transferencia Entero a Cola — Gastos Indirectos"),
                ["DWF"] = ("- 1 b -", "Transferencia Entero a Cola — GFD"),
                ["DWP"] = ("- 1 c -", "Transferencia Entero a Cola — Producto Terminado"),
                ["DCP"] = ("- 2 -", "Transferencia costo de producción a producción en proceso"),
                ["DPT"] = ("- 3 -", "Transferencia costo de producción a producto terminado"),
                ["DMP"] = ("- 4 -", "Transferencia materia prima a producción en proceso")
            };

        public static DiariosCierreDto Construir(
            int anio,
            int mes,
            IEnumerable<DiarioMovimientoPersistenciaDto> filas,
            IEnumerable<DiarioMovimientoCuentaDto> cuentas,
            IEnumerable<string>? advertenciasRetorno = null,
            bool guardado = false)
        {
            List<DiarioMovimientoPersistenciaDto> lstFilas =
                (filas ?? Array.Empty<DiarioMovimientoPersistenciaDto>())
                .Where(x => !string.Equals(
                    x.strProcesoCodigo,
                    "DVS",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<DiarioMovimientoCuentaDto> lstCuentas =
                (cuentas ?? Array.Empty<DiarioMovimientoCuentaDto>()).ToList();

            var dto = new DiariosCierreDto
            {
                intIdGeneracion = anio * 100 + mes,
                intAnio = anio,
                intMes = mes,
                strPeriodoTexto = PeriodoTexto(anio, mes),
                blGuardado = guardado,
                strEstado = guardado ? "GUARDADO" : "GENERADO"
            };

            dto.objRetornos.lstAdvertencias =
                (advertenciasRetorno ?? Array.Empty<string>()).ToList();

            ConstruirRetornos(dto, lstFilas, lstCuentas);
            ConstruirTransferencias(dto, lstFilas, lstCuentas, anio, mes);

            return dto;
        }

        private static void ConstruirRetornos(
            DiariosCierreDto dto,
            List<DiarioMovimientoPersistenciaDto> filas,
            List<DiarioMovimientoCuentaDto> cuentas)
        {
            int id = 1;

            var grupos = filas
                .Where(x => string.Equals(
                    x.strProcesoCodigo,
                    "DRC",
                    StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.strAgrupacion)
                .OrderBy(x => x.Key);

            foreach (var grupo in grupos)
            {
                int orden = 1;

                var diario = new DiarioRetornoCierreDto
                {
                    intId = id++,
                    strTitulo = "DIARIO DE DEVOLUCIÓN DE CONTENEDOR",
                    strSubtitulo = grupo.Key,
                    strNumeroDocumento =
                        grupo.Key.StartsWith("NC:", StringComparison.OrdinalIgnoreCase)
                            ? grupo.Key.Substring(3)
                            : grupo.Key,
                    strReferencia = grupo.First().strGrupo,
                    strGlosa = $"P/R COSTO DE VENTA, {grupo.Key}. {grupo.First().strGrupo}"
                };

                foreach (DiarioMovimientoPersistenciaDto row in grupo)
                {
                    DiarioMovimientoCuentaDto? cuenta = BuscarCuenta(cuentas, row);

                    diario.lstFilas.Add(new DiarioRetornoFilaDto
                    {
                        intId = orden,
                        intOrden = orden++,
                        // ANTERIOR: strCodigo = null, reemplazado por la cuenta.
                        strCodigo = row.strCuenta,
                        strCuentaContable = row.strCuenta,
                        strClave = cuenta?.strClave,
                        strDescripcion = cuenta?.strDescripcion ?? row.strCuenta,
                        strMoneda = null,
                        dcDebe = row.strNaturaleza == "D" ? Math.Abs(row.dcTotal) : 0m,
                        dcHaber = row.strNaturaleza == "H" ? Math.Abs(row.dcTotal) : 0m,
                        strDetalle = row.strGrupo
                    });
                }

                dto.objRetornos.lstDiarios.Add(diario);
            }

            dto.objRetornos.strMensaje =
                dto.objRetornos.lstDiarios.Count == 0
                    ? "No existen NC de retorno de contenedor para el período."
                    : $"{dto.objRetornos.lstDiarios.Count} diario(s) de retorno calculado(s).";
        }

        private static void ConstruirTransferencias(
            DiariosCierreDto dto,
            List<DiarioMovimientoPersistenciaDto> filas,
            List<DiarioMovimientoCuentaDto> cuentas,
            int anio,
            int mes)
        {
            int id = 1;

            foreach (var proc in _map)
            {
                List<DiarioMovimientoPersistenciaDto> detalle =
                    filas
                        .Where(x =>
                            string.Equals(
                                x.strProcesoCodigo,
                                proc.Key,
                                StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => OrdenNaturaleza(x.strNaturaleza))
                        .ThenBy(x => OrdenTipoProducto(x.strSubcentroCodigo))
                        .ThenBy(x => OrdenClase(x.strSubcentroCodigo))
                        .ThenBy(x => x.strCuenta)
                        .ToList();

                if (detalle.Count == 0)
                    continue;

                decimal totalDebe = detalle
                    .Where(x => x.strNaturaleza == "D")
                    .Sum(x => Math.Abs(x.dcTotal));

                var diario = new DiarioTransferenciaCierreDto
                {
                    intId = id++,
                    strCodigo = proc.Value.Codigo,
                    strTitulo = proc.Value.Titulo,
                    strGlosa = $"{proc.Value.Titulo} {PeriodoTexto(anio, mes)}"
                };

                int orden = 1;

                foreach (DiarioMovimientoPersistenciaDto row in detalle)
                {
                    DiarioMovimientoCuentaDto? cuenta = BuscarCuenta(cuentas, row);

                    decimal? calculo = null;
                    if (proc.Key == "DGF" &&
                        row.strNaturaleza == "D" &&
                        totalDebe != 0m)
                    {
                        calculo = Math.Abs(row.dcTotal) / totalDebe;
                    }

                    diario.lstFilas.Add(new DiarioTransferenciaFilaDto
                    {
                        intId = orden,
                        intOrden = orden++,
                        dcCalculo = calculo,
                        // ANTERIOR: strCodigo = null, reemplazado por la cuenta.
                        strCodigo = row.strCuenta,
                        strClave = cuenta?.strClave,
                        strNombreCuenta = cuenta?.strDescripcion ?? row.strCuenta,
                        dcDebe = row.strNaturaleza == "D" ? Math.Abs(row.dcTotal) : 0m,
                        dcHaber = row.strNaturaleza == "H" ? Math.Abs(row.dcTotal) : 0m
                    });
                }

                dto.objTransferenciaGif.lstDiarios.Add(diario);
            }

            dto.objTransferenciaGif.strPeriodoTexto = PeriodoTexto(anio, mes);

            dto.objTransferenciaGif.dcTotalGastoIndirecto =
                dto.objTransferenciaGif.lstDiarios
                .Where(x => x.strCodigo == "- 1 -")
                .SelectMany(x => x.lstFilas)
                .Sum(x => x.dcHaber);

            dto.objTransferenciaGif.dcBaseCalculo =
                dto.objTransferenciaGif.lstDiarios
                .Where(x => x.strCodigo == "- 2 -")
                .SelectMany(x => x.lstFilas)
                .Where(x => x.dcDebe > 0m)
                .Sum(x => x.dcDebe);
        }

        private static DiarioMovimientoCuentaDto? BuscarCuenta(
            IEnumerable<DiarioMovimientoCuentaDto> cuentas,
            DiarioMovimientoPersistenciaDto row)
        {
            return cuentas.FirstOrDefault(x =>
                string.Equals(
                    x.strProcesoCodigo,
                    row.strProcesoCodigo,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    x.strRol,
                    row.strSubcentroCodigo,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    x.strCuenta,
                    row.strCuenta,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static string PeriodoTexto(int anio, int mes)
        {
            var cultura = new CultureInfo("es-EC");
            string nombre = cultura.DateTimeFormat.GetMonthName(mes);
            return $"{char.ToUpper(nombre[0], cultura)}{nombre[1..]} {anio}";
        }
    }



}
