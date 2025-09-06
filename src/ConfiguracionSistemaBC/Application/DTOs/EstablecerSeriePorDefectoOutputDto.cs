using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>Resultado de marcar una serie como "por defecto".</summary>
    public sealed class EstablecerSeriePorDefectoOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;
        public Guid SerieComprobanteId { get; init; }
        public Guid EstablecimientoId { get; init; }
        public string TipoComprobante { get; init; } = string.Empty; // "01" | "03"
        public string Serie { get; init; } = string.Empty;

        /// <summary>True si la serie ya estaba marcada como por defecto (operación idempotente).</summary>
        public bool YaEraPorDefecto { get; init; }

        /// <summary>Id de la serie que estaba como por defecto antes del cambio (si existía y era distinta).</summary>
        public Guid? AnteriorSeriePorDefectoId { get; init; }

        /// <summary>Versión aplicada/esperada (para trazabilidad de concurrencia).</summary>
        public int VersionAplicada { get; init; }
    }
}
