using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Events
{
	public class ComprobanteCorregidoDomainEvent : DomainEvent
	{
		public Guid ComprobanteId { get; }
		public EmpresaId EmpresaId { get; }
		public EstablecimientoId EstablecimientoId { get; }
		public DateTime FechaCorreccion { get; }
		public string? MotivoCorreccion { get; }

		public ComprobanteCorregidoDomainEvent(EmpresaId empresaId, EstablecimientoId establecimientoId, Guid comprobanteId, DateTime fechaCorreccion, string? motivoCorreccion = null, Guid? eventId = null, DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			EmpresaId = empresaId;
			EstablecimientoId = establecimientoId;
			ComprobanteId = comprobanteId;
			FechaCorreccion = fechaCorreccion;
			MotivoCorreccion = motivoCorreccion;
		}
	}
}
