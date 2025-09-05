using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Datos para registrar un nuevo establecimiento dentro de la empresa del contexto.
    /// </summary>
    public sealed class RegistrarEstablecimientoInputDto
    {
        /// <summary>Código del establecimiento (ej: "02").</summary>
        public string Codigo { get; init; } = string.Empty;

        /// <summary>Nombre del establecimiento (ej: "Tienda Centro").</summary>
        public string Nombre { get; init; } = string.Empty;

        /// <summary>Dirección fiscal (PE) del establecimiento.</summary>
        public DireccionFiscalDto Direccion { get; init; } = new();

    // Eliminado: la opción de marcar como principal ya no es parte del flujo de registro de establecimiento.

        public sealed class DireccionFiscalDto
        {
            /// <summary>País (fijo: "PE").</summary>
            public string PaisCodigo { get; init; } = "PE";

            /// <summary>Ubigeo SUNAT (ej: "150101").</summary>
            public string Ubigeo { get; init; } = string.Empty;

            /// <summary>Texto de dirección (línea).</summary>
            public string Direccion { get; init; } = string.Empty;

            /// <summary>Referencia opcional.</summary>
            public string? Referencia { get; init; }
        }
    }
}
