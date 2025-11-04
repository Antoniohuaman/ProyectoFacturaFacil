using System.Threading;
using System.Threading.Tasks;

namespace GestionInventarioBC.Application.UseCases.Politicas
{
	/// <summary>
	/// Configura parámetros de reserva (no persistidos en este BC; se devuelve acuse de configuración).
	/// </summary>
	public sealed class ConfigurarPoliticaReservaUseCase
	{
		public readonly record struct Request(bool ModoEstrictamenteDisponible = true);
		public readonly record struct Response(bool ModoEstrictamenteDisponible);

		public Task<Response> Handle(Request req, CancellationToken _)
			=> Task.FromResult(new Response(req.ModoEstrictamenteDisponible));
	}
}

