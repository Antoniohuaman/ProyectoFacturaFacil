namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Tipos de movimiento de inventario.
	/// </summary>
	public enum TipoMovimiento
	{
		Ingreso = 0,
		Egreso = 1,
		AjustePositivo = 2,
		AjusteNegativo = 3,
		TransferenciaEntrada = 4,
		TransferenciaSalida = 5
	}
}

