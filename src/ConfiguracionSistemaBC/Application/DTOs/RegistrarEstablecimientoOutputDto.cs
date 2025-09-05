using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resultado del registro de establecimiento.
    /// </summary>
    public sealed class RegistrarEstablecimientoOutputDto
    {
        /// <summary>Identidad opaca de la empresa (UUID string).</summary>
        public string EmpresaId { get; init; } = string.Empty;

        /// <summary>Identificador del establecimiento creado.</summary>
        public Guid EstablecimientoId { get; init; }

        /// <summary>Código de establecimiento.</summary>
        public string Codigo { get; init; } = string.Empty;

        /// <summary>Nombre de establecimiento.</summary>
        public string Nombre { get; init; } = string.Empty;

        /// <summary>Dirección (línea textual).</summary>
        public string Direccion { get; init; } = string.Empty;

        /// <summary>Ubigeo SUNAT.</summary>
        public string Ubigeo { get; init; } = string.Empty;

        /// <summary>Indica si quedó como establecimiento principal.</summary>
        public bool EsPrincipal { get; init; }
    }
}
