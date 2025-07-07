using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces;

namespace GestionClientesBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public bool WasCommitted { get; private set; }

        public Task CommitAsync()
        {
            WasCommitted = true;
            return Task.CompletedTask;
        }
    }
}