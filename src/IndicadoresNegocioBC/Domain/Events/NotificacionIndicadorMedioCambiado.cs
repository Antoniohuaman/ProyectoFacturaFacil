namespace IndicadoresNegocioBC.Domain.Events
{
    using SharedKernel.Events;
    public class NotificacionIndicadorMedioCambiado : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public string NuevoMedio { get; }

        public NotificacionIndicadorMedioCambiado(Guid notificacionIndicadorId, string nuevoMedio)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            NuevoMedio = nuevoMedio;
        }
    }
}
