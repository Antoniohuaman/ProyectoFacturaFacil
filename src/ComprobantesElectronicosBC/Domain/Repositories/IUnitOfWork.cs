using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComprobantesElectronicosBC.Domain.Repositories
{
    /// <summary>
    /// Contrato de Unidad de Trabajo para coordinar persistencia y transacciones.
    /// La implementación concreta vive en Adapters/Infrastructure (EF Core, etc.).
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>Confirma los cambios pendientes.</summary>
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        /// <summary>Abre una transacción. Úsala cuando un caso de uso haga varios pasos atómicos.</summary>
        Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
    }

    /// <summary>Manejador de transacción abstracto (independiente de EF).</summary>
    public interface ITransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}
