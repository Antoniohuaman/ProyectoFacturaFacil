namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorAsuntoCambiado : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public string NuevoAsunto { get; }

        public NotificacionIndicadorAsuntoCambiado(Guid notificacionIndicadorId, string nuevoAsunto)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            NuevoAsunto = nuevoAsunto;
        }
    }
}
