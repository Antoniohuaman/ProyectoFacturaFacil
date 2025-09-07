using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que representa la actualización de datos relevantes de un cliente.
    /// </summary>
    public sealed class ClienteActualizado : DomainEvent
    {
        public Guid ClienteId { get; }
        public string TipoDocumento { get; }
        public string NumeroDocumento { get; }
        public string RazonSocial { get; }
        public string Nombres { get; }
        public DateTime FechaActualizacion { get; }

        public ClienteActualizado(
            Guid clienteId,
            string tipoDocumento,
            string numeroDocumento,
            string razonSocial,
            string nombres,
            DateTime fechaActualizacion,
            Guid? eventId = null,
            DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
            TipoDocumento = tipoDocumento;
            NumeroDocumento = numeroDocumento;
            RazonSocial = razonSocial;
            Nombres = nombres;
            FechaActualizacion = fechaActualizacion;
        }
    }
}
