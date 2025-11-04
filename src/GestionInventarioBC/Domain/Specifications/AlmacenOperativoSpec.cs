using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.Specifications;

namespace GestionInventarioBC.Domain.Specifications
{
	/// <summary>
	/// Un almacén operativo es uno que está activo/habilitado.
	/// </summary>
	public sealed class AlmacenOperativoSpec : IBooleanSpecification<Almacen>
	{
		public bool IsSatisfiedBy(Almacen a) => a is not null && a.Activo;
	}
}

