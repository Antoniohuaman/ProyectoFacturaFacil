using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Aggregates;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Domain.Repositories
{
    /// <summary>
    /// Repositorio para el agregado NotificacionIndicador.
    /// </summary>
    public interface INotificacionIndicadorRepository
    {
        /// <summary>
        /// Obtiene todas las notificaciones configuradas.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetAllAsync();

        /// <summary>
        /// Obtiene una notificación por su Id.
        /// </summary>
        Task<NotificacionIndicador?> GetByIdAsync(Guid id);

        /// <summary>
        /// Agrega una nueva notificación.
        /// </summary>
        Task AddAsync(NotificacionIndicador notificacion);

        /// <summary>
        /// Actualiza una notificación existente.
        /// </summary>
        Task UpdateAsync(NotificacionIndicador notificacion);

        /// <summary>
        /// Elimina una notificación por su Id.
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Obtiene todas las notificaciones activas para un indicador específico dentro del scope de una empresa.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasPorIndicadorAsync(Guid indicadorId, EmpresaId empresaId);

        /// <summary>
        /// Obtiene todas las notificaciones activas para un usuario específico dentro del scope de una empresa.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasPorUsuarioAsync(UsuarioId usuarioId, EmpresaId empresaId);

        /// <summary>
        /// Obtiene todas las notificaciones activas para un establecimiento específico dentro del scope de una empresa.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasPorEstablecimientoAsync(EstablecimientoId establecimientoId, EmpresaId empresaId);

        /// <summary>
        /// Busca notificaciones activas que deban enviarse en un instante dado (por ejemplo, para el scheduler) dentro del scope de una empresa.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasParaEnvioEnAsync(DateTimeOffset instanteUtc, EmpresaId empresaId);
    }
}
