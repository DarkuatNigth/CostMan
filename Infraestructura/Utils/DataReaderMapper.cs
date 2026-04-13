using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Reflection;

namespace CostManagement.Infraestructura.Utils
{
    public static class DataReaderMapper
    {
        public static List<T> MapToList<T>(this DbDataReader reader) where T : new()
        {
            var entities = new List<T>();
            var props = typeof(T).GetProperties();

            // Permite múltiples columnas con el mismo nombre
            var columnLookup = Enumerable.Range(0, reader.FieldCount)
                .ToLookup(i => reader.GetName(i), i => i, StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                T obj = new T();

                foreach (var prop in props)
                {
                    var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    if (columnAttr == null)
                        continue;

                    var columnName = columnAttr.Name;

                    if (!columnLookup.Contains(columnName))
                        continue;

                    int index;

                    // si la propiedad termina en "2" usar segunda columna
                    if (prop.Name.EndsWith("2"))
                        index = columnLookup[columnName].Skip(1).FirstOrDefault();
                    else
                        index = columnLookup[columnName].First();

                    var value = reader.GetValue(index);

                    if (value == DBNull.Value)
                        continue;

                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    var safeValue = Convert.ChangeType(value, propType);

                    prop.SetValue(obj, safeValue);
                }

                entities.Add(obj);
            }

            return entities;
        }
    }
}