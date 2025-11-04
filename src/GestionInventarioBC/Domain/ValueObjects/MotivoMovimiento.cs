namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Motivo del movimiento. Enum genérico; los motivos específicos del negocio
	/// pueden mapearse a estos valores o extenderse en un catálogo.
	/// </summary>
	public enum MotivoMovimiento
	{
		Desconocido = 0,
		Compra = 1,
		Venta = 2,
		DevolucionCompra = 3,
		DevolucionVenta = 4,
		Ajuste = 5,
		Transferencia = 6,
		Produccion = 7
	}
}

