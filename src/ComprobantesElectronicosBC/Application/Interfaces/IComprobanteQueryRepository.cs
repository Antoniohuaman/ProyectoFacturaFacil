using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.ReadModels;

namespace ComprobantesElectronicosBC.Application.Interfaces
{
    /// <summary>
    /// Puerto de consultas (read side) para listar comprobantes en forma de
    /// proyecciones livianas (ReadModels), con filtros y paginación.
    /// La implementación concreta vive en Adapters/Infrastructure.
    /// </summary>
    public interface IComprobanteQueryRepository
    {
        /// <summary>
        /// Lista comprobantes resumidos aplicando filtros/orden/paginación.
        /// Devuelve la página solicitada y el total de elementos (para meta de paginación).
        /// </summary>
        Task<(IReadOnlyList<ComprobanteResumenDto> Items, int TotalItems)> ListarAsync(
            DateOnly? desde,
            DateOnly? hasta,
            string? sortBy,
            bool sortDesc,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);
    }
}
