using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Petición para registrar una o más formas de pago personalizadas para la empresa actual.
    /// Si EmpresaId no se provee, se usa el del ITenantContext.
    /// </summary>
    public sealed class RegistrarFormasDePagoInputDto
    {
        /// <summary>Empresa destino (GUID-string). Opcional: si no se envía, se toma del contexto.</summary>
        public string? EmpresaId { get; init; }

        /// <summary>Conjunto de formas de pago a crear.</summary>
        public List<FormaPagoItem> Items { get; init; } = new();

        public sealed class FormaPagoItem
        {
            /// <summary>Tipo: "CONTADO" o "CREDITO".</summary>
            public string Tipo { get; init; } = string.Empty;

            /// <summary>
            /// Método para CONTADO (opcional): "EFECTIVO", "TARJETA", "TRANSFERENCIA", "YAPE", "PLIN", "DEPOSITO".
            /// Ignorado para "CREDITO".
            /// </summary>
            public string? Metodo { get; init; }

            /// <summary>Nombre visible para UI/impresión, requerido. (p.ej., "Efectivo", "Crédito 45 días").</summary>
            public string Nombre { get; init; } = string.Empty;

            /// <summary>Visibilidad en UI. Por defecto true.</summary>
            public bool Visible { get; init; } = true;

            /// <summary>Orden opcional (si no se envía, el aggregate asigna el siguiente).</summary>
            public int? Orden { get; init; }

            /// <summary>Si se marca, esta nueva forma de pago se establecerá como “por defecto”.</summary>
            public bool EsPorDefecto { get; init; } = false;
        }
    }
}
