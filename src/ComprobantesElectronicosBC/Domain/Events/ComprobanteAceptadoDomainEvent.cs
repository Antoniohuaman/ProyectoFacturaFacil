using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
		public class ComprobanteAceptadoDomainEvent : DomainEvent
	{
		public Guid ComprobanteId { get; }
		public EmpresaId EmpresaId { get; }
		public EstablecimientoId EstablecimientoId { get; }
		public DateTime FechaAceptacion { get; }
		public string? Observaciones { get; }

		   public ComprobanteAceptadoDomainEvent(EmpresaId empresaId, EstablecimientoId establecimientoId, Guid comprobanteId, DateTime fechaAceptacion, string? observaciones = null, Guid? eventId = null, DateTime? occurredOnUtc = null)
			   : base(eventId, occurredOnUtc)
		   {
			   EmpresaId = empresaId;
			   EstablecimientoId = establecimientoId;
			   ComprobanteId = comprobanteId;
			   FechaAceptacion = fechaAceptacion;
			   Observaciones = observaciones;
		   }
	}
}
