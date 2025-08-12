using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;

namespace ConfiguracionSistemaBC.Domain.Repositories
{
    public interface IConfiguracionEmpresaRepository
    {
        Task<ConfiguracionEmpresa?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
        Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default);
        Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default);
    }
}
