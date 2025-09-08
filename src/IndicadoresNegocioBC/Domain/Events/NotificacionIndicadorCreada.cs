using System;
using SharedKernel.Events;

namespace IndicadoresNegocioBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio que indica que se creó una notificación de indicador.
    /// </summary>
    public class NotificacionIndicadorCreada : DomainEvent
    {
        public Guid NotificacionIndicadorId { get; }
        public Guid IndicadorId { get; }

        public NotificacionIndicadorCreada(Guid notificacionIndicadorId, Guid indicadorId)
        {
            NotificacionIndicadorId = notificacionIndicadorId;
            IndicadorId = indicadorId;
        }
    }
}
