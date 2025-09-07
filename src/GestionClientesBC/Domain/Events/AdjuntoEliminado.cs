using System;
using SharedKernel.Events;
using GestionClientesBC.Domain.Entities;

namespace GestionClientesBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que indica que se ha eliminado un adjunto de un cliente.
	/// </summary>
	public sealed class AdjuntoEliminado : DomainEvent
	{
		public Guid ClienteId { get; }
		public Guid AdjuntoId { get; }

		public AdjuntoEliminado(Guid clienteId, Guid adjuntoId, Guid? eventId = null, DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ClienteId = clienteId;
			AdjuntoId = adjuntoId;
		}
	}
}
