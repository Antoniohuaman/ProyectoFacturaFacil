using System;
using SharedKernel.ValueObjects;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    public sealed class ClienteCreado : DomainEvent
    {
        public Guid ClienteId { get; }
        public string TipoDocumento { get; }
        public string NumeroDocumento { get; }
        public string RazonSocial { get; }
        public string Nombres { get; }
        public DateTime FechaRegistro { get; }

        public ClienteCreado(
            Guid clienteId,
            string tipoDocumento,
            string numeroDocumento,
            string razonSocial,
            string nombres,
            DateTime fechaRegistro,
            Guid? eventId = null,
            DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            ClienteId = clienteId;
            TipoDocumento = tipoDocumento;
            NumeroDocumento = numeroDocumento;
            RazonSocial = razonSocial;
            Nombres = nombres;
            FechaRegistro = fechaRegistro;
        }
    }
}