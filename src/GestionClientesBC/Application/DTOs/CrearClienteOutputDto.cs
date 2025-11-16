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
        public string? NombreComercial { get; init; }
        public string? PaginaWeb { get; init; }
        public string? Observaciones { get; init; }
        public string? FotoPerfilNombreArchivo { get; init; }
        public string? FotoPerfilUrl { get; init; }
        public string Estado { get; init; } = null!;
        public DateTime FechaRegistroUtc { get; init; }
    }
}
