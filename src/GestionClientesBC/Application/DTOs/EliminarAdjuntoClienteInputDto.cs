using System;

namespace GestionClientesBC.Application.Clientes.Adjuntos.Eliminar
{
    /// <summary>
    /// Entrada para eliminar un adjunto previamente registrado en el cliente.
    /// </summary>
    public sealed class EliminarAdjuntoClienteInputDto
    {
        public Guid ClienteId { get; init; }
        public Guid AdjuntoId { get; init; }
        public int? ExpectedVersion { get; init; }
    }
}
