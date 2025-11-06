using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using SharedKernel.ValueObjects; // EmpresaId, ProductoId, EstablecimientoId
using System;

namespace ListaPreciosBC.Domain.Repositories
{
    /// <summary>
    /// Contrato de persistencia para el agregado PrecioProducto (precios por SKU).
    /// No impone detalles de almacenamiento ni de partición (empresa/sucursal).
    /// </summary>
    public interface IPrecioProductoRepository
    {
    /// <summary>Obtiene los precios por ProductoId (o null si no existe).</summary>
    Task<PrecioProducto?> ObtenerPorProductoIdAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, CancellationToken ct = default);

        /// <summary>
        /// Guarda con concurrencia optimista (expectedVersion = 0 para altas).
        /// Debe lanzar si la versión actual difiere de la esperada.
        /// </summary>
    Task GuardarAsync(PrecioProducto agregado, EmpresaId empresaId, EstablecimientoId? establecimientoId, int expectedVersion, CancellationToken ct = default);

        /// <summary>Elimina los precios por ProductoId (idempotente).</summary>
        Task EliminarAsync(EmpresaId empresaId, EstablecimientoId? establecimientoId, ProductoId productoId, int? expectedVersion = null, CancellationToken ct = default);
    }
}
