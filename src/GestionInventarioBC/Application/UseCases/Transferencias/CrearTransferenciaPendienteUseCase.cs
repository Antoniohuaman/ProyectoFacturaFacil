using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Transferencias
{
	/// <summary>
	/// Crea una transferencia pendiente (no mueve stock hasta confirmación).
	/// </summary>
	public sealed class CrearTransferenciaPendienteUseCase
	{
		public readonly record struct Request(
			Guid OrigenEstablecimientoId,
			Guid OrigenAlmacenId,
			Guid DestinoEstablecimientoId,
			Guid DestinoAlmacenId,
			string? Sku,
			Guid? ProductoId,
			decimal Cantidad
		);

		public readonly record struct Response(Guid TransferenciaId);

		private readonly ITransferenciaInventarioRepository _repo;
		private readonly ICatalogoReadModel _catalogo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public CrearTransferenciaPendienteUseCase(ITransferenciaInventarioRepository repo, ICatalogoReadModel catalogo, ITenantContext tenant, IUnitOfWork uow)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var origenEst = EstablecimientoId.From(req.OrigenEstablecimientoId);
			var origenAlm = AlmacenId.From(req.OrigenAlmacenId);
			var destEst = EstablecimientoId.From(req.DestinoEstablecimientoId);
			var destAlm = AlmacenId.From(req.DestinoAlmacenId);
			// Resolver ProductoId y validar consistencia si llega SKU y ProductoId
			ProductoId? productoId = null;
			if (req.ProductoId.HasValue)
				productoId = ProductoId.From(req.ProductoId.Value);
			if (!string.IsNullOrWhiteSpace(req.Sku))
			{
				var resolved = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, req.Sku!, ct)
							  ?? throw new SharedKernel.Exceptions.NotFoundException("No existe producto para el SKU indicado.");
				if (productoId is not null && !productoId.Value.Equals(resolved))
					throw new SharedKernel.Exceptions.BusinessRuleException("SKU y ProductoId no corresponden al mismo producto.");
				productoId ??= resolved;
			}
			if (productoId is null)
				throw new ArgumentException("Debe especificar SKU o ProductoId.");
			var pid = productoId.Value; // no-nullable guard
			var cant = CantidadStock.From(req.Cantidad);

			var t = TransferenciaInventario.Crear(empresaId, origenEst, origenAlm, destEst, destAlm, pid, cant);
			await _repo.GuardarAsync(t, ct);
			await _uow.CommitAsync(ct);
			return new Response(t.TransferenciaId);
		}
	}
}

