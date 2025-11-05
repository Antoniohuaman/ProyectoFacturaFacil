using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Application.Interfaces;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Consultas
{
	/// <summary>
	/// Consulta la disponibilidad de un SKU en un almacén específico.
	/// </summary>
	public sealed class ConsultarDisponibilidadProductoUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, string Sku);

		public readonly record struct Response(
			string Sku,
			string Nombre,
			decimal Real,
			decimal Reservado,
			decimal Disponible
		);

		private readonly IStockPorAlmacenRepository _repo;
		private readonly ITenantContext _tenant;
        private readonly ICatalogoReadModel _catalogo;

		public ConsultarDisponibilidadProductoUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant, ICatalogoReadModel catalogo)
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
			var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, req.Sku, ct)
                ?? throw new NotFoundException("No existe producto para el SKU indicado.");

			var stock = await _repo.ObtenerAsync(empresaId, estId, almId, productoId, ct);
			if (stock is null)
				throw new NotFoundException("No se encontró stock para el producto en el almacén indicado.");

			var present = await _catalogo.TryGetSkuYNombreAsync(empresaId, productoId, ct);

			// Opcional: usar VO DisponibilidadStock si conviene
			var disp = DisponibilidadStock.Crear(stock.Real, stock.Reservado);
			return new Response(
				Sku: req.Sku,
				Nombre: present?.Nombre ?? string.Empty,
				Real: stock.Real.Value,
				Reservado: stock.Reservado.Value,
				Disponible: disp.Disponible.Value
			);
		}
	}
}

