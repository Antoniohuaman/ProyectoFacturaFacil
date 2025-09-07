using System;
using GestionClientesBC.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using SharedKernel.Events;
using SharedKernel.Exceptions;
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
    public EmpresaId EmpresaId { get; } // Multi-empresa: obligatorio
    public DocumentoIdentidad Documento { get; private set; } // Obligatorio
    public RazonSocial? RazonSocial { get; private set; } // Obligatorio solo si RUC
    public NombrePersona? Nombres { get; private set; } // Obligatorio solo si no es RUC
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
    public DateTime? FechaUltimaModificacion { get; private set; } // Nueva: fecha de última modificación

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public Cliente(
    Guid clienteId,
    EmpresaId empresaId,
    DocumentoIdentidad documento,
    RazonSocial? razonSocial,
    NombrePersona? nombres,
    Email? correo = null,
    Telefono? telefono = null,
    DomicilioFiscal? domicilioFiscal = null,
    TipoCliente? tipoCliente = null,
    RolCliente? rolCliente = null,
    EstadoCliente? estado = null)
        {
            if (clienteId == Guid.Empty)
                throw new ArgumentException("El Id no puede ser vacío.", nameof(clienteId));
            if (empresaId is null || empresaId.IsEmpty)
                throw new ArgumentNullException(nameof(empresaId), "El EmpresaId es obligatorio.");
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
            EmpresaId = empresaId;
            Documento = documento;
            RazonSocial = razonSocial;
            Nombres = nombres;
            Correo = correo;
            Telefono = telefono;
            DomicilioFiscal = domicilioFiscal;
            TipoCliente = tipoCliente ?? TipoCliente.Cliente;
            RolCliente = rolCliente;
            Estado = estado ?? EstadoCliente.Habilitado;
            FechaRegistro = DateTime.UtcNow;

            // Evento de dominio: ClienteCreado (ajustar según nuevos campos)
            _domainEvents.Add(new ClienteCreado(
                ClienteId,
                EmpresaId,
                documento.Tipo.ToString(),
                documento.Numero,
                razonSocial?.Valor ?? string.Empty,
                nombres?.Completo ?? string.Empty,
                FechaRegistro
            ));
        }

        public void Habilitar()
        {
            if (Estado == EstadoCliente.Habilitado)
                throw new BusinessRuleException("El cliente ya está habilitado.");

            Estado = EstadoCliente.Habilitado;
            RegistrarEvento(new ClienteHabilitado(ClienteId, EmpresaId));
        }

        public void ActualizarDatosContacto(Email nuevoCorreo, string nuevoCelular)
        {
            if (nuevoCorreo == null)
                throw new ArgumentNullException(nameof(nuevoCorreo));
            if (string.IsNullOrWhiteSpace(nuevoCelular))
                throw new ArgumentNullException(nameof(nuevoCelular));

            Correo = nuevoCorreo;
            Telefono = Telefono.FromTexto(nuevoCelular);
        }

        // --- Métodos de edición para el caso de uso EditarCliente ---

    public void ActualizarDireccion(DomicilioFiscal nuevaDireccion)
        {
            if (nuevaDireccion == null)
                throw new ArgumentNullException(nameof(nuevaDireccion));
            DomicilioFiscal = nuevaDireccion;
            FechaUltimaModificacion = DateTime.UtcNow;
        }

        public void ActualizarNombre(object nuevoNombre)
        {
            if (Documento.Tipo == TipoDocumento.Ruc)
            {
                if (nuevoNombre is not RazonSocial razonSocial)
                    throw new ArgumentException("Debe proporcionar una RazonSocial para RUC.", nameof(nuevoNombre));
                RazonSocial = razonSocial;
            }
            else
            {
                if (nuevoNombre is not NombrePersona nombrePersona)
                    throw new ArgumentException("Debe proporcionar un NombrePersona para este tipo de documento.", nameof(nuevoNombre));
                Nombres = nombrePersona;
            }
            FechaUltimaModificacion = DateTime.UtcNow;
        }

        public void ActualizarTipoCliente(TipoCliente nuevoTipo)
        {
            TipoCliente = nuevoTipo;
            FechaUltimaModificacion = DateTime.UtcNow;
        }

        public void ActualizarRolCliente(RolCliente? nuevoRol)
        {
            RolCliente = nuevoRol;
            FechaUltimaModificacion = DateTime.UtcNow;
        }

        public void ActualizarDocumentoIdentidad(DocumentoIdentidad nuevoDocumento)
        {
            if (nuevoDocumento == null) throw new ArgumentNullException(nameof(nuevoDocumento));
            if (this.Documento != null && this.Documento.Equals(nuevoDocumento)) return;

            // Revalidar nombre según el tipo de documento
            if (nuevoDocumento.EsRuc)
            {
                if (RazonSocial == null || string.IsNullOrWhiteSpace(RazonSocial.Valor))
                    throw new BusinessRuleException("Para RUC se requiere una razón social válida.");
            }
            else
            {
                if (Nombres == null || string.IsNullOrWhiteSpace(Nombres?.Completo ?? string.Empty))
                    throw new BusinessRuleException("Para documentos distintos de RUC se requieren nombres válidos.");
            }

            this.Documento = nuevoDocumento;
            FechaUltimaModificacion = DateTime.UtcNow;
        }

        public void RegistrarModificacion(IDictionary<string, (object? anterior, object? nuevo)> cambios)
        {
            // Evento de dominio: ClienteActualizado (ajustar según nuevos campos)
            _domainEvents.Add(new ClienteActualizado(
                ClienteId,
                EmpresaId,
                Documento.Tipo.ToString(),
                Documento.Numero,
                RazonSocial?.Valor ?? string.Empty,
                Nombres?.Completo ?? string.Empty,
                DateTime.UtcNow
            ));
        }

        public void Deshabilitar(string? motivo, DateTime fecha)
        {
            Estado = EstadoCliente.Inhabilitado;
            var fechaUtc = fecha.Kind == DateTimeKind.Utc ? fecha : fecha.ToUniversalTime();
            FechaDeshabilitacion = fechaUtc;
            MotivoDeshabilitacion = motivo;
            FechaUltimaModificacion = DateTime.UtcNow;
            RegistrarEvento(new ClienteDeshabilitado(ClienteId, EmpresaId, motivo, fechaUtc));
        }

        public void RegistrarDeshabilitacion(string? motivo, DateTime fecha)
        {
            // Obsoleto: ahora el evento se registra en Deshabilitar()
        }

        /// <summary>
        /// Agrega un nuevo contacto secundario al cliente.
        /// </summary>
        public void AgregarContacto(ContactoCliente contacto)
        {
            if (contacto == null)
                throw new ArgumentNullException(nameof(contacto));
            // Validar duplicidad: mismo nombre y mismos emails y mismos teléfonos
            if (_contactos.Any(c =>
                c.NombreContacto.Equals(contacto.NombreContacto)
                && c.Emails.SequenceEqual(contacto.Emails)
                && c.Telefonos.SequenceEqual(contacto.Telefonos)
                && string.Equals(c.Direccion, contacto.Direccion, StringComparison.OrdinalIgnoreCase)
                ))
            {
                throw new BusinessRuleException(
                    "Ya existe un contacto igual para este cliente.");
            }
            _contactos.Add(contacto);
            RegistrarEvento(new ContactoAgregado(ClienteId, EmpresaId, contacto));
        }

        /// <summary>
        /// Elimina un contacto secundario por su identificador.
        /// </summary>
        public void EliminarContacto(Guid contactoId)
        {
            var contacto = _contactos.FirstOrDefault(c => c.ContactoId == contactoId);
            if (contacto == null)
                throw new BusinessRuleException("Contacto no encontrado.");
            _contactos.Remove(contacto);
            RegistrarEvento(new ContactoEliminado(ClienteId, EmpresaId, contactoId));
        }

    // Método EditarContacto eliminado: ahora los datos de contacto pueden ser editados directamente en la entidad ContactoCliente.
        public void RegistrarEvento(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));
            _domainEvents.Add(domainEvent);
        }
        public void AgregarAdjunto(AdjuntoCliente adjunto)
        {
            _adjuntos.Add(adjunto);
            // Registrar evento de dominio
            _domainEvents.Add(new AdjuntoAgregado(ClienteId, EmpresaId, adjunto));
        }

        public void EliminarAdjunto(Guid adjuntoId)
        {
            var adjunto = _adjuntos.FirstOrDefault(a => a.AdjuntoId == adjuntoId);
            if (adjunto != null)
            {
                _adjuntos.Remove(adjunto);
                // Registrar evento de dominio
                _domainEvents.Add(new AdjuntoEliminado(ClienteId, EmpresaId, adjuntoId));
            }
        }
    // Eliminado: método AgregarOperacion(OperacionCliente operacion)

        

        // Métodos de comportamiento (crear, editar, deshabilitar, eliminar, etc.) se agregan aquí
        /// <summary>
        /// Marca el cliente como eliminado y registra el evento correspondiente.
        /// </summary>
        public void EliminarCliente()
        {
            RegistrarEvento(new ClienteEliminado(ClienteId, EmpresaId));
        }
    }
}