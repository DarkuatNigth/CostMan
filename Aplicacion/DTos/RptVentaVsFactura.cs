using CostManagement.Dominio.Entidades;
using CostManagementService.Infraestructura.EF_Core.SONG;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Aplicacion.DTos
{
    public class RptVentaVsFactura
    {
        #region Propiedades de Base de Datos

        [JsonProperty("TIPO")]
        [Column("TIPO")]
        public string strTipo { get; set; }

        [JsonProperty("TRAN CAM")]
        [Column("TRAN CAM")]
        public int intTranCam { get; set; }

        [JsonProperty("FECHA")]
        [Column("FECHA")]
        public DateOnly dtFecha { get; set; }

        [JsonProperty("CodProd")]
        [Column("CodProd")]
        public int intCodProd { get; set; }

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

        [JsonProperty("CLTE")]
        [Column("CLTE")]
        public string strClte { get; set; }

        [JsonProperty("PAIS")]
        [Column("PAIS")]
        public string strPais { get; set; }

        [JsonProperty("FACT")]
        [Column("FACT")]
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

        [JsonIgnore]
        public bool blActivo { get; set; }

        [JsonIgnore]
        public NumDocXFactRef objDocFactRef { get; set; }
        #endregion





        public RptVentaVsFactura(RepFactPesoRealResult objPesosReales)
        {
            this.strFact = objPesosReales.strFact?.Trim();
            this.strTipo = this.strFact.Contains("R") == true ?  "VENTA MARINA" : "VENTA EXPORTACION";
            this.intTranCam = (int)objPesosReales.intTcdNumero;
            this.dtFecha = objPesosReales.dtEmbFechaped.HasValue ? DateOnly.FromDateTime(objPesosReales.dtEmbFechaped.Value) : default;
            this.intCodProd = int.TryParse(objPesosReales.strTcdProduc, out int parsedCod) ? parsedCod : 0;
            this.strDescProducto = objPesosReales.strProDesexp;
            this.strTalla = objPesosReales.strTalDescri;
            this.intTalla = (int)objPesosReales.intTcdCodtal;
            this.strClte = objPesosReales.strCliDescripcion;
            this.strPais = objPesosReales.strPaiDescri;
            this.dcLibras = (decimal)objPesosReales.dcLbs1;
            this.dcCostoVenta = 0m; // Inicializado en cero, se calculará posteriormente
            this.objDocFactRef = new NumDocXFactRef(this.intTranCam, this.strFact);
            // Ejecución de métodos de cálculo
            CalculoFob((decimal)objPesosReales.dcPemPeso, (decimal)objPesosReales.dcMasters, (decimal)objPesosReales.dcPemPrecio);
            CalculoPrcXLb();
            CalculoCostoTotal();
        }
        public RptVentaVsFactura(FacturaResult objFactVentLocal)
        {
            try
            {
                this.strFact = LimpiarNumeroDocumento(objFactVentLocal.strMovNumdoc).ToString();
                this.strTipo = "VENTA LOCAL";
                this.intTranCam = 0;//(int)objFactVentLocal.intTcdNumero;
                this.dtFecha = DateOnly.FromDateTime((DateTime)ParsearFechaCadena(objFactVentLocal.dtMovFecha));
                this.intCodProd = int.TryParse(objFactVentLocal.strDetCodart, out int parsedCod) ? parsedCod : 0;
                this.strDescProducto = objFactVentLocal.strDetNomart;
                this.strTalla = objFactVentLocal.strTalDescri;
                this.intTalla = (int)objFactVentLocal.intTalCodigo;
                this.strClte = objFactVentLocal.strCliNomcom;
                this.strPais = objFactVentLocal.strExpoPais;
                this.dcLibras = (decimal)objFactVentLocal.dcDetLibras;
                this.dcCostoVenta = 0m; // Inicializado en cero, se calculará posteriormente
                this.objDocFactRef = new NumDocXFactRef(this.intTranCam, this.strFact);
                decimal dcBase12 = objFactVentLocal.dcDetPoriva > 0 ?
                        (((decimal?)(objFactVentLocal.dcDetValiva) ?? 0m) * 100) / (decimal)objFactVentLocal.dcDetPoriva : 0m;
                this.dcFob = ((decimal?)(objFactVentLocal.dcDetSubtot) ?? 0m) - dcBase12;
                this.dcPrcXLb = this.dcFob / this.dcLibras;
                // Ejecución de métodos de cálculo
                // CalculoFob((decimal)objFactVentLocal.dcDetLibras, (decimal)objFactVentLocal.dcDetCanti, (decimal)objFactVentLocal.dcDetPreuni);
                //CalculoPrcXLb();
                //CalculoCostoTotal();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear RptVentaVsFactura desde FacturaResult: {ex.Message}", ex);
            }
        }


        #region Calculos Automaticos del Reporte
        public void CalculosReporte()
        {
            CalculoCostoTotal();
            CalculoMargen();
            if (this.dcMargenTotal > 0)
            {
                CalculoMargenTotal();
                CalculoMargenUnitario();
            }
        }
        private void CalculoFob(decimal dcPemPeso, decimal dcMasters, decimal dcPemPrecio)
        {
            this.dcFob = dcPemPeso * dcMasters * Math.Round(dcPemPrecio, 6);
            this.dcFob = Math.Round(this.dcFob,2);
        }

        private void CalculoPrcXLb()
        {
            this.dcPrcXLb = (this.dcLibras != 0) ? (this.dcFob / this.dcLibras) : 0m;
            this.dcPrcXLb = Math.Round(this.dcPrcXLb, 2);
        }

        private void CalculoCostoTotal()
        {
            this.dcCostoTotal = this.dcLibras * this.dcCostoVenta;
            this.dcCostoTotal = Math.Round(this.dcCostoTotal, 2);
        }
        private void CalculoMargen()
        {
            this.dcMargen1 = this.dcFob - this.dcCostoTotal;
        }

        private void CalculoMargenTotal()
        {
            this.dcMargenTotal = this.dcMargen1 - ( this.dcGastosAdministrativos + this.dcGastosComercializacion + this.dcGastosExportacion);
        }

        private void CalculoMargenUnitario()
        {
            this.dcMargenUnitario = Math.Round(this.dcMargenTotal / this.dcLibras,2);
        }
        #endregion


        #region Helpers de Conversión

        /// <summary>
        /// Toma un formato "001-003-000023450", extrae la última parte y lo convierte a entero (eliminando ceros a la izquierda automáticamente).
        /// </summary>
        private static int LimpiarNumeroDocumento(string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento)) return 0;

            var partes = documento.Split('-');
            var ultimaParte = partes[partes.Length - 1];

            // int.TryParse limpia los ceros iniciales de forma natural (ej: "000023450" -> 23450)
            if (int.TryParse(ultimaParte, out int numeroLimpio))
            {
                return numeroLimpio;
            }

            return 0;
        }

        /// <summary>
        /// Convierte un campo string de fecha de la base de datos a un DateTime nullable.
        /// </summary>
        private static DateTime? ParsearFechaCadena(string? fechaStr)
        {
            if (DateTime.TryParse(fechaStr, out DateTime dt))
            {
                return dt;
            }
            return null;
        }

        #endregion
    }
}
