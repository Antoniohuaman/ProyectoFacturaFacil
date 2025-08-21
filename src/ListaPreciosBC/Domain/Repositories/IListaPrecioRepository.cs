using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;

namespace ListaPreciosBC.Domain.Repositories
{
    /// <summary>
    /// Contrato de persistencia para el agregado ListaPrecio (plantilla de columnas).
    /// El dominio asume 1 plantilla activa; la infraestructura decide cómo almacenarla.
    /// </summary>
    public interface IListaPrecioRepository
    {
        /// <summary>Obtiene la plantilla activa (o null si no existe).</summary>
        Task<ListaPrecio?> ObtenerActivaAsync(CancellationToken ct = default);

        /// <summary>Obtiene una plantilla por Id (o null si no existe).</summary>
        Task<ListaPrecio?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Guarda el agregado con concurrencia optimista.
        /// Usa <paramref name="expectedVersion"/> = 0 para altas.
        /// Debe lanzar si la versión actual difiere de la esperada.
        /// </summary>
        Task GuardarAsync(ListaPrecio agregado, int expectedVersion, CancellationToken ct = default);
    }
}
