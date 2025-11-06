using System.Threading;
using System.Threading.Tasks;
namespace ComprobantesElectronicosBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync(CancellationToken ct = default);
    }
}
