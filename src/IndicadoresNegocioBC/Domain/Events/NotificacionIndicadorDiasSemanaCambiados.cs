namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorDiasSemanaCambiados : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public DayOfWeek[] NuevosDiasSemana { get; }

        public NotificacionIndicadorDiasSemanaCambiados(Guid notificacionIndicadorId, DayOfWeek[] nuevosDiasSemana)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            NuevosDiasSemana = nuevosDiasSemana;
        }
    }
}
