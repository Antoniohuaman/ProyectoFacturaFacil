using System;

namespace GestionClientesBC.Application.Clientes.Habilitar
{
    /// <summary>
    /// Resultado de habilitar a un cliente.
    /// </summary>
    public sealed class HabilitarClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;

        public bool Habilitado { get; init; }
        public string EstadoCodigo { get; init; } = null!; // "HAB"
        public DateTime FechaHabilitacionUtc { get; init; }

        // Trazabilidad
        public string TipoDocumento { get; init; } = null!;
        public string NumeroDocumento { get; init; } = null!;
    }
}
