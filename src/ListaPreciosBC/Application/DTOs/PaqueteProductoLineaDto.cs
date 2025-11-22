using ListaPreciosBC.Domain.Aggregates;

namespace ListaPreciosBC.Application.DTOs
{
    /// <summary>
    /// Línea de producto que compone un paquete.
    /// Por simplicidad en esta fase usamos directamente la línea de dominio.
    /// </summary>
    public sealed class PaqueteProductoLineaDto
    {
        /// <summary>
        /// Línea ya construida en dominio (ProductoId, UnidadDeMedida, Cantidad, PrecioUnitario).
        /// El mapeo desde SKU/unidad del front se hará en los adapters.
        /// </summary>
        public ProductoPaquete.LineaProductoPaquete Linea { get; init; }

        public PaqueteProductoLineaDto(ProductoPaquete.LineaProductoPaquete linea)
        {
            Linea = linea;
        }
    }
}
