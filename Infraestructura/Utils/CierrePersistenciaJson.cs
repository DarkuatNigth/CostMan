using CostManagement.Aplicación.DTos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace CostManagement.Infraestructura.Utils;

// contrato interno; nunca serializar navegaciones EF.
internal sealed class CierreJsonFila
{
    public int idFilaCliente { get; set; }
    public int id { get; set; }
    public bool anular { get; set; }
    public JObject cabecera => fnCabecera?.Invoke() ?? new JObject();
    [JsonIgnore] public Func<JObject>? fnCabecera { get; set; }
    public List<CierreDetalleDto> detalles { get; set; } = new();
    [JsonIgnore] public Action<int, decimal?, bool>? fnAplicar { get; set; }
}

internal sealed class CierreJsonOpciones
{
    public bool blUsarSp { get; }
    public int intBloque { get; }
    public int intTimeout { get; }
    public CierreJsonOpciones(IConfiguration? objConfiguracion)
    {
        blUsarSp = objConfiguracion?.GetValue<bool?>("Cierre:UsarSpJson") ?? true;
        intBloque = objConfiguracion?.GetValue<int?>("Cierre:CabecerasPorBloque") ?? 5000;
        intTimeout = objConfiguracion?.GetValue<int?>("Cierre:CommandTimeoutSegundos") ?? 900;
        if (blUsarSp && (intBloque <= 0 || intTimeout <= 0))
            throw new InvalidOperationException("Cierre: bloque y CommandTimeout deben ser positivos.");
    }
}

internal sealed record CierreJsonResumen(int intInsertados, int intActualizados, int intAnulados,
    int intDetallesAnulados, int intDetallesInsertados);
internal sealed record CierreJsonMapa(int intFila, int intId, decimal? dcUnitario, bool blManual);

internal static class CierrePersistenciaJson
{
    internal const string strInstalar = @"Falta instalar costos.sp_guardarValorizacionCierre (Recursos\SGCAM\09_SP_GUARDAR_VALORIZACION_CIERRE.sql)";
    internal static Exception ErrorSql(int intNumero, Exception objError) => intNumero == 2812
        ? new InvalidOperationException(strInstalar, objError) : objError;

    internal static JObject Cabecera<T>(T objCabecera) where T : class
    {
        var objJson = new JObject();
        foreach (var objPropiedad in typeof(T).GetProperties().Where(x => x.GetCustomAttribute<ColumnAttribute>() != null))
        {
            object? objValor = objPropiedad.GetValue(objCabecera);
            objJson[objPropiedad.Name] = objValor is DateOnly dtFecha
                ? new JValue(dtFecha.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
                : objValor == null ? JValue.CreateNull() : JToken.FromObject(objValor);
        }
        return objJson;
    }

    // Orden de entrada estable, IDs existentes por PK/ordinal; sobrantes al final por PK.
    internal static List<CierreJsonFila> Emparejar<TLinea, TCabecera, TClave>(
        IEnumerable<TLinea> lstLineas, IEnumerable<TCabecera> lstExistentes,
        Func<TLinea, TCabecera> fnCabecera, Func<TCabecera, TClave> fnClave, Func<TCabecera, int> fnId,
        Func<TLinea, List<CierreDetalleDto>> fnDetalles, Action<TLinea, int, decimal?, bool> fnAplicar)
        where TCabecera : class where TClave : notnull
    {
        var dicColas = lstExistentes.OrderBy(fnId).GroupBy(fnClave).ToDictionary(x => x.Key, x => new Queue<TCabecera>(x));
        var lstFilas = new List<CierreJsonFila>();
        foreach (var objLinea in lstLineas)
        {
            var objCabecera = fnCabecera(objLinea);
            int intId = dicColas.TryGetValue(fnClave(objCabecera), out var objCola) && objCola.Count > 0 ? fnId(objCola.Dequeue()) : 0;
            lstFilas.Add(new CierreJsonFila { idFilaCliente = lstFilas.Count + 1, id = intId,
                fnCabecera = () => Cabecera(objCabecera), detalles = fnDetalles(objLinea),
                fnAplicar = (intNuevoId, dcUnitario, blManual) => fnAplicar(objLinea, intNuevoId, dcUnitario, blManual) });
        }
        foreach (var objSobrante in dicColas.Values.SelectMany(x => x).OrderBy(fnId))
            lstFilas.Add(new CierreJsonFila { idFilaCliente = lstFilas.Count + 1, id = fnId(objSobrante), anular = true });
        return lstFilas;
    }

    internal static IEnumerable<CierreJsonFila[]> Bloques(IEnumerable<CierreJsonFila> lstFilas, int intTamano)
        => lstFilas.Chunk(intTamano);
    internal static string Serializar(CierreJsonFila[] lstBloque) => JsonConvert.SerializeObject(lstBloque, Formatting.None);

    internal static void Conciliar(CierreJsonFila[] lstBloque, CierreJsonResumen objResumen, List<CierreJsonMapa> lstMapa)
    {
        if (objResumen.intInsertados != lstBloque.Count(x => !x.anular && x.id == 0) ||
            objResumen.intActualizados != lstBloque.Count(x => !x.anular && x.id > 0) ||
            objResumen.intAnulados != lstBloque.Count(x => x.anular) ||
            objResumen.intDetallesInsertados != lstBloque.Sum(x => x.detalles.Count) || objResumen.intDetallesAnulados < 0 ||
            lstMapa.Count != lstBloque.Length || lstMapa.Select(x => x.intFila).Distinct().Count() != lstMapa.Count ||
            lstMapa.Select(x => x.intId).Distinct().Count() != lstMapa.Count || lstMapa.Any(x => x.intId <= 0 || (x.blManual && !x.dcUnitario.HasValue)))
            throw new InvalidOperationException("No concilia el resumen o mapa de IDs del bloque SP JSON.");
        var dicMapa = lstMapa.ToDictionary(x => x.intFila);
        foreach (var objFila in lstBloque)
            if (!dicMapa.TryGetValue(objFila.idFilaCliente, out var objMapa) || (objFila.id > 0 && objFila.id != objMapa.intId))
                throw new InvalidOperationException("No concilia la identidad de cabecera del bloque SP JSON.");
    }

    internal static async Task<CierreJsonResumen> EjecutarBloquesAsync(List<CierreJsonFila> lstFilas, int intTamano,
        Func<CierreJsonFila[], Task<(CierreJsonResumen, List<CierreJsonMapa>)>> fnEjecutar)
    {
        var objTotal = new CierreJsonResumen(0, 0, 0, 0, 0);
        foreach (var lstBloque in Bloques(lstFilas, intTamano))
        {
            var (objResumen, lstMapa) = await fnEjecutar(lstBloque);
            Conciliar(lstBloque, objResumen, lstMapa);
            var dicMapa = lstMapa.ToDictionary(x => x.intFila);
            foreach (var objFila in lstBloque)
            {
                var objMapa = dicMapa[objFila.idFilaCliente];
                objFila.fnAplicar?.Invoke(objMapa.intId, objMapa.dcUnitario, objMapa.blManual);
            }
            objTotal = new(objTotal.intInsertados + objResumen.intInsertados, objTotal.intActualizados + objResumen.intActualizados,
                objTotal.intAnulados + objResumen.intAnulados, objTotal.intDetallesAnulados + objResumen.intDetallesAnulados,
                objTotal.intDetallesInsertados + objResumen.intDetallesInsertados);
        }
        return objTotal;
    }
}
