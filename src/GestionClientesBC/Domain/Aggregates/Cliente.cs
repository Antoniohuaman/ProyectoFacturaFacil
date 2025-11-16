using System;
using System.Collections.Generic;
using System.Linq;
using GestionClientesBC.Domain.Entities;
using GestionClientesBC.Domain.Events;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Aggregates
{
    /// <summary>
    /// Agregado raíz que representa un cliente (multi-empresa).
    /// </summary>
    public class Cliente
    {
        private readonly List<ContactoCliente> _contactos = new();
        private readonly List<AdjuntoCliente> _adjuntos = new();
        private readonly List<IDomainEvent> _domainEvents = new();

        // Identidad y multi-empresa
        public Guid ClienteId { get; }
        public EmpresaId EmpresaId { get; } // Multi-empresa: obligatorio

        // Identificación
        public DocumentoIdentidad Documento { get; private set; } // Obligatorio
        public RazonSocial? RazonSocial { get; private set; }     // Obligatorio solo si RUC
        public NombrePersona? Nombres { get; private set; }       // Obligatorio solo si no es RUC

        // Datos de contacto básicos
        public Email? Correo { get; private set; }                // Opcional
        public Telefono? Telefono { get; private set; }           // Opcional

        // Dirección
        public DomicilioFiscal? DomicilioFiscal { get; private set; } // Opcional

        // Perfil de negocio
        public TipoCliente? TipoCliente { get; private set; }     // Opcional (por defecto Cliente)
        public RolCliente? RolCliente { get; private set; }       // Opcional
        public EstadoCliente? Estado { get; private set; }        // Opcional (por defecto Habilitado)

        // NUEVO: metadatos de presentación / negocio
        public NombreCliente? NombreComercial { get; private set; }        // Opcional
        public PaginaWebCliente? PaginaWeb { get; private set; }           // Opcional
        public ObservacionesCliente? Observaciones { get; private set; }   // Opcional
        public FotoPerfilCliente? FotoPerfil { get; private set; }         // Opcional
        public DatosSunatCliente? DatosSunat { get; private set; }         // Opcional (solo lectura lógica)

        // Estado de cuenta
        public DateTime FechaRegistro { get; private set; }       // Se inicializa en UtcNow
        public string? MotivoDeshabilitacion { get; private set; } // Texto simple (para compatibilidad)
        public DateTime? FechaDeshabilitacion { get; private set; } // Opcional

        // Navegación
        public IReadOnlyCollection<ContactoCliente> Contactos => _contactos.AsReadOnly();
        public IReadOnlyCollection<AdjuntoCliente> Adjuntos => _adjuntos.AsReadOnly();

        // Auditoría / concurrencia
        public DateTime? FechaUltimaModificacion { get; private set; } // Fecha de última mutación
        public int Version { get; private set; }                       // Concurrencia optimista

        // Eventos de dominio
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
            EstadoCliente? estado = null,
            // nuevos VOs (todos opcionales, no rompen llamadas existentes)
            NombreCliente? nombreComercial = null,
            PaginaWebCliente? paginaWeb = null,
            ObservacionesCliente? observaciones = null,
            FotoPerfilCliente? fotoPerfil = null,
            DatosSunatCliente? datosSunat = null)
        {
            if (clienteId == Guid.Empty)
                throw new ArgumentException("El Id no puede ser vacío.", nameof(clienteId));

            if (empresaId is null || empresaId.IsEmpty)
                throw new ArgumentNullException(nameof(empresaId), "El EmpresaId es obligatorio.");

            Documento = documento ?? throw new ArgumentNullException(nameof(documento));

            // Regla: para RUC se exige razón social; para otros, nombres de persona
            if (Documento.Tipo == TipoDocumento.Ruc)
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
            RazonSocial = razonSocial;
            Nombres = nombres;
            Correo = correo;
            Telefono = telefono;
            DomicilioFiscal = domicilioFiscal;
            TipoCliente = tipoCliente ?? TipoCliente.Cliente;
            RolCliente = rolCliente;
            Estado = estado ?? EstadoCliente.Habilitado;

            NombreComercial = nombreComercial;
            PaginaWeb = paginaWeb;
            Observaciones = observaciones;
            FotoPerfil = fotoPerfil;
            DatosSunat = datosSunat;

            FechaRegistro = DateTime.UtcNow;
            Version = 0;

            // Evento de dominio: ClienteCreado (se mantiene la firma actual del evento)
            _domainEvents.Add(new ClienteCreado(
                ClienteId,
                EmpresaId,
                Documento.Tipo.ToString(),
                Documento.Numero,
                razonSocial?.Valor ?? string.Empty,
                nombres?.Completo ?? string.Empty,
                FechaRegistro));
        }

        #region Comportamiento principal

        public void Habilitar()
        {
            if (Estado == EstadoCliente.Habilitado)
                throw new BusinessRuleException("El cliente ya está habilitado.");

            Estado = EstadoCliente.Habilitado;
            MotivoDeshabilitacion = null;
            FechaDeshabilitacion = null;
            Touch();
            RegistrarEvento(new ClienteHabilitado(ClienteId, EmpresaId));
        }

        public void Deshabilitar(string? motivo, DateTime fecha)
        {
            Estado = EstadoCliente.Deshabilitado;

            var fechaUtc = fecha.Kind == DateTimeKind.Utc ? fecha : fecha.ToUniversalTime();
            FechaDeshabilitacion = fechaUtc;
            MotivoDeshabilitacion = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();

            Touch();
            RegistrarEvento(new ClienteDeshabilitado(ClienteId, EmpresaId, MotivoDeshabilitacion, fechaUtc));
        }

        public void ActualizarDatosContacto(Email nuevoCorreo, string nuevoCelular)
        {
            if (nuevoCorreo is null)
                throw new ArgumentNullException(nameof(nuevoCorreo));
            if (string.IsNullOrWhiteSpace(nuevoCelular))
                throw new ArgumentNullException(nameof(nuevoCelular));

            Correo = nuevoCorreo;
            Telefono = Telefono.FromTexto(nuevoCelular);

            Touch();
        }

        public void ActualizarDireccion(DomicilioFiscal nuevaDireccion)
        {
            if (nuevaDireccion is null)
                throw new ArgumentNullException(nameof(nuevaDireccion));

            DomicilioFiscal = nuevaDireccion;
            Touch();
        }

        /// <summary>
        /// Actualiza el nombre según el tipo de documento (RUC = Razón Social, otros = NombrePersona).
        /// </summary>
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

            Touch();
        }

        public void ActualizarTipoCliente(TipoCliente nuevoTipo)
        {
            TipoCliente = nuevoTipo ?? throw new ArgumentNullException(nameof(nuevoTipo));
            Touch();
        }

        public void ActualizarRolCliente(RolCliente? nuevoRol)
        {
            RolCliente = nuevoRol;
            Touch();
        }

        public void ActualizarDocumentoIdentidad(DocumentoIdentidad nuevoDocumento)
        {
            if (nuevoDocumento is null)
                throw new ArgumentNullException(nameof(nuevoDocumento));

            if (Documento.Equals(nuevoDocumento))
                return;

            // Revalidar nombre según el nuevo tipo de documento
            if (nuevoDocumento.EsRuc)
            {
                if (RazonSocial == null || string.IsNullOrWhiteSpace(RazonSocial.Valor))
                    throw new BusinessRuleException("Para RUC se requiere una razón social válida.");
            }
            else
            {
                if (Nombres == null || string.IsNullOrWhiteSpace(Nombres.Completo))
                    throw new BusinessRuleException("Para documentos distintos de RUC se requieren nombres válidos.");
            }

            Documento = nuevoDocumento;
            Touch();
        }

        /// <summary>
        /// Registra un conjunto de cambios significativos en el cliente (auditoría lógica).
        /// </summary>
        public void RegistrarModificacion(IDictionary<string, (object? anterior, object? nuevo)> cambios)
        {
            // En este punto solo disparamos el evento; los detalles de "cambios" pueden usarse en handlers.
            _domainEvents.Add(new ClienteActualizado(
                ClienteId,
                EmpresaId,
                Documento.Tipo.ToString(),
                Documento.Numero,
                RazonSocial?.Valor ?? string.Empty,
                Nombres?.Completo ?? string.Empty,
                DateTime.UtcNow));

            Version++;
        }

        /// <summary>
        /// Marca el cliente como eliminado (lógico) y registra el evento.
        /// </summary>
        public void EliminarCliente()
        {
            RegistrarEvento(new ClienteEliminado(ClienteId, EmpresaId));
            Version++;
        }

        #endregion

        #region Nuevos comportamientos para los ValueObjects agregados

        public void ActualizarNombreComercial(NombreCliente? nombreComercial)
        {
            NombreComercial = nombreComercial;
            Touch();
        }

        public void ActualizarPaginaWeb(PaginaWebCliente? paginaWeb)
        {
            PaginaWeb = paginaWeb;
            Touch();
        }

        public void ActualizarObservaciones(ObservacionesCliente? observaciones)
        {
            Observaciones = observaciones;
            Touch();
        }

        public void ActualizarFotoPerfil(FotoPerfilCliente? fotoPerfil)
        {
            FotoPerfil = fotoPerfil;
            Touch();
        }

        /// <summary>
        /// Actualiza el snapshot de datos SUNAT asociado al cliente.
        /// Normalmente se invoca solo desde el flujo de consulta a SUNAT.
        /// </summary>
        public void ActualizarDatosSunat(DatosSunatCliente? datosSunat)
        {
            DatosSunat = datosSunat;
            Touch();
        }

        #endregion

        #region Contactos y adjuntos

        /// <summary>
        /// Agrega un nuevo contacto secundario al cliente.
        /// </summary>
        public void AgregarContacto(ContactoCliente contacto)
        {
            if (contacto is null)
                throw new ArgumentNullException(nameof(contacto));

            // Regla de duplicidad: mismo nombre + mismos emails + mismos teléfonos + misma dirección
            if (_contactos.Any(c =>
                    c.NombreContacto.Equals(contacto.NombreContacto)
                    && c.Emails.SequenceEqual(contacto.Emails)
                    && c.Telefonos.SequenceEqual(contacto.Telefonos)
                    && string.Equals(c.Direccion, contacto.Direccion, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BusinessRuleException("Ya existe un contacto igual para este cliente.");
            }

            _contactos.Add(contacto);
            Touch();
            RegistrarEvento(new ContactoAgregado(ClienteId, EmpresaId, contacto.ContactoId));
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
            Touch();
            RegistrarEvento(new ContactoEliminado(ClienteId, EmpresaId, contactoId));
        }

        public void AgregarAdjunto(AdjuntoCliente adjunto)
        {
            if (adjunto is null)
                throw new ArgumentNullException(nameof(adjunto));

            if (_adjuntos.Any(a => a.AdjuntoId == adjunto.AdjuntoId))
                throw new BusinessRuleException("Ya existe un adjunto con el mismo Id.");

            if (_adjuntos.Any(a =>
                    string.Equals(a.NombreArchivo, adjunto.NombreArchivo, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.Ruta, adjunto.Ruta, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BusinessRuleException("Ya existe un adjunto con el mismo nombre y ruta.");
            }

            _adjuntos.Add(adjunto);
            Touch();
            _domainEvents.Add(new AdjuntoAgregado(ClienteId, EmpresaId, adjunto.AdjuntoId));
        }

        public void EliminarAdjunto(Guid adjuntoId)
        {
            var adjunto = _adjuntos.FirstOrDefault(a => a.AdjuntoId == adjuntoId);
            if (adjunto == null)
                return;

            _adjuntos.Remove(adjunto);
            Touch();
            _domainEvents.Add(new AdjuntoEliminado(ClienteId, EmpresaId, adjuntoId));
        }

        #endregion

        #region Infraestructura de eventos / helpers

        public void RegistrarEvento(IDomainEvent domainEvent)
        {
            if (domainEvent is null)
                throw new ArgumentNullException(nameof(domainEvent));

            _domainEvents.Add(domainEvent);
        }

        private void Touch()
        {
            FechaUltimaModificacion = DateTime.UtcNow;
            Version++;
        }

        #endregion
    }
}
