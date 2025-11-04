using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Consultas
{
	/// <summary>
	/// Lista transferencias en estado Creada (pendientes) con filtros opcionales de origen/destino.
	/// </summary>
	public sealed class ConsultarTransferenciasPendientesUseCase
	{
		public readonly record struct Request(
			Guid? OrigenEstablecimientoId,
			Guid? OrigenAlmacenId,
			Guid? DestinoEstablecimientoId,
			Guid? DestinoAlmacenId
		);

		public readonly record struct Item(
			Guid TransferenciaId,
			Guid OrigenEstablecimientoId,
			Guid OrigenAlmacenId,
			Guid DestinoEstablecimientoId,
			Guid DestinoAlmacenId,
			Guid ProductoId,
			decimal Cantidad,
			DateTimeOffset CreadoEn
		);

		public readonly record struct Response(IReadOnlyList<Item> Transferencias);

		private readonly ITransferenciaInventarioRepository _repo;
		private readonly ITenantContext _tenant;

		public ConsultarTransferenciasPendientesUseCase(ITransferenciaInventarioRepository repo, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var origenEst = req.OrigenEstablecimientoId.HasValue ? EstablecimientoId.From(req.OrigenEstablecimientoId.Value) : null;
			var origenAlm = req.OrigenAlmacenId.HasValue ? AlmacenId.From(req.OrigenAlmacenId.Value) : null;
			var destEst = req.DestinoEstablecimientoId.HasValue ? EstablecimientoId.From(req.DestinoEstablecimientoId.Value) : null;
			var destAlm = req.DestinoAlmacenId.HasValue ? AlmacenId.From(req.DestinoAlmacenId.Value) : null;

			var lista = await _repo.ListarPendientesAsync(empresaId, origenEst, origenAlm, destEst, destAlm, ct);
			var items = lista.Select(t => new Item(
				t.TransferenciaId,
				t.OrigenEstablecimientoId.Value,
				t.OrigenAlmacenId.Value,
				t.DestinoEstablecimientoId.Value,
				t.DestinoAlmacenId.Value,
				t.ProductoId.Value,
				t.Cantidad.Value,
				t.CreadoEn
			)).ToList();

			return new Response(items);
		}
	}
}

