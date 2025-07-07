using System.Threading.Tasks;


namespace GestionClientesBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
    }
}