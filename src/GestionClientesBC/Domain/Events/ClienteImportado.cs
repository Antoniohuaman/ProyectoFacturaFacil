using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
	/// <summary>
	/// Evento emitido cuando un cliente se registra mediante un proceso de importación.
	/// </summary>
	public sealed class ClienteImportado : DomainEvent
	{
		public Guid ClienteId { get; }
		public EmpresaId EmpresaId { get; }
		public string Plantilla { get; }
		public DateTime FechaImportacionUtc { get; }

		public ClienteImportado(
			Guid clienteId,
			EmpresaId empresaId,
			string plantilla,
			DateTime fechaImportacionUtc,
			Guid? eventId = null,
			DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ClienteId = clienteId;
			EmpresaId = empresaId;
			Plantilla = string.IsNullOrWhiteSpace(plantilla) ? "DESCONOCIDA" : plantilla.Trim();
			FechaImportacionUtc = fechaImportacionUtc.Kind == DateTimeKind.Utc
				? fechaImportacionUtc
				: fechaImportacionUtc.ToUniversalTime();
		}
	}
}
