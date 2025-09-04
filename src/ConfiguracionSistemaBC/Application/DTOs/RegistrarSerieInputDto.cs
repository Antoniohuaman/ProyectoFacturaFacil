using System;

namespace ConfiguracionSistemaBC.Application.UseCases.Dtos
{
    /// <summary>
    /// DTO de entrada para registrar una nueva serie de comprobante.
    /// Los códigos siguen la normativa SUNAT:
    /// - Tipo de Comprobante: "01" Factura, "03" Boleta, etc.
    /// - Tipo de Operación: "0101" Venta interna, etc. (opcional: si es null se usa el Default)
    /// </summary>
    public sealed class RegistrarSerieInputDto
    {
        public string Ruc { get; init; } = string.Empty;
        public Guid EstablecimientoId { get; init; }

        /// <summary>Código del tipo de comprobante (p.ej., "01", "03").</summary>
        public string TipoComprobanteCodigo { get; init; } = string.Empty;

        /// <summary>Código de la serie (p.ej., "FE01", "BE01").</summary>
        public string Serie { get; init; } = string.Empty;

        /// <summary>Correlativo inicial (p.ej., 1).</summary>
        public int CorrelativoInicial { get; init; }

        /// <summary>Código del tipo de operación SUNAT (p.ej., "0101"). Opcional.</summary>
        public string? TipoOperacionCodigo { get; init; }

        /// <summary>Si se debe marcar como serie por defecto para el tipo.</summary>
        public bool EsPorDefecto { get; init; } = false;
    }
}
