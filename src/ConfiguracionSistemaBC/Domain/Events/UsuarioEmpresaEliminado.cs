using System;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace ConfiguracionSistemaBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que representa la eliminación de un UsuarioEmpresa (pendiente, sin actividad, o por error administrativo).
	/// </summary>
	public sealed class UsuarioEmpresaEliminado : DomainEvent
	{
		public EmpresaId EmpresaId { get; }
		public IReadOnlyCollection<EstablecimientoId> Establecimientos { get; }
		public Email Email { get; }
		public NombrePersona Nombre { get; }

		public UsuarioEmpresaEliminado(
			EmpresaId empresaId,
			IReadOnlyCollection<EstablecimientoId> establecimientos,
			Email email,
			NombrePersona nombre,
			DateTime? occurredOnUtc = null,
			Guid? eventId = null)
			: base(eventId, occurredOnUtc)
		{
			EmpresaId = empresaId;
			Establecimientos = establecimientos;
			Email = email;
			Nombre = nombre;
		}
	}
}
