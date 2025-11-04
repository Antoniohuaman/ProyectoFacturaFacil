using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Almacen
{
	/// <summary>
	/// Lista los almacenes de un establecimiento de la empresa actual.
	/// </summary>
	public sealed class ListarAlmacenesUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId);

		public readonly record struct Item(
			Guid EstablecimientoId,
			Guid AlmacenId,
			string Nombre,
			bool Activo,
			int Version
		);

		public readonly record struct Response(IReadOnlyList<Item> Almacenes);

		private readonly IAlmacenRepository _repo;
		private readonly ITenantContext _tenant;

		public ListarAlmacenesUseCase(IAlmacenRepository repo, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);

			var lista = await _repo.ListarAsync(empresaId, estId, ct);
			var items = lista.Select(a => new Item(
				EstablecimientoId: a.EstablecimientoId.Value,
				AlmacenId: a.AlmacenId.Value,
				Nombre: a.Nombre,
				Activo: a.Activo,
				Version: a.Version
			)).ToList();

			return new Response(items);
		}
	}
}

