namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorRangoFechasCambiado : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public DateTimeOffset? NuevaFechaInicio { get; }
        public DateTimeOffset? NuevaFechaFin { get; }

        public NotificacionIndicadorRangoFechasCambiado(Guid notificacionIndicadorId, DateTimeOffset? nuevaFechaInicio, DateTimeOffset? nuevaFechaFin)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            NuevaFechaInicio = nuevaFechaInicio;
            NuevaFechaFin = nuevaFechaFin;
        }
    }
}
