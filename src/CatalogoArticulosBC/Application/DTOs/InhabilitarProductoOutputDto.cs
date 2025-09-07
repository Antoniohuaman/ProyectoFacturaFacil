using System;

namespace CatalogoArticulosBC.Application.UseCases.InhabilitarProducto
{
    /// <summary>
    /// Resultado de la inhabilitación.
    /// </summary>
    public sealed class InhabilitarProductoOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;
        public Guid ProductoId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string Motivo { get; init; } = string.Empty;

        /// <summary>Estado actual del producto (debe ser false tras inhabilitar).</summary>
        public bool Habilitado { get; init; }

        /// <summary>Indica si el producto ya estaba inhabilitado antes de ejecutar el caso.</summary>
        public bool YaEstabaInhabilitado { get; init; }

        public DateTimeOffset EjecutadoEnUtc { get; init; }
        public bool Exitoso { get; init; }
    }
}
