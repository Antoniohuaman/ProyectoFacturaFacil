using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestionInventarioBC.Application.UseCases.Politicas
{
	/// <summary>
	/// Configura el método de valoración de inventario (acuse, sin persistencia en este BC).
	/// </summary>
	public sealed class ConfigurarPoliticaValoracionUseCase
	{
		public readonly record struct Request(string Metodo);
		public readonly record struct Response(string Metodo);

		public Task<Response> Handle(Request req, CancellationToken _)
		{
			// Por ahora sólo soportamos PromedioPonderado (coincide con política de dominio actual)
			if (!string.Equals(req.Metodo, "PromedioPonderado", StringComparison.Ordinal))
				throw new ArgumentException("Método de valoración no soportado. Use 'PromedioPonderado'.", nameof(req.Metodo));

			return Task.FromResult(new Response(req.Metodo));
		}
	}
}

