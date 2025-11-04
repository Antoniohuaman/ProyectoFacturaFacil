using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
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
			decimal Real,
			decimal Reservado,
			decimal Disponible
		);

		private readonly IStockPorAlmacenRepository _repo;
		private readonly ITenantContext _tenant;

		public ConsultarDisponibilidadProductoUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);
			var sku = Sku.Crear(req.Sku);

			var stock = await _repo.ObtenerAsync(empresaId, estId, almId, sku, ct);
			if (stock is null)
				throw new NotFoundException("No se encontró stock para el SKU en el almacén indicado.");

			// Opcional: usar VO DisponibilidadStock si conviene
			var disp = DisponibilidadStock.Crear(stock.Real, stock.Reservado);
			return new Response(
				Sku: sku.Valor,
				Real: stock.Real.Value,
				Reservado: stock.Reservado.Value,
				Disponible: disp.Disponible.Value
			);
		}
	}
}

