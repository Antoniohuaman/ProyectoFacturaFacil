using System;

namespace GestionClientesBC.Application.Clientes.Adjuntos.Eliminar
{
    /// <summary>
    /// Resultado de eliminar un adjunto.
    /// </summary>
    public sealed class EliminarAdjuntoClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;
        public Guid AdjuntoId { get; init; }
        public int TotalAdjuntos { get; init; }
        public DateTime? FechaEventoUtc { get; init; }
        public int Version { get; init; }
    }
}
