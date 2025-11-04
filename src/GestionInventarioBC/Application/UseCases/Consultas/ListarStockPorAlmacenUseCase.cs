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
	/// Lista el stock por SKU para un almacén específico.
	/// </summary>
	public sealed class ListarStockPorAlmacenUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId);

		public readonly record struct Item(
			string Sku,
			decimal Real,
			decimal Reservado,
			decimal Disponible
		);

		public readonly record struct Response(IReadOnlyList<Item> Items);

		private readonly IStockPorAlmacenRepository _repo;
		private readonly ITenantContext _tenant;

		public ListarStockPorAlmacenUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant)
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
			var items = lista.Select(s => new Item(
				Sku: s.Sku.Valor,
				Real: s.Real.Value,
				Reservado: s.Reservado.Value,
				Disponible: s.Disponible.Value
			)).ToList();
			return new Response(items);
		}
	}
}

