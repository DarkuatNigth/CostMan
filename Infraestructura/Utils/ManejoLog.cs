using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CostManagement.Infraestructura.Utils
{
    public static class ManejoLog<T> where T : class
    {
        // Registra el inicio de una operación.
        public static void Inicializador(ILogger<T> objLogger, string strClase, string strMetodo)
            => objLogger.LogInformation(
                "[{Clase}].[{Metodo}] Inicio de ejecución.",
                strClase, strMetodo);

        // Registra un mensaje informativo dentro de una operación.
        public static void Informacion(ILogger<T> objLogger, string strClase, string strMetodo, string strMensaje)
            => objLogger.LogInformation(
                "[{Clase}].[{Metodo}] {Mensaje}",
                strClase, strMetodo, strMensaje);

        // Registra un error con tipo de excepción, archivo y número de línea.
        public static void Error(ILogger<T> objLogger, string strClase, string strMetodo, Exception objException)
        {
            var objFrame = new StackTrace(objException, true).GetFrame(0);
            int intLinea = objFrame?.GetFileLineNumber() ?? 0;
            string strArchivo = Path.GetFileName(objFrame?.GetFileName()) ?? "N/D";
            string strMessage = objException.InnerException?.Message ?? objException.Message;
            objLogger.LogError(
                objException,
                "[{Clase}].[{Metodo}] Ocurrió un error: {Mensaje} | Tipo: {Tipo} | Archivo: {Archivo} | Línea: {Linea}",
                strClase, strMetodo, strMessage,
                objException.GetType().Name, strArchivo, intLinea);
        }
    }
}
