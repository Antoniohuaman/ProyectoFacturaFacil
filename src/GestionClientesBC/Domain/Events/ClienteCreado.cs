using System;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
    public class ClienteCreado : IDomainEvent
    {
        public Guid ClienteId { get; }
        public DocumentoIdentidad DocumentoIdentidad { get; }
        public string RazonSocialONombres { get; }
        public string Correo { get; }
        public string Celular { get; }
        public string DireccionPostal { get; }
        public TipoCliente TipoCliente { get; }
        public EstadoCliente Estado { get; }
        public DateTime FechaRegistro { get; }
        public DateTime OccurredOn { get; } // Implementación de la interfaz

        public ClienteCreado(
            Guid clienteId,
            DocumentoIdentidad documentoIdentidad,
            string razonSocialONombres,
            string correo,
            string celular,
            string direccionPostal,
            TipoCliente tipoCliente,
            EstadoCliente estado,
            DateTime fechaRegistro)
        {
            ClienteId = clienteId;
            DocumentoIdentidad = documentoIdentidad;
            RazonSocialONombres = razonSocialONombres;
            Correo = correo;
            Celular = celular;
            DireccionPostal = direccionPostal;
            TipoCliente = tipoCliente;
            Estado = estado;
            FechaRegistro = fechaRegistro;
            OccurredOn = DateTime.UtcNow;
        }
    }
}