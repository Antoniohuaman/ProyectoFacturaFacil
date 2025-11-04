#nullable enable
using System;

namespace SharedKernel.Events
{
    /// <summary>
    /// Clase base opcional para eventos de dominio.
    /// Provee metadatos estándar: <see cref="EventId"/> y <see cref="OccurredOn"/> (UTC).
    /// Úsala si quieres homogeneidad en tus eventos; si prefieres records simples,
    /// implementa sólo <see cref="IDomainEvent"/>.
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        /// <summary>Identificador único del evento.</summary>
        public Guid EventId { get; }

        /// <summary>Fecha/hora UTC en la que ocurrió el evento.</summary>
        public DateTime OccurredOn { get; }

        /// <summary>
        /// Crea un evento de dominio con metadatos. Permite inyectar fecha/ID (útil en replays/tests).
        /// </summary>
        /// <param name="eventId">Si es null, se genera un Guid nuevo.</param>
        /// <param name="occurredOnUtc">Si es null, se usa <see cref="DateTime.UtcNow"/>.</param>
        protected DomainEvent(Guid? eventId = null, DateTime? occurredOnUtc = null)
        {
            EventId     = eventId        ?? Guid.NewGuid();
            OccurredOn  = (occurredOnUtc ?? DateTime.UtcNow).ToUniversalTime();
        }
    }
}
