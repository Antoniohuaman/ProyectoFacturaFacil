using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;
using GestionInventarioBC.Application.Interfaces;

namespace GestionInventarioBC.Application.UseCases.OperacionesMasivas
{
	/// <summary>
	/// Exporta el listado de stock de un almacén (estructura en memoria; la serialización la maneja el adaptador).
	/// </summary>
	public sealed class ExportarListadoStockUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId);
		public readonly record struct Item(Guid ProductoId, string Sku, string Nombre, decimal Real, decimal Reservado, decimal Disponible);
		public readonly record struct Response(IReadOnlyList<Item> Items);

		private readonly IStockPorAlmacenRepository _repo;
		private readonly ITenantContext _tenant;
		private readonly ICatalogoReadModel _catalogo;

		public ExportarListadoStockUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant, ICatalogoReadModel catalogo)
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
			var items = new List<Item>(lista.Count);
			foreach (var s in lista)
			{
				var present = await _catalogo.TryGetSkuYNombreAsync(empresaId, s.ProductoId, ct);
				items.Add(new Item(
					ProductoId: s.ProductoId.Value,
					Sku: present?.Sku ?? string.Empty,
					Nombre: present?.Nombre ?? string.Empty,
					Real: s.Real.Value,
					Reservado: s.Reservado.Value,
					Disponible: s.Disponible.Value
				));
			}
			return new Response(items);
		}
	}
}

