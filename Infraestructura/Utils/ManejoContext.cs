using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace CostManagement.Infraestructura.Utils
{
    public static class ManejoContext<T> where T : DbContext
    {
        // Flujo sin transacción — con retorno
        public static async Task<TResult> EjecutarAsync<TResult>(
            IDbContextFactory<T> objFactory,
            Func<T, Task<TResult>> funcOperacion,
            int intTimeout = 180)
        {
            T? objContext = null;
            try
            {
                objContext = await objFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(intTimeout);
                return await funcOperacion(objContext);
            }
            finally
            {
                await CerrarYLiberar(objContext);
            }
        }

        // Flujo sin transacción — void
        public static async Task EjecutarAsync(
            IDbContextFactory<T> objFactory,
            Func<T, Task> funcOperacion,
            int intTimeout = 180)
        {
            T? objContext = null;
            try
            {
                objContext = await objFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(intTimeout);
                await funcOperacion(objContext);
            }
            finally
            {
                await CerrarYLiberar(objContext);
            }
        }

        // Flujo con transacción — con retorno
        // nivelAislamiento = null → usa el nivel de aislamiento por defecto del proveedor.
        // blRequiereCommit = false → para transacciones de solo lectura (ReadUncommitted NOLOCK)
        //   que no necesitan commit; el Dispose de la transacción es suficiente.
        public static async Task<TResult> EjecutarEnTransaccionAsync<TResult>(
            IDbContextFactory<T> objFactory,
            Func<T, Task<TResult>> funcOperacion,
            int intTimeout = 180,
            IsolationLevel? nivelAislamiento = null,
            bool blRequiereCommit = true)
        {
            T? objContext = null;
            IDbContextTransaction? objTransaction = null;
            try
            {
                objContext = await objFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(intTimeout);
                objTransaction = nivelAislamiento.HasValue
                    ? await objContext.Database.BeginTransactionAsync(nivelAislamiento.Value)
                    : await objContext.Database.BeginTransactionAsync();
                TResult objResultado = await funcOperacion(objContext);
                if (blRequiereCommit)
                    await objTransaction.CommitAsync();
                return objResultado;
            }
            catch
            {
                if (objTransaction != null)
                    await objTransaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (objTransaction != null)
                    await objTransaction.DisposeAsync();
                await CerrarYLiberar(objContext);
            }
        }

        // Flujo con transacción — void
        public static async Task EjecutarEnTransaccionAsync(
            IDbContextFactory<T> objFactory,
            Func<T, Task> funcOperacion,
            int intTimeout = 180,
            IsolationLevel? nivelAislamiento = null,
            bool blRequiereCommit = true)
        {
            T? objContext = null;
            IDbContextTransaction? objTransaction = null;
            try
            {
                objContext = await objFactory.CreateDbContextAsync();
                objContext.Database.SetCommandTimeout(intTimeout);
                objTransaction = nivelAislamiento.HasValue
                    ? await objContext.Database.BeginTransactionAsync(nivelAislamiento.Value)
                    : await objContext.Database.BeginTransactionAsync();
                await funcOperacion(objContext);
                if (blRequiereCommit)
                    await objTransaction.CommitAsync();
            }
            catch
            {
                if (objTransaction != null)
                    await objTransaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (objTransaction != null)
                    await objTransaction.DisposeAsync();
                await CerrarYLiberar(objContext);
            }
        }

        // Red de seguridad: cierra la conexión si sigue abierta y libera el contexto.
        private static async Task CerrarYLiberar(T? objContext)
        {
            if (objContext == null) return;
            if (objContext.Database.GetDbConnection().State == ConnectionState.Open)
                await objContext.Database.CloseConnectionAsync();
            await objContext.DisposeAsync();
        }
    }
}
