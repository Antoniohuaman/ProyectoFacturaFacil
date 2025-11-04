namespace GestionInventarioBC.Domain.ValueObjects
{
	/// <summary>
	/// Disponibilidad derivada del stock real y reservado.
	/// </summary>
	public sealed record DisponibilidadStock
	{
		public CantidadStock Real { get; }
		public CantidadStock Reservado { get; }
		public CantidadStock Disponible => Real - Reservado; // lanzará si Reservado > Real

		private DisponibilidadStock(CantidadStock real, CantidadStock reservado)
		{
			Real = real;
			Reservado = reservado;
		}

		public static DisponibilidadStock Crear(CantidadStock real, CantidadStock reservado)
		{
			// Validación temprana: provocar la resta para asegurar Reservado <= Real
			_ = real - reservado; // lanzará BusinessRuleException si reservado > real
			return new(real, reservado);
		}
	}
}

