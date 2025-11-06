using System.Threading;
using System.Threading.Tasks;
namespace GestionInventarioBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync(CancellationToken ct = default);
    }
}

