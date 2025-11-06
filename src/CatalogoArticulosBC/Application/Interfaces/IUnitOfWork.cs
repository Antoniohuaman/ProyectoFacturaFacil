using System.Threading;
using System.Threading.Tasks;
namespace CatalogoArticulosBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync(CancellationToken ct = default);
    }
}
