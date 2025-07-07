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
            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            WasCommitted = true;
            return Task.CompletedTask;
        }
    }
}