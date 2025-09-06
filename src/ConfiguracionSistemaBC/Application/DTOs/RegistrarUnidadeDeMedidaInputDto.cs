using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Petición para registrar una o más unidades de medida personalizadas en la empresa.
    /// Si EmpresaId no se provee, se usa el del ITenantContext.
    /// </summary>
    public sealed class RegistrarUnidadDeMedidaInputDto
    {
        /// <summary>Empresa destino (GUID-string). Opcional: si no se envía, se toma del contexto.</summary>
        public string? EmpresaId { get; init; }

        /// <summary>Unidades a crear (seleccionadas del catálogo SUNAT/UNECE por código).</summary>
        public List<Item> Items { get; init; } = new();

        public sealed class Item
        {
            /// <summary>
            /// Código de la unidad (SUNAT/UNECE), p.ej.: "CMT", "MMT", "TNE".
            /// El usuario lo elige de un autocompletar normativo (no inventa el código).
            /// </summary>
            public string UnidadCodigo { get; init; } = string.Empty;

            /// <summary>
            /// Nombre visible en el sistema (texto mostrado al usuario), p.ej., "CENTÍMETRO".
            /// </summary>
            public string Nombre { get; init; } = string.Empty;

            /// <summary>Visibilidad del ítem. Por defecto true.</summary>
            public bool Visible { get; init; } = true;

            /// <summary>Orden opcional (si no se envía, el aggregate asigna el siguiente).</summary>
            public int? Orden { get; init; }

            /// <summary>
            /// Si se marca, esta unidad recién creada se establecerá como "por defecto".
            /// (máx. una por petición)
            /// </summary>
            public bool EsPorDefecto { get; init; } = false;
        }
    }
}
