using System;

namespace CatalogoArticulosBC.Application.UseCases.HabilitarProducto
{
    /// <summary>
    /// Resultado de la habilitación del producto.
    /// </summary>
    public sealed class HabilitarProductoOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;
        public Guid ProductoId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;

        /// <summary>Usuario responsable de la habilitación.</summary>
        public string Usuario { get; init; } = string.Empty;

        /// <summary>Motivo opcional (auditoría).</summary>
        public string? Motivo { get; init; }

        /// <summary>Estado actual del producto (debe ser true tras habilitar).</summary>
        public bool Habilitado { get; init; }

        /// <summary>Indica si el producto ya estaba habilitado antes de ejecutar el caso.</summary>
        public bool YaEstabaHabilitado { get; init; }

        public DateTimeOffset EjecutadoEnUtc { get; init; }
        public bool Exitoso { get; init; }
    }
}
