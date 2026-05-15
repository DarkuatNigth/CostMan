namespace CostManagement.Aplicación.DTos
{
    public class MovimientoPrecioDto
    {
        public int intMovNummov { get; set; }
        public int intCietun { get; set; }
        public double dbDetPreuni { get; set; }

        public int intEftItem { get; set; }

        public DateOnly dtFechaEgreso { get; set; }

        public MovimientoPrecioDto(string movNummov, decimal? cieTun,double detPreuni, string EftItem, string FechaEgreso)
        {
            intMovNummov = int.TryParse(movNummov, out var n) ? n : 0;
            intCietun = (int)(cieTun ?? 0);
            dbDetPreuni = detPreuni;
            intEftItem = int.TryParse(EftItem, out var n2) ? n2 : 0;
            dtFechaEgreso = DateOnly.Parse(FechaEgreso);
        }
    }

    public class CostoMatEmpProdXCietunDto
    {
        public int intLiqLote { get; set; }
        public int intCtuNumero { get; set; }

        // Datos del Producto Principal
        public string strProCodCor { get; set; }
        //public string strColCodigo { get; set; }

        // Datos del Item de Ficha Técnica (Explosión de materiales)
        public int intEftItem { get; set; }

        public string strEftGrupo { get; set; }
        public double dbEftCantidad { get; set; }
        public double dbPrecioUltConsumo { get; set; }
        public double? dbPrecioUnit { get; set; }
        public string strEstadoFicha { get; set; }

        // Datos Técnicos y de Embalaje
        public float dcLibrasXMasters { get; set; }
        public decimal dcMedCodigo { get; set; }
        public string strEmbCodigo { get; set; }
        public string strTipCodigo { get; set; }
        public double dbEmbPeso { get; set; }
        public decimal dcCostoDesperdicioBobina { get; set; }

        //Datos egreso
        public DateOnly dtFechaEgreso { get; set; }

        public CostoMatEmpProdXCietunDto()
        {
            // Constructor vacío para inicialización manual
        }
        // Constructor para mapeo directo desde el query unificado con manejo de nulos
        public CostoMatEmpProdXCietunDto(
            decimal liqLote,
            int? ctuNumero,
            string? proCodCor,
            int? eftItem,
            string? eftGrupo,
            double? eftCantidad,
            float? librasXMasters,
            decimal? costoDesperdicio,
            double? embPeso,
            decimal? medCodigo,
            string? embCodigo,
            string TipCodigo/*,
            string? colCodigo*/)
        {
            intLiqLote = (int)liqLote;
            strTipCodigo = TipCodigo ?? string.Empty;
            intCtuNumero = ctuNumero ?? 0;
            strProCodCor = proCodCor ?? string.Empty;
            intEftItem = eftItem ?? 0;
            strEftGrupo = eftGrupo ?? string.Empty;
            dbEftCantidad = eftCantidad ?? 0;
            dcLibrasXMasters = librasXMasters ?? 0f;
            dcCostoDesperdicioBobina = costoDesperdicio ?? 0m;
            dbEmbPeso = embPeso ?? 0;
            dcMedCodigo = medCodigo ?? 0m;
            strEmbCodigo = embCodigo?.Trim() ?? string.Empty;
            //strColCodigo = colCodigo ?? string.Empty;

            // Inicialización de campos de precio por defecto para evitar nulos posteriores
            strEstadoFicha = "X";
            dbPrecioUltConsumo = 0;
        }
        public CostoMatEmpProdXCietunDto(
            decimal liqLote,
            int? ctuNumero,
            string? proCodCor,
            int? eftItem,
            string? eftGrupo,
            double? eftCantidad,
            float? librasXMasters,
            decimal? costoDesperdicio,
            double? embPeso,
            decimal? medCodigo,
            string? embCodigo/*,
            string? colCodigo*/)
        {
            intLiqLote = (int)liqLote;
            intCtuNumero = ctuNumero ?? 0;
            strProCodCor = proCodCor ?? string.Empty;
            intEftItem = eftItem ?? 0;
            strEftGrupo = eftGrupo ?? string.Empty;
            dbEftCantidad = eftCantidad ?? 0;
            dcLibrasXMasters = librasXMasters ?? 0f;
            dcCostoDesperdicioBobina = costoDesperdicio ?? 0m;
            dbEmbPeso = embPeso ?? 0;
            dcMedCodigo = medCodigo ?? 0m;
            strEmbCodigo = embCodigo?.Trim() ?? string.Empty;
            //strColCodigo = colCodigo ?? string.Empty;

            // Inicialización de campos de precio por defecto para evitar nulos posteriores
            strEstadoFicha = "X";
            dbPrecioUltConsumo = 0;
        }
    }


    public class CostoMovArtDto
    {
        public int intItemCod { get; set; }
        public string strItemDescripcion { get; set; }
        public string strMovFecha { get; set; }
        public decimal dcCantidad { get; set; }
        public decimal dcConsumoTotal { get; set; }
        public decimal dcPrecioProm { get; set; }

        public CostoMovArtDto(string itemCod, string ItemDescripcion, string movFecha, double? ConsumoCantidad, double? ConsumoTotal)
        {
            intItemCod = int.TryParse(itemCod, out var n) ? n : 0;
            strMovFecha = movFecha;
            dcCantidad = (decimal)(ConsumoCantidad ?? 0);
            dcConsumoTotal = (decimal)(ConsumoTotal ?? 0);
            strItemDescripcion = ItemDescripcion ?? string.Empty;
                dcPrecioProm = dcCantidad != 0 ? dcConsumoTotal / dcCantidad : 0;   

        }
        public CostoMovArtDto(int itemCod, string ItemDescripcion, string movFecha, decimal ConsumoCantidad, decimal ConsumoTotal, decimal PrecioProm)
        {
            intItemCod = itemCod;
            strItemDescripcion = ItemDescripcion ?? string.Empty;
            dcPrecioProm = dcCantidad != 0 ? dcConsumoTotal / dcCantidad : 0;
            strMovFecha = movFecha;
            dcCantidad = ConsumoCantidad;
            dcConsumoTotal = ConsumoTotal;
            dcPrecioProm = PrecioProm;
        }
    }
}
