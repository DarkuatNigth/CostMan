using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Newtonsoft.Json;
using System.Reflection;

namespace CostManagement.Infraestructura.Utils
{

    public static class DataTableHelper
    {
        /// <summary>
        /// Convierte List<T> a DataTable respetando atributos [Column] y [JsonIgnore]
        /// </summary>
        //public static DataTable ADataTable<T>(this List<T> objData)
        //{
        //    var lstProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        //    DataTable objTable = new DataTable();

        //    foreach (var objProp in lstProperties)
        //    {
        //        // Ignorar propiedades con [JsonIgnore]
        //        if (objProp.GetCustomAttribute<JsonIgnoreAttribute>() != null)
        //        {
        //            continue;
        //        }

        //        // Obtener el nombre de columna del atributo [Column] o usar el nombre de la propiedad
        //        string strNombreColum = ObtenerNombreColumna(objProp);

        //        // Manejar tipos nullable
        //        Type tpTipoColumn = objProp.PropertyType;
        //        if (tpTipoColumn.IsGenericType && tpTipoColumn.GetGenericTypeDefinition() == typeof(Nullable<>))
        //        {
        //            tpTipoColumn = Nullable.GetUnderlyingType(tpTipoColumn);
        //        }

        //        // Manejar DateOnly -> convertir a DateTime para DataTable
        //        if (tpTipoColumn == typeof(DateOnly))
        //        {
        //            tpTipoColumn = typeof(DateTime);
        //        }

        //        objTable.Columns.Add(strNombreColum, tpTipoColumn ?? typeof(object));
        //    }

        //    foreach (T item in objData)
        //    {
        //        DataRow dtFila = objTable.NewRow();

        //        foreach (var objProp in lstProperties)
        //        {
        //            // Ignorar propiedades con [JsonIgnore]
        //            if (objProp.GetCustomAttribute<JsonIgnoreAttribute>() != null)
        //            {
        //                continue;
        //            }

        //            string strNombreColum = ObtenerNombreColumna(objProp);
        //            object objValor = objProp.GetValue(item);

        //            // Convertir DateOnly a DateTime
        //            if (objValor != null && objValor.GetType() == typeof(DateOnly))
        //            {
        //                objValor = ((DateOnly)objValor).ToDateTime(TimeOnly.MinValue);
        //            }
        //            else if (objValor != null && objValor.GetType() == typeof(DateOnly?))
        //            {
        //                var dateOnlyValue = (DateOnly?)objValor;
        //                objValor = dateOnlyValue?.ToDateTime(TimeOnly.MinValue);
        //            }

        //            dtFila[strNombreColum] = objValor ?? DBNull.Value;
        //        }

        //        objTable.Rows.Add(dtFila);
        //    }

