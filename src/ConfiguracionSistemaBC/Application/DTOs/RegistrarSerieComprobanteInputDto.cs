using System;

namespace ConfiguracionSistemaBC.Application.UseCases.Series
{
    /// <summary>
    /// Datos para registrar una nueva Serie de Comprobante en la empresa del contexto.
    /// </summary>
    public sealed class RegistrarSerieComprobanteInputDto
    {
        /// <summary>Código o alias del tipo de comprobante (ej. "01", "FACTURA", "03", "BOLETA").</summary>
        public string TipoComprobante { get; init; } = string.Empty;

        /// <summary>Código de serie (4 chars). Debe respetar convención: F*** para 01, B*** para 03.</summary>
        public string Serie { get; init; } = string.Empty;

        /// <summary>Correlativo con el que se inicia la numeración (>=1).</summary>
        public int CorrelativoInicial { get; init; } = 1;

        /// <summary>
        /// Id del establecimiento (Guid en string) al que se asigna la serie.
        /// Debe existir dentro de la empresa del contexto.
        /// </summary>
        public string EstablecimientoId { get; init; } = string.Empty;

        /// <summary>
        /// Código o alias del tipo de operación (Cat. 51). Por defecto "0101" (Venta interna).
        /// </summary>
        public string? TipoOperacion { get; init; } = "0101";

        /// <summary>Marca esta serie como “por defecto” para el tipo (exclusivo por tipo).</summary>
        public bool EsPorDefecto { get; init; } = false;

        /// <summary>Visible/habilitada para emisión. Por defecto true.</summary>
        public bool Habilitada { get; init; } = true;
    }
}
