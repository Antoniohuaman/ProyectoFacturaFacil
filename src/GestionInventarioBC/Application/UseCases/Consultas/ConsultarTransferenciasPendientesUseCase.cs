using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;
using GestionInventarioBC.Application.Interfaces;

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
			Guid? DestinoAlmacenId,
			int? Page = null,
			int? PageSize = null
		);

		public readonly record struct Item(
			Guid TransferenciaId,
			Guid OrigenEstablecimientoId,
			Guid OrigenAlmacenId,
			Guid DestinoEstablecimientoId,
			Guid DestinoAlmacenId,
			Guid ProductoId,
			string Sku,
			string Nombre,
			decimal Cantidad,
			DateTimeOffset CreadoEn
		);

		public readonly record struct Response(int Total, IReadOnlyList<Item> Items);

		private readonly ITransferenciaInventarioRepository _repo;
		private readonly ITenantContext _tenant;
		private readonly ICatalogoReadModel _catalogo;

		public ConsultarTransferenciasPendientesUseCase(ITransferenciaInventarioRepository repo, ITenantContext tenant, ICatalogoReadModel catalogo)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var origenEst = req.OrigenEstablecimientoId.HasValue ? EstablecimientoId.From(req.OrigenEstablecimientoId.Value) : null;
			var origenAlm = req.OrigenAlmacenId.HasValue ? AlmacenId.From(req.OrigenAlmacenId.Value) : null;
			var destEst = req.DestinoEstablecimientoId.HasValue ? EstablecimientoId.From(req.DestinoEstablecimientoId.Value) : null;
			var destAlm = req.DestinoAlmacenId.HasValue ? AlmacenId.From(req.DestinoAlmacenId.Value) : null;

			var lista = await _repo.ListarPendientesAsync(empresaId, origenEst, origenAlm, destEst, destAlm, ct);
			var enriched = new List<Item>(lista.Count);
			foreach (var t in lista)
			{
				var present = await _catalogo.TryGetSkuYNombreAsync(empresaId, t.ProductoId, ct);
				enriched.Add(new Item(
					TransferenciaId: t.TransferenciaId,
					OrigenEstablecimientoId: t.OrigenEstablecimientoId.Value,
					OrigenAlmacenId: t.OrigenAlmacenId.Value,
					DestinoEstablecimientoId: t.DestinoEstablecimientoId.Value,
					DestinoAlmacenId: t.DestinoAlmacenId.Value,
					ProductoId: t.ProductoId.Value,
					Sku: present?.Sku ?? string.Empty,
					Nombre: present?.Nombre ?? string.Empty,
					Cantidad: t.Cantidad.Value,
					CreadoEn: t.CreadoEn
				));
			}

			// Paginación in-memory (TODO: mover a repo si procede)
			var total = enriched.Count;
			var page = req.Page.GetValueOrDefault(1);
			var pageSize = req.PageSize.GetValueOrDefault(50);
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = 50;
			var items = enriched.Skip((page - 1) * pageSize).Take(pageSize).ToList();

			return new Response(total, items);
		}
	}
}

