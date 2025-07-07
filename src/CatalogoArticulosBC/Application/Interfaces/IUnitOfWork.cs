// IUnitOfWork.cs
using System.Threading.Tasks;
namespace CatalogoArticulosBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken ct = default);
        Task CommitAsync();
    }
}
