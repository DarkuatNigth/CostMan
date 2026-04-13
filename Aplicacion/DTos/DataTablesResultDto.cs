using CostManagement.Infraestructura.Utils;
using Newtonsoft.Json;
using System.Data;
using System.Text.Json.Serialization;

namespace CostManagement.Aplicación.DTos
{
    public class DataTablesResultDto
    {
            [JsonProperty("Table")]
            public List<Dictionary<string, object>>? Table { get; set; }

            [JsonProperty("Table1", NullValueHandling = NullValueHandling.Ignore)]
            public List<Dictionary<string, object>>? Table1 { get; set; }

            [JsonProperty("Table2", NullValueHandling = NullValueHandling.Ignore)]
            public List<Dictionary<string, object>>? Table2 { get; set; }

            /// <summary>
            /// Crea resultado desde DataTable
            /// </summary>
            public static DataTablesResultDto FromDataTable(DataTable dt, int tableIndex = 0)
            {
                var result = new DataTablesResultDto();
                var dataList = ConvertDataTableToList(dt);

                switch (tableIndex)
                {
                    case 0:
                        result.Table = dataList;
                        break;
                    case 1:
                        result.Table1 = dataList;
                        break;
                    case 2:
                        result.Table2 = dataList;
                        break;
                }

                return result;
            }

            /// <summary>
            /// Crea resultado desde List<T> usando reflexión y atributos
            /// </summary>
            public static DataTablesResultDto FromList<T>(List<T> data, int tableIndex = 0)
            {
                var result = new DataTablesResultDto();
                var dataList = data.AListaDeDiccionarios(); // Usa el nuevo método helper

                switch (tableIndex)
                {
                    case 0:
                        result.Table = dataList;
                        break;
                    case 1:
                        result.Table1 = dataList;
                        break;
                    case 2:
                        result.Table2 = dataList;
                        break;
                }

                return result;
            }

            /// <summary>
            /// Crea resultado desde List<T> convirtiéndolo primero a DataTable
            /// </summary>
            public static DataTablesResultDto FromListViaDataTable<T>(List<T> data, int tableIndex = 0)
            {
                var dataTable = data.ADataTable(); // Usa el ADataTable que respeta atributos
            return FromDataTable(dataTable, tableIndex);
            }

            /// <summary>
            /// Convierte DataTable a List<Dictionary>
            /// </summary>
            private static List<Dictionary<string, object>> ConvertDataTableToList(DataTable dt)
            {
                var rows = new List<Dictionary<string, object>>();

                foreach (DataRow dr in dt.Rows)
                {
                    var row = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        row[col.ColumnName] = dr[col] == DBNull.Value ? null : dr[col];
                    }
                    rows.Add(row);
                }

                return rows;
            }
        }
    }
