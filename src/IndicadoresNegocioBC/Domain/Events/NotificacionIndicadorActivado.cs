namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorActivado : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }

        public NotificacionIndicadorActivado(Guid notificacionIndicadorId)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
        }
    }
}
