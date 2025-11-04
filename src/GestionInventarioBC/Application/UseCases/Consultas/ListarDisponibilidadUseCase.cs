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
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, string? FiltroSku = null, bool SoloConDisponible = false);

		public readonly record struct Item(
			string Sku,
			decimal Real,
			decimal Reservado,
			decimal Disponible
		);

		public readonly record struct Response(IReadOnlyList<Item> Items);

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

			var items = lista.Select(s => new Item(
				Sku: s.ProductoId.Value.ToString(),
				Real: s.Real.Value,
				Reservado: s.Reservado.Value,
				Disponible: s.Disponible.Value
			)).ToList();

			return new Response(items);
		}
	}
}

