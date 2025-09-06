using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>Resultado de la eliminación de una serie.</summary>
    public sealed class EliminarSerieComprobanteOutputDto
    {
        public bool Eliminado { get; init; }
        public Guid SerieComprobanteId { get; init; }
        public string EmpresaId { get; init; } = string.Empty;
        public Guid EstablecimientoId { get; init; }
        public string TipoComprobante { get; init; } = string.Empty; // "01" | "03"
        public string Serie { get; init; } = string.Empty;           // "FE01"/"BE01"/...
        public int VersionEliminada { get; init; }
    }
}
