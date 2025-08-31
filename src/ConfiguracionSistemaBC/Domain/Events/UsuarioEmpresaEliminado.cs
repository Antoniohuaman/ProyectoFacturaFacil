using System;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.Events;

namespace ConfiguracionSistemaBC.Domain.Events
{
	/// <summary>
	/// Evento de dominio que representa la eliminación de un UsuarioEmpresa (pendiente, sin actividad, o por error administrativo).
	/// Ahora se asocia a uno o varios establecimientos, no sucursales.
	/// </summary>
	public sealed record UsuarioEmpresaEliminado(
		Guid UsuarioEmpresaId,
		EmpresaId EmpresaId,
		IReadOnlyCollection<EstablecimientoId> Establecimientos,
		Email Email,
		NombrePersona Nombre
	) : IDomainEvent;
}
