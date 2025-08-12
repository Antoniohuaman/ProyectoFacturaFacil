using System.Threading;
using System.Threading.Tasks;

namespace IndicadoresNegocioBC.Application.Interfaces
{
    /// <summary>Puerto para publicar eventos de integración (bus, cola, etc.).</summary>
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event, CancellationToken ct = default);
    }
}