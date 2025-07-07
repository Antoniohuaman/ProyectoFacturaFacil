// IUnitOfWork.cs
namespace CatalogoArticulosBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
