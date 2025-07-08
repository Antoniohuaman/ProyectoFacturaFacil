using System.Threading.Tasks;

namespace ListaPreciosBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
    }
}