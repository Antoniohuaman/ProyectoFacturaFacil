using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>Resultado del registro de serie en establecimiento.</summary>
    public sealed class RegistrarSerieEnEstablecimientoOutputDto
    {
        public Guid SerieComprobanteId { get; init; }
        public string EmpresaId { get; init; } = string.Empty;
        public Guid EstablecimientoId { get; init; }

        public string TipoComprobante { get; init; } = string.Empty; // "01" | "03"
        public string Serie { get; init; } = string.Empty;           // "FE01", "BE01", etc.
        public string TipoOperacion { get; init; } = string.Empty;   // "0101", ...
        public int SiguienteCorrelativo { get; init; }               // entero normalizado
        public bool EsPorDefecto { get; init; }
        public bool Habilitada { get; init; }
        public int Version { get; init; }
    }
}
