using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates; // RolEmpresa

namespace ConfiguracionSistemaBC.Domain.Repositories
{
    public interface IRolEmpresaRepository
    {
        Task<RolEmpresa?> GetByIdAsync(Guid rolId, CancellationToken ct);
        // Puedes agregar otros métodos según necesidad
    }
}