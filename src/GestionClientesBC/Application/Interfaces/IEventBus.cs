using System.Threading.Tasks;

namespace GestionClientesBC.Application.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync(object evento);
    }
}