using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using Parquet;               // Librería oficial 6.x
using Parquet.Serialization; // Necesario para ParquetSerializer

namespace CostManagementService.Infraestructura.Utils
{
    public class AlmacenAnaliticoDirecto
    {
        /// <summary>
        /// Consulta datos de Parquet pasándole la ruta de forma directa de tu objeto de configuración.
        /// Si se le envía un índice, busca la subcarpeta física de Spark; si no, lee la raíz.
        /// </summary>
        public async Task<List<T>> ConsultarDatosAsync<T>(string rutaBase, string? nombreIndice = null, string? valorIndice = null) where T : class, new()
        {
            var listaResultados = new List<T>();

            if (string.IsNullOrEmpty(rutaBase))
            {
                throw new ArgumentNullException(nameof(rutaBase), "🔴 Error: La ruta física del Data Warehouse no puede estar vacía.");
            }

            string rutaFinalLectura = rutaBase;

            if (!string.IsNullOrEmpty(nombreIndice) && !string.IsNullOrEmpty(valorIndice))
            {
                rutaFinalLectura = Path.Combine(rutaBase, $"{nombreIndice.ToUpper().Trim()}={valorIndice.ToUpper().Trim()}");
            }

            if (!Directory.Exists(rutaFinalLectura))
            {
                Console.WriteLine($"⚠️ La ruta física especificada no existe: {rutaFinalLectura}");
                return listaResultados;
            }

            // 1. 🎯 MAPEO ESTRICTO: Buscamos mediante Reflexión SOLO las propiedades que tengan [Column]
            PropertyInfo[] propiedades = typeof(T).GetProperties();
            var columnasConAtributo = new List<PropertyInfo>();

            foreach (var prop in propiedades)
            {
                var atributoColumna = prop.GetCustomAttribute<ColumnAttribute>();
                if (atributoColumna != null && !string.IsNullOrEmpty(atributoColumna.Name))
                {
                    columnasConAtributo.Add(prop);
                }
            }

            // Si el DTO no tiene ninguna columna decorada con [Column], salimos temprano
            if (!columnasConAtributo.Any())
            {
                Console.WriteLine($"⚠️ Advertencia: El tipo {typeof(T).Name} no tiene propiedades decoradas con [Column(Name = \"...\")].");
                return listaResultados;
            }

            // 2. Escaneo de archivos (.parquet) en la ruta final
            string[] archivosParquet = Directory.GetFiles(rutaFinalLectura, "*.parquet");

            foreach (string archivo in archivosParquet)
            {
                try
                {
                    using (Stream fileStream = File.OpenRead(archivo))
                    {
                        // 3. 🎯 SOLUCIÓN DEFINITIVA: Deserializamos usando ParquetSerializer
                        var resultadoDeserializacion = await ParquetSerializer.DeserializeAsync<T>(fileStream);

                        // Extraemos la colección real desde la propiedad interna .Data que pide la versión 6.x
                        IReadOnlyCollection<T> datosArchivo = (IReadOnlyCollection<T>)resultadoDeserializacion.Data;

                        if (datosArchivo != null && datosArchivo.Any())
                        {
                            // 4. 🎯 LIMPIEZA POST-DESERIALIZACIÓN: Forzamos el borrado de cualquier propiedad sin [Column]
                            // para que la aplicación no tome basura o mapeos erróneos como objLotkey.
                            foreach (T item in datosArchivo)
                            {
                                foreach (var prop in propiedades)
                                {
                                    // Si la propiedad NO está en nuestra lista aprobada de [Column], la ponemos en null/defecto
                                    if (!columnasConAtributo.Contains(prop) && prop.CanWrite)
                                    {
                                        // Evitamos inyectar datos en el índice de partición si coincide que no tiene el atributo
                                        if (prop.Name == nombreIndice || prop.GetCustomAttribute<ColumnAttribute>()?.Name == nombreIndice)
                                        {
                                            continue;
                                        }

                                        prop.SetValue(item, null);
                                    }
                                }

                                // 5. Inyección del índice de partición de Spark (ej: TIPOLIQ=LIQ_PFR) si corresponde
                                if (!string.IsNullOrEmpty(nombreIndice) && !string.IsNullOrEmpty(valorIndice))
                                {
                                    var propIndice = propiedades.FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Name == nombreIndice || p.Name == nombreIndice);
                                    if (propIndice != null && propIndice.CanWrite)
                                    {
                                        propIndice.SetValue(item, Convert.ChangeType(valorIndice, propIndice.PropertyType));
                                    }
                                }
                            }

                            listaResultados.AddRange(datosArchivo);
                        }
                    }
                }
                catch (Exception exArchivo)
                {
                    Console.WriteLine($"⚠️ Error procesando el archivo Parquet [{Path.GetFileName(archivo)}]: {exArchivo.Message}");
                }
            }

            return listaResultados;
        }
    }
}