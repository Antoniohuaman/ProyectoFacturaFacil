using System;
using System.Threading.Tasks;

namespace CatalogoArticulosBC.Domain.Events
{
    /// <summary>
    /// Interfaz que marca un evento de dominio, proporcionando un identificador único
    /// y la fecha/hora en que ocurrió (en UTC).
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Identificador único del evento.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Fecha y hora (UTC) en que ocurrió el evento.
        /// </summary>
        DateTime OccurredOn { get; }
    }

    /// <summary>
    /// Clase base para eventos de dominio, implementa IDomainEvent y genera
    /// automáticamente EventId y OccurredOn.
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bus para publicar eventos de dominio a la infraestructura o handlers externos.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publica un evento de dominio para suscripción y manejo asíncrono.
        /// </summary>
        /// <param name="domainEvent">Evento de dominio a publicar.</param>
        Task Publish(IDomainEvent domainEvent);
    }
}
