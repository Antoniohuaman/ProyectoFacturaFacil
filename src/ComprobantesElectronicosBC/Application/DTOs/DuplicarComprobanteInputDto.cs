using System;

namespace ComprobantesElectronicosBC.Application.UseCases.DuplicarComprobante
{
    /// <summary>
    /// Parámetros para duplicar un comprobante existente.
    /// - SourceId: Id del comprobante a duplicar (obligatorio).
    /// - Serie/Número: opcionales; si ambos se indican, se valida unicidad antes de duplicar.
    /// - NuevaFechaEmision: opcional; si se indica, la implementación del duplicador debe respetarla.
    /// </summary>
    public sealed record DuplicarComprobanteInputDto
    {
        /// <summary>Id del comprobante origen a duplicar.</summary>
        public Guid SourceId { get; init; }

        /// <summary>Serie destino (1..4 A-Z/0-9). Debe venir junto con <see cref="Numero"/> si se usa.</summary>
        public string? Serie { get; init; }

        /// <summary>Número destino (1..99,999,999). Debe venir junto con <see cref="Serie"/> si se usa.</summary>
        public int? Numero { get; init; }

        /// <summary>
        /// Fecha de emisión a aplicar al duplicado (opcional). 
        /// Si se omite, la implementación puede usar "hoy" u otra regla definida.
        /// </summary>
        public DateOnly? NuevaFechaEmision { get; init; }
    }
}
