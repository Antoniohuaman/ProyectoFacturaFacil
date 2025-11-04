using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.OperacionesMasivas
{
	/// <summary>
	/// Exporta el listado de stock de un almacén (estructura en memoria; la serialización la maneja el adaptador).
	/// </summary>
	public sealed class ExportarListadoStockUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId);
		public readonly record struct Item(string Sku, decimal Real, decimal Reservado, decimal Disponible);
		public readonly record struct Response(IReadOnlyList<Item> Items);

		private readonly IStockPorAlmacenRepository _repo;
		private readonly ITenantContext _tenant;

		public ExportarListadoStockUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var lista = await _repo.ListarPorAlmacenAsync(empresaId, estId, almId, ct);
			var items = lista.Select(s => new Item(s.Sku.Valor, s.Real.Value, s.Reservado.Value, s.Disponible.Value)).ToList();
			return new Response(items);
		}
	}
}

