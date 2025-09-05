using System;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;          // ConfiguracionEmpresa
using ConfiguracionSistemaBC.Domain.ValueObjects;        // Ruc, TipoComprobanteCodigo, SerieCodigo, UnidadDeMedida
using SharedKernel.ValueObjects;                         // EmpresaId, EstablecimientoId

namespace ConfiguracionSistemaBC.Domain.Repositories
{
    /// <summary>
    /// Repositorio del Aggregate Root ConfiguracionEmpresa.
    /// En este BC la identidad operacional es EmpresaId (tenant == empresa).
    /// </summary>
    public interface IConfiguracionEmpresaRepository
    {
        // ---------- CRUD principal sobre el aggregate ----------
        /// <summary>Obtiene la configuración de empresa por EmpresaId (identidad opaca).</summary>
        Task<ConfiguracionEmpresa?> GetByEmpresaIdAsync(
            EmpresaId empresaId,
            CancellationToken ct = default);

        /// <summary>Agrega una nueva configuración de empresa.</summary>
        Task AddAsync(
            ConfiguracionEmpresa aggregate,
            CancellationToken ct = default);

        /// <summary>Actualiza la configuración de empresa.</summary>
        Task UpdateAsync(
            ConfiguracionEmpresa aggregate,
            CancellationToken ct = default);

        /// <summary>
        /// Actualiza usando concurrencia optimista; true si coincidió la versión.
        /// </summary>
        Task<bool> UpdateIfVersionMatchAsync(
            ConfiguracionEmpresa aggregate,
            int expectedVersion,
            CancellationToken ct = default);

        /// <summary>Elimina (lógica o física) por EmpresaId.</summary>
        Task DeleteAsync(
            EmpresaId empresaId,
            CancellationToken ct = default);

        // ---------- Búsquedas auxiliares ----------
        /// <summary>Busca por RUC (útil en migraciones/validaciones).</summary>
        Task<ConfiguracionEmpresa?> FindByRucAsync(
            Ruc ruc,
            CancellationToken ct = default);

        // ---------- Checks livianos (opcionales, para pre-validaciones sin hidratar el aggregate) ----------
        /// <summary>Verifica existencia de un establecimiento dentro de la empresa.</summary>
        Task<bool> EstablecimientoExisteAsync(
            EmpresaId empresaId,
            EstablecimientoId establecimientoId,
            CancellationToken ct = default);

        /// <summary>
        /// Verifica si existe una serie (única por tipo+serie) en la empresa.
        /// Útil para validar formularios antes de cargar el aggregate.
        /// </summary>
        Task<bool> SerieExisteAsync(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            CancellationToken ct = default);

        /// <summary>
        /// Verifica si existe una unidad de medida (por código SUNAT/UNECE) en la empresa.
        /// </summary>
        Task<bool> UnidadDeMedidaExisteAsync(
            EmpresaId empresaId,
            UnidadDeMedida unidad,
            CancellationToken ct = default);
    }
}
