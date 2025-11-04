namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Estados posibles de una reserva de stock.
	/// </summary>
	public enum EstadoReserva
	{
		Pendiente = 0,
		Confirmada = 1,
		Liberada = 2,
		Vencida = 3,
		Cancelada = 4
	}
}

