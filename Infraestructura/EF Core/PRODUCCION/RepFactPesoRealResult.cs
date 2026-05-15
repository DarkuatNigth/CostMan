using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace CostManagementService.Infraestructura.EF_Core.SONG
{
    public class RepFactPesoRealResult
    {// --- NÚMEROS SIN DECIMAL (int) ---
     // Usamos long para numeric(18,0) para evitar desbordamientos

        [Column("tcd_numero")]
        public long intTcdNumero { get; set; } // numeric(18,0), null: 0

        [Column("tcd_codtalvta")]
        public long? intTcdCodtalvta { get; set; } // numeric(18,0), null: 1

        [Column("pro_unimed")]
        public long intProUnimed { get; set; } // decimal(18,0), null: 0

        [Column("tcd_codtal")]
        public long intTcdCodtal { get; set; } // numeric(18,0), null: 0

        [Column("PEM_PROMED")]
        public long? intPemPromed { get; set; } // numeric(18,0), null: 1

        [Column("emb_numeroEgreso")]
        public long? intEmbNumeroEgreso { get; set; } // numeric(18,0), null: 1


        // --- NÚMEROS CON DECIMAL (dc) ---

        [Column("con_cantid")]
        public double dcConCantid { get; set; } // float, null: 0

        [Column("lbs1")]
        public double? dcLbs1 { get; set; } // float, null: 1

        [Column("cantid")]
        public decimal? dcCantid { get; set; } // numeric(38,2), null: 1

        [Column("masters")]
        public double? dcMasters { get; set; } // float, null: 1

        [Column("emb_cantid")]
        public double dcEmbCantid { get; set; } // float, null: 0

        [Column("emb_peso")]
        public double dcEmbPeso { get; set; } // float, null: 0

        [Column("med_factor")]
        public double? dcMedFactor { get; set; } // float, null: 1

        [Column("CON_CANTID")]
        public double dcConCantidUpper { get; set; } // float, null: 0 (Duplicado)

        [Column("LBS")]
        public double? dcLbs { get; set; } // float, null: 1

        [Column("PEM_PESO")]
        public double? dcPemPeso { get; set; } // float, null: 1

        [Column("EMB_CANTID")]
        public double? dcEmbCantidUpper { get; set; } // float, null: 1

        [Column("PEM_PRECIO")]
        public double? dcPemPrecio { get; set; } // float, null: 1


        // --- STRINGS (str) ---

        [Column("tcd_producvta")]
        public string? strTcdProducvta { get; set; } // varchar(30), null: 1

        [Column("trc_embfactura")]
        public string? strTrcEmbfactura { get; set; } // varchar(15), null: 1

        [Column("tal_descri")]
        public string? strTalDescri { get; set; } // varchar(15), null: 1

        [Column("pro_desexp")]
        public string? strProDesexp { get; set; } // varchar(150), null: 1

        [Column("tcd_produc")]
        public string strTcdProduc { get; set; } = string.Empty; // varchar(30), null: 0

        [Column("NomCliente")]
        public string? strNomCliente { get; set; } // varchar(125), null: 1

        [Column("EMB_FACTURA")]
        public string? strEmbFactura { get; set; } // varchar(12), null: 1

        [Column("FACT")]
        public string? strFact { get; set; } // varchar(6), null: 1

        [Column("PEM_CODPROD")]
        public string? strPemCodprod { get; set; } // varchar(10), null: 1

        [Column("PEM_TALLA")]
        public string strPemTalla { get; set; } = string.Empty; // varchar(10), null: 0

        [Column("cli_descripcion")]
        public string strCliDescripcion { get; set; } = string.Empty; // varchar(100), null: 0

        [Column("pai_descri")]
        public string strPaiDescri { get; set; } = string.Empty; // char(30), null: 0

        [Column("NomCliente")]
        public string strNomClienteRequired { get; set; } = string.Empty; // varchar(100), null: 0 (Duplicado)

        [Column("desde")]
        public string strDesde { get; set; } = string.Empty; // varchar(10), null: 0

        [Column("hasta")]
        public string strHasta { get; set; } = string.Empty; // varchar(10), null: 0


        // --- FECHAS (dt) ---

        [Column("trc_fecha")]
        public DateTime dtTrcFecha { get; set; } // datetime, null: 0

        [Column("EMB_FECHAPED")]
        public DateTime? dtEmbFechaped { get; set; } // datetime, null: 1

        public static ConcurrentDictionary<int, string> CrearDicFacturaPorMovimiento(
                List<RepFactPesoRealResult> lstPesosReales)
        {
            return new ConcurrentDictionary<int, string>(
                lstPesosReales
                    .Where(x => !string.IsNullOrWhiteSpace(x.strTrcEmbfactura))
                    .GroupBy(x =>(int) x.intTcdNumero)
                    .ToDictionary(
                        grp => grp.Key,
                        grp => grp.First().strTrcEmbfactura!
                    )
            );
        }

        public static ConcurrentDictionary<int, RepFactPesoRealResult> CrearDicEmbFacPorMovimiento(
                List<RepFactPesoRealResult> lstPesosReales)
        {
            return new ConcurrentDictionary<int, RepFactPesoRealResult>(
                lstPesosReales
                    .Where(x => !string.IsNullOrWhiteSpace(x.strTrcEmbfactura))
                    .GroupBy(x => (int)x.intTcdNumero)
                    .ToDictionary(
                        grp => grp.Key,
                        grp => grp.First()
                    )
            );
        }
    }
}
