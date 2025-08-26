using System;
using SharedKernel.Events;

namespace ProyectoFacturaFacil.ComprobantesElectronicosBC.Domain.Events
{
	public class ComprobanteCorregidoDomainEvent : DomainEvent
	{
		public Guid ComprobanteId { get; }
		public DateTime FechaCorreccion { get; }
		public string? MotivoCorreccion { get; }

		public ComprobanteCorregidoDomainEvent(Guid comprobanteId, DateTime fechaCorreccion, string? motivoCorreccion = null, Guid? eventId = null, DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ComprobanteId = comprobanteId;
			FechaCorreccion = fechaCorreccion;
			MotivoCorreccion = motivoCorreccion;
		}
	}
}
