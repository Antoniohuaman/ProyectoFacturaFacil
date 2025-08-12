using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Domain.Repositories
{
    /// <summary>
    /// Contrato de persistencia del Aggregate Root IndicadorNegocio.
    /// La “clave natural” es: TipoIndicador + Periodo (alineado) + SegmentoIndicador.
    /// </summary>
    public interface IIndicadorNegocioRepository
    {
        /// <summary>
        /// Obtiene un agregado por su clave natural; null si no existe.
        /// </summary>
        Task<IndicadorNegocio?> GetByClaveAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            CancellationToken ct = default);

        /// <summary>
        /// Agrega un nuevo agregado.
        /// </summary>
        Task AddAsync(IndicadorNegocio agregado, CancellationToken ct = default);

        /// <summary>
        /// Actualiza un agregado existente (usa concurrencia optimista con Version si aplica).
        /// </summary>
        Task UpdateAsync(IndicadorNegocio agregado, CancellationToken ct = default);

        /// <summary>
        /// (Opcional) Verifica existencia por clave natural sin hidratar el agregado.
        /// </summary>
        Task<bool> ExistsAsync(
            IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            CancellationToken ct = default);
    }
}