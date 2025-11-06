using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
 

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
				// Validar SKU con reglas básicas (alineado a UBL an..30, inicia alfanumérico; permite A-Z 0-9 espacio - /. .)
				var sku = l.Sku?.Trim() ?? string.Empty;
				if (!EsSkuValido(sku))
				{
					errores.Add(new Error(i, l.Sku ?? string.Empty, "SKU inválido"));
					continue;
				}

				// Validar cantidad
				if (l.Cantidad < 0m)
				{
					errores.Add(new Error(i, sku, "La cantidad no puede ser negativa."));
					continue;
				}
			}

			return Task.FromResult(new Response(
				Total: req.Lineas.Count,
				ConErrores: errores.Count,
				Errores: errores
			));
		}

		private static bool EsSkuValido(string sku)
		{
			if (string.IsNullOrWhiteSpace(sku)) return false;
			var n = sku.Trim().ToUpperInvariant();
			if (n.Length < 1 || n.Length > 30) return false;
			if (!char.IsLetterOrDigit(n[0])) return false;
			for (int i = 0; i < n.Length; i++)
			{
				char c = n[i];
				if (char.IsLetterOrDigit(c)) continue;
				if (c == ' ' || c == '-' || c == '/' || c == '.') continue;
				return false;
			}
			return true;
		}
	}
}

