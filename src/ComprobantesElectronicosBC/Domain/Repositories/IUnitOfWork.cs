using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComprobantesElectronicosBC.Domain.Repositories
{
    /// <summary>
    /// Legacy infrastructure contract used by Adapters. Application layer uses its own IUnitOfWork in Application.Interfaces.
    /// </summary>
    public interface ITransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Legacy infrastructure contract used by Adapters. Application layer uses its own IUnitOfWork in Application.Interfaces.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
    }
}
