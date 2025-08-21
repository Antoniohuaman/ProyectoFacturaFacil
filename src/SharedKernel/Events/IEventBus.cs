#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Events
{
    /// <summary>
    /// Puerto para publicar eventos de dominio hacia la infraestructura (outbox, cola, handlers).
    /// La implementación concreta vive fuera del dominio.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>Publica un único evento.</summary>
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);

        /// <summary>Publica varios eventos.</summary>
        Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);
    }
}
