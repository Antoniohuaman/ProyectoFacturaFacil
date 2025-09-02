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
        DocumentoIdentidad DocumentoIdentidad,
        string RazonSocialONombres,
        Email Correo,
        string Celular,
    DomicilioFiscal? DomicilioFiscal,
        TipoCliente TipoCliente,
        EstadoCliente Estado,
        DateTime FechaActualizacion
    ) : IDomainEvent;
}
