using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Almacen
{
	/// <summary>
	/// Deshabilita un almacén (si ya está deshabilitado, es idempotente).
	/// </summary>
	public sealed class DeshabilitarAlmacenUseCase
	{
		public readonly record struct Request(
			Guid EstablecimientoId,
			Guid AlmacenId
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

		public DeshabilitarAlmacenUseCase(IAlmacenRepository repo, ITenantContext tenant, IUnitOfWork uow)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var agregado = await _repo.ObtenerAsync(empresaId, estId, almId, ct);
			if (agregado is null)
				throw new NotFoundException("Almacén no encontrado.");

			agregado.Deshabilitar();
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

