using System;
using System.Collections.Generic;
using System.Linq;
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
        public FechaRegistro FechaRegistro { get; private set; }

        public IReadOnlyCollection<ContactoCliente> Contactos => _contactos.AsReadOnly();
        public IReadOnlyCollection<AdjuntoCliente> Adjuntos => _adjuntos.AsReadOnly();
        public IReadOnlyCollection<OperacionCliente> Operaciones => _operaciones.AsReadOnly();

        public Cliente(
            Guid clienteId,
            DocumentoIdentidad documentoIdentidad,
            string razonSocialONombres,
            string correo,
            string celular,
            string direccionPostal,
            TipoCliente tipoCliente,
            EstadoCliente estado,
            FechaRegistro fechaRegistro)
        {
            ClienteId = clienteId != Guid.Empty ? clienteId : throw new ArgumentException("El Id no puede ser vacío.", nameof(clienteId));
            DocumentoIdentidad = documentoIdentidad ?? throw new ArgumentNullException(nameof(documentoIdentidad));
            RazonSocialONombres = razonSocialONombres ?? throw new ArgumentNullException(nameof(razonSocialONombres));
            Correo = correo ?? throw new ArgumentNullException(nameof(correo));
            Celular = celular ?? throw new ArgumentNullException(nameof(celular));
            DireccionPostal = direccionPostal ?? throw new ArgumentNullException(nameof(direccionPostal));
            TipoCliente = tipoCliente;
            Estado = estado;
            FechaRegistro = fechaRegistro ?? throw new ArgumentNullException(nameof(fechaRegistro));
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