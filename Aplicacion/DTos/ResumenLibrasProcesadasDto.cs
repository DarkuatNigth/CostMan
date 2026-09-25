using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Aplicacion.DTos
{
    public class ResumenLibrasProcesadasDto
    {
        [Column("Codigo")]
        [JsonProperty("Codigo")]
        public string strCodigo { get; set; } = string.Empty;

        [Column("Descripcion")]
        [JsonProperty("Descripcion")]
        public string strDescripcion { get; set; } = string.Empty;

        [Column("Libras_Entero")]
        [JsonProperty("Libras_Entero")]
        public decimal dcLibrasEntero { get; set; }

        [Column("Libras_Cola")]
        [JsonProperty("Libras_Cola")]
        public decimal dcLibrasCola { get; set; }

        [Column("Libras_Valor_Agregado")]
        [JsonProperty("Libras_Valor_Agregado")]
        public decimal dcLibrasValorAgregado { get; set; }

        [Column("Dolares_Entero")]
        [JsonProperty("Dolares_Entero")]
        public decimal dcDolaresEntero { get; set; }

        [Column("Dolares_Cola")]
        [JsonProperty("Dolares_Cola")]
        public decimal dcDolaresCola { get; set; }

        [Column("Dolares_Valor_Agregado")]
        [JsonProperty("Dolares_Valor_Agregado")]
        public decimal dcDolaresValorAgregado { get; set; }

        [Column("Dolares_Total")]
        [JsonProperty("Dolares_Total")]
        public decimal dcDolaresTotal =>
            dcDolaresEntero +
            dcDolaresCola +
            dcDolaresValorAgregado;

        [Column("Total")]
        [JsonProperty("Total")]
        public decimal dcTotal => dcLibrasEntero + dcLibrasCola + dcLibrasValorAgregado;

        /// <summary>
        /// Indica si la fila participa en el total del cuadro.
        /// "Camarón Liquidado Reproceso" es una fila informativa de conciliación:
        /// muestra lo que fue descontado de RPC/VAG por pertenecer a Fresco, por lo
        /// que NO debe volver a sumarse en el total general de materia prima.
        /// </summary>
        [Column("IncluirEnTotal")]
        [JsonProperty("IncluirEnTotal")]
        public bool blIncluirEnTotal { get; set; } = true;

        [Column("Orden")]
        [JsonProperty("Orden")]
        public int intOrden { get; set; }
    }

    public class DetalleEtapaProcesoDto
    {
        [Column("Etapa")]
        [JsonProperty("Etapa")]
        public string strEtapa { get; set; } = string.Empty;

        [Column("Origen")]
        [JsonProperty("Origen")]
        public string strOrigen { get; set; } = string.Empty;

        [Column("CodigoProceso")]
        [JsonProperty("CodigoProceso")]
        public string strProcesoCodigo { get; set; } = string.Empty;

        [Column("ProcesoProductivo")]
        [JsonProperty("ProcesoProductivo")]
        public string strProcesoProductivo { get; set; } = string.Empty;

        [Column("TipoProducto")]
        [JsonProperty("TipoProducto")]
        public string strTipoProducto { get; set; } = string.Empty;
        /// <summary>
        /// Solo se informa para Brine. Se obtiene de MatPrimaReproceso.strTipEmbala
        /// y permite separar visualmente FONDO / PANERA.
        /// </summary>
        [Column("TipoEmbalaje")]
        [JsonProperty("TipoEmbalaje")]
        public string strTipoEmbalaje { get; set; } = string.Empty;


        [Column("ParticionCod")]
        [JsonProperty("ParticionCod")]
        public string strParticionCod { get; set; } = string.Empty;

        [Column("Particion")]
        [JsonProperty("Particion")]
        public string strParticion { get; set; } = string.Empty;

        [Column("Libras")]
        [JsonProperty("Libras")]
        public decimal dcLibras { get; set; }

        [Column("Orden")]
        [JsonProperty("Orden")]
        public int intOrden { get; set; }
    }

    public class ParamProcVistaResultadoDto
    {
        public List<ResumenLibrasProcesadasDto> lstResumen { get; set; } = new();
        public List<DetalleEtapaProcesoDto> lstDetalleEtapa { get; set; } = new();
    }

    /// <summary>
    /// Arma la información de presentación de ParamProc usando las mismas fuentes que
    /// ya fueron cargadas por ObtenerParametroProceso. No ejecuta consultas adicionales.
    /// El detalle de etapas queda listo en la respuesta inicial para abrir los modales
    /// del front sin una segunda llamada HTTP.
    /// </summary>
    public static class ParamProcVistaBuilder
    {
        private static readonly HashSet<string> _procPrimRpc = new(StringComparer.OrdinalIgnoreCase)
        {
            "DE", "R6", "R7", "UNI"
        };

        private static readonly HashSet<string> _procCostosRpc = new(StringComparer.OrdinalIgnoreCase)
        {
            "EZP", "PYDTO", "PYD", "P3", "ENTER", "EZ", "P4", "VF",
            "PYD1", "BD", "EPP", "PYDS", "PYD4"
        };

        private static readonly HashSet<string> _noCongelacionRpc = new(StringComparer.OrdinalIgnoreCase)
        {
            "CAM", "RLL", "R1", "CDI", "R2", "REC", "LB04", "RPY", "R3",
            "RS", "DV", "RVVL", "BDP", "VE", "VR"
        };

        private static readonly Dictionary<string, int> _ordenEtapas = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Material Empaque"] = 10,
            ["Logistica"] = 15,
            ["Recepcion"] = 20,
            ["Clasificacion"] = 30,
            ["Cajas"] = 40,
            ["Tunel"] = 50,
            ["Brine"] = 60,
            ["IQF"] = 70,
            ["Descabezado"] = 80,
            ["Hidratacion"] = 90,
            ["Cocido"] = 100,
            ["Pelado"] = 110,
            ["Retractilado"] = 120,
            ["Decorado"] = 130,
            ["C.D.Variables"] = 200,
            ["C.D.Fijos"] = 210,
            ["C.I.Variables"] = 220,
            ["C.I.Fijos"] = 230,
            ["C.Copacking"] = 300
        };

        public static ParamProcVistaResultadoDto Construir(DataProcesoParam objData)
        {
            var resultado = new ParamProcVistaResultadoDto();
            var detalle = new Dictionary<string, DetalleEtapaProcesoDto>(StringComparer.OrdinalIgnoreCase);

            var lstFrs = objData.lstLiqFresco ?? new List<LiquidacionResultado>();
            var lstRpc = objData.lstLiqReproCompleto ?? new List<MatPrimaReproceso>();
            var lstRpcProcesado = lstRpc
                .Where(x => string.Equals(x.strAgrupacion, "2. PROCESADO", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var lstRpcRecibido = lstRpc
                .Where(x => string.Equals(x.strAgrupacion, "1. RECIBIDO", StringComparison.OrdinalIgnoreCase))
                .ToList();


            // Si la fuente no trae agrupación por alguna razón, mantener compatibilidad.
            if (!lstRpcProcesado.Any()) lstRpcProcesado = lstRpc;

            // IMPORTANTE:
            // El resumen usa la lista RPC completa para poder identificar qué RECIBIDO
            // proviene de LIQ_PFR y descontar esa participación tanto de RPC Entero/Cola
            // como de VAG. No modifica lstRpcProcesado ni el costeo real del reproceso.
            resultado.lstResumen = ConstruirResumen(lstFrs, lstRpcProcesado, lstRpc);

            // -------------------------- FRESCO --------------------------
            AgregarFrs(detalle, "Material Empaque", lstFrs, x => (decimal)x.dcLibras);
            AgregarFrs(detalle, "Recepcion", lstFrs, x => (decimal)x.dcLibras); 
            AgregarFrs(detalle, "Logistica", lstFrs, x => (decimal)x.dcLibras);
            AgregarFrs(detalle, "Clasificacion", lstFrs, x => (decimal)x.dcLibras);
            AgregarFrs(detalle, "Cajas", lstFrs, x => (decimal)x.dcLibras);

            var frsTunel = lstFrs.Where(x =>
                objData.lstCongTunel != null &&
                objData.lstCongTunel.Contains(x.intProCongela) &&
                string.Equals(x.strProClas03, "PT", StringComparison.OrdinalIgnoreCase));
            AgregarFrs(detalle, "Tunel", frsTunel, x => (decimal)x.dcLibras);

            var frsDescabezado = lstFrs.Where(x =>
                string.Equals(x.strProClas01, "SC", StringComparison.OrdinalIgnoreCase));
            AgregarFrs(detalle, "Descabezado", frsDescabezado, x => (decimal)x.dcLibras);

            var frsRetractilado = lstFrs.Where(x => (x.dcLibrasRetractilado ?? 0m) > 0m);
            AgregarFrs(detalle, "Retractilado", frsRetractilado, x => x.dcLibrasRetractilado ?? 0m);

            var frsCopacking = lstFrs.Where(x => x.intCodCopacking > 0 &&
                                                  !string.Equals(x.strPlanta, "SONGA", StringComparison.OrdinalIgnoreCase));
            AgregarFrs(detalle, "C.Copacking", frsCopacking, x => (decimal)x.dcLibras);

            foreach (var etapa in new[] { "C.D.Variables", "C.D.Fijos", "C.I.Variables", "C.I.Fijos" })
                AgregarFrs(detalle, etapa, lstFrs, x => (decimal)x.dcLibras);

            // ----------------------- REPROCESO / VAG -----------------------
            var rpcPrimario = lstRpcProcesado.Where(x =>
                string.Equals(x.strLotTipo, "RE", StringComparison.OrdinalIgnoreCase) &&
                _procPrimRpc.Contains(x.strTipCod ?? string.Empty)).ToList();

            AgregarRpc(detalle, "Recepcion", rpcPrimario, x => (decimal)x.dbLibras);
            AgregarRpc(detalle, "Clasificacion", rpcPrimario, x => (decimal)x.dbLibras);
            AgregarRpc(detalle, "Cajas", rpcPrimario, x => (decimal)x.dbLibras);
            AgregarRpc(detalle, "Material Empaque", lstRpcProcesado, x => (decimal)x.dbLibras);

            var rpcAplicanCostos = lstRpcProcesado.Where(x =>
                _procPrimRpc.Contains(x.strTipCod ?? string.Empty) ||
                _procCostosRpc.Contains(x.strTipCod ?? string.Empty)).ToList();

            foreach (var etapa in new[] { "C.D.Variables", "C.D.Fijos", "C.I.Variables", "C.I.Fijos" })
                AgregarRpc(detalle, etapa, rpcAplicanCostos, x => (decimal)x.dbLibras);

            AgregarRpc(detalle, "Cocido", lstRpcProcesado.Where(x =>
                !string.IsNullOrEmpty(x.strRecNombre) &&
                string.Equals(x.strRecTipo, "COC", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.strLotTipo, "VA", StringComparison.OrdinalIgnoreCase)),
                x => (decimal)x.dbLibras);

            AgregarRpc(detalle, "Hidratacion", lstRpcProcesado.Where(x =>
                !string.IsNullOrEmpty(x.strRecNombre) &&
                !string.Equals(x.strRecTipo, "COC", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.strLotTipo, "VA", StringComparison.OrdinalIgnoreCase)),
                x => (decimal)x.dbLibras);

            AgregarRpc(detalle, "Retractilado", lstRpcProcesado.Where(x => x.blRetractilado),
                x => x.dcLibrasRetractilado ?? 0m);
            AgregarRpc(detalle, "Pelado", lstRpcProcesado.Where(x => x.blPelado),
                x => x.dcLibrasPelado ?? 0m);
            AgregarRpc(detalle, "Decorado", lstRpcProcesado.Where(x => x.blDecorado),
                x => (decimal)x.dbLibras);
            AgregarRpc(detalle, "Descongelado", lstRpcRecibido.Where(x => x.blEsDescongelado == true),
                x => (decimal)x.dbLibras);
            AgregarRpc(detalle, "Descabezado", lstRpcProcesado.Where(x => x.blEsDescabezado),
                x => (decimal)x.dbLibras);

            AgregarRpc(detalle, "IQF", lstRpcProcesado.Where(x =>
                string.Equals((x.strCongeProduc ?? string.Empty).Trim(), "IQF", StringComparison.OrdinalIgnoreCase) &&
                !_noCongelacionRpc.Contains(x.strTipCod ?? string.Empty)),
                x => (decimal)x.dbLibras);

            AgregarRpc(detalle, "Brine", lstRpcProcesado.Where(x =>
                string.Equals((x.strCongeProduc ?? string.Empty).Trim(), "BRINE", StringComparison.OrdinalIgnoreCase) &&
                !_noCongelacionRpc.Contains(x.strTipCod ?? string.Empty)),
                x => (decimal)x.dbLibras);

            AgregarRpc(detalle, "Tunel", lstRpcProcesado.Where(x =>
                (string.Equals((x.strCongeProduc ?? string.Empty).Trim(), "BLOCK", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals((x.strCongeProduc ?? string.Empty).Trim(), "SEMI IQF", StringComparison.OrdinalIgnoreCase)) &&
                !_noCongelacionRpc.Contains(x.strTipCod ?? string.Empty)),
                x => (decimal)x.dbLibras);

            AgregarRpc(detalle, "C.Copacking", lstRpcProcesado.Where(x => x.intCodCopacking != 0),
                x => (decimal)x.dbLibras);

            // Los tarifarios también quedan consultables en el modal por su strTipDescri/strTipCod.
            if (objData.lstProcesoTarifa != null)
            {
                foreach (var tarifa in objData.lstProcesoTarifa.Where(x => !string.IsNullOrWhiteSpace(x.strCodTip)))
                {
                    var items = lstRpcProcesado.Where(x =>
                        string.Equals(x.strTipCod, tarifa.strCodTip, StringComparison.OrdinalIgnoreCase));
                    AgregarRpc(detalle, tarifa.strDescripcion, items, x => (decimal)x.dbLibras);
                }
            }

            resultado.lstDetalleEtapa = detalle.Values
                .Where(x => x.dcLibras != 0m)
                .OrderBy(x => x.intOrden)
                .ThenBy(x => x.strEtapa)
                .ThenBy(x => x.strOrigen)
                .ThenBy(x => x.strProcesoProductivo)
                .ThenBy(x => ParticionCosteo.Orden(x.strParticionCod))
                .ToList();

            return resultado;
        }

        private static List<ResumenLibrasProcesadasDto> ConstruirResumen(
            List<LiquidacionResultado> lstFrs,
            List<MatPrimaReproceso> lstRpcProcesado,
            List<MatPrimaReproceso> lstRpcCompleto)
        {
            var frsLibras =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var frsDolares =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (LiquidacionResultado item in lstFrs)
            {
                string? particion =
                    ParticionCosteo.ClasificarFrs(
                        item.strProClas01,
                        item.strProClas05);

                AcumularParticion(
                    frsLibras,
                    particion,
                    Convert.ToDecimal(item.dcLibras));

                // Costo TOTAL de materia prima valorizada de Fresco.
                AcumularParticion(
                    frsDolares,
                    particion,
                    Convert.ToDecimal(item.dcTotalDol));
            }

            var rpcLibras =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var rpcDolares =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // Conciliación: aquí guardamos exactamente las libras y dólares que se
            // descuentan de RPC/VAG porque su origen ya fue reconocido en LIQ_PFR.
            // Esta información se devuelve en una fila adicional llamada
            // "Camarón Liquidado Reproceso".
            var rpcRestadoLibras =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var rpcRestadoDolares =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // Identifica qué porcentaje de cada lote RPC proviene de LIQ_PFR.
            // El motor también propaga el origen Fresco a través de cadenas RPC -> RPC,
            // de forma que Fresco directo a VAG o Fresco -> RPC -> VAG no se duplique.
            Dictionary<LoteRpcKeyXSec, ParticipacionOrigenFrescoRpc> dictOrigenFresco =
                MotorOrigenMateriaPrimaRpc.Calcular(
                    lstFrs,
                    lstRpcCompleto);

            // IMPORTANTE:
            // lstRpcProcesado es exclusivamente 2. PROCESADO.
            // Para el resumen se conserva solo la proporción que NO pertenece a Fresco.
            // El objeto MatPrimaReproceso original no se modifica.
            foreach (MatPrimaReproceso item in lstRpcProcesado)
            {
                string? particion =
                    ParticionCosteo.ClasificarRpc(
                        item.strTipoProducto);

                if (string.IsNullOrWhiteSpace(particion))
                    continue;

                decimal pctFrescoLbs = 0m;
                decimal pctFrescoDol = 0m;

                if (dictOrigenFresco.TryGetValue(
                    item.objLoteKey,
                    out ParticipacionOrigenFrescoRpc? participacion))
                {
                    pctFrescoLbs = participacion.dcPorcentajeFrescoLibras;
                    pctFrescoDol = participacion.dcPorcentajeFrescoDolares;
                }

                decimal librasOriginales =
                    Math.Max(
                        0m,
                        Convert.ToDecimal(item.dbLibras));

                decimal dolaresOriginales =
                    Math.Max(
                        0m,
                        Convert.ToDecimal(item.dbCostoTotal));

                // Parte que ya pertenece a Fresco y, por tanto, se resta de RPC/VAG.
                decimal librasRestadas =
                    Math.Max(
                        0m,
                        librasOriginales * pctFrescoLbs);

                decimal dolaresRestados =
                    Math.Max(
                        0m,
                        dolaresOriginales * pctFrescoDol);

                // Parte neta que sí debe permanecer en Reproceso / VAG.
                decimal librasRpcNetas =
                    Math.Max(
                        0m,
                        librasOriginales - librasRestadas);

                decimal dolaresRpcNetos =
                    Math.Max(
                        0m,
                        dolaresOriginales - dolaresRestados);

                AcumularParticion(
                    rpcLibras,
                    particion,
                    Math.Round(librasRpcNetas, 5));

                AcumularParticion(
                    rpcDolares,
                    particion,
                    Math.Round(dolaresRpcNetos, 5));

                // Fila informativa de conciliación. Conserva la partición FINAL del
                // procesado (EN/SH/EV/SV), por lo que Fresco que terminó directamente
                // o a través de una cadena RPC en VAG aparecerá en Valor Agregado.
                AcumularParticion(
                    rpcRestadoLibras,
                    particion,
                    Math.Round(librasRestadas, 5));

                AcumularParticion(
                    rpcRestadoDolares,
                    particion,
                    Math.Round(dolaresRestados, 5));
            }

            return new List<ResumenLibrasProcesadasDto>
            {
                CrearResumen(
                    "PFR",
                    "Camarón Libras liquidadas",
                    frsLibras,
                    frsDolares,
                    1,
                    incluirBase: true,
                    incluirVa: true,
                    incluirEnTotal: true),

                // Muestra, en positivo, todo lo descontado de RPC/VAG por tener
                // origen Fresco. Es informativa y NO participa en la fila Total,
                // porque esos valores ya están contenidos en Camarón Libras liquidadas.
                CrearResumen(
                    "RLIQ",
                    "Camarón Liquidado Reproceso",
                    rpcRestadoLibras,
                    rpcRestadoDolares,
                    2,
                    incluirBase: true,
                    incluirVa: true,
                    incluirEnTotal: false),

                CrearResumen(
                    "RPC",
                    "Camarón Reproceso",
                    rpcLibras,
                    rpcDolares,
                    3,
                    incluirBase: true,
                    incluirVa: false,
                    incluirEnTotal: true),

                CrearResumen(
                    "VAG",
                    "Camarón VAG",
                    rpcLibras,
                    rpcDolares,
                    4,
                    incluirBase: false,
                    incluirVa: true,
                    incluirEnTotal: true)
            };
        }

        private static ResumenLibrasProcesadasDto CrearResumen(
            string codigo,
            string descripcion,
            Dictionary<string, decimal> dicLibras,
            Dictionary<string, decimal> dicDolares,
            int orden,
            bool incluirBase,
            bool incluirVa,
            bool incluirEnTotal = true)
        {
            decimal librasEntero = incluirBase
                ? dicLibras.GetValueOrDefault(ParticionCosteo.ENTERO, 0m)
                : 0m;

            decimal librasCola = incluirBase
                ? dicLibras.GetValueOrDefault(ParticionCosteo.COLA, 0m)
                : 0m;

            decimal librasVa = incluirVa
                ? dicLibras.GetValueOrDefault(ParticionCosteo.ENTERO_VA, 0m)
                  + dicLibras.GetValueOrDefault(ParticionCosteo.COLA_VA, 0m)
                : 0m;

            decimal dolaresEntero = incluirBase
                ? dicDolares.GetValueOrDefault(ParticionCosteo.ENTERO, 0m)
                : 0m;

            decimal dolaresCola = incluirBase
                ? dicDolares.GetValueOrDefault(ParticionCosteo.COLA, 0m)
                : 0m;

            decimal dolaresVa = incluirVa
                ? dicDolares.GetValueOrDefault(ParticionCosteo.ENTERO_VA, 0m)
                  + dicDolares.GetValueOrDefault(ParticionCosteo.COLA_VA, 0m)
                : 0m;

            return new ResumenLibrasProcesadasDto
            {
                strCodigo = codigo,
                strDescripcion = descripcion,

                dcLibrasEntero = librasEntero,
                dcLibrasCola = librasCola,
                dcLibrasValorAgregado = librasVa,

                dcDolaresEntero = dolaresEntero,
                dcDolaresCola = dolaresCola,
                dcDolaresValorAgregado = dolaresVa,

                blIncluirEnTotal = incluirEnTotal,
                intOrden = orden
            };
        }

        private static void AcumularParticion(Dictionary<string, decimal> dic, string? particion, decimal libras)
        {
            if (string.IsNullOrWhiteSpace(particion)) return;
            dic[particion] = dic.TryGetValue(particion, out var actual) ? actual + libras : libras;
        }

        private static void AgregarFrs(
            Dictionary<string, DetalleEtapaProcesoDto> dic,
            string etapa,
            IEnumerable<LiquidacionResultado> items,
            Func<LiquidacionResultado, decimal> fnLibras)
        {
            foreach (var item in items)
            {
                string? particion = ParticionCosteo.ClasificarFrs(item.strProClas01, item.strProClas05);
                if (string.IsNullOrWhiteSpace(particion)) continue;

                string codigo = string.IsNullOrWhiteSpace(item.strTipoLiq) ? "LIQ_PFR" : item.strTipoLiq!.Trim();
                Agregar(dic, etapa, "PFR", codigo, codigo,
                    ParticionCosteo.Descripcion(particion),
                    tipoEmbalaje: string.Empty, particion, fnLibras(item));
            }
        }

        private static void AgregarRpc(
            Dictionary<string, DetalleEtapaProcesoDto> dic,
            string etapa,
            IEnumerable<MatPrimaReproceso> items,
            Func<MatPrimaReproceso, decimal> fnLibras)
        {
            foreach (var item in items)
            {
                string? particion = ParticionCosteo.ClasificarRpc(item.strTipoProducto);
                if (string.IsNullOrWhiteSpace(particion)) continue;

                // Solo Brine conserva este criterio de subdivisión.
                string tipoEmbalaje = ObtenerTipoEmbalajeBrine(etapa, item.strTipEmbala);
                string codigo = (item.strTipCod ?? string.Empty).Trim();
                string proceso = string.IsNullOrWhiteSpace(item.strTipDescri) ? codigo : item.strTipDescri.Trim();
                Agregar(dic, etapa, "RPC", codigo, proceso,
                    (item.strTipoProducto ?? string.Empty).Trim(), tipoEmbalaje, particion, fnLibras(item));
            }
        }

        private static string ObtenerTipoEmbalajeBrine(string etapa, string? valor)
        {
            if (!string.Equals(etapa, "Brine", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string tipo = (valor ?? string.Empty).Trim().ToUpperInvariant();

            if (tipo.Contains("FONDO")) return "FONDO";
            if (tipo.Contains("PANERA")) return "PANERA";

            // No altera las libras si aparece un valor inesperado; simplemente
            // queda sin clasificación visual en el grupo Brine.
            return string.Empty;
        }

        private static void Agregar(
            Dictionary<string, DetalleEtapaProcesoDto> dic,
            string etapa,
            string origen,
            string codigo,
            string proceso,
            string tipoProducto,
            string tipoEmbalaje,
            string particion,
            decimal libras)
        {
            if (libras == 0m) return;

            int orden = _ordenEtapas.TryGetValue(etapa, out var ordenEtapa) ? ordenEtapa : 500;

            // TipoEmbalaje solo tendrá valor en Brine. Incluirlo en la llave evita
            // que FONDO y PANERA se consoliden antes de llegar al front.
            string key = $"{etapa}|{origen}|{codigo}|{proceso}|{tipoProducto}|{tipoEmbalaje}|{particion}";

            if (!dic.TryGetValue(key, out var fila))
            {
                fila = new DetalleEtapaProcesoDto
                {
                    strEtapa = etapa,
                    strOrigen = origen,
                    strProcesoCodigo = codigo,
                    strProcesoProductivo = proceso,
                    strTipoProducto = tipoProducto,
                    strTipoEmbalaje = tipoEmbalaje,
                    strParticionCod = particion,
                    strParticion = ParticionCosteo.Descripcion(particion),
                    dcLibras = 0m,
                    intOrden = orden
                };
                dic[key] = fila;
            }

            fila.dcLibras += libras;
        }
    }
}
