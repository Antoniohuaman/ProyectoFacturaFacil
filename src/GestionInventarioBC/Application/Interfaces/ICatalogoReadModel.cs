using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.Interfaces
{
    /// <summary>
    /// Read model/consulta al catálogo para resolver identidades de productos a partir de SKU/Nombre.
    /// Implementación en Adapters de Integración. Aquí sólo la interfaz.
    /// </summary>
    public interface ICatalogoReadModel
    {
        /// <summary>Resuelve un ProductoId por SKU dentro de la empresa actual. Devuelve null si no existe.</summary>
        Task<ProductoId?> TryGetProductoIdBySkuAsync(EmpresaId empresaId, string sku, CancellationToken ct = default);

        /// <summary>Busca productoIds por filtros opcionales de SKU y/o Nombre.</summary>
        Task<IReadOnlyList<ProductoId>> BuscarProductoIdsAsync(
            EmpresaId empresaId,
            string? sku,
            string? nombre,
            CancellationToken ct = default);

        /// <summary>
        /// Obtiene datos de presentación (SKU y Nombre) para un producto dado.
        /// Devuelve null si el producto no existe o no pertenece a la empresa.
        /// </summary>
        Task<(string Sku, string Nombre)?> TryGetSkuYNombreAsync(
            EmpresaId empresaId,
            ProductoId productoId,
            CancellationToken ct = default);
    }
}
