using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Text.Json;

namespace CostManagement.Infraestructura.Utils
{
    public class OpenJsonToInClauseInterceptor : DbCommandInterceptor
    {
        private const int BATCH_SIZE = 2000;
        private const int SQL_SERVER_MAX_PARAMS = 2100;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            RewriteCommand(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            RewriteCommand(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void RewriteCommand(DbCommand command)
        {
            if (!command.CommandText.Contains("OPENJSON"))
                return;

            var parametrosJson = ExtraerParametrosJson(command);
            if (parametrosJson.Count == 0)
                return;

            // Calcular cuántos parámetros no-JSON ya existen
            int parametrosExistentes = command.Parameters.Count - parametrosJson.Count;

            foreach (var kvp in parametrosJson)
            {
                var paramName = kvp.Key;
                var valores = kvp.Value;

                // Calcular límite disponible
                int limiteDisponible = SQL_SERVER_MAX_PARAMS - parametrosExistentes - 50; // 50 de margen

                if (valores.Count > limiteDisponible)
                {
                    Console.WriteLine($"⚠️ WARNING: Lista '{paramName}' tiene {valores.Count} elementos.");
                    Console.WriteLine($"   Límite disponible: {limiteDisponible}. El query puede fallar.");
                    Console.WriteLine($"   Recomendación: Usar batching en la capa de aplicación.");
                }

                ReemplazarOpenJsonConIn(command, paramName, valores);
            }
        }

        private Dictionary<string, List<object>> ExtraerParametrosJson(DbCommand command)
        {
            var parametrosJson = new Dictionary<string, List<object>>();

            foreach (DbParameter param in command.Parameters)
            {
                if (param.Value == null || param.Value == DBNull.Value)
                    continue;

                var stringValue = param.Value.ToString();

                if (stringValue.StartsWith("[") && stringValue.EndsWith("]"))
                {
                    try
                    {
                        var valores = DeserializarJson(stringValue);
                        if (valores != null && valores.Count > 0)
                        {
                            parametrosJson[param.ParameterName] = valores;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return parametrosJson;
        }

        private List<object> DeserializarJson(string jsonValue)
        {
            // Orden optimizado: strings son más comunes
            try
            {
                var strings = JsonSerializer.Deserialize<List<string>>(jsonValue);
                if (strings != null) return strings.Cast<object>().ToList();
            }
            catch { }

            try
            {
                var ints = JsonSerializer.Deserialize<List<int>>(jsonValue);
                if (ints != null) return ints.Cast<object>().ToList();
            }
            catch { }

            try
            {
                var longs = JsonSerializer.Deserialize<List<long>>(jsonValue);
                if (longs != null) return longs.Cast<object>().ToList();
            }
            catch { }

            try
            {
                var decimals = JsonSerializer.Deserialize<List<decimal>>(jsonValue);
                if (decimals != null) return decimals.Cast<object>().ToList();
            }
            catch { }

            return null;
        }

        private void ReemplazarOpenJsonConIn(
            DbCommand command,
            string paramName,
            List<object> valores)
        {
            // Crear parámetros individuales
            var parametrosIn = new List<string>(valores.Count);
            var parametrosNuevos = new List<DbParameter>(valores.Count);

            for (int i = 0; i < valores.Count; i++)
            {
                var nuevoParamName = $"{paramName}__{i}";
                parametrosIn.Add(nuevoParamName);

                var nuevoParam = command.CreateParameter();
                nuevoParam.ParameterName = nuevoParamName;
                nuevoParam.Value = valores[i];
                parametrosNuevos.Add(nuevoParam);
            }

            var inClause = string.Join(", ", parametrosIn);
            var sql = command.CommandText;

            // Buscar y reemplazar OPENJSON
            string marcador = $"OPENJSON({paramName})";
            int pos = 0;
            bool reemplazado = false;

            while ((pos = sql.IndexOf(marcador, pos, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                int inStart = BuscarInAnterior(sql, pos);

                if (inStart == -1)
                {
                    pos++;
                    continue;
                }

                // Encontrar cierre - optimizado
                int parenCount = 1;
                int endPos = inStart;

                while (endPos < sql.Length && parenCount > 0)
                {
                    char c = sql[endPos];
                    if (c == '(') parenCount++;
                    else if (c == ')') parenCount--;
                    endPos++;
                }

                if (parenCount == 0)
                {
                    // Reemplazo eficiente usando indices
                    var antes = sql.Substring(0, inStart);
                    var despues = sql.Substring(endPos - 1);

                    sql = string.Concat(antes, inClause, despues);
                    reemplazado = true;

                    pos = antes.Length + inClause.Length;
                }
                else
                {
                    pos++;
                }
            }

            if (!reemplazado)
            {
                return;
            }

            // Actualizar comando
            command.CommandText = sql;

            // Remover parámetro JSON (optimizado)
            DbParameter paramToRemove = null;
            foreach (DbParameter p in command.Parameters)
            {
                if (p.ParameterName == paramName)
                {
                    paramToRemove = p;
                    break;
                }
            }

            if (paramToRemove != null)
            {
                command.Parameters.Remove(paramToRemove);
            }

            // Agregar nuevos parámetros
            foreach (var param in parametrosNuevos)
            {
                command.Parameters.Add(param);
            }
        }

        private int BuscarInAnterior(string sql, int desdePosicion)
        {
            for (int i = desdePosicion - 1; i >= 2; i--)
            {
                if (char.ToUpper(sql[i - 1]) == 'I' &&
                    char.ToUpper(sql[i]) == 'N')
                {
                    char prevChar = i >= 2 ? sql[i - 2] : ' ';
                    if (char.IsWhiteSpace(prevChar) || prevChar == ']' || prevChar == ')')
                    {
                        // Buscar "(" después de "IN"
                        int j = i + 1;
                        while (j < sql.Length && char.IsWhiteSpace(sql[j]))
                            j++;

                        if (j < sql.Length && sql[j] == '(')
                        {
                            return j + 1;
                        }
                    }
                }
            }

            return -1;
        }
    }
}