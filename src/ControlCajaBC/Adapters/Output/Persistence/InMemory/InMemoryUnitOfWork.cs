using System.Threading;
using System.Threading.Tasks;
using ControlCajaBC.Application.Interfaces;

namespace ControlCajaBC.Adapters.Output.Persistence.InMemory
{
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        // El test hace Assert.IsTrue(uow.WasCommitted)
        public bool WasCommitted { get; private set; }

        public Task CommitAsync(CancellationToken ct = default)
        {
            WasCommitted = true;
            return Task.CompletedTask;
        }
    }
}
