using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;      // SerieComprobante
using ConfiguracionSistemaBC.Domain.ValueObjects;    // TipoComprobanteCodigo, SerieCodigo
using SharedKernel.ValueObjects;                     // EmpresaId, EstablecimientoId

namespace ConfiguracionSistemaBC.Domain.Repositories
{
    /// <summary>
    /// Repositorio del Aggregate Root <see cref="SerieComprobante"/>.
    /// Todas las consultas/escrituras están acotadas al <see cref="EmpresaId"/> (multiempresa).
    /// </summary>
    public interface ISerieComprobanteRepository
    {
        // -------------------- Lectura por identidad --------------------

        Task<SerieComprobante?> GetByIdAsync(
            Guid id,
            CancellationToken ct = default);

        /// <summary>Clave natural: (EmpresaId + Tipo + Serie).</summary>
        Task<SerieComprobante?> GetByEmpresaTipoSerieAsync(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            CancellationToken ct = default);

        /// <summary>True si ya existe (EmpresaId + Tipo + Serie). Útil para validar unicidad.</summary>
        Task<bool> ExistsByTipoSerieAsync(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            SerieCodigo serie,
            CancellationToken ct = default);

        /// <summary>Devuelve la serie marcada como “por defecto” para un tipo, si existe.</summary>
        Task<SerieComprobante?> GetDefaultByTipoAsync(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            CancellationToken ct = default);

        // -------------------- Listados / conteo --------------------

        /// <summary>
        /// Lista series con filtros opcionales.
        /// - tipo: filtra por tipo de comprobante (01/03, etc.)
        /// - habilitada: true/false
        /// - establecimientoId: filtra por establecimiento
        /// Paginación: skip/take.
        /// </summary>
        Task<IReadOnlyList<SerieComprobante>> ListAsync(
            EmpresaId empresaId,
            TipoComprobanteCodigo? tipo = null,
            bool? habilitada = null,
            EstablecimientoId? establecimientoId = null,
            int skip = 0,
            int take = 100,
            CancellationToken ct = default);

        Task<int> CountAsync(
            EmpresaId empresaId,
            TipoComprobanteCodigo? tipo = null,
            bool? habilitada = null,
            EstablecimientoId? establecimientoId = null,
            CancellationToken ct = default);

        // -------------------- Escritura (concurrencia optimista) --------------------

        Task AddAsync(
            SerieComprobante aggregate,
            CancellationToken ct = default);

        Task UpdateAsync(
            SerieComprobante aggregate,
            int expectedVersion,
            CancellationToken ct = default);

        /// <summary>Elimina por Id (usa expectedVersion). La App valida aggregate.PuedeEliminar.</summary>
        Task DeleteAsync(
            Guid id,
            int expectedVersion,
            CancellationToken ct = default);

        // -------------------- Helper (exclusividad del “por defecto”) --------------------

        /// <summary>
        /// Desmarca cualquier serie actualmente “por defecto” para ese (EmpresaId, Tipo).
        /// Útil para garantizar exclusividad antes de marcar una nueva.
        /// Nota: es una optimización de infraestructura; también puede resolverse cargando y actualizando aggregates.
        /// </summary>
        Task UnsetDefaultForTipoAsync(
            EmpresaId empresaId,
            TipoComprobanteCodigo tipo,
            CancellationToken ct = default);
    }
}
