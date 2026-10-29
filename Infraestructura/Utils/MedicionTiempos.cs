using System.Collections.Concurrent;
using System.Diagnostics;

namespace CostManagement.Infraestructura.Utils
{
    #region Medicion de tiempos (sin cambios de costeo)
    internal sealed class MedicionTiempos : IDisposable
    {
        private static readonly AsyncLocal<MedicionTiempos?> _objActual = new();
        private readonly Stopwatch _objReloj = Stopwatch.StartNew();
        private readonly ConcurrentQueue<string> _lstTramos = new();
        private readonly MedicionTiempos? _objPadre;
        private readonly ILogger? _objLogger;
        private readonly string _strNombre;
        private readonly string _strId = Guid.NewGuid().ToString("N");
        private readonly bool _blMetodo;
        private readonly Func<int?>? _objContarFilas;
        private bool _blFinalizado;
        #region Acumulados
        // medir bucles sin multiplicar los logs por cada entidad.
        private bool _blAcumulado;
        private readonly ConcurrentDictionary<string, (long ticks, int filas)> _dicAcumulados = new();

        internal static MedicionTiempos IniciarAcumulado(string strNombre)
            => new(strNombre, _objActual.Value?._objLogger, false, null) { _blAcumulado = true };

        internal static void RegistrarDetalle(string strDetalle)
        {
            for (var objResumen = _objActual.Value; objResumen != null; objResumen = objResumen._objPadre)
                objResumen._lstTramos.Enqueue(strDetalle);
        }
        #endregion
        internal int? intFilas { get; set; }

        private MedicionTiempos(string strNombre, ILogger? objLogger, bool blMetodo, Func<int?>? objContarFilas)
        {
            _strNombre = strNombre;
            _objLogger = objLogger;
            _blMetodo = blMetodo;
            _objContarFilas = objContarFilas;
            _objPadre = _objActual.Value;
        }

        internal static MedicionTiempos IniciarMetodo(ILogger objLogger, string strNombre, Func<int?> objContarFilas)
        {
            var objMedicion = new MedicionTiempos(strNombre, objLogger, true, objContarFilas);
            _objActual.Value = objMedicion;
            return objMedicion;
        }

        internal static MedicionTiempos IniciarTramo(string strNombre, Func<int?>? objContarFilas = null)
            => new(strNombre, _objActual.Value?._objLogger, false, objContarFilas);

        // El reloj comienza antes de invocar la operación; conserva el arranque concurrente de las tareas.
        internal static async Task<T> MedirAsync<T>(string strNombre, Func<Task<T>> objOperacion, Func<T, int?> objContarFilas)
        {
            using var objMedicion = IniciarTramo(strNombre);
            T objResultado = await objOperacion();
            objMedicion.intFilas = objContarFilas(objResultado);
            return objResultado;
        }

        internal static async Task MedirAsync(string strNombre, Func<Task> objOperacion, Func<int?> objContarFilas)
        {
            using var objMedicion = IniciarTramo(strNombre, objContarFilas);
            await objOperacion();
        }

        public void Dispose()
        {
            if (_blFinalizado) return;
            _blFinalizado = true;
            _objReloj.Stop();
            // Publicar acumulados antes del resumen del metodo propietario.
            if (_blAcumulado)
            {
                _objPadre?._dicAcumulados.AddOrUpdate(_strNombre, (_objReloj.ElapsedTicks, 1),
                    (_, anterior) => (anterior.ticks + _objReloj.ElapsedTicks, anterior.filas + 1));
                return;
            }
            if (_blMetodo)
                foreach (var tramo in _dicAcumulados.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    double ms = tramo.Value.ticks * 1000d / Stopwatch.Frequency;
                    string detalle = FormattableString.Invariant($"{_strNombre}/{tramo.Key}={ms:F3}ms filas={tramo.Value.filas}");
                    RegistrarDetalle(detalle);
                    _objLogger?.LogInformation("[TIEMPOS] Metodo={Metodo} Id={Id} Tramo={Tramo} Milisegundos={Milisegundos} Filas={Filas}",
                        _strNombre, _strId, tramo.Key, ms, tramo.Value.filas);
                }
            int? intCantidad = _objContarFilas?.Invoke() ?? intFilas;
            string strTramo = _blMetodo ? "total" : _strNombre;
            var objMetodo = _blMetodo ? this : _objPadre;
            if (objMetodo == null) return; // Los consumidores fuera de los tres métodos no se instrumentan.
            string strDetalle = $"{objMetodo._strNombre}/{strTramo}={_objReloj.ElapsedMilliseconds}ms filas={intCantidad?.ToString() ?? "N/A"}";
            // Los resúmenes incluyen los tramos internos; son inclusivos y no deben sumarse.
            for (var objResumen = objMetodo; objResumen != null; objResumen = objResumen._objPadre)
                objResumen._lstTramos.Enqueue(strDetalle);
            _objLogger?.LogInformation(
                "[TIEMPOS] Metodo={Metodo} Id={Id} Tramo={Tramo} Milisegundos={Milisegundos} Filas={Filas}",
                objMetodo._strNombre, objMetodo._strId, strTramo, _objReloj.ElapsedMilliseconds, intCantidad);
            if (_blMetodo)
            {
                _objActual.Value = _objPadre;
                _objLogger?.LogInformation(
                    "[TIEMPOS] Metodo={Metodo} Id={Id} Tramo=resumen Milisegundos={Milisegundos} Filas={Filas} Tramos={Tramos}",
                    _strNombre, _strId, _objReloj.ElapsedMilliseconds, intCantidad, string.Join("; ", _lstTramos));
            }
        }
    }
    #endregion
}
