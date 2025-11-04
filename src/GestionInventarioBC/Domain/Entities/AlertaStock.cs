using System;
using SharedKernel.ValueObjects; // Sku

namespace GestionInventarioBC.Domain.Entities
{
	/// <summary>
	/// Alerta cuando la disponibilidad cae por debajo del mínimo configurado.
	/// </summary>
	public sealed record AlertaStock
	{
		public DateTimeOffset Fecha { get; }
		public Sku Sku { get; }
		public decimal Disponible { get; }
		public decimal Minimo { get; }
		public string? Observacion { get; }

		private AlertaStock(DateTimeOffset fecha, Sku sku, decimal disponible, decimal minimo, string? observacion)
		{
			Fecha = fecha;
			Sku = sku ?? throw new ArgumentNullException(nameof(sku));
			Disponible = disponible;
			Minimo = minimo;
			Observacion = observacion;
		}

		public static AlertaStock Crear(Sku sku, decimal disponible, decimal minimo, string? observacion = null, DateTimeOffset? fecha = null)
			=> new(fecha ?? DateTimeOffset.UtcNow, sku, disponible, minimo, observacion);
	}
}

