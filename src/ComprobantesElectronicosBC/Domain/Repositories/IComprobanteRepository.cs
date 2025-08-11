using System;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Domain.Aggregates;

namespace ComprobantesElectronicosBC.Domain.Repositories
{
    /// <summary>
    /// Repositorio del agregado <see cref="ComprobanteElectronico"/> dentro del BC ComprobantesElectronicosBC.
    /// Mantén la infraestructura (EF/Core Dapper, etc.) implementando esta interfaz en otra capa.
    /// </summary>
    public interface IComprobanteRepository
    {
        /// <summary>Obtiene un comprobante por su identidad.</summary>
        Task<ComprobanteElectronico?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Busca por Serie–Número (si aún no existe devolverá null).
        /// Útil para validar unicidad o mostrar detalles por identificador visible.
        /// </summary>
        Task<ComprobanteElectronico?> GetBySerieNumeroAsync(string serie, int numero, CancellationToken ct = default);

        /// <summary>Devuelve true si ya existe un comprobante con la Serie–Número dada.</summary>
        Task<bool> ExistsSerieNumeroAsync(string serie, int numero, CancellationToken ct = default);

        /// <summary>Agrega un nuevo agregado al almacenamiento.</summary>
        Task AddAsync(ComprobanteElectronico aggregate, CancellationToken ct = default);

        /// <summary>Marca el agregado como modificado (si usas EF Core, el tracking suele bastar).</summary>
        Task UpdateAsync(ComprobanteElectronico aggregate, CancellationToken ct = default);

        /// <summary>Elimina por Id (opcional si en tu dominio prefieres “anulación lógica”).</summary>
        Task RemoveAsync(Guid id, CancellationToken ct = default);
    }
}
