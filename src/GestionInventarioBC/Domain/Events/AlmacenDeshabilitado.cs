using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Events
{
	public sealed record AlmacenDeshabilitado(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId
	) : IDomainEvent;
}

