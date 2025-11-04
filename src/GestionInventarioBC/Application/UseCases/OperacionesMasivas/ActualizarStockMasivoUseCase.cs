using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.OperacionesMasivas
{
	/// <summary>
	/// Actualiza el stock real por SKU masivamente al valor destino indicado.
	/// </summary>
	public sealed class ActualizarStockMasivoUseCase
	{
		public readonly record struct Linea(string Sku, decimal Cantidad);
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, IReadOnlyList<Linea> Lineas);
		public readonly record struct Response(int Procesados);

		private readonly IStockPorAlmacenRepository _repo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public ActualizarStockMasivoUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant, IUnitOfWork uow)
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

			var count = 0;
			foreach (var l in req.Lineas)
			{
				var sku = Sku.Crear(l.Sku);
				var destino = CantidadStock.From(l.Cantidad);

				var stock = await _repo.ObtenerAsync(empresaId, estId, almId, sku, ct)
							?? StockPorAlmacen.CrearNuevo(empresaId, estId, almId, sku);

				var actual = stock.Real.Value;
				if (destino.Value > actual)
				{
					stock.Ingresar(CantidadStock.From(destino.Value - actual));
				}
				else if (destino.Value < actual)
				{
					stock.Egresar(CantidadStock.From(actual - destino.Value));
				}

				await _repo.GuardarAsync(stock, ct);
				count++;
			}

			await _uow.CommitAsync(ct);
			return new Response(count);
		}
	}
}