        //    return objTable;
        //}
        public static DataTable ADataTable<T>(this List<T> objData)
        {
            try
            {

                var lstProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                DataTable objTable = new DataTable();

                // GUARDAR TIPO PARA EL SERVICE (Para que sea anónimo)
                objTable.ExtendedProperties["SourceType"] = typeof(T);

                // 1. DEFINICIÓN DE COLUMNAS
                foreach (var objProp in lstProperties)
                {
                    if (objProp.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

                    // --- CAMBIO MÍNIMO: SOPORTE PARA OBJETOS ANIDADOS ---
                    if (objProp.PropertyType.IsClass && objProp.PropertyType != typeof(string))
                    {
                        var subProps = objProp.PropertyType.GetProperties();
                        foreach (var sp in subProps)
                        {
                            string nombreSub = sp.GetCustomAttribute<ColumnAttribute>()?.Name ?? sp.Name;
                            Type tpSub = Nullable.GetUnderlyingType(sp.PropertyType) ?? sp.PropertyType;
                            if (tpSub == typeof(DateOnly)) tpSub = typeof(DateTime);
                            objTable.Columns.Add(nombreSub, tpSub);
                        }
                    }
                    else
                    {
                        // Lógica original para tipos simples
                        string strNombreColum = ObtenerNombreColumna(objProp);
                        Type tpTipoColumn = Nullable.GetUnderlyingType(objProp.PropertyType) ?? objProp.PropertyType;
                        if (tpTipoColumn == typeof(DateOnly)) tpTipoColumn = typeof(DateTime);
                        objTable.Columns.Add(strNombreColum, tpTipoColumn ?? typeof(object));
                    }
                }

                // 2. LLENADO DE FILAS
                foreach (T item in objData)
                {
                    DataRow dtFila = objTable.NewRow();
                    foreach (var objProp in lstProperties)
                    {
                        if (objProp.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

                        // --- CAMBIO MÍNIMO: VALORES DE OBJETOS ANIDADOS ---
                        if (objProp.PropertyType.IsClass && objProp.PropertyType != typeof(string))
                        {
                            var subObjeto = objProp.GetValue(item);
                            foreach (var sp in objProp.PropertyType.GetProperties())
                            {
                                string nombreSub = sp.GetCustomAttribute<ColumnAttribute>()?.Name ?? sp.Name;
                                object valSub = subObjeto != null ? sp.GetValue(subObjeto) : null;

                                // Conversión DateOnly (tu lógica original)
                                if (valSub is DateOnly d) valSub = d.ToDateTime(TimeOnly.MinValue);
                                dtFila[nombreSub] = valSub ?? DBNull.Value;
                            }
                        }
                        else
                        {
                            // Lógica original
                            string strNombreColum = ObtenerNombreColumna(objProp);
                            object objValor = objProp.GetValue(item);
                            if (objValor is DateOnly d) objValor = d.ToDateTime(TimeOnly.MinValue);
                            dtFila[strNombreColum] = objValor ?? DBNull.Value;
                        }
                    }
                    objTable.Rows.Add(dtFila);
                }

                return objTable;
            }
            catch(Exception objException)
            {
                Console.WriteLine($"Error al convertir la lista a datatable: ${objException.Message} ");
                throw;
            }
        }


        /// <summary>
        /// Convierte List<T> a DataTable con mapeo personalizado, respetando [Column] y [JsonIgnore]
        /// </summary>
        public static DataTable MapearADataTable<T>(
            this List<T> objData,
            Dictionary<string, string>? dcPropiedadColumMap = null)
        {
            var lstPropiedad = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            DataTable dtTabla = new DataTable();

            foreach (var objProp in lstPropiedad)
            {
                // Ignorar propiedades con [JsonIgnore]
                if (objProp.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }

                // Prioridad: mapeo personalizado > atributo [Column] > nombre de propiedad
                string strNombreColum = ObtenerNombreColumnaConMapeo(objProp, dcPropiedadColumMap);

                Type tpTipoColumn = objProp.PropertyType;
                if (tpTipoColumn.IsGenericType && tpTipoColumn.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    tpTipoColumn = Nullable.GetUnderlyingType(tpTipoColumn);
                }

                // Manejar DateOnly
                if (tpTipoColumn == typeof(DateOnly))
                {
                    tpTipoColumn = typeof(DateTime);
                }

                dtTabla.Columns.Add(strNombreColum, tpTipoColumn ?? typeof(object));
            }

            foreach (T item in objData)
            {
                DataRow dtFila = dtTabla.NewRow();

                foreach (var objProp in lstPropiedad)
                {
                    // Ignorar propiedades con [JsonIgnore]
                    if (objProp.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    {
                        continue;
                    }

                    string strNombreColum = ObtenerNombreColumnaConMapeo(objProp, dcPropiedadColumMap);
                    object objValor = objProp.GetValue(item);

                    // Convertir DateOnly a DateTime
                    if (objValor != null)
                    {
                        if (objValor.GetType() == typeof(DateOnly))
                        {
                            objValor = ((DateOnly)objValor).ToDateTime(TimeOnly.MinValue);
                        }
                        else if (objValor.GetType() == typeof(DateOnly?))
                        {
                            var dtdateOnlyValor = (DateOnly?)objValor;
                            objValor = dtdateOnlyValor?.ToDateTime(TimeOnly.MinValue);
                        }
                    }

                    dtFila[strNombreColum] = objValor ?? DBNull.Value;
                }

                dtTabla.Rows.Add(dtFila);
            }

            return dtTabla;
        }

        /// <summary>
        /// Obtiene el nombre de columna desde el atributo [Column] o usa el nombre de la propiedad
        /// </summary>
        private static string ObtenerNombreColumna(PropertyInfo objProp)
        {
            var strAtributoColumn = objProp.GetCustomAttribute<ColumnAttribute>();
            return strAtributoColumn?.Name ?? objProp.Name;
        }

        /// <summary>
        /// Obtiene el nombre de columna con prioridad: mapeo manual > [Column] > nombre propiedad
        /// </summary>
        private static string ObtenerNombreColumnaConMapeo(
            PropertyInfo objProp,
            Dictionary<string, string>? dcMapaPropiedadAColumna)
        {
            // Prioridad 1: Mapeo manual
            if (dcMapaPropiedadAColumna != null && dcMapaPropiedadAColumna.ContainsKey(objProp.Name))
            {
                return dcMapaPropiedadAColumna[objProp.Name];
            }

            // Prioridad 2: Atributo [Column]
            var columnAttr = objProp.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.Name))
            {
                return columnAttr.Name;
            }

            // Prioridad 3: Nombre de la propiedad
            return objProp.Name;
        }

        /// <summary>
        /// Convierte List<T> directamente a List<Dictionary> sin pasar por DataTable
        /// Útil para serialización JSON directa
        /// </summary>
        public static List<Dictionary<string, object>> AListaDeDiccionarios<T>(this List<T> objData)
        {
            var lstPropiedad = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var lstDiccionario = new List<Dictionary<string, object>>();

            foreach (T objItem in objData)
            {
                var objDiccionarioFila = new Dictionary<string, object>();

                foreach (var objProp in lstPropiedad)
                {
                    // Ignorar propiedades con [JsonIgnore]
                    if (objProp.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    {
                        continue;
                    }

                    string strNombreColumna = ObtenerNombreColumna(objProp);
                    object objValor = objProp.GetValue(objItem);

                    // Convertir DateOnly a string ISO para JSON
                    if (objValor != null)
                    {
                        if (objValor.GetType() == typeof(DateOnly))
                        {
                            objValor = ((DateOnly)objValor).ToString("yyyy-MM-dd");
                        }
                        else if (objValor.GetType() == typeof(DateOnly?))
                        {
                            var dtdateOnlyValor = (DateOnly?)objValor;
                            objValor = dtdateOnlyValor?.ToString("yyyy-MM-dd");
                        }
                        else if (objValor.GetType() == typeof(DateTime))
                        {
                            objValor = ((DateTime)objValor).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }

                    objDiccionarioFila[strNombreColumna] = objValor;
                }

                lstDiccionario.Add(objDiccionarioFila);
            }

            return lstDiccionario;
        }
    }
}
