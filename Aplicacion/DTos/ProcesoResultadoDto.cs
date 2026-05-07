using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.EF_Core;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class ProcesoResultadoDto
    {
        [Column("Codigo")]
        [JsonProperty("Codigo")]
        public int intCodigo { get; set; }

        [JsonIgnore]
        public int intCodDet { get; set; }

        [JsonIgnore]
        public string? strEstado { get; set; }

        [Column("codTip")]
        [JsonProperty("codTip")]
        public string? strCodTip { get; set; }

        [Column("editable")]
        [JsonProperty("editable")]
        public bool blEditable { get; set; }

        [Column("TipLote")]
        [JsonProperty("TipLote")]
        public string? strTipoLote { get; set; }

        [Column("Descripcion")]
        [JsonProperty("Descripcion")]   
        public string strDescripcion { get; set; }

        [Column("Valor")]
        [JsonProperty("Valor")]
        public decimal dcValor { get; set; }  // El ISNULL(pu_valor, 0)

        [Column("Libras_Proceso")]
        [JsonProperty("Libras_Proceso")]
        public decimal dcLibras { get; set; }

        [Column("Costo_Unitario")]
        [JsonProperty("Costo_Unitario")]
        public decimal? dcCostUnitario { get; set; }


        /// <summary>Costo promedio ponderado por prod+talla — fuente FRS (para exportaciones).</summary>
        public static ConcurrentDictionary<string, decimal> ConstruirDictProc(
            List<ProcesoResultadoDto> lst)
        {
            if (!lst.Any()) return new ConcurrentDictionary<string, decimal>();

            return new ConcurrentDictionary<string, decimal>(
                lst
                    .Where(p => !string.IsNullOrWhiteSpace(p.strDescripcion))
                .ToDictionary(
                        p => p.strDescripcion.Trim(),
                        p => (decimal) p.dcCostUnitario)
                );
        }
        /// <summary>
        /// Construye un diccionario que asocia el Código del Proceso (Ej: "CAM") 
        /// con su Valor de Tarifa (dcValor), utilizando un diccionario puente de descripciones.
        /// </summary>
        public static ConcurrentDictionary<string, decimal> ConstruirDictTarifasPorCodigo(
            List<ProcesoResultadoDto> lstProcesoTarifa,
            ConcurrentDictionary<string, string> objDicTiplot)
        {
            var dicTarifas = new ConcurrentDictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            if (lstProcesoTarifa == null || objDicTiplot == null || !lstProcesoTarifa.Any())
                return dicTarifas;

            foreach (var tarifa in lstProcesoTarifa)
            {
                // Si encontramos a qué código pertenece esta descripción, guardamos su valor ($)
                if (objDicTiplot.TryGetValue(tarifa.strDescripcion, out string codigoReal))
                {
                    dicTarifas.TryAdd(codigoReal, tarifa.dcValor);
                }
            }

            return dicTarifas;
        }


        public static ConcurrentDictionary<string, string> ConstruirDiccionarioPuente(
            List<ProcesoResultadoDto> lst)
        {
            var dic = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (lst == null) return dic;

            foreach (var item in lst)
                dic.TryAdd(item.strDescripcion.Trim(), item.strCodTip.Trim());
            return dic;
        }
    }

    public class DataProcesoParam
    {
        #region Datos Procesos
        public List<ProcesoResultadoDto> lstProcesoFrs { get; set; }
        public List<ProcesoResultadoDto> lstProcesoRpc { get; set; }
        public List<ProcesoResultadoDto> lstProcesoTarifa { get; set; }
        #endregion

        #region Datos Reprocesos
        public List<MatPrimaReproceso> lstLiqRepro  { get; set; }
        #endregion

        #region Datos Fresco
        public List<LiquidacionResultado> lstLiqFresco { get; set; }
        public List<LiquidacionResultado> lstLiqFrsRpc { get; set; }
        public List<RptGrncLibras> lstLibrasProduccion { get; set; }
        public List<RptCongInd> lstLotOpcon { get; set; }
        public List<ResumenEstiloLbsDto> lstResumenEstiloLbs { get; set; }
        public List<LbsCongelamiento> lstFrsConge { get; set; }
        public List<int> lstCongTunel { get; set; }
        public List<int> lstCongIqf { get; set; } // Se recibe por si se requiere expandir lógica
        public List<int> lstCongBrine { get; set; } // Se recibe por si se requiere expandir lógica
        public List<CopackingLbs> lstCopackingLbs { get; set; }
        public List<ProcesoResultadoDto> lstResultados { get; set; }
        public List<int> lstLotesFrsRpc { get; set; }

        #endregion
        #region Parametros Generales
        public List<string> lstProdTerm { get; set; }
        public List<string> lstDescTotFresco { get; set; }
        #endregion
    }

    public class GuardarParametrosRequest
    {
        [JsonProperty("anio")]
        public string strAnio { get; set; }

        [JsonProperty("mes")]
        public string strMes { get; set; }

        [JsonProperty("usuario")]
        public string strUsuario { get; set; }

        [JsonProperty("valores")]
        public List<ProcesoResultadoDto> LstValores { get; set; }


    }

}
