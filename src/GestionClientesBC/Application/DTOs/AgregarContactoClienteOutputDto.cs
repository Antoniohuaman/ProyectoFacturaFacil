using System;

namespace GestionClientesBC.Application.Clientes.Contactos.Agregar
{
    public sealed class AgregarContactoClienteOutputDto
    {
        // Identidad
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;
        public Guid ContactoId { get; init; }

        // Datos del contacto
        public string NombreContacto { get; init; } = null!;
        public string? DocumentoIdentidad { get; init; } // Ej: "DNI 12345678"
        public string[] Emails { get; init; } = Array.Empty<string>();
        public string[] Telefonos { get; init; } = Array.Empty<string>();
        public string? Direccion { get; init; }

        // Trazabilidad
        public DateTime FechaCreacionUtc { get; init; }
        public DateTime FechaEventoUtc { get; init; }
    }
}
