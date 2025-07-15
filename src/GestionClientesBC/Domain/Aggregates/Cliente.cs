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
        public DocumentoIdentidad DocumentoIdentidad { get; private set; }
        public string RazonSocialONombres { get; private set; }
        public string Correo { get; private set; }
        public string Celular { get; private set; }
        public string DireccionPostal { get; private set; }
        public TipoCliente TipoCliente { get; private set; }
        public EstadoCliente Estado { get; private set; }
        public DateTime FechaRegistro { get; private set; }

        // Propiedades para deshabilitación
        public string? MotivoDeshabilitacion { get; private set; }
        public DateTime? FechaDeshabilitacion { get; private set; }

        public IReadOnlyCollection<ContactoCliente> Contactos => _contactos.AsReadOnly();
        public IReadOnlyCollection<AdjuntoCliente> Adjuntos => _adjuntos.AsReadOnly();
        public IReadOnlyCollection<OperacionCliente> Operaciones => _operaciones.AsReadOnly();

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

        // --- Métodos de edición para el caso de uso EditarCliente ---

        public void ActualizarDireccion(string nuevaDireccion)
        {
            if (nuevaDireccion == null)
                throw new ArgumentNullException(nameof(nuevaDireccion));
            DireccionPostal = nuevaDireccion;
            FechaRegistro = DateTime.UtcNow; // Actualiza la fecha internamente
        }

        public void ActualizarNombre(string nuevoNombre)
        {
            if (string.IsNullOrWhiteSpace(nuevoNombre))
                throw new ArgumentException("El nombre/razón social no puede estar vacío.");
            RazonSocialONombres = nuevoNombre;
        }

        public void ActualizarTipoCliente(TipoCliente nuevoTipo)
        {
            TipoCliente = nuevoTipo;
        }

        public void ActualizarDocumentoIdentidad(DocumentoIdentidad nuevoDocumento)
        {
            DocumentoIdentidad = nuevoDocumento ?? throw new ArgumentNullException(nameof(nuevoDocumento));
        }

        public void RegistrarModificacion(IDictionary<string, (object? anterior, object? nuevo)> cambios)
        {
            // Evento de dominio: ClienteModificado
            _domainEvents.Add(new ClienteModificado(
                ClienteId,
                cambios,
                DateTime.UtcNow
            ));
        }

        public void Deshabilitar(string? motivo, DateTime fecha)
        {
            Estado = EstadoCliente.Inactivo;
            FechaDeshabilitacion = fecha;
            MotivoDeshabilitacion = motivo;
        }

        public void RegistrarDeshabilitacion(string? motivo, DateTime fecha)
        {
            _domainEvents.Add(new ClienteDeshabilitado(ClienteId, motivo, fecha));
        }

        /// <summary>
        /// Agrega un nuevo contacto secundario al cliente.
        /// </summary>
        public void AgregarContacto(ContactoCliente contacto)
        {
            if (contacto == null)
                throw new ArgumentNullException(nameof(contacto));
            if (_contactos.Any(c => c.Tipo == contacto.Tipo && c.Valor.Equals(contacto.Valor, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ya existe un contacto igual para este cliente.");
            _contactos.Add(contacto);
        }

        /// <summary>
        /// Elimina un contacto secundario por su identificador.
        /// </summary>
        public void EliminarContacto(Guid contactoId)
        {
            var contacto = _contactos.FirstOrDefault(c => c.ContactoId == contactoId);
            if (contacto == null)
                throw new InvalidOperationException("Contacto no encontrado.");
            _contactos.Remove(contacto);
        }

        /// <summary>
        /// Edita el valor de un contacto secundario existente.
        /// </summary>
        public void EditarContacto(Guid contactoId, string nuevoValor)
        {
            var contacto = _contactos.FirstOrDefault(c => c.ContactoId == contactoId);
            if (contacto == null)
                throw new InvalidOperationException("Contacto no encontrado.");
            contacto.ActualizarValor(nuevoValor);
        }
        public void RegistrarEvento(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));
            _domainEvents.Add(domainEvent);
        }
        public void AgregarAdjunto(AdjuntoCliente adjunto)
        {
            _adjuntos.Add(adjunto);
        }

        public void EliminarAdjunto(Guid adjuntoId)
        {
            var adjunto = _adjuntos.FirstOrDefault(a => a.AdjuntoId == adjuntoId && a.Activo);
            if (adjunto != null)
                adjunto.MarcarInactivo();
        }

        

        // Métodos de comportamiento (crear, editar, deshabilitar, eliminar, etc.) se agregan aquí
    }
}