using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Newtonsoft.Json;
using System.Data;
using CostManagement.Aplicación.DTos;
using CostManagement.Dominio.Entidades;
using CostManagement.Dominio.Reglas;
using CostManagementService.Aplicacion.DTos;

namespace CostManagement.Infraestructura.Utils;

// proyección exclusiva de reportes MP; los conversores globales no cambian.
public static class ReporteMateriaPrimaHelper
{
    public static void Preparar<T>(List<T> lstFilas, ConfiguracionCostoProductivoRuntimeDto? objConfiguracion)
    {
        var lstProcesos = objConfiguracion?.lstProcesos.Where(CatalogoProcesosDinamicos.EsDinamico).ToList()
            ?? new List<ProcesoCostoMantenimientoDto>();
        // Validar el catálogo completo antes de proyectar cualquier fila.
        var dicGrupos = Grupos(typeof(T));
        var hshNombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hshExistentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (typeof(T) == typeof(MatPrimaReproceso)) hshExistentes.UnionWith(new[] { "Origen Costo", "Usado Como Fuente",
            "Costo Pond Historico", "Nivel Historico", "Costo Referencial", "Libras Consumidas", "Libras Disponibles", "Bloqueado Historico", "Libras Tomadas", "Lote Usable" });
        if (typeof(T) == typeof(MatPrimaReproceso)) hshExistentes.UnionWith(new[] { "Excluido Costeo", "Tipo Proceso Cod", "Clase Prod Norm" });
        foreach (var objProp in typeof(T).GetProperties().Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null))
        {
            hshExistentes.Add((objProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? objProp.Name).Trim());
            if (!objProp.PropertyType.IsClass || objProp.PropertyType == typeof(string)) continue;
            foreach (var objHija in objProp.PropertyType.GetProperties())
            {
                hshExistentes.Add((objHija.GetCustomAttribute<ColumnAttribute>()?.Name ?? objHija.Name).Trim());
                hshExistentes.Add((objHija.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? objHija.Name).Trim());
            }
        }
        foreach (var objProceso in lstProcesos)
        {
            string strNombre = objProceso.strNombre ?? string.Empty;
            if (string.IsNullOrWhiteSpace(strNombre) || !hshNombres.Add(strNombre.Trim()) || hshExistentes.Contains(strNombre.Trim()))
                throw new InvalidOperationException($"Nombre de proceso dinámico vacío o duplicado: '{strNombre}'.");
            if (!dicGrupos.ContainsKey(Grupo(objProceso.strEtapaCodigo)))
                throw new InvalidOperationException($"Etapa de proceso sin grupo de reporte: '{objProceso.strEtapaCodigo}'.");
        }
        foreach (var objFila in lstFilas)
        {
            var dicMontos = objFila switch
            {
                LiquidacionResultado obj => obj.ProcesosAdicionales,
                MatPrimaReproceso obj => obj.ProcesosAdicionales,
                _ => throw new ArgumentException("El reporte requiere filas de materia prima.")
            };
            var dicVista = lstProcesos.Count == 0 ? null : lstProcesos.GroupBy(obj => Grupo(obj.strEtapaCodigo))
                .ToDictionary(objGrupo => objGrupo.Key, objGrupo => objGrupo.ToDictionary(
                    obj => obj.strNombre, obj => dicMontos.GetValueOrDefault(obj.strCodigo.Trim().ToUpperInvariant())));
            if (objFila is LiquidacionResultado objFresco) objFresco.dicProcesosReporte = dicVista;
            if (objFila is MatPrimaReproceso objRepro) objRepro.dicProcesosReporte = dicVista;
        }
    }

    public static List<Dictionary<string, object>> AReporteMateriaPrima<T>(this List<T> lstFilas)
    {
        var lstReporte = lstFilas.AListaDeDiccionarios();
        var dicGrupos = Grupos(typeof(T));
        for (int i = 0; i < lstFilas.Count; i++)
        {
            if (lstFilas[i] is MatPrimaReproceso objRepro)
            {
                // Proyectar al final, incluso sin procesos dinámicos y sin depender de atributos en la entidad.
                var dicFila = lstReporte[i].Where(x => x.Key != nameof(MatPrimaReproceso.strOrigenCosto) && x.Key != nameof(MatPrimaReproceso.blUsadoComoFuente)
                    && x.Key != "Origen Costo" && x.Key != "Usado Como Fuente").ToDictionary(x => x.Key, x => x.Value);
                dicFila.Add("Origen Costo", objRepro.strOrigenCosto);
                dicFila.Add("Usado Como Fuente", objRepro.blUsadoComoFuente);
                lstReporte[i] = dicFila;
            }
            var dicVista = Vista(lstFilas[i]);
            if (dicVista == null) continue;
            foreach (var objGrupo in dicVista)
            {
                var objOriginal = lstReporte[i][objGrupo.Key];
                var objPropGrupo = dicGrupos[objGrupo.Key];
                // conservar los nombres JSON originales (CD/CI difieren de Excel).
                var dicGrupo = objPropGrupo.PropertyType.GetProperties().ToDictionary(
                    p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name,
                    p => objOriginal == null ? null : p.GetValue(objOriginal));
                foreach (var objMonto in objGrupo.Value) dicGrupo.Add(objMonto.Key, objMonto.Value);
                lstReporte[i][objGrupo.Key] = dicGrupo;
            }
        }
        return lstReporte;
    }

    public static DataTable ATablaReporteMateriaPrima<T>(this List<T> lstFilas)
    {
        var objTabla = lstFilas.ADataTable();
        var dicVista = lstFilas.Count == 0 ? null : Vista(lstFilas[0]);
        var dicColumnas = dicVista?.ToDictionary(obj => obj.Key, obj => obj.Value.Keys.ToArray()) ?? new();
        objTabla.ExtendedProperties["Grupos"] = dicColumnas;
        if (dicVista == null) return AgregarHistoricoExcel(objTabla, lstFilas);
        foreach (string strNombre in dicColumnas.Values.SelectMany(arr => arr))
            objTabla.Columns.Add(strNombre, typeof(decimal));
        var arrOrden = OrdenarColumnas(typeof(T), objTabla.Columns.Cast<DataColumn>().Select(obj => obj.ColumnName), dicColumnas);
        for (int i = 0; i < arrOrden.Length; i++) objTabla.Columns[arrOrden[i]]!.SetOrdinal(i);
        for (int i = 0; i < lstFilas.Count; i++)
        {
            foreach (var objMonto in Vista(lstFilas[i])!.Values.SelectMany(dic => dic))
                objTabla.Rows[i][objMonto.Key] = objMonto.Value;
        }
        return AgregarHistoricoExcel(objTabla, lstFilas);
    }

    private static DataTable AgregarHistoricoExcel<T>(DataTable objTabla, List<T> lstFilas)
    {
        if (typeof(T) != typeof(MatPrimaReproceso)) return objTabla;
        // Solo Excel: agregar al final, después de ordenar los procesos dinámicos, incluso sin filas.
        objTabla.Columns.Add("Costo Pond Historico", typeof(decimal));
        objTabla.Columns.Add("Nivel Historico", typeof(int));
        objTabla.Columns.Add("Costo Referencial", typeof(bool));
        objTabla.Columns.Add("Libras Consumidas", typeof(double));
        objTabla.Columns.Add("Libras Disponibles", typeof(double));
        objTabla.Columns.Add("Bloqueado Historico", typeof(bool));
        objTabla.Columns.Add("Libras Tomadas", typeof(double));
        objTabla.Columns.Add("Lote Usable", typeof(bool));
        bool blAgregarExcluido = !objTabla.Columns.Contains("Excluido Costeo") && !objTabla.Columns.Contains(nameof(MatPrimaReproceso.blExcluidoCosteo));
        bool blAgregarTipo = !objTabla.Columns.Contains("Tipo Proceso Cod") && !objTabla.Columns.Contains(nameof(MatPrimaReproceso.strTipCod));
        bool blAgregarClase = !objTabla.Columns.Contains("Clase Prod Norm") && !objTabla.Columns.Contains("Clase Prod") && !objTabla.Columns.Contains(nameof(MatPrimaReproceso.strClaseProd));
        if (blAgregarExcluido) objTabla.Columns.Add("Excluido Costeo", typeof(bool));
        if (blAgregarTipo) objTabla.Columns.Add("Tipo Proceso Cod", typeof(string));
        if (blAgregarClase) objTabla.Columns.Add("Clase Prod Norm", typeof(string));
        for (int i = 0; i < lstFilas.Count; i++)
        {
            var objRepro = (MatPrimaReproceso)(object)lstFilas[i]!;
            objTabla.Rows[i]["Costo Pond Historico"] = objRepro.dcCostoPondHistorico;
            objTabla.Rows[i]["Nivel Historico"] = objRepro.intNivelHistorico;
            objTabla.Rows[i]["Costo Referencial"] = objRepro.blCostoReferencial;
            objTabla.Rows[i]["Libras Consumidas"] = objRepro.dbLibrasConsumidas;
            objTabla.Rows[i]["Libras Disponibles"] = objRepro.dbLibrasDisponibles;
            objTabla.Rows[i]["Bloqueado Historico"] = objRepro.blBloqueadoHistorico;
            objTabla.Rows[i]["Libras Tomadas"] = objRepro.dbLibrasTomadas;
            objTabla.Rows[i]["Lote Usable"] = objRepro.blLoteUsable;
            if (blAgregarExcluido) objTabla.Rows[i]["Excluido Costeo"] = objRepro.blExcluidoCosteo;
            if (blAgregarTipo) objTabla.Rows[i]["Tipo Proceso Cod"] = (object?)objRepro.strTipCod ?? DBNull.Value;
            if (blAgregarClase) objTabla.Rows[i]["Clase Prod Norm"] = (object?)(objRepro.strClaseProd switch
                { "A" or "A+" => "A", "B" or "N" => "B", _ => objRepro.strClaseProd }) ?? DBNull.Value;
        }
        return objTabla;
    }

    private static Dictionary<string, Dictionary<string, decimal>>? Vista<T>(T objFila) => objFila switch
    {
        LiquidacionResultado obj => obj.dicProcesosReporte,
        MatPrimaReproceso obj => obj.dicProcesosReporte,
        _ => null
    };
    // Insertar después de la última columna seleccionada de cada etapa;
    // conservar exactamente el orden relativo de las columnas preexistentes.
    public static string[] OrdenarColumnas(Type objTipo, IEnumerable<string> lstColumnas, Dictionary<string, string[]> dicDinamicas)
    {
        var hshDinamicas = dicDinamicas.Values.SelectMany(arr => arr).ToHashSet();
        var lstOrden = lstColumnas.Where(str => str != "Otros Procesos" && str != "Procesos Adicionales" && !hshDinamicas.Contains(str)).ToList();
        foreach (var objGrupo in Grupos(objTipo))
        {
            if (!dicDinamicas.TryGetValue(objGrupo.Key, out var arrNombres)) continue;
            var hshHijas = objGrupo.Value.PropertyType.GetProperties()
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name).ToHashSet();
            int intUltima = lstOrden.FindLastIndex(str => hshHijas.Contains(str));
            lstOrden.InsertRange(intUltima < 0 ? lstOrden.Count : intUltima + 1, arrNombres);
        }
        return lstOrden.ToArray();
    }

    private static Dictionary<string, PropertyInfo> Grupos(Type objTipo) => objTipo.GetProperties()
        .Where(p => p.PropertyType.IsClass && p.PropertyType != typeof(string)
            && p.GetCustomAttribute<JsonIgnoreAttribute>() == null && p.GetCustomAttribute<ColumnAttribute>() != null)
        .ToDictionary(p => p.GetCustomAttribute<ColumnAttribute>()!.Name!, p => p);

    private static string Grupo(string? strEtapa) => strEtapa?.Trim().ToUpperInvariant() switch
    {
        "PP" => "Proceso Primario", "PS" => "Proceso Secundario",
        "PR" => "Proceso Presentacion", "PC" => "Proceso Congelacion",
        "CD" => "Costos Directos", "CI" => "Costos Indirectos",
        _ => throw new InvalidOperationException($"Etapa de proceso sin grupo de reporte: '{strEtapa}'.")
    };
}
