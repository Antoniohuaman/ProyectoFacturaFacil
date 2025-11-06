using System.Threading;
using System.Threading.Tasks;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.Interfaces
{
    /// <summary>
    /// Read model del Catálogo para resolver identidades de productos a partir de SKU.
    /// Implementación real en Adapters de integración; aquí sólo la interfaz.
    /// </summary>
    public interface ICatalogoReadModel
    {
        /// <summary>Resuelve un ProductoId por SKU dentro de la empresa actual. Devuelve null si no existe.</summary>
        Task<ProductoId?> TryGetProductoIdBySkuAsync(EmpresaId empresaId, string sku, CancellationToken ct = default);
    }
}
