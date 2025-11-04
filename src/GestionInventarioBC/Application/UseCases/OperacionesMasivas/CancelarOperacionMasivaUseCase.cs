using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestionInventarioBC.Application.UseCases.OperacionesMasivas
{
	/// <summary>
	/// Marca como cancelada una operación masiva (no hay persistencia en este BC; es un no-op con acuse).
	/// </summary>
	public sealed class CancelarOperacionMasivaUseCase
	{
		public readonly record struct Request(Guid OperacionId);
		public readonly record struct Response(Guid OperacionId, bool Cancelada);

		public Task<Response> Handle(Request req, CancellationToken _)
			=> Task.FromResult(new Response(req.OperacionId, true));
	}
}

