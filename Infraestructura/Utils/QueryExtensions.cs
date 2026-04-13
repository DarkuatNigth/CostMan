using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace CostManagement.Infraestructura.Utils
{
    public static class QueryExtensions
    {
        private const int BATCH_SIZE = 2000;

        /// <summary>
        /// WHERE IN con batching automático para listas grandes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<List<T>> WhereInBatchAsync<T, TKey>(
            this IQueryable<T> query,
            Expression<Func<T, TKey>> keySelector,
            IEnumerable<TKey> values,
            CancellationToken cancellationToken = default) where T : class
        {
            var valuesList = values as List<TKey> ?? values.ToList();

            if (valuesList.Count == 0)
                return new List<T>();

            // Lista pequeña - usar Contains directo
            if (valuesList.Count <= BATCH_SIZE)
            {
                return await query
                    .Where(BuildContainsExpression(keySelector, valuesList))
                    .ToListAsync(cancellationToken);
            }

            // Lista grande - batching automático
            var resultados = new Dictionary<TKey, T>();
            var keyFunc = keySelector.Compile();

            for (int i = 0; i < valuesList.Count; i += BATCH_SIZE)
            {
                var batchSize = Math.Min(BATCH_SIZE, valuesList.Count - i);
                var lote = valuesList.GetRange(i, batchSize);

                var parcial = await query
                    .Where(BuildContainsExpression(keySelector, lote))
                    .ToListAsync(cancellationToken);

                foreach (var item in parcial)
                {
                    var key = keyFunc(item);
                    if (!resultados.ContainsKey(key))
                    {
                        resultados[key] = item;
                    }
                }
            }

            return resultados.Values.ToList();
        }

        /// <summary>
        /// WHERE IN con batching para queries complejos con joins
        /// </summary>
        public static async Task<List<TResult>> SelectManyBatchAsync<T, TKey, TResult>(
            this IQueryable<T> baseQuery,
            Expression<Func<T, TKey>> keySelector,
            IEnumerable<TKey> values,
            Func<IQueryable<T>, IQueryable<TResult>> selector,
            CancellationToken cancellationToken = default) where T : class
        {
            var valuesList = values as List<TKey> ?? values.ToList();

            if (valuesList.Count == 0)
                return new List<TResult>();

            if (valuesList.Count <= BATCH_SIZE)
            {
                var filtered = baseQuery.Where(BuildContainsExpression(keySelector, valuesList));
                return await selector(filtered).ToListAsync(cancellationToken);
            }

            var resultados = new List<TResult>();

            for (int i = 0; i < valuesList.Count; i += BATCH_SIZE)
            {
                var batchSize = Math.Min(BATCH_SIZE, valuesList.Count - i);
                var lote = valuesList.GetRange(i, batchSize);

                var filtered = baseQuery.Where(BuildContainsExpression(keySelector, lote));
                var parcial = await selector(filtered).ToListAsync(cancellationToken);

                resultados.AddRange(parcial);
            }

            return resultados;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Expression<Func<T, bool>> BuildContainsExpression<T, TKey>(
            Expression<Func<T, TKey>> keySelector,
            List<TKey> values)
        {
            var parameter = keySelector.Parameters[0];
            var member = keySelector.Body;

            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TKey));

            var containsCall = Expression.Call(
                containsMethod,
                Expression.Constant(values),
                member);

            return Expression.Lambda<Func<T, bool>>(containsCall, parameter);
        }

        #region  CLAVES COMPUESTAS
        // ============================================================
        // NUEVOS MÉTODOS - CLAVES COMPUESTAS
        // ============================================================

        /// <summary>
        /// WHERE IN con batching para 2 claves compuestas
        /// </summary>
        public static async Task<List<TResult>> SelectManyBatchByCompositeKeyAsync<T, TKey1, TKey2, TResult>(
            this IQueryable<T> baseQuery,
            Expression<Func<T, TKey1>> keySelector1,
            Expression<Func<T, TKey2>> keySelector2,
            IEnumerable<(TKey1, TKey2)> values,
            Func<IQueryable<T>, IQueryable<TResult>> selector,
            CancellationToken cancellationToken = default) where T : class
        {
            var valuesList = values.Distinct().ToList();

            if (valuesList.Count == 0)
                return new List<TResult>();

            var resultados = new List<TResult>();
            int batchSize = Math.Min(BATCH_SIZE, 1000);

            for (int i = 0; i < valuesList.Count; i += batchSize)
            {
                var batch = valuesList.Skip(i).Take(batchSize).ToList();

                var keys1 = batch.Select(x => x.Item1).Distinct().ToList();
                var keys2 = batch.Select(x => x.Item2).Distinct().ToList();

                var filtered = baseQuery
                    .Where(BuildContainsExpression(keySelector1, keys1))
                    .Where(BuildContainsExpression(keySelector2, keys2));

                var parcial = await selector(filtered).ToListAsync(cancellationToken);
                resultados.AddRange(parcial);
            }

            return resultados;
        }

        /// <summary>
        /// WHERE IN con batching para 3 claves compuestas
        /// </summary>
        public static async Task<List<TResult>> SelectManyBatchByCompositeKeyAsync<T, TKey1, TKey2, TKey3, TResult>(
            this IQueryable<T> baseQuery,
            Expression<Func<T, TKey1>> keySelector1,
            Expression<Func<T, TKey2>> keySelector2,
            Expression<Func<T, TKey3>> keySelector3,
            IEnumerable<(TKey1, TKey2, TKey3)> values,
            Func<IQueryable<T>, IQueryable<TResult>> selector,
            CancellationToken cancellationToken = default) where T : class
        {
            var valuesList = values.Distinct().ToList();

            if (valuesList.Count == 0)
                return new List<TResult>();

            var resultados = new List<TResult>();
            int batchSize = Math.Min(BATCH_SIZE, 800);

            for (int i = 0; i < valuesList.Count; i += batchSize)
            {
                var batch = valuesList.Skip(i).Take(batchSize).ToList();

                var keys1 = batch.Select(x => x.Item1).Distinct().ToList();
                var keys2 = batch.Select(x => x.Item2).Distinct().ToList();
                var keys3 = batch.Select(x => x.Item3).Distinct().ToList();

                var filtered = baseQuery
                    .Where(BuildContainsExpression(keySelector1, keys1))
                    .Where(BuildContainsExpression(keySelector2, keys2))
                    .Where(BuildContainsExpression(keySelector3, keys3));

                var parcial = await selector(filtered).ToListAsync(cancellationToken);
                resultados.AddRange(parcial);
            }

            return resultados;
        }

        /// <summary>
        /// WHERE IN con batching para 4 claves compuestas
        /// </summary>
        public static async Task<List<TResult>> SelectManyBatchByCompositeKeyAsync<T, TKey1, TKey2, TKey3, TKey4, TResult>(
            this IQueryable<T> baseQuery,
            Expression<Func<T, TKey1>> keySelector1,
            Expression<Func<T, TKey2>> keySelector2,
            Expression<Func<T, TKey3>> keySelector3,
            Expression<Func<T, TKey4>> keySelector4,
            IEnumerable<(TKey1, TKey2, TKey3, TKey4)> values,
            Func<IQueryable<T>, IQueryable<TResult>> selector,
            CancellationToken cancellationToken = default) where T : class
        {
            var valuesList = values.Distinct().ToList();

            if (valuesList.Count == 0)
                return new List<TResult>();

            var resultados = new List<TResult>();
            int batchSize = Math.Min(BATCH_SIZE, 500);

            for (int i = 0; i < valuesList.Count; i += batchSize)
            {
                var batch = valuesList.Skip(i).Take(batchSize).ToList();

                var keys1 = batch.Select(x => x.Item1).Distinct().ToList();
                var keys2 = batch.Select(x => x.Item2).Distinct().ToList();
                var keys3 = batch.Select(x => x.Item3).Distinct().ToList();
                var keys4 = batch.Select(x => x.Item4).Distinct().ToList();

                var filtered = baseQuery
                    .Where(BuildContainsExpression(keySelector1, keys1))
                    .Where(BuildContainsExpression(keySelector2, keys2))
                    .Where(BuildContainsExpression(keySelector3, keys3))
                    .Where(BuildContainsExpression(keySelector4, keys4));

                var parcial = await selector(filtered).ToListAsync(cancellationToken);
                resultados.AddRange(parcial);
            }

            return resultados;
        }

        /// <summary>
        /// WHERE IN con batching para 5 claves compuestas
        /// </summary>
        public static async Task<List<TResult>> SelectManyBatchByCompositeKeyAsync<T, TKey1, TKey2, TKey3, TKey4, TKey5, TResult>(
            this IQueryable<T> baseQuery,
            Expression<Func<T, TKey1>> keySelector1,
            Expression<Func<T, TKey2>> keySelector2,
            Expression<Func<T, TKey3>> keySelector3,
            Expression<Func<T, TKey4>> keySelector4,
            Expression<Func<T, TKey5>> keySelector5,
            IEnumerable<(TKey1, TKey2, TKey3, TKey4, TKey5)> values,
            Func<IQueryable<T>, IQueryable<TResult>> selector,
            CancellationToken cancellationToken = default) where T : class
        {
            var valuesList = values.Distinct().ToList();

            if (valuesList.Count == 0)
                return new List<TResult>();

            var resultados = new List<TResult>();
            int batchSize = Math.Min(BATCH_SIZE, 400);

            for (int i = 0; i < valuesList.Count; i += batchSize)
            {
                var batch = valuesList.Skip(i).Take(batchSize).ToList();

                var keys1 = batch.Select(x => x.Item1).Distinct().ToList();
                var keys2 = batch.Select(x => x.Item2).Distinct().ToList();
                var keys3 = batch.Select(x => x.Item3).Distinct().ToList();
                var keys4 = batch.Select(x => x.Item4).Distinct().ToList();
                var keys5 = batch.Select(x => x.Item5).Distinct().ToList();

                var filtered = baseQuery
                    .Where(BuildContainsExpression(keySelector1, keys1))
                    .Where(BuildContainsExpression(keySelector2, keys2))
                    .Where(BuildContainsExpression(keySelector3, keys3))
                    .Where(BuildContainsExpression(keySelector4, keys4))
                    .Where(BuildContainsExpression(keySelector5, keys5));

                var parcial = await selector(filtered).ToListAsync(cancellationToken);
                
                resultados.AddRange(parcial);
            }

            return resultados;
        }
        #endregion

        /// <summary>
        /// WHERE IN con batching para queries complejos con joins.
        /// Separa la proyección SQL por lote del procesamiento final en memoria.
        /// Útil cuando el selector final contiene GroupBy/First/Distinct que deben
        /// aplicarse sobre TODA la data consolidada, no por cada lote por separado.
        /// </summary>
        public static async Task<List<TResult>> SelectManyBatchAsync<T, TKey, TIntermediate, TResult>(
            this IQueryable<T> baseQuery,
            Expression<Func<T, TKey>> keySelector,
            IEnumerable<TKey> values,
            Func<IQueryable<T>, IQueryable<TIntermediate>> batchSelector,
            Func<IEnumerable<TIntermediate>, IEnumerable<TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            var valuesList = values as List<TKey> ?? values.ToList();

            if (valuesList.Count == 0)
                return new List<TResult>();

            // Lista pequeña - caso directo
            if (valuesList.Count <= BATCH_SIZE)
            {
                var filtered = baseQuery.Where(BuildContainsExpression(keySelector, valuesList));
                var result = await batchSelector(filtered).ToListAsync(cancellationToken);
                return selector(result).ToList();
            }

            // Lista grande - recolectar intermedios de todos los lotes, luego finalizar
            var allIntermediate = new List<TIntermediate>();

            for (int i = 0; i < valuesList.Count; i += BATCH_SIZE)
            {
                var batchSize = Math.Min(BATCH_SIZE, valuesList.Count - i);
                var lote = valuesList.GetRange(i, batchSize);

                var filtered = baseQuery.Where(BuildContainsExpression(keySelector, lote));
                var parcial = await batchSelector(filtered).ToListAsync(cancellationToken);
                allIntermediate.AddRange(parcial);
            }

            // El finalSelector se ejecuta UNA sola vez sobre toda la data consolidada
            return selector(allIntermediate).ToList();
        }

        /// <summary>
        /// Versión simplificada: sin batchSelector, proyecta T directamente.
        /// Útil cuando solo necesitas el finalSelector sobre toda la data consolidada.
        /// </summary>
        public static async Task<List<TResult>> SelectManyBatchWithFinalizeAsync<T, TKey, TResult>(
            this IQueryable<T> baseQuery,
            Expression<Func<T, TKey>> keySelector,
            IEnumerable<TKey> values,
            Func<IEnumerable<T>, IEnumerable<TResult>> finalSelector,
            CancellationToken cancellationToken = default) where T : class
        {
            var valuesList = values as List<TKey> ?? values.ToList();

            if (valuesList.Count == 0)
                return new List<TResult>();

            if (valuesList.Count <= BATCH_SIZE)
            {
                var result = await baseQuery
                    .Where(BuildContainsExpression(keySelector, valuesList))
                    .ToListAsync(cancellationToken);
                return finalSelector(result).ToList();
            }

            var allItems = new List<T>();

            for (int i = 0; i < valuesList.Count; i += BATCH_SIZE)
            {
                var batchSize = Math.Min(BATCH_SIZE, valuesList.Count - i);
                var lote = valuesList.GetRange(i, batchSize);

                var parcial = await baseQuery
                    .Where(BuildContainsExpression(keySelector, lote))
                    .ToListAsync(cancellationToken);

                allItems.AddRange(parcial);
            }

            return finalSelector(allItems).ToList();
        }
    }
}




