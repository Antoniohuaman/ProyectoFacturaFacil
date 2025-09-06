using System;

namespace ConfiguracionSistemaBC.Application.UseCases.Series
{
    /// <summary>Resumen de la serie registrada (útil para UI).</summary>
    public sealed class RegistrarSerieComprobanteOutputDto
    {
        public Guid Id { get; init; }
        public string EmpresaId { get; init; } = string.Empty;

        public string TipoComprobante { get; init; } = string.Empty; // "01"/"03"
        public string Serie { get; init; } = string.Empty;

        public string EstablecimientoId { get; init; } = string.Empty; // Guid en string
        public string TipoOperacion { get; init; } = "0101";

        public int CorrelativoInicial { get; init; } = 1;

        public bool EsPorDefecto { get; init; }
        public bool Habilitada { get; init; }

        public int Version { get; init; }
    }
}
