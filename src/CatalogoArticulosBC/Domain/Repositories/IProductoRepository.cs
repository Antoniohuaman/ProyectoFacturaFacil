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
        /// <param name="empresaId">Empresa (tenant) a la que debe pertenecer el producto.</param>
        /// <returns>El producto o null si no existe.</returns>
        Task<ProductoSimple?> GetByIdAsync(Guid id, EmpresaId empresaId);

    /// <summary>
    /// Obtiene un <see cref="ProductoSimple"/> por su SKU.
    /// </summary>
    /// <param name="sku">El SKU del producto.</param>
    /// <returns>El producto o null si no existe.</returns>
        Task<ProductoSimple?> GetBySkuAsync(Sku sku, EmpresaId empresaId);

        /// <summary>
        /// Devuelve todos los productos del catálogo.
        /// </summary>
        /// <returns>Una colección de <see cref="ProductoSimple"/>.</returns>
        Task<IReadOnlyCollection<ProductoSimple>> GetAllAsync(EmpresaId empresaId);

    /// <summary>
    /// Inserta un nuevo <see cref="ProductoSimple"/> en el repositorio.
    /// </summary>
    /// <param name="producto">La entidad a agregar.</param>
    /// <param name="ct">Token de cancelación.</param>
        Task AddAsync(ProductoSimple producto, System.Threading.CancellationToken ct);

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
        Task<int> DeleteAllAsync(EmpresaId empresaId, CancellationToken ct = default);
        /// <summary>
        /// Elimina múltiples productos por sus IDs para una empresa específica.
        /// </summary>
        /// <param name="productoIds">IDs de los productos a eliminar.</param>
        /// <param name="empresaId">Empresa a la que pertenecen los productos.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Cantidad de productos eliminados.</returns>
        Task<int> DeleteManyAsync(IReadOnlyCollection<Guid> productoIds, EmpresaId empresaId, System.Threading.CancellationToken ct = default);
        

    // Consultas especializadas
    Task<ProductoSimple?> GetByCodigoBarrasAsync(string codigoBarras, EmpresaId empresaId);
    Task<ProductoSimple?> GetByCodigoFabricaAsync(string codigoFabrica, EmpresaId empresaId);
    Task<ProductoSimple?> GetByNombreAsync(string nombre, EmpresaId empresaId);
    Task<IEnumerable<ProductoSimple>> ListarPorCategoriaAsync(Categoria categoria, EmpresaId empresaId);
    Task<IEnumerable<ProductoSimple>> ListarHabilitadosAsync(EmpresaId empresaId);
    Task<IEnumerable<ProductoSimple>> ListarDeshabilitadosAsync(EmpresaId empresaId);
        Task<IEnumerable<ProductoSimple>> BuscarPorFiltroAsync(FiltroProducto filtro);

    // Verificaciones
        Task<bool> ExistsByCodigoAsync(string codigo);
        Task<bool> ExistsByNombreAsync(string nombre);
        /// <summary>
        /// Verifica si existe un SKU para una empresa específica.
        /// </summary>
        /// <param name="sku">SKU a verificar.</param>
        /// <param name="empresaId">Empresa a la que pertenece el SKU.</param>
        /// <param name="ct">Token de cancelación.</param>
        Task<bool> ExisteSkuAsync(Sku sku, EmpresaId empresaId, System.Threading.CancellationToken ct);

        // Operaciones de importación/exportación
        Task ImportarProductosAsync(IEnumerable<ProductoSimple> productos);
        Task<IEnumerable<ProductoSimple>> ExportarProductosAsync(FiltroExportacion filtro);

        // Multimedia asociada
        Task<IEnumerable<MultimediaProducto>> GetMultimediaByProductoIdAsync(Guid productoId);
    }
}
