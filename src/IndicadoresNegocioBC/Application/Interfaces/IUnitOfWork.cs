using System.Threading;
using System.Threading.Tasks;

namespace IndicadoresNegocioBC.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task CommitAsync(CancellationToken ct = default);
    }
}
