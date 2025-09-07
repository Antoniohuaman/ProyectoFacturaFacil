using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la actualización de datos relevantes de un cliente.
    /// </summary>
    public sealed record ClienteActualizado(
        Guid ClienteId,
        string TipoDocumento,
        string NumeroDocumento,
        string RazonSocial,
        string Nombres,
        DateTime FechaActualizacion
    ) : IDomainEvent;
}
