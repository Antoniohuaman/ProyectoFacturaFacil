using System;

namespace ComprobantesElectronicosBC.Application.UseCases.ConsultarComprobante
{
    /// <summary>
    /// Criterios de consulta:
    ///  - Por Id (preferente)
    ///  - Por Serie–Número
    /// Si ambos se proveen, se prioriza Id.
    /// </summary>
    public sealed record ConsultarComprobanteInputDto
    {
        /// <summary>Id del comprobante (preferido por robustez).</summary>
        public Guid? ComprobanteId { get; init; }

        /// <summary>Serie 1..4 (A–Z, 0–9). La validación fina la realiza el VO <see cref="ComprobantesElectronicosBC.Domain.ValueObjects.SerieYNumero"/>.</summary>
        public string? Serie { get; init; }

        /// <summary>Número correlativo (1..99’999’999).</summary>
        public int? Numero { get; init; }

        public bool EsBusquedaPorId => ComprobanteId.HasValue && ComprobanteId.Value != Guid.Empty;
        public bool EsBusquedaPorSerieNumero => !EsBusquedaPorId && !string.IsNullOrWhiteSpace(Serie) && Numero.HasValue;
    }
}
