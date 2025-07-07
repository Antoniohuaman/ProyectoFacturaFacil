using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        // Los tests verificarán que esto quede en true tras el Commit
        public bool WasCommitted { get; private set; }

        public Task CommitAsync()
        {
            WasCommitted = true;
            return Task.CompletedTask;
        }
    }
}
