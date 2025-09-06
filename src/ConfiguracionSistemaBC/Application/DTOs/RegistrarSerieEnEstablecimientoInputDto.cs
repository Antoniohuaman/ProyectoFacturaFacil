using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Solicitud para registrar una nueva serie dentro de un establecimiento existente.
    /// </summary>
    public sealed class RegistrarSerieEnEstablecimientoInputDto
    {
        /// <summary>Id del establecimiento (Guid en texto). Es del contexto de la empresa actual.</summary>
        public string EstablecimientoId { get; init; } = string.Empty;

        /// <summary>Tipo de comprobante (código o alias). Ej.: "01", "FACTURA", "03", "BOLETA".</summary>
        public string TipoComprobante { get; init; } = "01";

        /// <summary>Código de la serie. Debe respetar prefijo por tipo (F para 01, B para 03). Ej.: "FE01", "BE01".</summary>
        public string Serie { get; init; } = string.Empty;

        /// <summary>Tipo de operación (Cat. 51). Código o alias. Default: "0101".</summary>
        public string TipoOperacion { get; init; } = "0101";

        /// <summary>Correlativo inicial (1..99,999,999). Acepta texto numérico con ceros a la izquierda.</summary>
        public string CorrelativoInicial { get; init; } = "1";

        /// <summary>Si debe quedar marcada como “por defecto” para el tipo. (opcional; default false)</summary>
        public bool? EsPorDefecto { get; init; }

        /// <summary>Visibilidad/habilitación inicial. (opcional; default true)</summary>
        public bool? Habilitada { get; init; }
    }
}
