using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Aggregates;

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
        /// Obtiene todas las notificaciones activas para un indicador específico.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasPorIndicadorAsync(Guid indicadorId);

        /// <summary>
        /// Obtiene todas las notificaciones activas para un usuario específico.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasPorUsuarioAsync(Guid usuarioId);

        /// <summary>
        /// Obtiene todas las notificaciones activas para un establecimiento específico.
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasPorEstablecimientoAsync(Guid establecimientoId);

        /// <summary>
        /// Busca notificaciones activas que deban enviarse en un instante dado (por ejemplo, para el scheduler).
        /// </summary>
        Task<IEnumerable<NotificacionIndicador>> GetActivasParaEnvioEnAsync(DateTimeOffset instanteUtc);
    }
}
