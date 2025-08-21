using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Repositories
{
    /// <summary>
    /// Contrato de persistencia para <see cref="ProductoSimple"/>.
    /// Define las operaciones CRUD y de consulta necesarias
    /// sobre el catálogo de productos.
    /// </summary>
    public interface IProductoRepository
    {
        /// <summary>
        /// Obtiene un <see cref="ProductoSimple"/> por su identificador.
        /// </summary>
        /// <param name="id">El Guid del producto.</param>
        /// <returns>El producto o null si no existe.</returns>
        Task<ProductoSimple?> GetByIdAsync(Guid id);

    /// <summary>
    /// Obtiene un <see cref="ProductoSimple"/> por su SKU.
    /// </summary>
    /// <param name="sku">El SKU del producto.</param>
    /// <returns>El producto o null si no existe.</returns>
    Task<ProductoSimple?> GetBySkuAsync(Sku sku);

        /// <summary>
        /// Devuelve todos los productos del catálogo.
        /// </summary>
        /// <returns>Una colección de <see cref="ProductoSimple"/>.</returns>
        Task<IReadOnlyCollection<ProductoSimple>> GetAllAsync();

        /// <summary>
        /// Inserta un nuevo <see cref="ProductoSimple"/> en el repositorio.
        /// </summary>
        /// <param name="producto">La entidad a agregar.</param>
        Task AddAsync(ProductoSimple producto);

        /// <summary>
        /// Actualiza un <see cref="ProductoSimple"/> existente.
        /// </summary>
        /// <param name="producto">La entidad con los cambios.</param>
        Task UpdateAsync(ProductoSimple producto);

        /// <summary>
        /// Elimina un <see cref="ProductoSimple"/> del repositorio.
        /// </summary>
        /// <param name="producto">La entidad a eliminar.</param>
        Task DeleteAsync(ProductoSimple producto);
    }
}
