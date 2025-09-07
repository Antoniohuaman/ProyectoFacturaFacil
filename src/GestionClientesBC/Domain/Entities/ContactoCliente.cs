using System;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Entities
{
    /// <summary>
    /// Representa un contacto secundario asociado a un cliente (email alterno, teléfono adicional, dirección, etc).
    /// </summary>
    public class ContactoCliente
    {
        /// <summary>
        /// Nombre de la persona de contacto (obligatorio).
        /// </summary>
        public SharedKernel.ValueObjects.NombrePersona NombreContacto { get; private set; }

        /// <summary>
        /// Documento de identidad del contacto (opcional, solo DNI).
        /// </summary>
        public SharedKernel.ValueObjects.DocumentoIdentidad? DocumentoIdentidad { get; private set; }
        /// <summary>
        /// Identificador único del contacto.
        /// </summary>
        public Guid ContactoId { get; private set; }



    /// <summary>
    /// Correos electrónicos del contacto (opcional, puede haber varios).
    /// </summary>
    public List<SharedKernel.ValueObjects.Email> Emails { get; private set; } = new();

    /// <summary>
    /// Teléfonos del contacto (opcional, puede haber varios).
    /// </summary>
    public List<SharedKernel.ValueObjects.Telefono> Telefonos { get; private set; } = new();

    /// <summary>
    /// Dirección del contacto (opcional).
    /// </summary>
    public string? Direccion { get; private set; }

        /// <summary>
        /// Fecha de creación del contacto.
        /// </summary>
        public DateTime FechaCreacion { get; private set; }

        /// <summary>
        /// Fecha de última modificación del contacto.
        /// </summary>
        public DateTime? FechaModificacion { get; private set; }

        /// <summary>
        /// Constructor para crear un nuevo contacto secundario.
        /// </summary>
        /// <param name="contactoId">Identificador único del contacto.</param>
        /// <param name="nombreContacto">Nombre de la persona de contacto (obligatorio).</param>
        /// <param name="documentoIdentidad">Documento de identidad (opcional, solo DNI).</param>
        /// <param name="emails">Lista de correos electrónicos (opcional).</param>
        /// <param name="telefonos">Lista de teléfonos (opcional).</param>
        /// <param name="direccion">Dirección (opcional).</param>
        public ContactoCliente(
            Guid contactoId,
            SharedKernel.ValueObjects.NombrePersona nombreContacto,
            SharedKernel.ValueObjects.DocumentoIdentidad? documentoIdentidad = null,
            List<SharedKernel.ValueObjects.Email>? emails = null,
            List<SharedKernel.ValueObjects.Telefono>? telefonos = null,
            string? direccion = null)
        {
            ContactoId = contactoId != Guid.Empty ? contactoId : throw new ArgumentException("El Id no puede ser vacío.", nameof(contactoId));
            NombreContacto = nombreContacto ?? throw new ArgumentNullException(nameof(nombreContacto), "El nombre de la persona de contacto es obligatorio.");
            if (documentoIdentidad != null && documentoIdentidad.Tipo != SharedKernel.ValueObjects.TipoDocumento.Dni)
                throw new ArgumentException("Solo se permite DNI como documento de contacto secundario.", nameof(documentoIdentidad));
            DocumentoIdentidad = documentoIdentidad;
            Emails = emails ?? new List<SharedKernel.ValueObjects.Email>();
            Telefonos = telefonos ?? new List<SharedKernel.ValueObjects.Telefono>();
            Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion;
            FechaCreacion = DateTime.UtcNow;
        }

        /// <summary>
        /// Actualiza los datos de contacto.
        /// </summary>
        public void ActualizarDatos(
            List<SharedKernel.ValueObjects.Email>? emails = null,
            List<SharedKernel.ValueObjects.Telefono>? telefonos = null,
            string? direccion = null)
        {
            Emails = emails ?? new List<SharedKernel.ValueObjects.Email>();
            Telefonos = telefonos ?? new List<SharedKernel.ValueObjects.Telefono>();
            Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion;
            FechaModificacion = DateTime.UtcNow;
        }
    }

    // Enum TipoContacto eliminado: ahora los datos de contacto son propiedades explícitas.
}