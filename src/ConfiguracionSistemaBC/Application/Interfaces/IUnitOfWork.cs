using System.Threading;
using System.Threading.Tasks;
namespace ConfiguracionSistemaBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync(CancellationToken ct = default);
    }
}
