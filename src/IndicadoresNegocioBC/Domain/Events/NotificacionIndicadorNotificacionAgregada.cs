namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorNotificacionAgregada : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public Guid NotificacionId { get; }

        public NotificacionIndicadorNotificacionAgregada(Guid notificacionIndicadorId, Guid notificacionId)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            NotificacionId = notificacionId;
        }
    }
}
