using SharedKernel.ValueObjects; // Sku, Dinero

namespace GestionInventarioBC.Domain.Entities
{
	/// <summary>
	/// Línea de movimiento de inventario para un SKU específico.
	/// </summary>
	public sealed record LineaMovimiento
	{
		public Sku Sku { get; }
		public ValueObjects.CantidadStock Cantidad { get; }
		public ValueObjects.CostoUnitario? CostoUnitario { get; }

		private LineaMovimiento(Sku sku, ValueObjects.CantidadStock cantidad, ValueObjects.CostoUnitario? costo)
		{
			Sku = sku ?? throw new System.ArgumentNullException(nameof(sku));
			Cantidad = cantidad; // valida no-negativo
			CostoUnitario = costo; // opcional (p.ej. egresos pueden no recalcular costo)
		}

		public static LineaMovimiento Crear(Sku sku, ValueObjects.CantidadStock cantidad, ValueObjects.CostoUnitario? costo = null)
			=> new(sku, cantidad, costo);
	}
}

