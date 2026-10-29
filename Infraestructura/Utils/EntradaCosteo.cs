using CostManagement.Dominio.Entidades;
using CostManagement.Aplicación.DTos;
using CostManagementService.Dominio.Entidades;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CostManagement.Infraestructura.Utils
{
    // copias de trabajo y orden total SOLO en las entradas de costeo.
    // Este contrato es interno: incluye JsonIgnore/claves y no modifica el JSON de la API.
    internal static class EntradaCosteo
    {
        private static readonly JsonSerializerSettings Opciones = new()
        {
            ContractResolver = new ContratoCompleto(),
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            DateParseHandling = DateParseHandling.None
        };

        internal static T Copiar<T>(T valor) => valor == null ? valor! :
            JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(valor, Opciones), Opciones)!;

        // Se evalúa una vez por fila, no dentro del comparador. Ante empate completo las
        // filas son indistinguibles por sus propiedades; OrderBy conserva su orden relativo.
        internal static string ClaveCompleta<T>(T valor) => JsonConvert.SerializeObject(valor, Opciones);

        internal static List<T> Ordenar<T>(IEnumerable<T> filas) =>
            filas.OrderBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        internal static List<InfoProd> Ordenar(IEnumerable<InfoProd> filas) => filas
            .OrderBy(x => x.strProCodcor, StringComparer.Ordinal)
            .ThenBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        internal static List<ProcesoResultadoDto> Ordenar(IEnumerable<ProcesoResultadoDto> filas) => filas
            .OrderBy(x => x.intCodigo).ThenBy(x => x.intCodDet)
            .ThenBy(x => x.strCodTip, StringComparer.Ordinal)
            .ThenBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        internal static List<PrecioFrsXMov> Ordenar(IEnumerable<PrecioFrsXMov> filas) => filas
            .OrderBy(x => x.intLidLote).ThenBy(x => x.intProCodcor).ThenBy(x => x.srtTcdCodtal)
            .ThenBy(x => x.strTrcTipo, StringComparer.Ordinal)
            .ThenBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        internal static List<InventarioVal> Ordenar(IEnumerable<InventarioVal> filas) => filas
            .OrderBy(x => x.intLote).ThenBy(x => x.strProCodcor, StringComparer.Ordinal).ThenBy(x => x.stTalCodigo)
            .ThenBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        internal static List<ParamRectrac> Ordenar(IEnumerable<ParamRectrac> filas) => filas
            .OrderBy(x => x.intLote).ThenBy(x => x.intCodProd).ThenBy(x => x.intCodTal)
            .ThenBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        internal static List<MatPrimaReproceso> OrdenarRepro(IEnumerable<MatPrimaReproceso> filas) => filas
            .OrderBy(x => x.intLotNumero).ThenBy(x => x.intLoteUnificado)
            .ThenBy(x => x.strAgrupacion, StringComparer.Ordinal)
            .ThenBy(x => x.intCodProd).ThenBy(x => x.intCodTal).ThenBy(x => x.intLoteOrigen)
            .ThenBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        internal static List<LiquidacionResultado> OrdenarFresco(IEnumerable<LiquidacionResultado> filas) => filas
            .OrderBy(x => x.intLote).ThenBy(x => x.intCodProd).ThenBy(x => x.intLidCodTal)
            .ThenBy(x => x.dtFechaLiq).ThenBy(x => x.intHoraLiq)
            .ThenBy(ClaveCompleta, StringComparer.Ordinal).ToList();

        private sealed class ContratoCompleto : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type tipo, MemberSerialization modo)
            {
                var propiedades = base.CreateProperties(tipo, modo);
                foreach (var propiedad in propiedades)
                {
                    propiedad.Ignored = false;
                    propiedad.PropertyName = propiedad.UnderlyingName;
                }
                return propiedades.OrderBy(x => x.PropertyName, StringComparer.Ordinal).ToList();
            }
        }
    }
}
