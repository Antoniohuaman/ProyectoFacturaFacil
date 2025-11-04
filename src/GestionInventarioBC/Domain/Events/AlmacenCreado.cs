using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Events
{
	public sealed record AlmacenCreado(
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		string Nombre
	) : IDomainEvent;
}

