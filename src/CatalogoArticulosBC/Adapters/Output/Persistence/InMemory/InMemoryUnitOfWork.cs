using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public bool WasCommitted { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Mantener compatibilidad; delega a CommitAsync.
            return CommitAsync(cancellationToken);
        }

        public Task CommitAsync(CancellationToken ct = default)
        {
            WasCommitted = true;
            return Task.CompletedTask;
        }
    }
}