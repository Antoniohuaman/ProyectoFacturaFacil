// InMemoryUnitOfWork.cs
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken ct = default)
            => Task.CompletedTask;
    }
}