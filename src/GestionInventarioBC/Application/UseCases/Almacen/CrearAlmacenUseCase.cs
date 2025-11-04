using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Almacen
{
	/// <summary>
	/// Crea un nuevo almacén dentro de un establecimiento de la empresa actual.
	/// </summary>
	public sealed class CrearAlmacenUseCase
	{
		public readonly record struct Request(
			Guid EstablecimientoId,
			string Nombre,
			Guid? AlmacenId = null
		);

		public readonly record struct Response(
			Guid EstablecimientoId,
			Guid AlmacenId,
			string Nombre,
			bool Activo,
			int Version
		);

		private readonly IAlmacenRepository _repo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public CrearAlmacenUseCase(IAlmacenRepository repo, ITenantContext tenant, IUnitOfWork uow)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = req.AlmacenId.HasValue && req.AlmacenId.Value != Guid.Empty
				? AlmacenId.From(req.AlmacenId.Value)
				: AlmacenId.New();

			if (string.IsNullOrWhiteSpace(req.Nombre))
				throw new BusinessRuleException("El nombre del almacén es obligatorio.");

			var existente = await _repo.ObtenerAsync(empresaId, estId, almId, ct);
			if (existente is not null)
				throw new BusinessRuleException("Ya existe un almacén con el identificador especificado.");

			var agregado = Domain.Aggregates.Almacen.Crear(empresaId, estId, almId, req.Nombre.Trim());
			await _repo.GuardarAsync(agregado, ct);
			await _uow.CommitAsync(ct);

			return new Response(
				EstablecimientoId: estId.Value,
				AlmacenId: almId.Value,
				Nombre: agregado.Nombre,
				Activo: agregado.Activo,
				Version: agregado.Version
			);
		}
	}
}

