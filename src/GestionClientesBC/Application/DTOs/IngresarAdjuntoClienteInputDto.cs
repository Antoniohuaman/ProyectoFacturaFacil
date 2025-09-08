using System;

namespace GestionClientesBC.Application.Clientes.Adjuntos.Ingresar
{
    /// <summary>
    /// Entrada para registrar un adjunto en la ficha de un cliente existente.
    /// </summary>
    public sealed class IngresarAdjuntoClienteInputDto
    {
        /// <summary>Cliente destino.</summary>
        public Guid ClienteId { get; init; }

        /// <summary>Identificador del adjunto. Si no se envía o es Guid.Empty, se generará uno nuevo.</summary>
        public Guid? AdjuntoId { get; init; }

        /// <summary>Nombre del archivo (por ejemplo: "contrato.pdf").</summary>
        public string NombreArchivo { get; init; } = null!;

        /// <summary>Ruta o localización del archivo (por ejemplo: "/files/contratos/contrato.pdf").</summary>
        public string Ruta { get; init; } = null!;

        /// <summary>Comentario opcional del usuario.</summary>
        public string? Comentario { get; init; }

        /// <summary>
        /// Fecha de subida; si es null se usa UtcNow. Si viene con Kind distinto a UTC, se normaliza a UTC.
        /// </summary>
        public DateTime? FechaSubida { get; init; }
    }
}
