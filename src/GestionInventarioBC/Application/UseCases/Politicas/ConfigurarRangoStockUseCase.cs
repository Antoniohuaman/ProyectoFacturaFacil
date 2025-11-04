using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Specifications;
using GestionInventarioBC.Domain.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Politicas
{
	/// <summary>
	/// Valida y devuelve un rango de stock propuesto (sin persistir configuración).
	/// </summary>
	public sealed class ConfigurarRangoStockUseCase
	{
		public readonly record struct Request(decimal Minimo, decimal Maximo);
		public readonly record struct Response(decimal Minimo, decimal Maximo);

		public Task<Response> Handle(Request req, CancellationToken _)
		{
			var min = StockMinimo.From(req.Minimo);
			var max = StockMaximo.From(req.Maximo);
			var rango = RangoStock.Crear(min, max);

			var spec = new RangoStockValidoSpec();
			if (!spec.IsSatisfiedBy(rango))
				throw new ArgumentException("El rango de stock es inválido (mínimo no puede superar al máximo).");

			return Task.FromResult(new Response(rango.Minimo.Value, rango.Maximo.Value));
		}
	}
}

