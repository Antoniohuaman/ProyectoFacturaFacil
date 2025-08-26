using System;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace ConfiguracionSistemaBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que representa la eliminación de un UsuarioEmpleado (pendiente, sin actividad, o por error administrativo).
	/// Ahora se asocia a uno o varios establecimientos, no sucursales.
	/// </summary>
	public sealed record UsuarioEmpleadoEliminado(
		Guid UsuarioEmpleadoId,
		EmpresaId EmpresaId,
		IReadOnlyCollection<EstablecimientoId> Establecimientos,
		CorreoElectronico Email,
		NombrePersona Nombre
	) : IDomainEvent;
}
