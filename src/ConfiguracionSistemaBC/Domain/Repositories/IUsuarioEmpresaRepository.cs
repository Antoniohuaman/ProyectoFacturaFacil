using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfiguracionSistemaBC.Domain.Aggregates;   // UsuarioEmpresa, UsuarioEmpresaEstado
using SharedKernel.ValueObjects;                  // EmpresaId, UsuarioId, EstablecimientoId, Email

namespace ConfiguracionSistemaBC.Domain.Repositories
{
    /// <summary>
    /// Repositorio del Aggregate Root <see cref="UsuarioEmpresa"/>.
    /// La identidad es compuesta: <see cref="EmpresaId"/> + <see cref="UsuarioId"/>.
    /// No expone detalles de infraestructura (EF, SQL, etc.).
    /// </summary>
    public interface IUsuarioEmpresaRepository
    {
        // -------------------- Lectura principal --------------------

        /// <summary>
        /// Obtiene un usuario de empresa por su clave compuesta.
        /// </summary>
        Task<UsuarioEmpresa?> GetAsync(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            CancellationToken ct = default);

        /// <summary>
        /// Verifica existencia por clave compuesta.
        /// </summary>
        Task<bool> ExistsAsync(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            CancellationToken ct = default);

        /// <summary>
        /// Obtiene un usuario de empresa por email (único por empresa).
        /// Retorna null si no existe.
        /// </summary>
        Task<UsuarioEmpresa?> GetByEmailAsync(
            EmpresaId empresaId,
            Email email,
            CancellationToken ct = default);

        // -------------------- Consultas de listado --------------------

        /// <summary>
        /// Lista usuarios de una empresa con filtros opcionales.
        /// - Filtra por estado (Invitado/Habilitado/Inhabilitado)
        /// - Filtra por establecimiento concreto
        /// - Filtra por rol (Guid de RolEmpresa) efectivo en ese establecimiento (si se pasa)
        /// Paginación: skip/take.
        /// </summary>
        Task<IReadOnlyList<UsuarioEmpresa>> ListAsync(
            EmpresaId empresaId,
            UsuarioEmpresaEstado? estado = null,
            EstablecimientoId? establecimientoId = null,
            Guid? rolId = null,
            int skip = 0,
            int take = 50,
            CancellationToken ct = default);

        /// <summary>
        /// Cuenta usuarios de una empresa con filtros opcionales por estado.
        /// Útil para paginación/indicadores.
        /// </summary>
        Task<int> CountAsync(
            EmpresaId empresaId,
            UsuarioEmpresaEstado? estado = null,
            CancellationToken ct = default);

        /// <summary>
        /// Lista los establecimientos a los que el usuario tiene acceso.
        /// </summary>
        Task<IReadOnlyList<EstablecimientoId>> ListEstablecimientosAsync(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            CancellationToken ct = default);

        /// <summary>
        /// Indica si el usuario tiene al menos un rol en el establecimiento dado.
        /// </summary>
        Task<bool> TieneAccesoEstablecimientoAsync(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            EstablecimientoId establecimientoId,
            CancellationToken ct = default);

        // -------------------- Reglas de unicidad --------------------

        /// <summary>
        /// Verifica si el email ya existe en la empresa (para mantener unicidad).
        /// </summary>
        Task<bool> EmailExisteEnEmpresaAsync(
            EmpresaId empresaId,
            Email email,
            CancellationToken ct = default);

        // -------------------- Escritura (UoW/Concurrencia) --------------------

        /// <summary>
        /// Agrega un nuevo agregado.
        /// </summary>
        Task AddAsync(
            UsuarioEmpresa agregado,
            CancellationToken ct = default);

        /// <summary>
        /// Actualiza el agregado usando concurrencia optimista.
        /// Lanza excepción de concurrencia si <paramref name="expectedVersion"/> no coincide.
        /// </summary>
        Task UpdateAsync(
            UsuarioEmpresa agregado,
            int expectedVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Elimina el agregado por clave compuesta (o realiza borrado lógico según implementación),
        /// validando la versión esperada.
        /// </summary>
        Task DeleteAsync(
            EmpresaId empresaId,
            UsuarioId usuarioId,
            int expectedVersion,
            CancellationToken ct = default);
    }
}
