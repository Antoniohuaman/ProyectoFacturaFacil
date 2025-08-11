using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Domain.Repositories;

namespace ComprobantesElectronicosBC.Adapters.Output.Persistence.InMemory
{
    /// <summary>UnitOfWork en memoria (no-op).</summary>
    public sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
            => Task.FromResult<ITransaction>(new NoopTx());

        private sealed class NoopTx : ITransaction
        {
            public Task CommitAsync(CancellationToken ct = default)   => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
