using System.Threading.Tasks;
using System.Threading;


namespace GestionClientesBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync(CancellationToken ct = default);
    }
}