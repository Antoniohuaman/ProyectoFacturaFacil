using System.Threading;
using System.Threading.Tasks;

namespace ConfiguracionSistemaBC.Application.Interfaces
{
    /// <summary>
    /// Contrato para la unidad de trabajo (transacciones y persistencia).
    /// </summary>
    public interface IUnitOfWork
    {
        Task CommitAsync(CancellationToken ct = default);
    }
}
