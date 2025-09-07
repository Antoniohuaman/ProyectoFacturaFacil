using System;
using GestionClientesBC.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Aggregates
{
    /// <summary>
    /// Agregado raíz que representa un cliente.
    /// </summary>
    public class Cliente
    {
        private readonly List<ContactoCliente> _contactos = new();
        private readonly List<AdjuntoCliente> _adjuntos = new();
    // Eliminado: private readonly List<OperacionCliente> _operaciones = new();

    public Guid ClienteId { get; }
    public DocumentoIdentidad Documento { get; private set; } // Obligatorio
    public NombreCliente? RazonSocial { get; private set; } // Obligatorio solo si RUC
    public NombreCliente? Nombres { get; private set; } // Obligatorio solo si no es RUC
    public Email? Correo { get; private set; } // Opcional
    public Telefono? Telefono { get; private set; } // Opcional
    public DomicilioFiscal? DomicilioFiscal { get; private set; } // Opcional
    public TipoCliente? TipoCliente { get; private set; } // Opcional
    public RolCliente? RolCliente { get; private set; } // Opcional
    public EstadoCliente? Estado { get; private set; } // Opcional
    public DateTime FechaRegistro { get; private set; } // Por defecto
    public string? MotivoDeshabilitacion { get; private set; } // Opcional
    public DateTime? FechaDeshabilitacion { get; private set; } // Opcional
    public IReadOnlyCollection<ContactoCliente> Contactos => _contactos.AsReadOnly();
    public IReadOnlyCollection<AdjuntoCliente> Adjuntos => _adjuntos.AsReadOnly();

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public Cliente(
            Guid clienteId,
            DocumentoIdentidad documento,
            NombreCliente? razonSocial,
            NombreCliente? nombres,
            Email? correo = null,
            Telefono? telefono = null,
            DomicilioFiscal? domicilioFiscal = null,
            TipoCliente? tipoCliente = null,
            RolCliente? rolCliente = null,
            EstadoCliente? estado = null)
        {
            if (clienteId == Guid.Empty)
                throw new ArgumentException("El Id no puede ser vacío.", nameof(clienteId));
            if (documento is null)
                throw new ArgumentNullException(nameof(documento));

            // Validación de razón social/nombres según tipo de documento
            if (documento.Tipo == TipoDocumento.Ruc)
            {
                if (razonSocial is null)
                    throw new ArgumentNullException(nameof(razonSocial), "La razón social es obligatoria para RUC.");
            }
            else
            {
                if (nombres is null)
                    throw new ArgumentNullException(nameof(nombres), "El nombre es obligatorio para este tipo de documento.");
            }

            ClienteId = clienteId;
            Documento = documento;
            RazonSocial = razonSocial;
            Nombres = nombres;
            Correo = correo;
            Telefono = telefono;
            DomicilioFiscal = domicilioFiscal;
            TipoCliente = tipoCliente;
            RolCliente = rolCliente;
            Estado = estado;
            FechaRegistro = DateTime.UtcNow;

            // Evento de dominio: ClienteCreado (ajustar según nuevos campos)
            _domainEvents.Add(new ClienteCreado(
                ClienteId,
                documento.Tipo.ToString(),
                documento.Numero,
                razonSocial?.Valor ?? string.Empty,
                nombres?.Valor ?? string.Empty,
                FechaRegistro
            ));
        }

        public void ActualizarDatosContacto(Email nuevoCorreo, string nuevoCelular)
        {
            if (nuevoCorreo == null)
                throw new ArgumentNullException(nameof(nuevoCorreo));
            Correo = nuevoCorreo;
        }

        // --- Métodos de edición para el caso de uso EditarCliente ---

    public void ActualizarDireccion(DomicilioFiscal nuevaDireccion)
        {
            if (nuevaDireccion == null)
                throw new ArgumentNullException(nameof(nuevaDireccion));
            DomicilioFiscal = nuevaDireccion;
            FechaRegistro = DateTime.UtcNow; // Actualiza la fecha internamente
        }

        public void ActualizarNombre(NombreCliente nuevoNombre)
        {
            if (Documento.Tipo == TipoDocumento.Ruc)
            {
                if (nuevoNombre is null)
                    throw new ArgumentException("La razón social no puede estar vacía.");
                RazonSocial = nuevoNombre;
            }
            else
            {
                if (nuevoNombre is null)
                    throw new ArgumentException("El nombre no puede estar vacío.");
                Nombres = nuevoNombre;
            }
        }

        public void ActualizarTipoCliente(TipoCliente nuevoTipo)
        {
            TipoCliente = nuevoTipo;
        }

        public void ActualizarRolCliente(RolCliente? nuevoRol)
        {
            if (nuevoRol is null)
                throw new ArgumentNullException(nameof(nuevoRol));
            RolCliente = nuevoRol;
        }

        public void ActualizarDocumentoIdentidad(DocumentoIdentidad nuevoDocumento)
        {
            if (nuevoDocumento is null)
                throw new ArgumentNullException(nameof(nuevoDocumento));
            Documento = nuevoDocumento;
        }

        public void RegistrarModificacion(IDictionary<string, (object? anterior, object? nuevo)> cambios)
        {
            // Evento de dominio: ClienteActualizado (ajustar según nuevos campos)
            _domainEvents.Add(new ClienteActualizado(
                ClienteId,
                Documento.Tipo.ToString(),
                Documento.Numero,
                RazonSocial?.Valor ?? string.Empty,
                Nombres?.Valor ?? string.Empty,
                DateTime.UtcNow
            ));
        }

        public void Deshabilitar(string? motivo, DateTime fecha)
        {
            Estado = EstadoCliente.Inhabilitado;
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
    // Eliminado: método AgregarOperacion(OperacionCliente operacion)

        

        // Métodos de comportamiento (crear, editar, deshabilitar, eliminar, etc.) se agregan aquí
    }
}