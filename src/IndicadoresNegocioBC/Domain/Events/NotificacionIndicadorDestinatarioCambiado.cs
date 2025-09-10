namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorDestinatarioCambiado : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public string NuevoDestinatario { get; }

        public NotificacionIndicadorDestinatarioCambiado(Guid notificacionIndicadorId, string nuevoDestinatario)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            NuevoDestinatario = nuevoDestinatario;
        }
    }
}
