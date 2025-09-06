using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Solicitud para marcar una serie como "por defecto" para su tipo dentro de la empresa actual.
    /// </summary>
    public sealed class EstablecerSeriePorDefectoInputDto
    {
        /// <summary>Id de la serie (Guid en texto).</summary>
        public string SerieComprobanteId { get; init; } = string.Empty;

        /// <summary>Versión esperada para concurrencia optimista.</summary>
        public int ExpectedVersion { get; init; }
    }
}
