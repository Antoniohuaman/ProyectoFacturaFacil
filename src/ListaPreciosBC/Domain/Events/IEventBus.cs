using System.Threading.Tasks;

namespace ListaPreciosBC.Domain.Events
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event);
    }
}