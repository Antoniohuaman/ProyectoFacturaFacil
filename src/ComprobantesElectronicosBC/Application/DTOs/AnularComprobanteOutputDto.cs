using System;

namespace ComprobantesElectronicosBC.Application.UseCases.AnularComprobante
{
    /// <summary>
    /// Resultado tras anular un comprobante.
    /// </summary>
    public sealed record AnularComprobanteOutputDto
    {
        public Guid ComprobanteId { get; init; }
        public string TipoComprobante { get; init; } = ""; // "01" / "03"
        public string Serie { get; init; } = "";
        public int Numero { get; init; }
        public string Estado { get; init; } = "Anulado";
        public DateTimeOffset AnuladoEnUtc { get; init; }

        /// <summary>Por conveniencia en la UI.</summary>
        public bool EstaAnulado => string.Equals(Estado, "Anulado", StringComparison.OrdinalIgnoreCase);
    }
}
