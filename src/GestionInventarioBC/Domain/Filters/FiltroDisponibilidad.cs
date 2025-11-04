using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Domain.Filters
{
	public sealed record FiltroDisponibilidad(
		EmpresaId EmpresaId,
		EstablecimientoId? EstablecimientoId,
		AlmacenId? AlmacenId,
		string? Sku // se usa string para permitir búsquedas parciales; resolver a Sku en el adapter
	);
}

