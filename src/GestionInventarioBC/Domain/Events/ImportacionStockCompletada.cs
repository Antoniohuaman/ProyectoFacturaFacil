using System;
using SharedKernel.Events;
using SharedKernel.ValueObjects; // EmpresaId, EstablecimientoId, AlmacenId

namespace GestionInventarioBC.Domain.Events
{
	public sealed record ImportacionStockCompletada(
		Guid ProcesoId,
		EmpresaId EmpresaId,
		EstablecimientoId EstablecimientoId,
		AlmacenId AlmacenId,
		int ItemsProcesados,
		int ItemsFallidos
	) : IDomainEvent;
}

