using System;

namespace GestionClientesBC.Application.Clientes.Deshabilitar
{
    public sealed class DeshabilitarClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;

        public bool Deshabilitado { get; init; }
        public string EstadoCodigo { get; init; } = null!; // "INH"
        public DateTime FechaDeshabilitacionUtc { get; init; }
        public string? MotivoDeshabilitacion { get; init; }

        // Trazabilidad
        public string TipoDocumento { get; init; } = null!;
        public string NumeroDocumento { get; init; } = null!;
    }
}
