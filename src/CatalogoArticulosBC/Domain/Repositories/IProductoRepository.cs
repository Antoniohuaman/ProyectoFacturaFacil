using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Filters;

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

    // Consultas especializadas
    Task<ProductoSimple?> GetByCodigoBarrasAsync(string codigoBarras);
    Task<ProductoSimple?> GetByCodigoFabricaAsync(string codigoFabrica);
    Task<ProductoSimple?> GetByNombreAsync(string nombre);
    Task<IEnumerable<ProductoSimple>> ListarPorCategoriaAsync(Categoria categoria);
    Task<IEnumerable<ProductoSimple>> ListarHabilitadosAsync();
    Task<IEnumerable<ProductoSimple>> ListarDeshabilitadosAsync();
    Task<IEnumerable<ProductoSimple>> BuscarPorFiltroAsync(FiltroProducto filtro);

    // Verificaciones
    Task<bool> ExistsByCodigoAsync(string codigo);
    Task<bool> ExistsByNombreAsync(string nombre);

    // Operaciones de importación/exportación
    Task ImportarProductosAsync(IEnumerable<ProductoSimple> productos);
    Task<IEnumerable<ProductoSimple>> ExportarProductosAsync(FiltroExportacion filtro);

    // Multimedia asociada
    Task<IEnumerable<MultimediaProducto>> GetMultimediaByProductoIdAsync(Guid productoId);
    }
}
