using System;

namespace GestionClientesBC.Application.Clientes.Deshabilitar
{
    /// <summary>
    /// Entrada para deshabilitar un cliente existente.
    /// </summary>
    public sealed class DeshabilitarClienteInputDto
    {
        /// <summary>Identificador del cliente a deshabilitar.</summary>
        public Guid ClienteId { get; init; }

        /// <summary>Motivo opcional de deshabilitación.</summary>
        public string? Motivo { get; init; }

        /// <summary>
        /// Fecha/hora de deshabilitación. Si es null, se usa UtcNow.
        /// Se normaliza a UTC internamente.
        /// </summary>
        public DateTime? FechaDeshabilitacion { get; init; }
        /// <summary>Versión esperada del agregado para concurrencia optimista.</summary>
        public int? ExpectedVersion { get; init; }
    }
}
