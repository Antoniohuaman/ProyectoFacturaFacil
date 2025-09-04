using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.DTOs
{
    /// <summary>
    /// Petición para registrar N unidades de medida personalizadas.
    /// </summary>
    public sealed class RegistrarUnidadDeMedidaInputDto
    {
        /// <summary>Identidad opaca de la empresa (derivada del RUC canonizado).</summary>
        public string EmpresaId { get; init; } = string.Empty;

        /// <summary>Unidades a crear.</summary>
        public List<UnidadDeMedidaItem> Items { get; init; } = new();

        public sealed class UnidadDeMedidaItem
        {
            /// <summary>Código SUNAT/UNECE (p.ej., "NIU", "KGM", "LTR").</summary>
            public string Codigo { get; init; } = string.Empty;

            /// <summary>Nombre visible para UI (p.ej., "UNIDAD", "KILOGRAMO").</summary>
            public string Nombre { get; init; } = string.Empty;

            /// <summary>Si no se especifica, se asume true.</summary>
            public bool? Visible { get; init; } = true;

            /// <summary>Orden opcional; si no se envía, el Aggregate lo asigna automáticamente.</summary>
            public int? Orden { get; init; }

            /// <summary>Si se marca true y Visible=true, se establecerá como por defecto.</summary>
            public bool? EsPorDefecto { get; init; } = false;
        }
    }
}
