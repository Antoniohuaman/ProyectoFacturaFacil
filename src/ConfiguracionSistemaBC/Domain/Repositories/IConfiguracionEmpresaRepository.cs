using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;

namespace ConfiguracionSistemaBC.Domain.Repositories
{
    public interface IConfiguracionEmpresaRepository
    {
        /// <summary>
        /// Obtiene la configuración de empresa por TenantId (multiempresa).
        /// </summary>
        Task<ConfiguracionEmpresa?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Agrega una nueva configuración de empresa.
        /// </summary>
        Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default);

        /// <summary>
        /// Actualiza la configuración de empresa. Puede incluir control de versión para concurrencia.
        /// </summary>
        Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default);

        /// <summary>
        /// Elimina (lógica o física) la configuración de empresa.
        /// </summary>
        Task DeleteAsync(Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Busca configuración por RUC (útil para migraciones, validaciones, etc.).
        /// </summary>
        Task<ConfiguracionEmpresa?> FindByRucAsync(string ruc, CancellationToken ct = default);

        /// <summary>
        /// Actualiza solo si la versión coincide (optimistic concurrency).
        /// </summary>
        Task<bool> UpdateIfVersionMatchAsync(ConfiguracionEmpresa aggregate, int expectedVersion, CancellationToken ct = default);
    }
}
