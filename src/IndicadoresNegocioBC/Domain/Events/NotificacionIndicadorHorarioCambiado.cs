namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorHorarioCambiado : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public string NuevoHorario { get; }

        public NotificacionIndicadorHorarioCambiado(Guid notificacionIndicadorId, string nuevoHorario)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            NuevoHorario = nuevoHorario;
        }
    }
}
