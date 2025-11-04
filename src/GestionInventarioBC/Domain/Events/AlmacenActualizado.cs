using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Events
{
	public sealed record AlmacenActualizado(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		string Nombre
	) : IDomainEvent;
}

