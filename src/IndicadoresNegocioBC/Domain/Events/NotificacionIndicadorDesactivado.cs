namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorDesactivado : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }

        public NotificacionIndicadorDesactivado(Guid notificacionIndicadorId)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
        }
    }
}
