using CostManagementService.Infraestructura.EF_Core;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagement.Aplicación.DTos
{
    public class DataProcesoParamDto
    {
        [JsonProperty("codigo")]
        [Column("codigo")]
        public int intId { get; set; }

        [JsonProperty("descripcion")]
        [Column("descripcion")]
        public string strDescripcion { get; set; }

        [JsonProperty("tipoData")]
        [Column("tipoData")]
        public string strTipoData { get; set; }



    }

    public class DistribucionCostoDto
    {
        [JsonProperty("ID")]
        [Column("ID")]
        public int intCodigo { get; set; }

        [JsonProperty("TIPODRIVER")]
        [Column("TIPODRIVER")]
        public string strTipo { get; set; }

        [JsonProperty("fechaCorte")]
        [Column("fechaCorte")]
        public DateOnly dtFechaCorte { get; set; }

        [JsonProperty("ANIO")]
        [Column("ANIO")]
        public int intAnio { get; set; }

        [JsonProperty("MES")]
        [Column("MES")]
        public int intMes { get; set; }

        [JsonProperty("PORCENTAJE")]
        [Column("PORCENTAJE")]
        public decimal dcPorcentaje { get; set; }

        [JsonProperty("MONTO")]
        [Column("MONTO")]
        public decimal dcMonto { get; set; }

        [JsonProperty("VALORKW")]
        [Column("VALORKW")]
        public decimal dcValorKw { get; set; }

        [JsonProperty("CUENTA")]
        [Column("CUENTA")]
        public string strCtaNumero { get; set; }

        [JsonProperty("TIPOCUENTA")]
        [Column("TIPOCUENTA")]
        public string strCtaNatura { get; set; }

        [JsonProperty("codigoMedidor")]
        [Column("codigoMedidor")]
        public string strCodMedidor { get; set; }

        [JsonProperty("paCodigo")]
        [Column("paCodigo")]
        public int intPaCodigo { get; set; }

        [JsonProperty("BOLSA")]
        [Column("BOLSA")]
        public string strBolsa { get; set; }

        [JsonProperty("CLAVE")]
        [Column("CLAVE")]
        public string strClave { get; set; }

        [JsonProperty("DESCRIPCION")]
        [Column("DESCRIPCION")]
        public string strDescripcion { get; set; }

        [JsonProperty("CENTRO_COSTO")]
        [Column("CENTRO_COSTO")]
        public string strCentroCosto { get; set; }

        // ---- ASIENTO y EDITABLE: el front los necesita, derivados ----
        [JsonProperty("ASIENTO")]
        [Column("ASIENTO")]
        public string strAsiento { get; set; }

        [JsonProperty("EDITABLE")]
        [Column("EDITABLE")]
        public bool blEditable { get; set; }

        [JsonProperty("Usuario")]
        [Column("Usuario")]
        public string? strUsuario { get; set; }

        public DistribucionCostoDto()
        {

        }

        public DistribucionCostoDto(TbDistribucionCosto obj)
        {
            intCodigo = obj.DpId;
            strTipo = obj.DpTipo;
            dtFechaCorte = obj.DpFechaCorte;
            intAnio = obj.DpAnio;
            intMes = obj.DpMes;
            dcPorcentaje = obj.DpPorcentaje;
            dcMonto = obj.DpMonto;
            dcValorKw = obj.DpValorKw;
            strCtaNumero = obj.DpCtaNumero;
            strCtaNatura = obj.DpCtaNatura;
            strCodMedidor = obj.DpCodigoMedidor;
            intPaCodigo = obj.DpPaCodigo.HasValue ? (int)obj.DpPaCodigo.Value : 0;
        }

        public void InicializarMaectCuenta(TbMaecta objMaecta, string CentroCosto)
        {
            this.strClave = objMaecta.CtaClave;
            this.strDescripcion = String.IsNullOrEmpty(objMaecta.CtaDescor) || String.IsNullOrWhiteSpace(objMaecta.CtaDescor) ? objMaecta.CtaDeslar : objMaecta.CtaDescor;
            this.strCentroCosto = CentroCosto;
        }
        public void InicializarPlanta(string DescPlanta)
        {
            strBolsa = DescPlanta;
            strAsiento = this.strBolsa == "COMIN" ? "COMIN" : "SONGA";
            blEditable = strAsiento == "SONGA" && strCtaNatura != "C";
        }
    }

    public class HaberDistribucionDTO
    {
        private Dictionary<int, string> _dicPlantaProc = new Dictionary<int, string>() { { 1, "SONGA1" }, { 11, "SONGA2" }, { 12, "COMIN" } };
        public string Tipo { get; set; }        // ENLEC, REBAS
        public string Bolsa { get; set; }       // SONGA1, SONGA2, COMIN
        public int Mes { get; set; }            // 1-12
        public decimal Monto { get; set; }      // USD
        public decimal ValorKw { get; set; }    // kWh
        public HaberDistribucionDTO()
        {
        }

        public HaberDistribucionDTO(TbDistribucionCosto obj)
        {
            Tipo = obj.DpTipo;
            Bolsa = _dicPlantaProc.ContainsKey((int)obj.DpPaCodigo) ? _dicPlantaProc[(int)obj.DpPaCodigo] : "SONGA";
            Mes = obj.DpMes;
            Monto = obj.DpMonto;
            ValorKw = obj.DpValorKw;
        }

        public static Dictionary<string, decimal[]> ConvertirADiccionarioMeses(
            List<HaberDistribucionDTO> datos,
            Func<HaberDistribucionDTO, decimal> selector)
        {
            var plantas = new[] { "SONGA1", "SONGA2", "COMIN" };
            var resultado = new Dictionary<string, decimal[]>();

            foreach (var planta in plantas)
            {
                // Inicializar array de 12 meses en 0
                var meses = new decimal[12];

                // Filtrar registros por planta
                var registrosPorPlanta = datos
                    .Where(d => d.Bolsa == planta)
                    .ToList();

                // Llenar array con valores reales
                foreach (var registro in registrosPorPlanta)
                {
                    if (registro.Mes >= 1 && registro.Mes <= 12)
                    {
                        meses[registro.Mes - 1] = selector(registro);
                    }
                }

                // Mapear nombre de planta (SONGA1 → "SONGA 1")
                string nombrePlanta = planta switch
                {
                    "SONGA1" => "SONGA 1",
                    "SONGA2" => "SONGA 2",
                    _ => "COMIN"
                };

                resultado[nombrePlanta] = meses;
            }

            return resultado;
        }



        public class ConsolidadoDTO
        {
            [JsonProperty("anio")]
            [Column("anio")]
            public int Anio { get; set; }

            [JsonProperty("usd")]
            [Column("usd")]
            public Dictionary<string, decimal[]> Usd { get; set; }      // 12 meses

            [JsonProperty("kwh")]
            [Column("kwh")]
            public Dictionary<string, decimal[]> Kwh { get; set; }      // 12 meses

            [JsonProperty("alumbrado")]
            [Column("alumbrado")]
            public Dictionary<string, decimal[]> Alumbrado { get; set; } // REBAS (12 meses)
        }
    }
}
