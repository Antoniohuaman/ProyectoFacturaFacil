namespace ListaPreciosBC.Application.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync(object evento);
    }
}