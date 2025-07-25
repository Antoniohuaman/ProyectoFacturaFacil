using System.Threading.Tasks;

namespace CatalogoArticulosBC.Domain.Events
{
    public interface IEventBus
    {
        Task Publish<TEvent>(TEvent @event) where TEvent : class;
    }
}