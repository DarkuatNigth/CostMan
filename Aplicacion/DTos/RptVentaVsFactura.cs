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
            {
                this.intCodProd = parsedCod;
            }
            this.strDescProducto = strDescProducto;
            this.strTalla = strTalla;
            this.intTalla = intTalla; // Asignación de la variable ignorada
            this.strClte = strClte;
            this.strPais = strPais;
            this.strFact = strFact.Trim();
            this.dcLibras = dcLibras;
            this.dcCostoVenta = dcCostoVenta;
            
            this.objDocFactRef = new NumDocXFactRef(this.intTranCam, this.strFact);

            #endregion

            // Ejecución de métodos de cálculo
            CalculoFob(dcPemPeso, dcMasters, dcPemPrecio);
            CalculoPrcXLb();
            CalculoCostoTotal();
        }

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
            ConcurrentDictionary<int, string> dicFactu,
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
                this.intCodProd = parsedCod;

            this.strDescProducto = strDescProducto;
            this.strTalla = strTalla;
            this.intTalla = intTalla; // Asignación de la variable ignorada
            this.strClte = strClte;
            this.strPais = strPais;
            this.strFact = dicFactu.GetValueOrDefault(this.intTranCam, string.Empty);
            this.dcLibras = dcLibras;
            this.dcCostoVenta = dcCostoVenta;

            #endregion

            // Ejecución de métodos de cálculo
            CalculoFob(dcPemPeso, dcMasters, dcPemPrecio);
            CalculoPrcXLb();
            CalculoCostoTotal();
        }

        public RptVentaVsFactura(
            //string strTipo,
            string strTranCam,
            DateTime? Fecha,
            string strCod,
            string strDescProducto,
            string strTalla,
            int intTalla,
            string strClte,
            string strPais,
            ConcurrentDictionary<string, TracamAutoResult> dicMovCam,
            decimal dcLibras,
            decimal dcPemPrecio,
            decimal dcCostoVenta,
            decimal dcPemPeso,
            decimal dcMasters
        )
        {
            #region asignación y tratamiento de datos para conversiones

            //this.strTipo = strTipo;
            //this.intTranCam = intTranCam;

            this.dtFecha = Fecha.HasValue ? DateOnly.FromDateTime(Fecha.Value) : default;

            if (int.TryParse(strCod, out int parsedCod))
                this.intCodProd = parsedCod;

            this.strDescProducto = strDescProducto;
            this.strTalla = strTalla;
            this.intTalla = intTalla; // Asignación de la variable ignorada
            this.strClte = strClte;
            this.strPais = strPais;
            TracamAutoResult objTracamAut = dicMovCam.GetValueOrDefault(strTranCam, null);
            if (objTracamAut != null)
            {
                this.strFact = objTracamAut.strTrcEmbfactura;
                this.strTipo = objTracamAut.strTrsDescri;
                this.intTranCam = (int)objTracamAut.intTrcNumsec;
            }
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


        public static List<RptVentaVsFactura> CrearListadoFacturas(List<FacturaResult> lstFacturas, ConcurrentDictionary<string, TracamAutoResult> dicMovCam)
        {
            if (!lstFacturas.Any()) throw new ArgumentException("La lista de resultados está vacía.", nameof(lstFacturas));
            return (
                    from peReal in lstFacturas
                    group peReal by new
                    {
                        peReal.strMovTipo,
                        peReal.strDetCodart,
                        peReal.strDetNomart,
                        peReal.strTalDescri,
                        peReal.intTalCodigo,
                        peReal.strCliNomcom,
                        peReal.strExpoPais,
                        peReal.dtMovFecha,
                        peReal.strMovNumdoc,
                    } into grpPeReal
                    select new RptVentaVsFactura(
                        //grpPeReal.Key.strMovTipo ?? "VENTA EXPORTACION",
                        //// AQUÍ VA EL MAPEO DEL NÚMERO LIMPIO PARA intTranCam
                        grpPeReal.Key.strMovNumdoc,
                        ParsearFechaCadena(grpPeReal.Key.dtMovFecha),
                        grpPeReal.Key.strDetCodart ?? "",
                        grpPeReal.Key.strDetNomart ?? "",
                        grpPeReal.Key.strTalDescri ?? "",
                        (int)(grpPeReal.Key.intTalCodigo ?? 0),
                        grpPeReal.Key.strCliNomcom ?? "",
                        grpPeReal.Key.strExpoPais ?? "",
                        dicMovCam,               //enviamos el diccionario para obtener el número de factura limpio
                        grpPeReal.Sum(x => (decimal)(x.dcDetLibras ?? 0)),
                        grpPeReal.Max(x => (decimal)(x.dcDetPreuni ?? 0)), // Usar Max o Average para precios
                        0m,
                        grpPeReal.Sum(x => (decimal)(x.dcDetCanti ?? 0)),
                        0m // dcMasters (puedes reemplazar 0m por el campo adecuado si existe en FacturaResult)
                    )
                   ).ToList();
        }

        public static List<RptVentaVsFactura> CrearListadoFacturas(List<FacturaResult> lstFacturas, ConcurrentDictionary<int, string> dicFactu)
        {
            if (!lstFacturas.Any()) throw new ArgumentException("La lista de resultados está vacía.", nameof(lstFacturas));
            return (
                    from peReal in lstFacturas
                    group peReal by new
                    {
                        peReal.strMovTipo,
                        peReal.strDetCodart,
                        peReal.strDetNomart,
                        peReal.strTalDescri,
                        peReal.intTalCodigo,
                        peReal.strCliNomcom,
                        peReal.strExpoPais,
                        peReal.dtMovFecha,
                        peReal.strMovNumdoc,
                    } into grpPeReal
                    select new RptVentaVsFactura(
                        grpPeReal.Key.strMovTipo ?? "VENTA EXPORTACION",
                        // AQUÍ VA EL MAPEO DEL NÚMERO LIMPIO PARA intTranCam
                        LimpiarNumeroDocumento(grpPeReal.Key.strMovNumdoc),
                        ParsearFechaCadena(grpPeReal.Key.dtMovFecha),
                        grpPeReal.Key.strDetCodart ?? "",
                        grpPeReal.Key.strDetNomart ?? "",
                        grpPeReal.Key.strTalDescri ?? "",
                        (int)(grpPeReal.Key.intTalCodigo ?? 0),
                        grpPeReal.Key.strCliNomcom ?? "",
                        grpPeReal.Key.strExpoPais ?? "",
                        dicFactu,               //enviamos el diccionario para obtener el número de factura limpio
                        grpPeReal.Sum(x => (decimal)(x.dcDetLibras ?? 0)),
                        grpPeReal.Max(x => (decimal)(x.dcDetPreuni ?? 0)), // Usar Max o Average para precios
                        0m,
                        grpPeReal.Sum(x => (decimal)(x.dcDetCanti ?? 0)),
                        0m // dcMasters (puedes reemplazar 0m por el campo adecuado si existe en FacturaResult)
                    )
                   ).ToList();
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
