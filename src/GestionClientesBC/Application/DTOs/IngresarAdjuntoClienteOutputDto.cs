using System;

namespace GestionClientesBC.Application.Clientes.Adjuntos.Ingresar
{
    /// <summary>
    /// Resultado de registrar un adjunto en un cliente.
    /// </summary>
    public sealed class IngresarAdjuntoClienteOutputDto
    {
        public Guid ClienteId { get; init; }
        public string EmpresaId { get; init; } = null!;

        public Guid AdjuntoId { get; init; }
        public string NombreArchivo { get; init; } = null!;
        public string Ruta { get; init; } = null!;
        public string? Comentario { get; init; }
        public DateTime FechaSubidaUtc { get; init; }

        /// <summary>Cantidad total de adjuntos del cliente tras la operación.</summary>
        public int TotalAdjuntosCliente { get; init; }

        /// <summary>Fecha/hora UTC tomada del evento de dominio, si estuvo disponible.</summary>
        public DateTime? FechaEventoUtc { get; init; }
    }
}
