using System;
using SharedKernel.ValueObjects; // ProductoId

namespace GestionInventarioBC.Domain.Entities
{
	/// <summary>
	/// Alerta cuando la disponibilidad cae por debajo del mínimo configurado.
	/// </summary>
	public sealed record AlertaStock
	{
		public DateTimeOffset Fecha { get; }
		public ProductoId ProductoId { get; }
		public decimal Disponible { get; }
		public decimal Minimo { get; }
		public string? Observacion { get; }

		private AlertaStock(DateTimeOffset fecha, ProductoId productoId, decimal disponible, decimal minimo, string? observacion)
		{
			Fecha = fecha;
			ProductoId = productoId;
			Disponible = disponible;
			Minimo = minimo;
			Observacion = observacion;
		}

		public static AlertaStock Crear(ProductoId productoId, decimal disponible, decimal minimo, string? observacion = null, DateTimeOffset? fecha = null)
			=> new(fecha ?? DateTimeOffset.UtcNow, productoId, disponible, minimo, observacion);
	}
}

