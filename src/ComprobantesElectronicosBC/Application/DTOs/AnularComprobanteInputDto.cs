using System;
using ComprobantesElectronicosBC.Domain.ValueObjects; // Para usar UsuarioSnapshot si deseas registrar quién anuló

namespace ComprobantesElectronicosBC.Application.UseCases.AnularComprobante
{
    /// <summary>
    /// Parámetros para anular (anulación lógica) un comprobante existente.
    /// Reglas en este DTO:
    /// - ComprobanteId es obligatorio.
    /// - Motivo es obligatorio (1..1000) — alineado con <see cref="NotaInterna"/> del dominio.
    /// - Usuario (opcional) por si quieres registrar quién ejecutó la anulación.
    /// </summary>
    public sealed record AnularComprobanteInputDto
    {
        /// <summary>Id del comprobante a anular.</summary>
        public Guid ComprobanteId { get; init; }

        /// <summary>Motivo/descripción de la anulación (1..1000).</summary>
        public string Motivo { get; init; } = string.Empty;

        /// <summary>Usuario que ejecuta la anulación (opcional).</summary>
        public UsuarioSnapshot? Usuario { get; init; }

        /// <summary>
        /// Marca temporal opcional que desees registrar; si es null, se usará el "ahora" de la capa que implemente la anulación.
        /// </summary>
        public DateTimeOffset? AnuladoEnUtc { get; init; }
    }
}
