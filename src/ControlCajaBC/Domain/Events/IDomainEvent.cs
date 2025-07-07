using System;

namespace ControlCajaBC.Domain.Events
{
    /// <summary>
    /// Marca un evento sucedido en el dominio.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Fecha y hora en que ocurrió el evento.
        /// </summary>
        DateTime OccurredOn { get; }
    }
}
