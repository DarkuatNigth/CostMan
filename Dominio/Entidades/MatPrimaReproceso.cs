using CostManagement.Aplicación.DTos;
using CostManagement.Infraestructura.EF_Core;
using CostManagement.Dominio.Reglas;
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

        [Column("Tipo Pelado")]
        public string? strTipoPelado { get; set; }

        [Column("Maquina Pelado")]
        public string? strMaquinaPelado { get; set; }

        [Column("Bod Congela")]
        public string strBodCongela { get; set; }

        [Column("Tip Embala")]
        public string strTipEmbala { get; set; }

        [Column("lid_clasificadora")]
        public string? strLidClasificadora { get; set; }

        [Column("Clasificadora")]
        public string? strClasificadora { get; set; }

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
        public int intCodProd { get; set; }

        [Column("DescriProduc")]
        public string strDescriProduc { get; set; }

        [Column("tal_descri")]
        public string strTalDescri { get; set; }

        [Column("Trazabilidad")]
        public string strNivel { get; set; }

        [NotMapped]
        public string strOrigenCosto { get; set; } = "";

        [NotMapped]
        public bool blUsadoComoFuente { get; set; }

        [NotMapped][JsonIgnore]
        public decimal dcCostoPondHistorico { get; set; }
        [NotMapped][JsonIgnore]
        public int intNivelHistorico { get; set; }
        [NotMapped][JsonIgnore]
        public bool blCostoReferencial { get; set; }
        [NotMapped][JsonIgnore]
        public double dbLibrasConsumidas { get; set; }
        [NotMapped][JsonIgnore]
        public double dbLibrasDisponibles { get; set; }
        [NotMapped][JsonIgnore]
        public bool blBloqueadoHistorico { get; set; }

        [NotMapped][JsonIgnore]
        public double dbLibrasTomadas { get; set; }
        private bool _blLoteUsable = true;
        [NotMapped][JsonIgnore]
        public bool blLoteUsable { get => _blLoteUsable && !blExcluidoCosteo && !blBloqueadoHistorico; set => _blLoteUsable = value; }

        // Peso del histórico vigente: no es el peso del recibido que lo consulta.
        private decimal _dcLibrasHistorico;
        private bool _blHistoricoAbsorbido;
        private EstadoHistorico _objEstadoHistorico;
        private const double ToleranciaLibras = 0.0001;

        [Column("Proc Prod")]
        public string strProClas03 { get; set; }

        [JsonIgnore]
        public double dbMasters { get; set; }

        [Column("Libras")]
        public double dbLibras { get; set; }

        [Column("Libras Pelado")]
        public decimal? dcLibrasPelado { get; set; }

        [Column("EsDescongelado")]
        public bool? blEsDescongelado { get; set; }

        [Column("Libras Retractilado")]
        public decimal? dcLibrasRetractilado { get; set; }

        [Column("Rendimiento %")]
        public double dbRendimiento { get; set; }

        [Column("Agrupacion")]
        public string strAgrupacion { get; set; }

        [Column("Certificado")]
        public string? strCertificado { get; set; }

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

        // montos calculados de procesos sin columna dedicada.
        [NotMapped][JsonIgnore]
        public Dictionary<string, decimal> ProcesosAdicionales { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        // [NotMapped][Column("Otros Procesos")]
        [NotMapped][JsonIgnore]
        public decimal? dcOtrosProcesos => ProcesosAdicionales.Count == 0 ? null : ProcesosAdicionales.Values.Sum();

        // vista del catálogo preparada por la feature, solo para reportes MP.
        [NotMapped][JsonIgnore]
        public Dictionary<string, Dictionary<string, decimal>>? dicProcesosReporte { get; set; }

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
        public decimal? dcDescongelado { get; set; }

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

        [NotMapped]
        [JsonIgnore]
        public LoteRpcValKey objLotRpc { get; set; }

        [NotMapped]
        [JsonIgnore]
        public LoteRpcKeyXSec objLoteKey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public LoteRpcValKey objValKey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public LoteRpcKeyXProdTal objProdTalKey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public LoteRpcKeyLoteXProd objLoteProdReciKey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public LoteRpcKeyReci objLoteProdTalReciKey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public LoteRpcKeyReci objRpcValOrKey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public LoteFrsKey objFrskey { get; set; }

        [NotMapped]
        [JsonIgnore]
        public InfoProd objInfoProd { get; set; }

        #region Constructor
        public MatPrimaReproceso() { }

        public MatPrimaReproceso
            (
                string tipCod, string tipDescri,string claseProd, string lotTipo, string CodCopacking, string tipoCopacking, decimal lotNumero,
                decimal? loteUnificado, string plantaProceso, string tipoProducto, string congeProduc,
                decimal loteOrigen, DateTime? fechaLote, decimal recibido, decimal? lotProces,
                DateTime? lotFecha, string prodCod, string descriProduc, string talDescri,
                double libras, string agrupacion, int codTal, string recTipo, string recNombre, bool pelado,
                bool decorado, bool retractilado, string ProClas03, string Certificado, decimal? Premio,
                decimal? RecPorHid, decimal? RecPorcSal, decimal? RtCodItem, decimal? lbsRetractilado,double? PesoPelado, string TipoPelado, string MaquinaPelado,
                string BodCod, string BodDescri,
                string EmbCodigo, decimal MedCodigo, double CantCaja, bool? bodEsBrine, decimal? RtaCodigo, decimal? TidCodigo, bool esDescabezado, decimal? proCongela,
                decimal? PorSal, decimal? PorHid, string LidClasificadora, string Clasificadora, string TipEmbala
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
                intCodCopacking = !String.IsNullOrWhiteSpace(CodCopacking) ? Convert.ToInt32(CodCopacking) : 0;
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
                intCodProd = Convert.ToInt32(prodCod);
                strDescriProduc = descriProduc;
                strTalDescri = talDescri;
                dbLibras = libras;
                strAgrupacion = agrupacion;
                intCodTal = codTal;
                strRecTipo = recTipo;
                strRecNombre = recNombre;
                blPelado = pelado;
                blDecorado = decorado;
                blRetractilado = (int)RtaCodigo.Value > 2 && strLotTipo == "VA" ? true : false ;
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
                dcLibrasRetractilado = 0m;
                strBodCod = BodCod;
                strBodCongela = BodDescri;
                strEmbCodigo = EmbCodigo;
                intMedCodigo = (int)MedCodigo;
                dbMasters = CantCaja;
                blBodEsBrine = bodEsBrine ?? false;
                intRtaCodigo = (int)RtaCodigo.Value;
                intTidCodigo = (int)TidCodigo.Value;
                blEsDescabezado = esDescabezado && lstTiplot.Contains(strTipCod) ? true : false;
                intProCongela = proCongela.HasValue ? (int)proCongela.Value : 0;
                strClasificadora = Clasificadora;
                strLidClasificadora = LidClasificadora;
                strTipoPelado = TipoPelado;
                strMaquinaPelado = MaquinaPelado;
                strTipEmbala = TipEmbala;
                InitializerKeys();
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
                HashSet<LoteRpcKeyReci> hashLotePiso, string proClas03, string proClas05, string proClas01
            )
        {
            try
            {
                List<string> lottiposValidos = new List<string> { "R2", "R5", "R6" };
                strTipCod = tipCod.Trim();
                strTipDescri = tipDescri.Trim();
                strClaseProd = claseProd;
                strLotTipo = lotTipo;
                intCodCopacking =  !String.IsNullOrWhiteSpace( CodCopacking) ?   Convert.ToInt32(CodCopacking): 0;
                strTipoCopacking = tipoCopacking;
                strProClas03 = proClas03;
                strTipoProducto = proClas01 == "CC" && proClas05 == "EN" ? "ENTERO" :
                        proClas01 == "SC" && proClas05 == "SH" ? "COLA" :
                        proClas01 == "CC" && proClas05 == "VA" ? "ENTERO VALOR AGREGADO" :
                        proClas01 == "SC" && proClas05 == "VA" ? "COLA VALOR AGREGADO" :
                        "OTRO";
                intLotNumero = (int)lotNumero;
                intLoteUnificado = (int)loteUnificado;
                strPlantaProceso = plantaProceso;
                strCongeProduc = congeProduc;
                intLoteOrigen = (int)loteOrigen;
                dtFechaLote = fechaLote.HasValue
                                ? DateOnly.FromDateTime(fechaLote.Value)
                                : null;
                dcRecibido = recibido;
                dcLotProces = (decimal)lotProces;
                dtLotFecha = (DateTime)lotFecha;
                intCodProd = Convert.ToInt32(prodCod);
                strDescriProduc = descriProduc;
                strTalDescri = talDescri;
                dbLibras = libras;
                strAgrupacion = agrupacion;
                intCodTal = codTal;
                blExcluidoCosteo = hashLotePiso?.Contains(
                        new LoteRpcKeyReci((int)loteOrigen, Convert.ToInt32(prodCod), codTal)) ?? false;
                blEsDescongelado = (
                    (lottiposValidos.Contains(strTipCod) && strLotTipo == "RE") ||
                    strLotTipo == "VA") && strProClas03 == "PT";
                InitializerKeys();
            }
            catch (Exception obj)
            {
                Console.WriteLine("", obj);
            }
        }
        #endregion

        #region Bloque Grafo
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
            var lstMat = lstMatPrimaRpc as IReadOnlyList<MatPrimaReproceso> ?? lstMatPrimaRpc.ToList();

            var indiceUnificado = lstMat
                .Select(x => x.objLoteKey)
                .Distinct()
                .GroupBy(k => k.intLoteUnificado)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Secuenciales que tienen al menos un PROCESADO con (prod, talla) específicos.
            // Sirve para descartar candidatos que no pueden ser la fuente real de un RECIBIDO.
            var hshProcesado = lstMat
                .Where(x => x.strAgrupacion == "2. PROCESADO")
                .Select(x => (x.objLoteKey, x.intCodProd, x.intCodTal))
                .ToHashSet();

            var dependencias = new Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>>();
            var dependientes = new Dictionary<LoteRpcKeyXSec, HashSet<LoteRpcKeyXSec>>();

            var recibidosCruzados = lstMat
                .Where(x => x.strAgrupacion == "1. RECIBIDO"
                         && indiceUnificado.ContainsKey(x.intLoteOrigen));

            foreach (var recibido in recibidosCruzados)
            {
                var loteDestino = recibido.objLoteKey;

                foreach (var candidatoFuente in indiceUnificado[recibido.intLoteOrigen])
                {
                    bool esAutoReferencia =
                        candidatoFuente.intLoteSecuencial == loteDestino.intLoteSecuencial &&
                        candidatoFuente.intLoteUnificado == loteDestino.intLoteUnificado;

                    if (esAutoReferencia) continue;

                    // Solo es dependencia real si ese secuencial tiene PROCESADO con
                    // la misma prod/talla que el RECIBIDO. Si hubo transformación de
                    // producto (entra P1/T1, sale P2/T2), la dependencia no se puede
                    // resolver vía grafo y el lote irá a PropagacionPostNV4.
                    if (!hshProcesado.Contains((candidatoFuente, recibido.intCodProd, recibido.intCodTal)))
                        continue;

                    if (!dependencias.TryGetValue(loteDestino, out var fuentesSet))
                    {
                        fuentesSet = new HashSet<LoteRpcKeyXSec>();
                        dependencias[loteDestino] = fuentesSet;
                    }
                    fuentesSet.Add(candidatoFuente);

                    if (!dependientes.TryGetValue(candidatoFuente, out var destinosSet))
                    {
                        destinosSet = new HashSet<LoteRpcKeyXSec>();
                        dependientes[candidatoFuente] = destinosSet;
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

        #endregion

        #region Generacion Diccionarios y Listas
        // REGLA: el PROCESADO consume por lote secuencial, proporcional a las libras
        // de TODOS sus RECIBIDO; el disponible nunca es negativo (tolerancia 0,0001 lb).
        public static void CalcularDisponiblePorLote(List<MatPrimaReproceso> lstRpc)
        {
            foreach (var item in lstRpc)
            {
                item.dbLibrasConsumidas = item.dbLibrasDisponibles = 0;
                item.blBloqueadoHistorico = false;
                item.dbLibrasTomadas = 0;
                item.blLoteUsable = !item.blExcluidoCosteo && item.strAgrupacion != "1. RECIBIDO";
                item._dcLibrasHistorico = 0m;
                item._blHistoricoAbsorbido = false;
                item._objEstadoHistorico = null;
            }
            foreach (var lote in lstRpc.GroupBy(x => x.intLotNumero))
            {
                var recibidos = lote.Where(x => x.strAgrupacion == "1. RECIBIDO" && x.dbLibras > 0).ToList();
                double lbsRec = recibidos.Sum(x => x.dbLibras);
                double lbsProc = lote.Where(x => x.strAgrupacion == "2. PROCESADO").Sum(x => Math.Max(0, x.dbLibras));
                double proporcion = lbsRec > 0 ? Math.Min(1, lbsProc / lbsRec) : 0;
                foreach (var rec in recibidos)
                {
                    rec.dbLibrasDisponibles = Math.Max(0, rec.dbLibras * (1 - proporcion));
                    if (rec.dbLibrasDisponibles <= ToleranciaLibras) rec.dbLibrasDisponibles = 0;
                    rec.dbLibrasConsumidas = rec.dbLibras - rec.dbLibrasDisponibles;
                    rec.blLoteUsable = !rec.blExcluidoCosteo && rec.dbLibrasDisponibles > ToleranciaLibras;
                }
                if (recibidos.Sum(x => x.dbLibrasDisponibles) <= ToleranciaLibras)
                    foreach (var proc in lote.Where(x => x.strAgrupacion == "2. PROCESADO"))
                    { proc.blLoteUsable = false; if (proc.dbCostoXSecuencial > 0) proc.blBloqueadoHistorico = true; }
            }
        }

        internal sealed class EstadoHistorico
        {
            internal decimal Costo, Libras;
            internal bool Usado;
            internal MatPrimaReproceso FilaReporte;
        }

        // Índices estructurales por pasada; costos y banderas se leen de las filas vivas.
        public sealed class IndiceDisponibleHistorico
        {
            internal readonly ILookup<(int, int), MatPrimaReproceso> PorProductoTalla;
            internal readonly ILookup<(int, int, int), MatPrimaReproceso> PorOrigen;
            internal readonly ILookup<int, MatPrimaReproceso> PorLote;
            internal readonly Dictionary<int, double> DisponiblePorLote;
            internal readonly Dictionary<(int, int, int), EstadoHistorico> Historicos = new();
            internal Action Confirmacion;
            internal MatPrimaReproceso Destino;
            public IndiceDisponibleHistorico(List<MatPrimaReproceso> filas)
            {
                PorProductoTalla = filas.ToLookup(x => (x.intCodProd, x.intCodTal));
                PorOrigen = filas.ToLookup(x => (x.intLoteUnificado, x.intCodProd, x.intCodTal));
                PorLote = filas.ToLookup(x => x.intLotNumero);
                DisponiblePorLote = PorLote.ToDictionary(g => g.Key,
                    g => g.Where(x => x.strAgrupacion == "1. RECIBIDO").Sum(x => x.dbLibrasDisponibles));
                foreach (var fila in filas.Where(x => x._objEstadoHistorico != null))
                {
                    var clave = (fila.intCodProd, fila.intCodTal, fila.intNivelHistorico);
                    if (!Historicos.TryGetValue(clave, out var previo) || previo.Usado)
                        Historicos[clave] = fila._objEstadoHistorico;
                }
            }
            // El motor llama una vez al iniciar un barrido con candidatos.
            public void BloquearProcesadosAgotados()
            {
                foreach (var lote in PorLote) BloquearProcesadosAgotados(lote.Key);
            }
            internal void BloquearProcesadosAgotados(int lote)
            {
                if (DisponiblePorLote.GetValueOrDefault(lote) > ToleranciaLibras) return;
                foreach (var proc in PorLote[lote].Where(x => x.strAgrupacion == "2. PROCESADO"))
                { proc.blLoteUsable = false; if (proc.dbCostoXSecuencial > 0) proc.blBloqueadoHistorico = true; }
            }
            public void Confirmar(MatPrimaReproceso rec)
            {
                if (ReferenceEquals(Destino, rec) && !rec.blExcluidoCosteo && rec.dbCostoXSecuencial > 0)
                    Confirmacion?.Invoke();
                Confirmacion = null;
                Destino = null;
            }
        }

        // Consultar no cambia las filas; el motor confirma después de asignar precio positivo.
        public static decimal ObtenerCostoDisponibleLote(MatPrimaReproceso rec, List<MatPrimaReproceso> lstRpc, IndiceDisponibleHistorico indice = null)
        {
            if (rec.blExcluidoCosteo || rec.dbCostoXSecuencial > 0 || rec.dbLibras <= ToleranciaLibras) return 0m;
            indice ??= new IndiceDisponibleHistorico(lstRpc);
            indice.Confirmacion = null;
            indice.Destino = rec;
            var fuentes = indice.PorOrigen[(rec.intLoteOrigen, rec.intCodProd, rec.intCodTal)]
                .Where(x => !ReferenceEquals(x, rec) && x.strAgrupacion == "1. RECIBIDO" && x.blLoteUsable
                    && !x.blBloqueadoHistorico && !x.blExcluidoCosteo && !x.blCostoReferencial
                    && x.dbCostoXSecuencial > 0 && x.dbLibras > 0 && x.dbLibrasDisponibles > ToleranciaLibras).ToList();
            double disponible = fuentes.Sum(x => x.dbLibrasDisponibles);
            if (disponible <= ToleranciaLibras) return 0m;
            decimal costo = Math.Round(fuentes.Sum(x => x.dbCostoTotal) / (decimal)fuentes.Sum(x => x.dbLibras), 4);
            if (costo <= 0m) return 0m;
            indice.Confirmacion = () =>
            {
                double tomadas = Math.Min(disponible, rec.dbLibras), pendientes = tomadas;
                foreach (var fuente in fuentes)
                {
                    double consumo = Math.Min(fuente.dbLibrasDisponibles, pendientes);
                    if (consumo <= 0) continue;
                    fuente.dbLibrasDisponibles = Math.Max(0, fuente.dbLibrasDisponibles - consumo);
                    fuente.dbLibrasConsumidas += consumo;
                    pendientes = Math.Max(0, pendientes - consumo);
                    fuente.blUsadoComoFuente = true;
                    indice.DisponiblePorLote[fuente.intLotNumero] -= consumo;
                    if (fuente.dbLibrasDisponibles <= ToleranciaLibras)
                    { fuente.blBloqueadoHistorico = true; fuente.blLoteUsable = false; }
                }
                foreach (int lote in fuentes.Select(x => x.intLotNumero).Distinct()) indice.BloquearProcesadosAgotados(lote);
                rec.dbLibrasTomadas = tomadas;
                rec.blCostoReferencial = false;
                rec.strOrigenCosto = "DISP";
            };
            return costo;
        }

        // Histórico por producto+talla+nivel; estado independiente de las libras del destino.
        public static decimal ObtenerCostoHistorico(MatPrimaReproceso rec, List<MatPrimaReproceso> lstRpc,
            IReadOnlyDictionary<string, int> mapaNiveles, IndiceDisponibleHistorico indice = null)
        {
            if (rec.blExcluidoCosteo || !ConfigRespaldoRpc.HistoricoPonderado || rec.dbCostoXSecuencial > 0 || rec.dbLibras <= ToleranciaLibras) return 0m;
            indice ??= new IndiceDisponibleHistorico(lstRpc);
            indice.Confirmacion = null;
            indice.Destino = rec;
            int nivel = NivelCosteo.ObtenerNivel(rec.strTipCod, mapaNiveles);
            EstadoHistorico anterior = null;
            for (int n = nivel; n >= 1; n--)
                if (indice.Historicos.TryGetValue((rec.intCodProd, rec.intCodTal, n), out var historico)
                    && !historico.Usado && !historico.FilaReporte.blExcluidoCosteo)
                { anterior = historico; break; }
            decimal libras = anterior?.Libras ?? 0m;
            decimal dolares = libras * (anterior?.Costo ?? 0m);
            var absorbidas = new List<MatPrimaReproceso>();
            var fuentes = indice.PorProductoTalla[(rec.intCodProd, rec.intCodTal)]
                .Where(x => !ReferenceEquals(x, rec) && x.strAgrupacion == "1. RECIBIDO" && x.blLoteUsable
                    && !x.blBloqueadoHistorico && !x.blExcluidoCosteo && !x.blCostoReferencial
                    && x.dbCostoXSecuencial > 0 && x.dbLibras > 0 && x.dbLibrasDisponibles > ToleranciaLibras
                    && (x.strNivel == "NV1" || (nivel > 2 && NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles) < nivel)))
                .GroupBy(x => x.intLoteUnificado);
            foreach (var grupo in fuentes)
            {
                double disponible = grupo.Sum(x => x.dbLibrasDisponibles);
                decimal costo = grupo.Sum(x => x.dbCostoTotal) / (decimal)grupo.Sum(x => x.dbLibras);
                if (costo <= 0m) continue;
                libras += (decimal)disponible;
                dolares += costo * (decimal)disponible;
                absorbidas.AddRange(grupo);
            }
            if (libras <= (decimal)ToleranciaLibras) return 0m;
            decimal ponderado = Math.Round(dolares / libras, 4);
            if (ponderado <= 0m) return 0m;
            indice.Confirmacion = () =>
            {
                rec.dcCostoPondHistorico = ponderado;
                rec.intNivelHistorico = nivel;
                rec._dcLibrasHistorico = libras;
                rec._blHistoricoAbsorbido = false;
                rec.blCostoReferencial = true;
                rec.strOrigenCosto = "HIST";
                foreach (var fuente in absorbidas)
                { fuente.blBloqueadoHistorico = true; fuente.blLoteUsable = false; fuente.blUsadoComoFuente = true; }
                if (anterior != null)
                {
                    anterior.Usado = true;
                    anterior.FilaReporte._blHistoricoAbsorbido = true;
                    anterior.FilaReporte.blBloqueadoHistorico = true;
                    anterior.FilaReporte.blLoteUsable = false;
                }
                rec._objEstadoHistorico = new EstadoHistorico { Costo = ponderado, Libras = libras, FilaReporte = rec };
                indice.Historicos[(rec.intCodProd, rec.intCodTal, nivel)] = rec._objEstadoHistorico;
            };
            return ponderado;
        }

        // REGLA: conservar la firma común y asignar con el motor existente, sin alterar signos.
        public static void RetrocostearEspeciales(List<MatPrimaReproceso> lstRpc, IReadOnlyDictionary<string, int> mapaNiveles)
        {
            var motor = new MotorAsignacionPrecios(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
            RetrocostearEspeciales(lstRpc, mapaNiveles, (rec, costo) => motor.AsignarPrecio(rec, costo, null));
        }

        private static string NormalizarClaseRetro(string clase) => clase?.Trim().ToUpperInvariant() switch
        { "A" or "A+" => "A", "B" or "N" => "B", _ => null };
        private static bool EsTipoExcluidoRetro(string tipo) => tipo?.Trim().ToUpperInvariant() is "R7";

        // REGLA ANTERIOR: UNI S/TALLA sin costo toman Σ MP / Σ libras de PROCESADO costeados por producto+clase A/B.
        // REGLA: RECIBIDO UNI/CDI sin costo, de cualquier talla, toman Σ dbCostoTotal / Σ libras de RECIBIDO
        // costeados no RETRO por código+clase normalizada A/B (strClaseProd = ProClas02), sin considerar talla; R7 queda fuera.
        public static void RetrocostearEspeciales(List<MatPrimaReproceso> lstRpc, IReadOnlyDictionary<string, int> mapaNiveles, Action<MatPrimaReproceso, decimal> asignarPrecio)
        {
            if (!ConfigRespaldoRpc.RetrocosteoEspeciales) return;
            var costos = lstRpc.Where(x => x.strAgrupacion == "1. RECIBIDO" && x.dbCostoXSecuencial > 0 && x.dbLibras > 0
                && x.dbCostoTotal > 0 && NormalizarClaseRetro(x.strClaseProd) != null
                && !x.blExcluidoCosteo && !string.Equals(x.strOrigenCosto?.Trim(), "RETRO", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => (x.intCodProd, strClase: NormalizarClaseRetro(x.strClaseProd)))
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.dbCostoTotal) / (decimal)g.Sum(x => x.dbLibras), 4));
            foreach (var rec in lstRpc.Where(x => x.strAgrupacion == "1. RECIBIDO" && x.dbCostoXSecuencial <= 0
                && !x.blExcluidoCosteo && !EsTipoExcluidoRetro(x.strTipCod)
                && (x.strTipCod?.Trim().ToUpperInvariant() is "UNI" or "CDI")))
                if (costos.TryGetValue((rec.intCodProd, NormalizarClaseRetro(rec.strClaseProd) ?? ""), out decimal costo) && costo > 0m)
                {
                    asignarPrecio(rec, costo);
                    if (rec.dbCostoXSecuencial <= 0) continue;
                    rec.strOrigenCosto = "RETRO";
                    rec.blCostoReferencial = true;
                }
        }

        // REGLA: respaldo por prod+talla = Σ dbCostoTotal / Σ dbLibras, redondeado a 4.
        // Actual conserva RECIBIDO global; N1 toma RECIBIDO costeados solo del nivel N-1.
        // Para destino NV2, N-1 corresponde a strNivel == "NV1"; sin muestra no hay respaldo global.
        // N1Excluyente toma PROCESADO costeados N-1 no usados como fuente (blUsadoComoFuente).
        // Esta bandera se marca al trasladar el costo y depende del orden topológico.
        // Se conservan los filtros positivos: dbCostoXSecuencial > 0 y dbLibras > 0.
        public static Dictionary<LoteRpcKeyXProdTal, decimal> GenerarPromedioRespaldo(IEnumerable<MatPrimaReproceso> lstUniverso, IReadOnlyDictionary<string, int> mapaNiveles, int intNivelDestino, ModoRespaldoRpc modo)
        {
            if (modo == ModoRespaldoRpc.N1 || modo == ModoRespaldoRpc.N1Excluyente)
            {
                var lstNivelAnterior = lstUniverso.Where(x => intNivelDestino == 2 ? x.strNivel == "NV1" : NivelCosteo.ObtenerNivel(x.strTipCod, mapaNiveles) == intNivelDestino - 1);
                if (modo == ModoRespaldoRpc.N1Excluyente)
                    return GenerarPromedioPonderadoProc(lstNivelAnterior.Where(x => !x.blUsadoComoFuente).ToList());
                return GenerarPromedioRespaldo(lstNivelAnterior, mapaNiveles, intNivelDestino, ModoRespaldoRpc.Actual);
            }

            // Actual: lógica vigente de MotorProrrateo, sin cambiar filtros, sumas ni redondeo.
            return lstUniverso
                .Where(x => x.strAgrupacion == "1. RECIBIDO"
                         && x.dbCostoXSecuencial > 0
                         && x.dbLibras > 0)
                .GroupBy(x => new LoteRpcKeyXProdTal(x.intCodProd, x.intCodTal))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal totDol = g.Sum(x => x.dbCostoTotal);
                        return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                    });
        }

        public static Dictionary<LoteRpcKeyXProdTal, decimal> GenerarPromedioPonderadoGlobal(
    List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            return lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "2. PROCESADO"
                         && x.dbCostoXSecuencial > 0
                         && x.dbLibras > 0)
                .GroupBy(x => x.objProdTalKey)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal totDol = g.Sum(x => x.dbCostoTotal);
                        return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                    });
        }

        /// <summary>
        /// Promedio full-cost por prod+talla: Σ dcTotalDolSum / Σ dbLibras de TODAS las
        /// PROCESADO del rango, sin importar lote ni lote unificado. A diferencia de
        /// GenerarPromedioPonderadoGlobal (que usa dbCostoTotal, solo materia prima), este
        /// usa dcTotalDolSum (proceso + empaque + tarifa + materia prima) para que un
        /// RECIBIDO que reutiliza un producto ya reprocesado herede su costo pleno.
        /// </summary>
        public static Dictionary<LoteRpcKeyXProdTal, decimal> GenerarPromedioFullCostGlobal(
            List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            return lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "2. PROCESADO" && x.dbLibras > 0)
                .GroupBy(x => x.objProdTalKey)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal totDol = g.Sum(x => x.dcTotalDolSum);
                        return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                    });
        }

        public static Dictionary<LoteRpcKeyReci, decimal> GenerarPromedioPonderadoReci(
List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            return lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "1. RECIBIDO"
                         && x.dbCostoXSecuencial > 0
                         && x.dbLibras > 0)
                .GroupBy(x => x.objLoteProdTalReciKey)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal totDol = g.Sum(x => x.dbCostoTotal);
                        return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                    });
        }

        public static Dictionary<LoteRpcKeyXProdTal, decimal> GenerarPromedioPonderadoProc(List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            return lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "2. PROCESADO"
                         && x.dbCostoXSecuencial > 0
                         && x.dbLibras > 0)
                .GroupBy(x => x.objProdTalKey)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal totDol = g.Sum(x => x.dbCostoTotal);
                        return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                    });
        }



        public static Dictionary<LoteRpcKeyReci, decimal> GenerarDicProcGlobal(List<MatPrimaReproceso> lstMatPrimaRpc, ILogger? objLogger = null)
        {
            var lstGrupos = lstMatPrimaRpc.Where(x => x.strAgrupacion == "2. PROCESADO" && x.dbCostoXSecuencial > 0 && x.dbLibras > 0)
                .GroupBy(x => x.objRpcValOrKey).ToList();
            // DiagnosticoPrecision.Diccionario(objLogger, "DiccionarioPROC", lstGrupos.Count,
                // lstGrupos.Count(g => DiagnosticoPrecision.EsUnitarioDosDecimales(g.First().dcCostoTotXLibra ?? 0m)));
            return lstGrupos.ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.dcTotalDolSum) / g.Sum(x => (decimal)x.dbLibras), 4));
        }

        public static Dictionary<LoteRpcKeyReci, decimal> GenerarPromedioPonderadoXLote(
    List<MatPrimaReproceso> lstMatPrimaRpc)
        {
            return lstMatPrimaRpc
                .Where(x => x.strAgrupacion == "2. PROCESADO"
                         && x.dbCostoXSecuencial > 0
                         && x.dbLibras > 0)
                .GroupBy(x => x.objRpcValOrKey)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        decimal totLbs = (decimal)g.Sum(x => x.dbLibras);
                        decimal totDol = g.Sum(x => x.dbCostoTotal);
                        return totLbs > 0 ? Math.Round(totDol / totLbs, 4) : 0m;
                    });
        }
        /// <summary>
        /// Costo completo de una fila PROCESADO: materia prima + proceso + material
        /// empaque + tarifario adicional. Única fórmula — la usan tanto
        /// RendimientoReproPlanRecibProc (recalculo final) como TransferirPrecioFuente
        /// (para que el destino herede el costo completo del origen, no solo materia prima).
        /// </summary>
        public static decimal CalcularCostoTotalDolSum(MatPrimaReproceso item) =>
            (item.dcCostTotalProc ?? 0m) + (item.dcCostoTotalMatEmp ?? 0m) + item.dbCostoTotal + (item.dcTarifaProc ?? 0m) + (item.dcCertificado ?? 0m);

        public static Dictionary<LoteRpcValKey, decimal> GenerarCostoReciX(
    List<MatPrimaReproceso> lstMatPrimaRpc, LoteRpcKeyXSec objLoteFuente, LoteRpcKeyXSec objLoteDestino)
        {
            // 1. Costo del PROCESADO del lote fuente, indexado por (producto, talla)
            var dicCostoFuente = lstMatPrimaRpc
                .Where(x => x.objLoteKey == objLoteFuente
                         && x.strAgrupacion == "2. PROCESADO"
                         && x.dbCostoXSecuencial > 0
                         && x.dbLibras > 0)
                .GroupBy(x => x.objProdTalKey)   // LoteRpcKeyXProdTal(intProdCod, intCodTal)
                .ToDictionary(
                    g => g.Key,
                    // Anterior: g => g.First().dbCostoXSecuencial); se usa el total completo conocido de PROCESADO.
                    g => g.Sum(x => CalcularCostoTotalDolSum(x)) / g.Sum(x => (decimal)x.dbLibras));

            // 2. RECIBIDO del destino: hereda el costo del fuente por (producto, talla)
            return lstMatPrimaRpc
                .Where(x => x.objLoteKey == objLoteDestino
                         && x.strAgrupacion == "1. RECIBIDO")
                .GroupBy(x => x.objLotRpc)
                .ToDictionary(
                    g => g.Key,
                    g => dicCostoFuente.TryGetValue(
                             new LoteRpcKeyXProdTal(g.Key.intCodProd, g.Key.intCodTal),
                             out var dcCosto) ? dcCosto : 0m);
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
                .GroupBy(x => new { x.intLotNumero, x.intLoteUnificado, x.intLoteOrigen, x.intCodProd, x.objLoteKey })
                .ToList()
                .GroupBy(g => g.Key.objLoteKey)
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
                            .GroupBy(g => g.objLoteKey)
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
                                    .GroupBy(x => x.objLoteProdReciKey)
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
        #endregion

        #region Inicializadores de Valores
        public void InitializerCostPremio(decimal? Premio)
        {
            decimal dcValCertPrecio = Premio ?? 0.0m;
            dcCertificado = Math.Round(dcValCertPrecio * (decimal)dbLibras, 2);
            dcValorCert = dcValCertPrecio;
        }

        private void InitializerKeys()
        {
            this.objLotRpc = new LoteRpcValKey(this.intLotNumero,this.intLoteUnificado,this.intCodProd,this.intCodTal); 
            this.objLoteKey = new LoteRpcKeyXSec(this.intLotNumero, this.intLoteUnificado);
            this.objValKey = new LoteRpcValKey(this.intLotNumero, this.intLoteUnificado, this.intCodProd, this.intCodTal);
            this.objProdTalKey = new LoteRpcKeyXProdTal(this.intCodProd, this.intCodTal);
            this.objLoteProdReciKey = new LoteRpcKeyLoteXProd(this.intLotNumero, this.intCodProd.ToString().Trim());
            if (this.strAgrupacion == "1. RECIBIDO")
                this.objLoteProdTalReciKey = new LoteRpcKeyReci((int)intLoteOrigen, this.intCodProd, this.intCodTal);
            if (this.strAgrupacion == "2. PROCESADO")
                this.objFrskey = new LoteFrsKey((int)intLoteUnificado, this.intCodProd, this.intCodTal);
            this.objRpcValOrKey = new LoteRpcKeyReci(this.intLoteUnificado, this.intCodProd, this.intCodTal);
        }
        #endregion

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

        public LoteFrsKey objLoteProdTalKey { get; set; }
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
            objLoteProdTalKey = new LoteFrsKey(intLidLote, intProCodcor, srtTcdCodtal);
        }

        /// <summary>
        /// Genera un diccionario con el costo promedio por Lote, Producto y Talla a partir de una lista de materia prima fresco.
        /// </summary>
        /// <param name="lstPrecioLiqOtrProc">Lista de objetos con los costos.</param>
        /// <returns>Diccionario donde la llave es (Lote, Producto, Talla) y el valor es el Precio Promedio calculado.</returns>
        public static Dictionary<LoteFrsKey, double> GenerarDiccionarioCostoXTalla(IEnumerable<PrecioFrsXMov> lstPrecioLiqOtrProc)
        {
            if (lstPrecioLiqOtrProc == null)
                return new Dictionary<LoteFrsKey, double>();

            return lstPrecioLiqOtrProc
                .GroupBy(p => p.objLoteProdTalKey)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        double totalLibras = g.Sum(p => p.dbLibras);
                        if (totalLibras == 0)
                            return 0.0;
                        double totalCosto = g.Sum(p => p.dbLidPrecio * p.dbLibras);
                        return totalCosto / totalLibras;
                    }
                );
        }

    }
}
