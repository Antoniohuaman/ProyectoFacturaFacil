using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.DTOs
{
    /// <summary>
    /// Petición para registrar N formas de pago personalizadas.
    /// </summary>
    public sealed class RegistrarFormasDePagoInputDto
    {
        /// <summary>Identidad opaca de la empresa (Aggregate), p.ej. derivada del RUC.</summary>
        public string EmpresaId { get; init; } = string.Empty;

        /// <summary>Lista de elementos a crear.</summary>
        public List<FormaDePagoItem> Items { get; init; } = new();

        public sealed class FormaDePagoItem
        {
            /// <summary>"10" (Contado) ó "20" (Crédito).</summary>
            public string PaymentMeansCode { get; init; } = string.Empty;

            /// <summary>
            /// Sólo aplica para CONTADO:
            /// código del método visible (EFECTIVO, TARJETA, TRANSFERENCIA, YAPE, PLIN, ...).
            /// Si es null/empty y PaymentMeansCode="10", se usará el método "CONTADO".
            /// </summary>
            public string? MetodoCodigo { get; init; }

            /// <summary>Etiqueta legible opcional para el método (se muestra en UI si viene).</summary>
            public string? MetodoNombre { get; init; }

            /// <summary>Nombre visible de la opción en UI (ej.: "Efectivo", "Crédito 30 días").</summary>
            public string Nombre { get; init; } = string.Empty;

            /// <summary>Si no se especifica, se asume true.</summary>
            public bool? Visible { get; init; } = true;

            /// <summary>Orden opcional; si no se envía, el Aggregate le asigna el siguiente.</summary>
            public int? Orden { get; init; }

            /// <summary>Si se marca true y Visible=true, se establece como por defecto.</summary>
            public bool? EsPorDefecto { get; init; } = false;
        }
    }
}
