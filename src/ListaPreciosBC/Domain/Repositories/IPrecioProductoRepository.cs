using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using SharedKernel.ValueObjects; // Sku

namespace ListaPreciosBC.Domain.Repositories
{
    /// <summary>
    /// Contrato de persistencia para el agregado PrecioProducto (precios por SKU).
    /// No impone detalles de almacenamiento ni de partición (empresa/sucursal).
    /// </summary>
    public interface IPrecioProductoRepository
    {
        /// <summary>Obtiene los precios de un SKU (o null si no existe).</summary>
        Task<PrecioProducto?> ObtenerPorSkuAsync(Sku sku, CancellationToken ct = default);

        /// <summary>
        /// Guarda con concurrencia optimista (expectedVersion = 0 para altas).
        /// Debe lanzar si la versión actual difiere de la esperada.
        /// </summary>
        Task GuardarAsync(PrecioProducto agregado, int expectedVersion, CancellationToken ct = default);

        /// <summary>Elimina los precios de un SKU (idempotente).</summary>
        Task EliminarAsync(Sku sku, int? expectedVersion = null, CancellationToken ct = default);
    }
}
