using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces;

namespace ListaPreciosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public bool WasCommitted { get; private set; } = false;

        public Task CommitAsync()
        {
            WasCommitted = true;
            return Task.CompletedTask;
        }
    }
}