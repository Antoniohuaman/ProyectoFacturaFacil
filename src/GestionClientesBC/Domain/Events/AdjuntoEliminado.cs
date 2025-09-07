
using System;
using SharedKernel.Events;
using GestionClientesBC.Domain.Entities;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que indica que se ha eliminado un adjunto de un cliente.
	/// </summary>
	public sealed class AdjuntoEliminado : DomainEvent
	{
		public Guid ClienteId { get; }
		public EmpresaId EmpresaId { get; }
		public Guid AdjuntoId { get; }

		public AdjuntoEliminado(Guid clienteId, EmpresaId empresaId, Guid adjuntoId, Guid? eventId = null, DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ClienteId = clienteId;
			EmpresaId = empresaId;
			AdjuntoId = adjuntoId;
		}
	}
}
