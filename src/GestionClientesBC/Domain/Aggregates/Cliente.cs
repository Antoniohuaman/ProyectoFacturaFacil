using System;
using System.Collections.Generic;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Domain.Aggregates
{
    /// <summary>
    /// Agregado raíz que representa un cliente.
    /// </summary>
    public class Cliente
    {
        private readonly List<ContactoCliente> _contactos = new();
        private readonly List<AdjuntoCliente> _adjuntos = new();
        private readonly List<OperacionCliente> _operaciones = new();

        public Guid ClienteId { get; }
        public DocumentoIdentidad DocumentoIdentidad { get; }
        public string RazonSocialONombres { get; private set; }
        public string Correo { get; private set; }
        public string Celular { get; private set; }
        public string DireccionPostal { get; private set; }
        public TipoCliente TipoCliente { get; private set; }
        public EstadoCliente Estado { get; private set; }
        public DateTime FechaRegistro { get; private set; }

        public IReadOnlyCollection<ContactoCliente> Contactos => _contactos.AsReadOnly();
        public IReadOnlyCollection<AdjuntoCliente> Adjuntos => _adjuntos.AsReadOnly();
        public IReadOnlyCollection<OperacionCliente> Operaciones => _operaciones.AsReadOnly();

        // Evento de dominio para ClienteCreado
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public Cliente(
            Guid clienteId,
            DocumentoIdentidad documentoIdentidad,
            string razonSocialONombres,
            string? correo,
            string? celular,
            string? direccionPostal,
            TipoCliente tipoCliente,
            EstadoCliente estado)
        {
            if (clienteId == Guid.Empty)
                throw new ArgumentException("El Id no puede ser vacío.", nameof(clienteId));
            ClienteId = clienteId;
            DocumentoIdentidad = documentoIdentidad ?? throw new ArgumentNullException(nameof(documentoIdentidad));
            RazonSocialONombres = !string.IsNullOrWhiteSpace(razonSocialONombres) ? razonSocialONombres : throw new ArgumentNullException(nameof(razonSocialONombres));
            Correo = correo ?? string.Empty;
            Celular = celular ?? string.Empty;
            DireccionPostal = direccionPostal ?? string.Empty;
            TipoCliente = tipoCliente;
            Estado = estado;
            FechaRegistro = DateTime.UtcNow;

            // Evento de dominio: ClienteCreado
            _domainEvents.Add(new ClienteCreado(
                ClienteId,
                DocumentoIdentidad,
                RazonSocialONombres,
                Correo,
                Celular,
                DireccionPostal,
                TipoCliente,
                Estado,
                FechaRegistro
            ));
        }

        public void ActualizarDatosContacto(string nuevoCorreo, string nuevoCelular)
        {
            if (string.IsNullOrWhiteSpace(nuevoCorreo))
                throw new ArgumentException("El correo no puede estar vacío.");
            if (string.IsNullOrWhiteSpace(nuevoCelular))
                throw new ArgumentException("El celular no puede estar vacío.");

            Correo = nuevoCorreo;
            Celular = nuevoCelular;
        }

        // Métodos de comportamiento (crear, editar, deshabilitar, eliminar, etc.) se agregan aquí
    }
}