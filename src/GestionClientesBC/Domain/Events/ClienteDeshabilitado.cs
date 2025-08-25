using System;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que un cliente ha sido deshabilitado.
    /// </summary>
    public sealed record ClienteDeshabilitado(
        Guid ClienteId,
        string? Motivo,
        DateTime Fecha
    ) : IDomainEvent
    {
        /// <summary>
        /// Fecha y hora en que ocurrió el evento (UTC).
        /// </summary>
        public DateTime OccurredOn => Fecha;
    }
}