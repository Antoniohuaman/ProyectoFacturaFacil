using System;
using System.Collections.Generic;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Application.Clientes.Contactos.Agregar
{
    /// <summary>
    /// Entrada para agregar un contacto secundario a un cliente existente.
    /// </summary>
    public sealed class AgregarContactoClienteInputDto
    {
        /// <summary>Cliente destino.</summary>
        public Guid ClienteId { get; init; }

        /// <summary>Nombre completo del contacto (obligatorio).</summary>
        public string NombreContacto { get; init; } = null!;

        /// <summary>
        /// Tipo de documento del contacto (opcional). Si se informa, DEBE ser DNI.
        /// </summary>
        public TipoDocumento? TipoDocumentoContacto { get; init; }

        /// <summary>
        /// Número de documento del contacto (opcional). Si se informa, emparejar con TipoDocumentoContacto = DNI.
        /// </summary>
        public string? NumeroDocumentoContacto { get; init; }

        /// <summary>Correos electrónicos opcionales (0..5 aprox).</summary>
        public List<string>? Emails { get; init; }

        /// <summary>Teléfonos opcionales (0..n). Cada item puede contener uno o varios números (VO Telefono).</summary>
        public List<string>? Telefonos { get; init; }

        /// <summary>Dirección opcional.</summary>
        public string? Direccion { get; init; }
    }
}
