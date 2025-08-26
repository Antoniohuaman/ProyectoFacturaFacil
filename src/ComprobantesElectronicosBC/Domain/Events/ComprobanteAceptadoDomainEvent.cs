using System;
using SharedKernel.Events;

namespace ProyectoFacturaFacil.ComprobantesElectronicosBC.Domain.Events
{
		public class ComprobanteAceptadoDomainEvent : DomainEvent
	{
		public Guid ComprobanteId { get; }
		public DateTime FechaAceptacion { get; }
		public string? Observaciones { get; }

		   public ComprobanteAceptadoDomainEvent(Guid comprobanteId, DateTime fechaAceptacion, string? observaciones = null, Guid? eventId = null, DateTime? occurredOnUtc = null)
			   : base(eventId, occurredOnUtc)
		   {
			   ComprobanteId = comprobanteId;
			   FechaAceptacion = fechaAceptacion;
			   Observaciones = observaciones;
		   }
	}
}
