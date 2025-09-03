using System;
using ConfiguracionSistemaBC.Domain.Aggregates;
using SharedKernel.ValueObjects;
using System.Threading;
using System.Threading.Tasks;


namespace ConfiguracionSistemaBC.Domain.Repositories
{
    public interface IConfiguracionEmpresaRepository
    {
    /// <summary>
    /// Obtiene la configuración de empresa por EmpresaId (identidad opaca).
    /// </summary>
    Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(EmpresaId empresaId, CancellationToken ct = default);

        /// <summary>
        /// Agrega una nueva configuración de empresa.
        /// </summary>
        Task AddAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default);

        /// <summary>
        /// Actualiza la configuración de empresa. Puede incluir control de versión para concurrencia.
        /// </summary>
        Task UpdateAsync(ConfiguracionEmpresa aggregate, CancellationToken ct = default);

    /// <summary>
    /// Elimina (lógica o física) la configuración de empresa por EmpresaId.
    /// </summary>
    Task DeleteAsync(EmpresaId empresaId, CancellationToken ct = default);

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
