using SharedKernel.ValueObjects; // ProductoId, Dinero

namespace GestionInventarioBC.Domain.Entities
{
	/// <summary>
	/// Línea de movimiento de inventario para un producto específico.
	/// </summary>
	public sealed record LineaMovimiento
	{
		public ProductoId ProductoId { get; }
		public ValueObjects.CantidadStock Cantidad { get; }
		public ValueObjects.CostoUnitario? CostoUnitario { get; }

		private LineaMovimiento(ProductoId productoId, ValueObjects.CantidadStock cantidad, ValueObjects.CostoUnitario? costo)
		{
			ProductoId = productoId;
			Cantidad = cantidad; // valida no-negativo
			CostoUnitario = costo; // opcional (p.ej. egresos pueden no recalcular costo)
		}

		public static LineaMovimiento Crear(ProductoId productoId, ValueObjects.CantidadStock cantidad, ValueObjects.CostoUnitario? costo = null)
			=> new(productoId, cantidad, costo);
	}
}

