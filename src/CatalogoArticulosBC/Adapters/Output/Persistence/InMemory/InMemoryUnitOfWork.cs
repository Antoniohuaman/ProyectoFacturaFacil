using System.Threading;
using System.Threading.Tasks;
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Implementación real o temporal
            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            // Implementación real o temporal
            return Task.CompletedTask;
        }
    }
}
