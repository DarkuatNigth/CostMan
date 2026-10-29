using CostManagement.Dominio.Entidades;

namespace CostManagementService.Aplicacion.DTos
{
    public class LibrasParticionDto
    {
        public string strProceso { get; set; } // "Recepcion", "Descabezado"...
        public string strGrupo { get; set; } // opcional, si lo resuelves aquí
        public string strOrigen { get; set; } // "PFR" / "RPC"
        public string strParticionCod { get; set; } // "EN" / "SH" / "EV" / "SV"
        public string strParticion { get; set; } // "Entero" / "Cola" / ...
        public decimal dcLibras { get; set; }
        public int intOrden { get; set; } // ParticionCosteo.Orden
    }
    public static class ParticionCosteo
    {
        public const string ENTERO = "EN";
        public const string COLA = "SH";
        public const string ENTERO_VA = "EV";
        public const string COLA_VA = "SV";

        // FRS: LiquidacionResultado trae clas01 + clas05
        public static string? ClasificarFrs(string? c01, string? c05)
        {
            if (c01 == "CC" && c05 == "EN") return ENTERO;
            if (c01 == "SC" && c05 == "SH") return COLA;
            if (c01 == "CC" && c05 == "VA") return ENTERO_VA;
            if (c01 == "SC" && c05 == "VA") return COLA_VA;
            return null; // no clasificable → no entra al split
        }

        // RPC: MatPrimaReproceso ya trae strTipoProducto resuelto
        public static string? ClasificarRpc(string? strTipoProducto) =>
            strTipoProducto?.Trim().ToUpperInvariant() switch
            {
                "ENTERO" => ENTERO,
                "COLA" => COLA,
                "ENTERO VALOR AGREGADO" => ENTERO_VA,
                "COLA VALOR AGREGADO" => COLA_VA,
                _ => null
            };

        public static string Descripcion(string cod) => cod switch
        {
            ENTERO => "Entero",
            COLA => "Cola",
            ENTERO_VA => "Entero Valor Agregado",
            COLA_VA => "Cola Valor Agregado",
            _ => "Otro"
        };

        public static int Orden(string cod) => cod switch
        { ENTERO => 1, ENTERO_VA => 2, COLA => 3, COLA_VA => 4, _ => 99 };
    }
    public class AcumuladorLibrasParticion
    {
        private readonly Dictionary<(string Proceso, string Particion), decimal> _dic = new();
        // detalle paralelo; no interviene en la matriz agregada ni en el costeo.
        private readonly Dictionary<(string Proceso, string Codigo, string Nombre, string TipoProducto, string Embalaje, string Particion), decimal> _dicDetalle = new();
        private DetalleEtapaProcesoDto _objDetalle = new();

        public void EstablecerDetalle(object item)
        {
            if (item is MatPrimaReproceso objRpc)
            {
                string strCodigo = (objRpc.strTipCod ?? string.Empty).Trim();
                _objDetalle = new DetalleEtapaProcesoDto
                {
                    strProcesoCodigo = strCodigo,
                    strProcesoProductivo = string.IsNullOrWhiteSpace(objRpc.strTipDescri) ? strCodigo : objRpc.strTipDescri.Trim(),
                    strTipoProducto = (objRpc.strTipoProducto ?? string.Empty).Trim(),
                    strTipoEmbalaje = objRpc.strTipEmbala ?? string.Empty
                };
            }
            else if (item is LiquidacionResultado objFrs)
            {
                string strCodigo = string.IsNullOrWhiteSpace(objFrs.strTipoLiq) ? "LIQ_PFR" : objFrs.strTipoLiq.Trim();
                _objDetalle = new DetalleEtapaProcesoDto { strProcesoCodigo = strCodigo, strProcesoProductivo = strCodigo };
            }
        }

        public List<DetalleEtapaProcesoDto> AListaDetalle(string strOrigen) => _dicDetalle.Select(x => new DetalleEtapaProcesoDto
        {
            strEtapa = x.Key.Proceso, strOrigen = strOrigen, strProcesoCodigo = x.Key.Codigo,
            strProcesoProductivo = x.Key.Nombre, strTipoProducto = x.Key.TipoProducto, strTipoEmbalaje = x.Key.Embalaje,
            strParticionCod = x.Key.Particion, strParticion = ParticionCosteo.Descripcion(x.Key.Particion), dcLibras = x.Value
        }).ToList();

        public void Acumular(string strProceso, string? strParticion, decimal dcLibras)
        {
            if (string.IsNullOrEmpty(strParticion)) return; // no clasificable
            var key = (strProceso.Trim(), strParticion);
            _dic[key] = _dic.TryGetValue(key, out var v) ? v + dcLibras : dcLibras;
            string strEmbalaje = string.Equals(strProceso.Trim(), "Brine", StringComparison.OrdinalIgnoreCase) ? _objDetalle.strTipoEmbalaje : string.Empty;
            var objClave = (strProceso.Trim(), _objDetalle.strProcesoCodigo, _objDetalle.strProcesoProductivo,
                string.IsNullOrWhiteSpace(_objDetalle.strTipoProducto) ? ParticionCosteo.Descripcion(strParticion) : _objDetalle.strTipoProducto, strEmbalaje, strParticion);
            _dicDetalle[objClave] = _dicDetalle.TryGetValue(objClave, out decimal dcActual) ? dcActual + dcLibras : dcLibras;
        }

        // Reemplaza cada .Where().Sum(): suma un origen agrupando por partición
        public void AcumularDesde<T>(string strProceso, IEnumerable<T> lstItems,
            Func<T, string?> fnParticion, Func<T, decimal> fnLibras)
        {
            foreach (var item in lstItems)
            {
                EstablecerDetalle(item);
                Acumular(strProceso, fnParticion(item), fnLibras(item));
            }
        }

        // Libras compartidas: Clasificacion/Cajas heredan el split de Recepcion
        public void Copiar(string strOrigen, string strDestino)
        {
            foreach (var kv in _dic.Where(k => k.Key.Proceso == strOrigen.Trim()).ToList())
                Acumular(strDestino, kv.Key.Particion, kv.Value);
        }

        public decimal TotalProceso(string strProceso)
            => _dic.Where(kv => kv.Key.Proceso == strProceso.Trim()).Sum(kv => kv.Value);

        public List<LibrasParticionDto> AListaDto(string strOrigen)
            => _dic.Select(kv => new LibrasParticionDto
            {
                strProceso = kv.Key.Proceso,
                strOrigen = strOrigen,
                strParticionCod = kv.Key.Particion,
                strParticion = ParticionCosteo.Descripcion(kv.Key.Particion),
                intOrden = ParticionCosteo.Orden(kv.Key.Particion),
                dcLibras = kv.Value
            }).ToList();
    }
}
