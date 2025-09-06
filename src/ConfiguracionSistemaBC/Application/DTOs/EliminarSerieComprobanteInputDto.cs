using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Solicitud para eliminar una serie de comprobante.
    /// Requiere la versión esperada para concurrencia optimista.
    /// </summary>
    public sealed class EliminarSerieComprobanteInputDto
    {
        /// <summary>Identificador de la serie (Guid en texto).</summary>
        public string SerieComprobanteId { get; init; } = string.Empty;

        /// <summary>Versión esperada del agregado (concurrencia optimista).</summary>
        public int ExpectedVersion { get; init; }
    }
}
