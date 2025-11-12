using System;

namespace GestionClientesBC.Application.Clientes.Habilitar
{
    /// <summary>
    /// Entrada para habilitar un cliente existente.
    /// </summary>
    public sealed class HabilitarClienteInputDto
    {
        /// <summary>Identificador del cliente a habilitar.</summary>
        public Guid ClienteId { get; init; }
        /// <summary>Versión esperada del agregado para concurrencia optimista.</summary>
        public int? ExpectedVersion { get; init; }
    }
}
