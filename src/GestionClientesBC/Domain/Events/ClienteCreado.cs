using System;
using SharedKernel.ValueObjects;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace GestionClientesBC.Domain.Events
{
    public class ClienteCreado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public string TipoDocumento { get; }
        public string NumeroDocumento { get; }
        public string RazonSocial { get; }
        public string Nombres { get; }
        public DateTime FechaRegistro { get; }
        public DateTime OccurredOn { get; }

        public ClienteCreado(
            Guid clienteId,
            string tipoDocumento,
            string numeroDocumento,
            string razonSocial,
            string nombres,
            DateTime fechaRegistro)
        {
            ClienteId = clienteId;
            TipoDocumento = tipoDocumento;
            NumeroDocumento = numeroDocumento;
            RazonSocial = razonSocial;
            Nombres = nombres;
            FechaRegistro = fechaRegistro;
            OccurredOn = DateTime.UtcNow;
        }
    }
}