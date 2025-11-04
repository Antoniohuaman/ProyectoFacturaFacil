using System;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace GestionInventarioBC.Domain.Specifications
{
	/// <summary>
	/// Verifica si una fecha cae dentro del periodo de inventario.
	/// </summary>
	public sealed class PeriodoInventarioAbiertoSpec : IBooleanSpecification<(PeriodoInventario Periodo, DateOnly Fecha)>
	{
		public bool IsSatisfiedBy((PeriodoInventario Periodo, DateOnly Fecha) x)
			=> x.Periodo.Contiene(x.Fecha);
	}
}

