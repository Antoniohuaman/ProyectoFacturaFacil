using System;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Domain.Repositories
{
    /// <summary>
    /// Contrato de persistencia del Aggregate Root IndicadorNegocio.
    /// La “clave natural” es: TipoIndicador + Periodo (alineado) + SegmentoIndicador.
    /// 
    /// Nota:
    /// - Los métodos con EmpresaId son overloads opcionales para aislar multi-tenant en la implementación.
    /// - UpdateAsync con expectedVersion habilita concurrencia optimista explícita (puedes usar uno u otro).
    /// - GetByClaveForUpdateAsync sugiere bloqueo de lectura para actualización (p. ej., SELECT ... FOR UPDATE).
    /// - GetOrCreateAsync permite operación atómica “cargar o crear” según la clave natural.
    /// </summary>
    public interface IIndicadorNegocioRepository
    {
        // -------------------- BÁSICOS (existentes) --------------------

        /// <summary>Obtiene un agregado por su Guid (Identidad interna).</summary>
        Task<IndicadorNegocio?> GetByIdAsync(Guid indicadorId, CancellationToken ct = default);

        /// <summary>Elimina un agregado por su Guid (Identidad interna).</summary>
        Task DeleteAsync(Guid indicadorId, CancellationToken ct = default);

        /// <summary>Obtiene un agregado por su clave natural; null si no existe.</summary>
        Task<IndicadorNegocio?> GetByClaveAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            CancellationToken ct = default);

        /// <summary>Agrega un nuevo agregado.</summary>
        Task AddAsync(IndicadorNegocio agregado, CancellationToken ct = default);

        /// <summary>
        /// Actualiza un agregado existente (usa concurrencia optimista con Version si aplica).
        /// La implementación debe lanzar ConcurrencyException si la versión almacenada difiere.
        /// </summary>
        Task UpdateAsync(IndicadorNegocio agregado, CancellationToken ct = default);

        /// <summary>(Opcional) Verifica existencia por clave natural sin hidratar el agregado.</summary>
        Task<bool> ExistsAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            CancellationToken ct = default);

        // -------------------- OVERLOADS MULTI-TENANT (opcionales) --------------------

        /// <summary>Obtiene un agregado por Id dentro del scope de una empresa/tenant.</summary>
        Task<IndicadorNegocio?> GetByIdAsync(Guid indicadorId, EmpresaId empresaId, CancellationToken ct = default);

        /// <summary>Obtiene un agregado por clave natural dentro del scope de una empresa/tenant.</summary>
        Task<IndicadorNegocio?> GetByClaveAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EmpresaId empresaId,
            CancellationToken ct = default);

        /// <summary>Verifica existencia por clave natural dentro del scope de una empresa/tenant.</summary>
        Task<bool> ExistsAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EmpresaId empresaId,
            CancellationToken ct = default);

        /// <summary>Elimina por clave natural (simetría con GetByClaveAsync).</summary>
        Task DeleteByClaveAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            CancellationToken ct = default);

        /// <summary>Elimina por clave natural dentro del scope de una empresa/tenant.</summary>
        Task DeleteByClaveAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EmpresaId empresaId,
            CancellationToken ct = default);

        // -------------------- CONCURRENCIA OPTIMISTA (explícita) --------------------

        /// <summary>
        /// Actualiza un agregado usando una versión esperada (concurrencia optimista explícita).
        /// Debe lanzar ConcurrencyException si expectedVersion ≠ versión almacenada.
        /// </summary>
        Task UpdateAsync(IndicadorNegocio agregado, int expectedVersion, CancellationToken ct = default);

        // -------------------- LECTURA CON BLOQUEO / PATRONES ATÓMICOS --------------------

        /// <summary>
        /// Obtiene por clave natural con intención de actualizar (p. ej., aplicando lock de fila).
        /// Útil para evitar condiciones de carrera en “cargar/modificar/guardar”.
        /// </summary>
        Task<IndicadorNegocio?> GetByClaveForUpdateAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            CancellationToken ct = default);

        /// <summary>
        /// Obtiene por clave natural con intención de actualizar dentro del scope de una empresa/tenant.
        /// </summary>
        Task<IndicadorNegocio?> GetByClaveForUpdateAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EmpresaId empresaId,
            CancellationToken ct = default);

        /// <summary>
        /// Operación atómica: retorna el agregado por clave natural o lo crea con la factory si no existe.
        /// La implementación debe garantizar unicidad con índice único para la clave natural.
        /// </summary>
        Task<IndicadorNegocio> GetOrCreateAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            Func<IndicadorNegocio> factory,
            CancellationToken ct = default);

        /// <summary>
        /// Operación atómica con scope de empresa/tenant: retorna por clave natural o crea con la factory si no existe.
        /// </summary>
        Task<IndicadorNegocio> GetOrCreateAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EmpresaId empresaId,
            Func<IndicadorNegocio> factory,
            CancellationToken ct = default);
    }
}
