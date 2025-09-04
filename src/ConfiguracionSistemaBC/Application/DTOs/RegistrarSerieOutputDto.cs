using System;

namespace ConfiguracionSistemaBC.Application.UseCases.Dtos
{
    /// <summary>Resultado de registrar una serie.</summary>
    public sealed class RegistrarSerieOutputDto
    {
        public Guid SerieId { get; init; }
        public string EmpresaId { get; init; } = string.Empty;
        public Guid EstablecimientoId { get; init; }

        public string TipoComprobanteCodigo { get; init; } = string.Empty;
        public string Serie { get; init; } = string.Empty;
        public int Correlativo { get; init; }

        public string TipoOperacionCodigo { get; init; } = string.Empty;
        public bool EsPorDefecto { get; init; }
        public bool Bloqueada { get; init; }
        public int Version { get; init; }
    }
}
