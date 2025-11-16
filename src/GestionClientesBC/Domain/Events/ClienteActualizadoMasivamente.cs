using System;
using System.Collections.Generic;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionClientesBC.Domain.Events
{
	/// <summary>
	/// Evento lanzado cuando un cliente es actualizado mediante procesos masivos/importaciones.
	/// </summary>
	public sealed class ClienteActualizadoMasivamente : DomainEvent
	{
		public Guid ClienteId { get; }
		public EmpresaId EmpresaId { get; }
		public IReadOnlyCollection<string> CamposActualizados { get; }
		public DateTime FechaActualizacionUtc { get; }

		public ClienteActualizadoMasivamente(
			Guid clienteId,
			EmpresaId empresaId,
			IReadOnlyCollection<string> camposActualizados,
			DateTime fechaActualizacionUtc,
			Guid? eventId = null,
			DateTime? occurredOnUtc = null)
			: base(eventId, occurredOnUtc)
		{
			ClienteId = clienteId;
			EmpresaId = empresaId;
			CamposActualizados = camposActualizados ?? Array.Empty<string>();
			FechaActualizacionUtc = fechaActualizacionUtc.Kind == DateTimeKind.Utc
				? fechaActualizacionUtc
				: fechaActualizacionUtc.ToUniversalTime();
		}
	}
}
