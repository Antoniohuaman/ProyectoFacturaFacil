using System.Threading;
using System.Threading.Tasks;
using System.Threading;
using GestionClientesBC.Application.Interfaces;

namespace GestionClientesBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public bool WasCommitted { get; private set; }

        public Task CommitAsync(CancellationToken ct = default)
        {
            WasCommitted = true;
            return Task.CompletedTask;
        }
    }
}