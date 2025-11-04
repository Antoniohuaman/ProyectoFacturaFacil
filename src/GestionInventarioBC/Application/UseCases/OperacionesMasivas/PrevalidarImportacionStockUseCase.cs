using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.OperacionesMasivas
{
	/// <summary>
	/// Prevalida un conjunto de líneas para importación masiva de stock (sin persistir cambios).
	/// </summary>
	public sealed class PrevalidarImportacionStockUseCase
	{
		public readonly record struct Linea(string Sku, decimal Cantidad);
		public readonly record struct Error(int Index, string Sku, string Mensaje);
		public readonly record struct Request(IReadOnlyList<Linea> Lineas);
		public readonly record struct Response(int Total, int ConErrores, IReadOnlyList<Error> Errores);

		public Task<Response> Handle(Request req, CancellationToken _)
		{
			if (req.Lineas is null) throw new ArgumentNullException(nameof(req.Lineas));

			var errores = new List<Error>();
			for (var i = 0; i < req.Lineas.Count; i++)
			{
				var l = req.Lineas[i];
				// Validar SKU
				if (!Sku.TryCrear(l.Sku, out var sku, out var errorSku))
				{
					errores.Add(new Error(i, l.Sku ?? string.Empty, errorSku ?? "SKU inválido"));
					continue;
				}

				// Validar cantidad
				if (l.Cantidad < 0m)
				{
					errores.Add(new Error(i, sku!.Valor, "La cantidad no puede ser negativa."));
					continue;
				}
			}

			return Task.FromResult(new Response(
				Total: req.Lineas.Count,
				ConErrores: errores.Count,
				Errores: errores
			));
		}
	}
}

