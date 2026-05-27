using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

namespace CostManagement.Infraestructura.Utils
{
    public static class DataReaderMapper
    {
        // Nuevo atributo — crear en una carpeta de Utils o Attributes
        [AttributeUsage(AttributeTargets.Property)]
        public class ColumnOccurrenceAttribute : Attribute
        {
            public int Occurrence { get; }
            public ColumnOccurrenceAttribute(int occurrence) => Occurrence = occurrence;
        }
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

                    var occurrenceAttr = prop.GetCustomAttribute<ColumnOccurrenceAttribute>();
                    int skip = (occurrenceAttr != null && columnLookup[columnName].Count() > occurrenceAttr.Occurrence)
                        ? occurrenceAttr.Occurrence
                        : 0;
                    index = columnLookup[columnName].Skip(skip).First();

                    var value = reader.GetValue(index);

                    if (value == DBNull.Value)
                        continue;

                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    // Agregar esto antes del Convert.ChangeType:
                    if (propType == typeof(DateOnly) && value is DateTime dt)
                    {
                        prop.SetValue(obj, DateOnly.FromDateTime(dt));
                        continue;
                    }

                    var safeValue = Convert.ChangeType(value, propType);

                    prop.SetValue(obj, safeValue);
                }

                entities.Add(obj);
            }

            return entities;
        }
    }
}