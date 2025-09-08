using System;

namespace GestionClientesBC.Application.Clientes.Crear
{
    public sealed class CrearClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;
        public string TipoDocumento { get; init; } = null!;
        public string NumeroDocumento { get; init; } = null!;
        public string? RazonSocial { get; init; }
        public string Nombres { get; init; } = string.Empty;
        public string Estado { get; init; } = null!;
        public DateTime FechaRegistroUtc { get; init; }
    }
}
