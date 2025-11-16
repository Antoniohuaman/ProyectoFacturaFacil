using System;

namespace GestionClientesBC.Application.Clientes.Editar
{
    public sealed class EditarClienteOutputDto
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
        public string? Correo { get; init; }
        public string? Telefonos { get; init; }
        public string? TipoCliente { get; init; } // "C","P","CP"
        public string? RolCliente { get; init; }  // "SIN","MAY","MIN","DIS","REV"
        public string? Estado { get; init; }      // "HAB","INH"
        public DateTime FechaRegistroUtc { get; init; }
        public DateTime? FechaUltimaModificacionUtc { get; init; }
    }
}
