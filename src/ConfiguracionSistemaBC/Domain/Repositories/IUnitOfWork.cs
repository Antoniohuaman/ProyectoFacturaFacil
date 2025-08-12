using System.Threading;
using System.Threading.Tasks;

namespace ConfiguracionSistemaBC.Domain.Repositories
{
    /// <summary>
    /// Abstracción de unidad de trabajo para persistir cambios atómicamente.
    /// La implementación real (EF Core, Dapper, etc.) va en Adapters/Infra.
    /// </summary>
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}