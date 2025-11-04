using GestionInventarioBC.Domain.Aggregates;
using SharedKernel.Specifications;
using SharedKernel.ValueObjects; // ProductoId

namespace GestionInventarioBC.Domain.Specifications
{
	/// <summary>
	/// Placeholder: asume que todos los productos están habilitados en el almacén.
	/// Si más adelante se modela una lista de habilitados, actualizar esta Spec.
	/// </summary>
	public sealed class ProductoHabilitadoEnAlmacenSpec : IBooleanSpecification<(Almacen Almacen, ProductoId ProductoId)>
	{
		public bool IsSatisfiedBy((Almacen Almacen, ProductoId ProductoId) x) => true;
	}
}

