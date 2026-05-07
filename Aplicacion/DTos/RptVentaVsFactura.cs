using CostManagementService.Infraestructura.EF_Core.SONG;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Aplicacion.DTos
{
    public class RptVentaVsFactura
    {
        #region Propiedades de Base de Datos

        [JsonProperty("TIPO")]
        [Column("TIPO")]
        public string strTipo { get; set; }

        [JsonProperty("TRAN. CAM.")]
        [Column("TRAN. CAM.")]
        public int intTranCam { get; set; }

        [JsonProperty("FECHA")]
        [Column("FECHA")]
        public DateOnly dtFecha { get; set; }

        [JsonProperty("COD.")]
        [Column("COD.")]
        public int intCod { get; set; }

        [JsonProperty("DESC PRODUCTO")]
        [Column("DESC PRODUCTO")]
        public string strDescProducto { get; set; }

        [JsonProperty("TALLA")]
        [Column("TALLA")]
        public string strTalla { get; set; }

        // Propiedad de apoyo, ignorada por EF y JSON
        [NotMapped]
        [JsonIgnore]
        public int intTalla { get; set; }

        [JsonProperty("CLTE.")]
        [Column("CLTE.")]
        public string strClte { get; set; }

        [JsonProperty("PAIS")]
        [Column("PAIS")]
        public string strPais { get; set; }

        [JsonProperty("FACT.")]
        [Column("FACT.")]
        public string strFact { get; set; }

        [JsonProperty("LIBRAS")]
        [Column("LIBRAS")]
        public decimal dcLibras { get; set; }

        [JsonProperty("COSTO DE VENTA")]
        [Column("COSTO DE VENTA")]
        public decimal dcCostoVenta { get; set; }

        #endregion

        #region Campos Calculados

        [JsonProperty("FOB")]
        [Column("FOB")]
        public decimal dcFob { get; set; }

        [JsonProperty("PRC  x LB")]
        [Column("PRC  x LB")]
        public decimal dcPrcXLb { get; set; }

        [JsonProperty("COSTO TOTAL")]
        [Column("COSTO TOTAL")]
        public decimal dcCostoTotal { get; set; }

        [JsonProperty("MARGEN 1")]
        [Column("MARGEN 1")]
        public decimal dcMargen1 { get; set; }

        [JsonProperty("GASTOS ADMINISTRATIVOS")]
        [Column("GASTOS ADMINISTRATIVOS")]
        public decimal dcGastosAdministrativos { get; set; }

        [JsonProperty("GASTOS COMERCIALIZACION")]
        [Column("GASTOS COMERCIALIZACION")]
        public decimal dcGastosComercializacion { get; set; }

        [JsonProperty("GASTOS EXPORTACION")]
        [Column("GASTOS EXPORTACION")]
        public decimal dcGastosExportacion { get; set; }

        [JsonProperty("Margen total")]
        [Column("Margen total")]
        public decimal dcMargenTotal { get; set; }

        [JsonProperty("Margen Unitario")]
        [Column("Margen Unitario")]
        public decimal dcMargenUnitario { get; set; }

        #endregion

        public RptVentaVsFactura(
            string strTipo,
            int intTranCam,
            DateTime? Fecha,
            string strCod,
            string strDescProducto,
            string strTalla,
            int intTalla,
            string strClte,
            string strPais,
            string strFact,
            decimal dcLibras,
            decimal dcPemPrecio,
            decimal dcCostoVenta,
            decimal dcPemPeso,
            decimal dcMasters
        )
        {
            #region asignación y tratamiento de datos para conversiones

            this.strTipo = strTipo;
            this.intTranCam = intTranCam;

            this.dtFecha = Fecha.HasValue ? DateOnly.FromDateTime(Fecha.Value) : default;

            if (int.TryParse(strCod, out int parsedCod))
                this.intCod = parsedCod;

            this.strDescProducto = strDescProducto;
            this.strTalla = strTalla;
            this.intTalla = intTalla; // Asignación de la variable ignorada
            this.strClte = strClte;
            this.strPais = strPais;
            this.strFact = strFact;
            this.dcLibras = dcLibras;
            this.dcCostoVenta = dcCostoVenta;

            #endregion

            // Ejecución de métodos de cálculo
            CalculoFob(dcPemPeso, dcMasters, dcPemPrecio);
            CalculoPrcXLb();
            CalculoCostoTotal();
        }

        public static List<RptVentaVsFactura> CrearListadoVentas(List<RepFactPesoRealResult> lstPesosReales)
        {
            if (!lstPesosReales.Any()) throw new ArgumentException("La lista de resultados está vacía.", nameof(lstPesosReales));
            return (
                    from peReal in lstPesosReales
                    group peReal by new
                    {
                        peReal.intTcdNumero,
                        peReal.strTcdProduc,
                        peReal.strProDesexp,
                        peReal.strTalDescri,
                        peReal.intTcdCodtal,
                        peReal.strCliDescripcion,
                        peReal.strPaiDescri,
                        peReal.dtEmbFechaped,
                        peReal.strFact,
                    } into grpPeReal
                    select new RptVentaVsFactura(
                        "VENTA EXPORTACION",
                        (int)grpPeReal.Key.intTcdNumero,
                        grpPeReal.Key.dtEmbFechaped,
                        grpPeReal.Key.strTcdProduc,
                        grpPeReal.Key.strProDesexp,
                        grpPeReal.Key.strTalDescri,
                        (int)grpPeReal.Key.intTcdCodtal,
                        grpPeReal.Key.strCliDescripcion,
                        grpPeReal.Key.strPaiDescri,
                        grpPeReal.Key.strFact,
                        grpPeReal.Sum(x => (decimal)x.dcLbs1),
                        grpPeReal.Sum(x => (decimal)x.dcPemPrecio),
                        0m,
                        grpPeReal.Sum(x => (decimal)x.dcPemPeso),
                        grpPeReal.Sum(x => (decimal)x.dcMasters)
                        )).ToList();
        }
        public static List<RptVentaVsFactura> CrearListadoFacturas(List<FacturaResult> lstFacturas)
        {
            if (!lstFacturas.Any()) throw new ArgumentException("La lista de resultados está vacía.", nameof(lstFacturas));
            return new List<RptVentaVsFactura>();
            //return (
            //        from peReal in lstFacturas
            //        group peReal by new
            //        {
            //            peReal.strMovTipo,
            //            peReal.strDetCodart,
            //            peReal.strDetNomart,
            //            peReal.strTalDescri,
            //            peReal.intTalCodigo,
            //            peReal.strCliNomcom,
            //            peReal.strExpoPais,
            //            peReal.dtMovFecha,
            //            peReal.strFact,
            //        } into grpPeReal
                    //select new RptVentaVsFactura(
                    //"VENTA EXPORTACION",
                    //(int)grpPeReal.Key.intTcdNumero,
                    //grpPeReal.Key.dtEmbFechaped,
                    //grpPeReal.Key.strTcdProduc,
                    //grpPeReal.Key.strProDesexp,
                    //grpPeReal.Key.strTalDescri,
                    //(int)grpPeReal.Key.intTcdCodtal,
                    //grpPeReal.Key.strCliDescripcion,
                    //grpPeReal.Key.strPaiDescri,
                    //grpPeReal.Key.strFact,
                    //grpPeReal.Sum(x => (decimal)x.dcLbs1),
                    //grpPeReal.Sum(x => (decimal)x.dcPemPrecio),
                    //0m,
                    //grpPeReal.Sum(x => (decimal)x.dcPemPeso),
                    //grpPeReal.Sum(x => (decimal)x.dcMasters)
                    //)
                  // ).ToList();
        }

        #region Calculos Automaticos del Reporte
        private void CalculoFob(decimal dcPemPeso, decimal dcMasters, decimal dcPemPrecio)
        {
            this.dcFob = dcPemPeso * dcMasters * Math.Round(dcPemPrecio, 6);
        }

        private void CalculoPrcXLb()
        {
            this.dcPrcXLb = (this.dcLibras != 0) ? (this.dcFob / this.dcLibras) : 0m;
        }

        private void CalculoCostoTotal()
        {
            this.dcCostoTotal = this.dcLibras * this.dcCostoVenta;
        }
        #endregion

    }
}
