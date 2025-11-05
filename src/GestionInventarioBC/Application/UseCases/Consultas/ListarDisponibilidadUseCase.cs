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
	/// Lista disponibilidad por SKU para un almacén, con filtros opcionales.
	/// </summary>
	public sealed class ListarDisponibilidadUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, string? FiltroSku = null, bool SoloConDisponible = false, int? Page = null, int? PageSize = null);

		public readonly record struct Item(
			string Sku,
			string Nombre,
			decimal Real,
			decimal Reservado,
			decimal Disponible
		);

		public readonly record struct Response(int Total, IReadOnlyList<Item> Items);

	private readonly IStockPorAlmacenRepository _repo;
	private readonly ITenantContext _tenant;
	private readonly ICatalogoReadModel _catalogo;

		public ListarDisponibilidadUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant, ICatalogoReadModel catalogo)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var lista = await _repo.ListarPorAlmacenAsync(empresaId, estId, almId, ct);

			if (!string.IsNullOrWhiteSpace(req.FiltroSku))
			{
				// Traducir filtro SKU a lista de ProductoId
				var productos = await _catalogo.BuscarProductoIdsAsync(empresaId, req.FiltroSku.Trim(), null, ct);
				var set = productos.Count > 0 ? new HashSet<Guid>(productos.Select(p => p.Value)) : new();
				lista = set.Count == 0 ? new List<Domain.Aggregates.StockPorAlmacen>() : lista.Where(s => set.Contains(s.ProductoId.Value)).ToList();
			}

			if (req.SoloConDisponible)
			{
				lista = lista.Where(s => s.Disponible.Value > 0m).ToList();
			}

			// Enriquecer con SKU/Nombre desde catálogo
			var enriched = new List<Item>(lista.Count);
			foreach (var s in lista)
			{
				var present = await _catalogo.TryGetSkuYNombreAsync(empresaId, s.ProductoId, ct);
				enriched.Add(new Item(
					Sku: present?.Sku ?? string.Empty,
					Nombre: present?.Nombre ?? string.Empty,
					Real: s.Real.Value,
					Reservado: s.Reservado.Value,
					Disponible: s.Disponible.Value
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

