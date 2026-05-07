using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Dominio.Entidades
{
    public class MatPrimaReproceso
    {
        [NotMapped] 
        [JsonIgnore]
        public string strTipCod { get; set; }

        [NotMapped]
        [JsonIgnore]
        public int intCodCopacking { get; set; }

        [Column("tip_descri")]
        public string strTipDescri { get; set; }

        [Column("Clase Prod")]
        public string strClaseProd { get; set; }

        [Column("lot_tipo")]
        public string strLotTipo { get; set; }

        [Column("TipoCopacking")]
        public string strTipoCopacking { get; set; }

        [Column("lot_numero")]
        public int intLotNumero { get; set; }

        [Column("LoteUnificado")]
        public int intLoteUnificado { get; set; }

        [Column("PlantaProceso")]
        public string strPlantaProceso { get; set; }

        [Column("TipoProducto")]
        public string strTipoProducto { get; set; }

        [Column("Congelamiento Producto")]
        public string strCongeProduc { get; set; }

        [Column("LoteOrigen")]
        public int intLoteOrigen { get; set; }

        [Column("FechaLote")]
        public DateOnly? dtFechaLote { get; set; }

        [Column("Recibido")]
        public decimal dcRecibido { get; set; }

        [Column("lot_proces")]
        public decimal dcLotProces { get; set; }

        [Column("lot_fecha")]
        public DateTime dtLotFecha { get; set; }

        [Column("rld_prodcod")]
        public int intProdCod { get; set; }

        [Column("DescriProduc")]
        public string strDescriProduc { get; set; }

        [Column("tal_descri")]
        public string strTalDescri { get; set; }

        [Column("Trazabilidad")]
        public string strNivel { get; set; }

        [Column("Proc Prod")]
        public string strProClas03 { get; set; }

        [JsonIgnore]
        public double dbMasters { get; set; }

        [Column("Libras")]
        public double dbLibras { get; set; }

        [JsonIgnore]
        [Column("Libras Pelado")]
        public decimal? dcLibrasPelado { get; set; }

        [Column("Rendimiento %")]
        public double dbRendimiento { get; set; }

        [Column("Agrupacion")]
        public string strAgrupacion { get; set; }

        [Column("Certificado")]
        public string? strCertificado { get; set; }

        //[JsonIgnore]
        [Column("Libras Retractilado")]
        public decimal? dcLibrasRetractilado { get; set; }

        [Column("Costo X Secuencial")]
        public decimal dbCostoXSecuencial { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Costo Mat Empaque")]
        public decimal dcCostoMatEmpaque { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Excedente")]
        public decimal? dcExcedente { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Copacking")]
        public decimal? dcCostoCopacking { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Proceso")]
        public decimal? dcTarifaProc { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Proceso Primario")] // Este nombre será el título del Merge en Excel
        public ProcesoPrimarioDto ProcesoPrimario { get; set; } = new ProcesoPrimarioDto();

        //[NotMapped]
        //[JsonIgnore]
        [Column("Proceso Secundario")] // Este nombre será el título del Merge en Excel
        public ProcesoSecundarioDto ProcesoSecundario { get; set; } = new ProcesoSecundarioDto();

        //[NotMapped]
        //[JsonIgnore]
        [Column("Proceso Presentacion")] // Este nombre será el título del Merge en Excel
        public ProcesoPresentacionDto ProcesoPresentacion { get; set; } = new ProcesoPresentacionDto();

        //[NotMapped]
        //[JsonIgnore]
        [Column("Proceso Congelacion")] // Este nombre será el título del Merge en Excel
        public ProcesoCongelacionDto ProcesoCongelacion { get; set; } = new ProcesoCongelacionDto();

        //[NotMapped]
        //[JsonIgnore]
        [Column("Costos Directos")]
        public ProcesoCostDirectoDto ProcesoCostFijo { get; set; } = new ProcesoCostDirectoDto();

        //[NotMapped]
        //[JsonIgnore]
        [Column("Costos Indirectos")]
        public ProcesoCostIndirectoDto ProcesoCostIndirecto { get; set; } = new ProcesoCostIndirectoDto();

        [Column("Premio")]
        public decimal? dcCertificado { get; set; }


        [Column("Total Materia Prima")]
        public decimal dbCostoTotal { get; set; }

        //[NotMapped]
        //[JsonIgnore]
        [Column("Total Mat Empaque")]
        public decimal? dcCostoTotalMatEmp { get; set; }

        [Column("Total Proceso")]
        public decimal? dcCostTotalProc { get; set; }

        [Column("Total Costos")]
        public decimal dcTotalDolSum { get; set; }
        //[NotMapped]
        //[JsonIgnore]
        [Column("Costo X Libra")]
        public decimal? dcCostoTotXLibra { get; set; }

        [Column("Validador")]
        public decimal dcValidador { get; set; }

        [NotMapped]
        [JsonIgnore]
        public int intCodTal { get; set; }

        [JsonIgnore]
        public decimal? dcRecPorHid { get; set; }

        [JsonIgnore]
        public decimal? dcRecPorSal { get; set; }

        [JsonIgnore]
        public decimal? dcCthHidlbs { get; set; }

        [JsonIgnore]
        public decimal? dcCthSallbs { get; set; }

        [NotMapped]
        [JsonIgnore]
        public string strRecTipo { get; set; }

        [NotMapped]
        [JsonIgnore]
        public string strRecNombre { get; set; }

        [NotMapped]
        [JsonIgnore]
        public bool blPelado { get; set; }

        [NotMapped]
        [JsonIgnore]
        public bool blDecorado { get; set; }

        [NotMapped]
        [JsonIgnore]
        public bool blRetractilado { get; set; }


        [JsonIgnore]
        public decimal? dcDecorado { get; set; }

        [JsonIgnore]
        public decimal? dcRetractilado { get; set; }

        [JsonIgnore]
        public decimal? dcRecepcion { get; set; }

        [JsonIgnore]
        public decimal? dcClasificacion { get; set; }

        [JsonIgnore]
        public decimal? dcCajas { get; set; }

        [JsonIgnore]
        public decimal? dcDescabezado { get; set; }

        [JsonIgnore]
        public decimal? dcBrine { get; set; }

        [JsonIgnore]
        public decimal? dcIQF { get; set; }

        [JsonIgnore]
        public decimal? dcTunel { get; set; }

        [JsonIgnore]
        public decimal? dcPelado { get; set; }

        [JsonIgnore]
        public decimal? dcHidratacion { get; set; }

        [JsonIgnore]
        public decimal? dcCocido { get; set; }

        [JsonIgnore]
        public decimal? dcCostFijVaria { get; set; }

        [JsonIgnore]
        public decimal? dcCostFijFijo { get; set; }

        [JsonIgnore]
        public decimal? dcCostIndirFijo { get; set; }

        [JsonIgnore]
        public decimal? dcCostIndirVaria { get; set; }

        [JsonIgnore]
        public decimal? dcValorCert { get; set; }

        [JsonIgnore]
        public decimal? dcValorSal { get; set; }

        [JsonIgnore]
        public decimal? dcValorHidra { get; set; }

        [JsonIgnore]
        public decimal? dcValorTipro { get; set; }

        [JsonIgnore]
        public int? intRtCodItem { get; set; }

        [JsonIgnore]
        public string strBodCod { get; set; }

        [JsonIgnore]
        public int intMedCodigo { get; set; }

        [JsonIgnore]
        public string strEmbCodigo { get; set; }

        [JsonIgnore]
        public int intProCongela { get; set; }

        [JsonIgnore]
        public bool blBodEsBrine { get; set; }

        [JsonIgnore]
        public bool blEsDescabezado { get; set; }

        [JsonIgnore]

        public int? intRtaCodigo { get; set; }

        [JsonIgnore]

        public int? intTidCodigo { get; set; }

        [NotMapped]
        [JsonIgnore]
        public bool blExcluidoCosteo { get; set; }

        public MatPrimaReproceso() { }


        //lstProc.Certificado,
        //                lstProc.LidPremio,
        //                lstProc.CthSallbs,
        //                lstProc.CthHidlbs,
        //                lstProc.RtCodItem,
        public MatPrimaReproceso
            (
                string tipCod, string tipDescri,string claseProd, string lotTipo, string CodCopacking, string tipoCopacking, decimal lotNumero,
                decimal? loteUnificado, string plantaProceso, string tipoProducto, string congeProduc,
                decimal loteOrigen, DateTime? fechaLote, decimal recibido, decimal? lotProces,
                DateTime? lotFecha, string prodCod, string descriProduc, string talDescri,
                double libras, string agrupacion, int codTal, string recTipo, string recNombre, bool pelado,
                bool decorado, bool retractilado, string ProClas03, string Certificado, decimal? Premio,
                decimal? RecPorHid, decimal? RecPorcSal, decimal? RtCodItem, decimal? lbsRetractilado,double? PesoPelado, string BodCod,
                string EmbCodigo, decimal MedCodigo, double CantCaja, bool? bodEsBrine, decimal? RtaCodigo, decimal? TidCodigo, bool esDescabezado, decimal? proCongela,
                decimal? PorSal, decimal? PorHid
            )
        {
            try
            {
                HashSet<string> lstTiplot =
               new(StringComparer.OrdinalIgnoreCase) { "R7", "R6", "UNI" };
                strTipCod = tipCod.Trim();
                strTipDescri = tipDescri.Trim();
                strClaseProd = claseProd;
                strLotTipo = lotTipo;
                intCodCopacking = Convert.ToInt32(CodCopacking);
                strTipoCopacking = tipoCopacking;
                intLotNumero = (int)lotNumero;
                intLoteUnificado = (int)loteUnificado;
                strPlantaProceso = plantaProceso;
                strTipoProducto = tipoProducto;
                strCongeProduc = congeProduc;
                intLoteOrigen = (int)loteOrigen;
                dtFechaLote = fechaLote.HasValue
                                ? DateOnly.FromDateTime(fechaLote.Value)
                                : null;
                dcRecibido = recibido;
                dcLotProces = (decimal)lotProces;
                dtLotFecha = (DateTime)lotFecha;
                intProdCod = Convert.ToInt32(prodCod);
                strDescriProduc = descriProduc;
                strTalDescri = talDescri;
                dbLibras = libras;
                strAgrupacion = agrupacion;
                intCodTal = codTal;
                strRecTipo = recTipo;
                strRecNombre = recNombre;
                blPelado = pelado;
                blDecorado = decorado;
                blRetractilado = retractilado;
                strProClas03 = ProClas03;
                strCertificado = Certificado;
                InitializerCostPremio(Premio);
                //decimal dcValCertPrecio = Premio ?? 0.0m;
                //dcCertificado = Math.Round((decimal)(dcValCertPrecio * (decimal)libras), 2);
                //dcValorCert = dcValCertPrecio;
                dcRecPorHid = PorHid ?? 0m;
                dcRecPorSal = PorSal ?? 0m;
                dcCthSallbs = RecPorcSal;
                dcCthHidlbs = RecPorHid;
                intRtCodItem = RtCodItem.HasValue ? (int)RtCodItem.Value : null;
                dcLibrasPelado = PesoPelado != null && pelado  ? (decimal)/*dbLibras*/PesoPelado : 0m;
                dcLibrasRetractilado = lbsRetractilado != null && retractilado ? (decimal)lbsRetractilado/*lbsRetractilado.Value */: 0m;
                strBodCod = BodCod;
                strEmbCodigo = EmbCodigo;
                intMedCodigo = (int)MedCodigo;
                dbMasters = CantCaja;
                blBodEsBrine = bodEsBrine ?? false;
                intRtaCodigo = RtaCodigo.HasValue ? (int)RtaCodigo.Value : null;
                intTidCodigo = TidCodigo.HasValue ? (int)TidCodigo.Value : null;
                blEsDescabezado = esDescabezado && lstTiplot.Contains(strTipCod) ? true : false;
                intProCongela = proCongela.HasValue ? (int)proCongela.Value : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al inicializar MatPrimaReprocesoDto", ex);
            }
        }

        public MatPrimaReproceso
            (
                string tipCod, string tipDescri, string claseProd, string lotTipo, string CodCopacking, string tipoCopacking, decimal lotNumero,
                decimal? loteUnificado, string plantaProceso, string tipoProducto, string congeProduc,
                decimal loteOrigen, DateTime? fechaLote, decimal recibido, decimal? lotProces,
                DateTime? lotFecha, string prodCod, string descriProduc, string talDescri,
                double libras, string agrupacion, int codTal,
                HashSet<LoteRpcKeyReci> hashLotePiso  // ← único parámetro nuevo
            )
        {
            strTipCod = tipCod.Trim();
            strTipDescri = tipDescri.Trim();
            strClaseProd = claseProd;
            strLotTipo = lotTipo;
            intCodCopacking = Convert.ToInt32(CodCopacking);
            strTipoCopacking = tipoCopacking;
            intLotNumero = (int)lotNumero;
            intLoteUnificado = (int)loteUnificado;
            strPlantaProceso = plantaProceso;
            strTipoProducto = tipoProducto;
            strCongeProduc = congeProduc;
            intLoteOrigen = (int)loteOrigen;
            dtFechaLote = fechaLote.HasValue
                            ? DateOnly.FromDateTime(fechaLote.Value)
                            : null;
            dcRecibido = recibido;
            dcLotProces = (decimal)lotProces;
            dtLotFecha = (DateTime)lotFecha;
            intProdCod = Convert.ToInt32(prodCod);
            strDescriProduc = descriProduc;
            strTalDescri = talDescri;
            dbLibras = libras;
            strAgrupacion = agrupacion;
            intCodTal = codTal;
            blExcluidoCosteo = hashLotePiso?.Contains(
                    new LoteRpcKeyReci((int)loteOrigen, Convert.ToInt32(prodCod), codTal)) ?? false;
        }

        

        /// <summary>
        /// Construye el grafo de dependencias entre lotes.
        /// Un lote DESTINO depende de un lote FUENTE cuando alguno de sus RECIBIDO
        /// tiene intLoteOrigen == intLoteUnificado del lote FUENTE.
        /// </summary>
        /// <returns>
        ///   dependencias : LoteDestino  → HashSet de LotesFuente que debe esperar
        ///   dependientes : LoteFuente   → HashSet de LotesDestino que lo necesitan
        /// </returns>
        public static (
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependencias,
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependientes
        ) DetectarCadenasDependencia(IEnumerable<MatPrimaReproceso> lstMatPrimaRpc)
        {
            var indiceUnificado = lstMatPrimaRpc
                .Select(x => new LoteRpcKeyXSec(x.intLotNumero, x.intLoteUnificado))
                .Distinct()
                .GroupBy(k => k.intLoteUnificado)
                .ToDictionary(g => g.Key, g => g.ToList());

            var dependencias = new Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>>();
            var dependientes = new Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>>();

            var recibidosCruzados = lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "1. RECIBIDO"
                         && indiceUnificado.ContainsKey(x.intLoteOrigen));

            foreach (var recibido in recibidosCruzados)
            {
                var loteDestino = new LoteRpcKeyXSec(recibido.intLotNumero, recibido.intLoteUnificado);

                foreach (var candidatoFuente in indiceUnificado[recibido.intLoteOrigen])
                {
                    //    (misma fila apuntando a su propio loteUnificado con su mismo lotNumero).
                    //    Si comparten intLoteUnificado pero tienen distinto intLoteSecuencial,
                    //    SÍ es una dependencia inter-lote válida.
                    bool esAutoReferencia =
                        candidatoFuente.intLoteSecuencial == loteDestino.intLoteSecuencial &&
                        candidatoFuente.intLoteUnificado == loteDestino.intLoteUnificado;

                    if (esAutoReferencia) continue;

                    var loteFuente = candidatoFuente;

                    if (!dependencias.TryGetValue(loteDestino, out var fuentesSet))
                    {
                        fuentesSet = new HashSet<LoteRpcKeyXSec>();
                        dependencias[loteDestino] = fuentesSet;
                    }
                    fuentesSet.Add(loteFuente);

                    if (!dependientes.TryGetValue(loteFuente, out var destinosSet))
                    {
                        destinosSet = new HashSet<LoteRpcKeyXSec>();
                        dependientes[loteFuente] = destinosSet;
                    }
                    destinosSet.Add(loteDestino);
                }
            }

            return (dependencias, dependientes);
        }


        /// <summary>
        /// Kahn's Algorithm (BFS topológico) sobre el grafo de dependencias de lotes.
        /// Devuelve los lotes en el orden correcto de procesamiento (fuentes primero).
        /// Si detecta un ciclo (raro, pero posible por errores de datos), lo reporta
        /// en lotesEnCiclo para tratamiento especial en NV4.
        /// </summary>
        // ─────────────────────────────────────────────────────────────────────────────
        // OrdenarLotesTopologicamente  — corregido a O(V+E)
        //
        //  Kahn BFS correcto: al sacar un nodo de la cola, recorremos SUS DEPENDIENTES
        //  (usando el diccionario 'dependientes'), no todos los nodos del grafo.
        //  Esto baja de O(V²) a O(V+E).
        //
        //  CAMBIO DE FIRMA: recibe también 'dependientes' para el BFS eficiente.
        //  Actualizar la llamada en DetectarCadenasDependencia / CostearTodosLotesEnOrden.
        // ─────────────────────────────────────────────────────────────────────────────

        public static (
            List<LoteRpcKeyXSec> ordenProcesamiento,
            List<LoteRpcKeyXSec> lotesEnCiclo
        ) OrdenarLotesTopologicamente(
            IEnumerable<LoteRpcKeyXSec> todosLosLotes,
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependencias,
            Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>> dependientes)
        {
            // Grado de entrada = número de fuentes de las que depende cada lote
            var gradoEntrada = todosLosLotes
                .ToDictionary(
                    lote => lote,
                    lote => dependencias.TryGetValue(lote, out var dep) ? dep.Count : 0);

            // Cola inicial: lotes sin dependencias (grado 0)
            var cola = new Queue<LoteRpcKeyXSec>(
                gradoEntrada.Where(kv => kv.Value == 0).Select(kv => kv.Key));

            var orden = new List<LoteRpcKeyXSec>(gradoEntrada.Count);

            while (cola.Count > 0)
            {
                var lote = cola.Dequeue();
                orden.Add(lote);

                // CORRECCIÓN O(V+E): recorrer solo los dependientes directos de 'lote'
                if (dependientes.TryGetValue(lote, out var destinosDeLote))
                {
                    foreach (var destino in destinosDeLote)
                    {
                        if (!gradoEntrada.ContainsKey(destino)) continue;
                        gradoEntrada[destino]--;
                        if (gradoEntrada[destino] == 0)
                            cola.Enqueue(destino);
                    }
                }
            }

            // Nodos con grado > 0 al finalizar → ciclo en el grafo
            var lotesEnCiclo = gradoEntrada
                .Where(kv => kv.Value > 0)
                .Select(kv => kv.Key)
                .ToList();

            return (orden, lotesEnCiclo);
        }

        public static Dictionary<LoteRpcKeyXProdTal, decimal> GenerarPromedioPonderadoGlobal(
    List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            return lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "2. PROCESADO"
                         && x.dbCostoXSecuencial > 0
                         && x.dbLibras > 0)
                .GroupBy(x => new LoteRpcKeyXProdTal(x.intProdCod, x.intCodTal))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal totDol = g.Sum(x => x.dbCostoTotal);
                        return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                    });
        }
        public static Dictionary<LoteRpcKeyXSec, decimal> GenerarDiccionarioReciXFil(List<MatPrimaReproceso> lstMatPrimaRpc, Func<MatPrimaReproceso, bool> filtroExtra = null)
        {
            try
            {
                if (lstMatPrimaRpc == null)
                    return new Dictionary<LoteRpcKeyXSec, decimal>();
                return lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "1. RECIBIDO" && 
                         (filtroExtra == null || filtroExtra(x)))
                .GroupBy(x => new { x.intLotNumero, x.intLoteUnificado, x.intLoteOrigen, x.intProdCod })
                .ToList()
                .GroupBy(g => new LoteRpcKeyXSec ( g.Key.intLotNumero, g.Key.intLoteUnificado ))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Sum(r => r.dbCostoTotal))
                );
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static Dictionary<LoteRpcKeyXSec, decimal> GenerarDiccionarioProceXFil(List<MatPrimaReproceso> lstMatPrimaRpc, Func<MatPrimaReproceso, bool> filtroExtra = null)
        {
            try
            {
                return lstMatPrimaRpc
                            .Where(x => x.strAgrupacion == "2. PROCESADO" &&
                                     (filtroExtra == null || filtroExtra(x)))
                            .GroupBy(g => new LoteRpcKeyXSec(g.intLotNumero, g.intLoteUnificado))
                            .ToDictionary(
                                g => g.Key,
                                g => g.Sum(x => (decimal)x.dbLibras)
                            );
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static Dictionary<LoteRpcKeyLoteXProd, Queue<List<MatPrimaReproceso>>> GenerarDiccionarioProcMatEmp(List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            try
            {
                return lstMatPrimaRpc
                                .Where(x => x.strProClas03 == "PT")
                                    .GroupBy(x => new LoteRpcKeyLoteXProd(x.intLotNumero, x.intProdCod.ToString().Trim()))
                                    .ToDictionary(
                                        g => g.Key,
                                        g => new Queue<List<MatPrimaReproceso>>(
                                            g.GroupBy(x => x.intCodTal)
                                             .Select(tg => tg.ToList())
                                        )
                                    );
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<string> ObtenerLstItemHidra(List<MatPrimaReproceso>  lstMatPrimaReproceso)
        {
            List<string> lstItemHidra;
            lstItemHidra = lstMatPrimaReproceso.Where(x =>
                                                        x.strAgrupacion == "2. PROCESADO" &&
                                                        !String.IsNullOrEmpty(x.strRecNombre)
                                                        )
                    .Select(x => x.intRtCodItem.ToString()!)
                    .Distinct()
                    .ToList();
            lstItemHidra.Add("18623");
            return lstItemHidra;
        }

        public static List<MatPrimaReproceso> GenerarLstFiltRec(List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            try
            {
                return lstMatPrimaRpc.Where(lbsRecProc =>
                    lbsRecProc.strAgrupacion == "1. RECIBIDO" &&
                    lbsRecProc.dbCostoXSecuencial == 0)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public void InitializerCostPremio(decimal? Premio)
        {
            decimal dcValCertPrecio = Premio ?? 0.0m;
            dcCertificado = Math.Round(dcValCertPrecio * (decimal)dbLibras, 2);
            dcValorCert = dcValCertPrecio;
        }


        public static List<long> ObtenerLstLoteXRepro(IEnumerable<MatPrimaReproceso> lstMatPrimaReproceso, MatPrimaReproceso item)
        {
            return lstMatPrimaReproceso
                        .Where(x => 
                            x.strAgrupacion == "1. RECIBIDO" &&
                            x.intLotNumero == item.intLotNumero && 
                            x.intLoteUnificado == item.intLoteUnificado &&
                            x.strTipCod == item.strTipCod
                            )
                        .Select(x => (long)x.intLoteOrigen)
                        .Distinct()
                        .ToList();
        }
    }

    public class PrecioFrsXMov
    {
        public int    intLidLote { get; set; }
        public string strTrcTipo { get; set; }

        public string strProClasePago { get; set; }

        public string strProClas01 { get; set; }

        public string strProClas05 { get; set; }

        public string strTalDescri { get; set; }

        public int    intProCodcor { get; set; }

        public string strProDesesp { get; set; }

        public short  srtTcdCodtal { get; set; }

        public double dbLidPrecio { get; set; }

        public double dbLibras { get; set; }
        public PrecioFrsXMov(
            long LidLote,
            string TrcTipo, string ProClasePago, string ProClas01,
            string ProClas05, string TalDescri, string ProCodCor,
            string ProDesesp, decimal TcdCodtal, double LidPrecio,
            double Libras
            )
        {
            intLidLote = (int)LidLote;
            strTrcTipo = TrcTipo;
            strProClasePago = ProClasePago;
            strProClas01 = ProClas01;
            strProClas05 = ProClas05;
            strTalDescri = TalDescri;
            intProCodcor = int.TryParse(ProCodCor, out var codCor) ? codCor : 0;
            strProDesesp = ProDesesp;
            srtTcdCodtal = (short)TcdCodtal;
            if (strProClas01 == "CC" && strProClas05 == "EN")
            {
                dbLidPrecio =
                        Math.Truncate(LidPrecio / 2.2046 * 10000) / 10000;
            }
            else
            {
                dbLidPrecio = LidPrecio;
            }
            dbLibras = (double)Libras;
        }

        /// <summary>
        /// Genera un diccionario con el costo promedio por Lote, Producto y Talla a partir de una lista de materia prima fresco.
        /// </summary>
        /// <param name="lstPrecioLiqOtrProc">Lista de objetos con los costos.</param>
        /// <returns>Diccionario donde la llave es (Lote, Producto, Talla) y el valor es el Precio Promedio calculado.</returns>
        public static Dictionary<(int Lote, int? Producto, int? Talla), double> GenerarDiccionarioCostoXTalla(IEnumerable<PrecioFrsXMov> lstPrecioLiqOtrProc)
        {
            if (lstPrecioLiqOtrProc == null)
                return new Dictionary<(int, int?, int?), double>();

            return lstPrecioLiqOtrProc
                .GroupBy(p => (Lote: p.intLidLote, Producto: (int?)p.intProCodcor, Talla: (int?)p.srtTcdCodtal))
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(x => x.dbLidPrecio)
                );
        }
    }
}
