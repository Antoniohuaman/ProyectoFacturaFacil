using System;

namespace GestionClientesBC.Domain.Entities
{
    /// <summary>
    /// Representa un contacto secundario asociado a un cliente (email alterno, teléfono adicional, dirección, etc).
    /// </summary>
    public class ContactoCliente
    {
        /// <summary>
        /// Identificador único del contacto.
        /// </summary>
        public Guid ContactoId { get; private set; }

        /// <summary>
        /// Tipo de contacto (EMAIL_SECUNDARIO, TELEFONO_SECUNDARIO, DIRECCION, etc).
        /// </summary>
        public TipoContacto Tipo { get; private set; }

        /// <summary>
        /// Valor del contacto (ejemplo: dirección de email, número telefónico, dirección física).
        /// </summary>
        public string Valor { get; private set; }

        /// <summary>
        /// Fecha de creación del contacto.
        /// </summary>
        public DateTime FechaCreacion { get; private set; }

        /// <summary>
        /// Fecha de última modificación del contacto.
        /// </summary>
        public DateTime? FechaModificacion { get; private set; }

        /// <summary>
        /// Constructor para crear un nuevo contacto.
        /// </summary>
        public ContactoCliente(Guid contactoId, TipoContacto tipo, string valor)
        {
            ContactoId = contactoId != Guid.Empty ? contactoId : throw new ArgumentException("El Id no puede ser vacío.", nameof(contactoId));
            Tipo = tipo;
            Valor = !string.IsNullOrWhiteSpace(valor) ? valor : throw new ArgumentNullException(nameof(valor));
            FechaCreacion = DateTime.UtcNow;
        }

        /// <summary>
        /// Actualiza el valor del contacto.
        /// </summary>
        public void ActualizarValor(string nuevoValor)
        {
            if (string.IsNullOrWhiteSpace(nuevoValor))
                throw new ArgumentNullException(nameof(nuevoValor));
            Valor = nuevoValor;
            FechaModificacion = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Tipos de contacto permitidos.
    /// </summary>
    public enum TipoContacto
    {
        EMAIL_SECUNDARIO,
        TELEFONO_SECUNDARIO,
        DIRECCION
    }
}