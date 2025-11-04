using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Entities;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Movimientos
{
	/// <summary>
	/// Registra un ajuste de inventario por SKU. Para delta &gt; 0 ingresa, para delta &lt; 0 egresa.
	/// </summary>
	public sealed class RegistrarAjusteInventarioUseCase
	{
		public readonly record struct Item(string Sku, decimal Delta, string Motivo);
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, DateTimeOffset Fecha, IReadOnlyList<Item> Items);
		public readonly record struct Response(Guid MovimientoId, int LineasAfectadas);

		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly IMovimientoInventarioRepository _movRepo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;
        private readonly ICatalogoReadModel _catalogo;

		public RegistrarAjusteInventarioUseCase(IStockPorAlmacenRepository stockRepo, IMovimientoInventarioRepository movRepo, ITenantContext tenant, IUnitOfWork uow, ICatalogoReadModel catalogo)
		{
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_movRepo = movRepo ?? throw new ArgumentNullException(nameof(movRepo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var lineas = new List<LineaMovimiento>(req.Items.Count);
			TipoMovimiento? tipoMov = null;

			foreach (var it in req.Items)
			{
				if (it.Delta == 0m) continue;
				var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, it.Sku, ct)
                        ?? throw new NotFoundException($"No existe producto para SKU {it.Sku}.");
				var valorAbs = Math.Abs(it.Delta);
				var cant = CantidadStock.From(valorAbs);
				var stock = await _stockRepo.ObtenerAsync(empresaId, estId, almId, productoId, ct)
					   ?? StockPorAlmacen.CrearNuevo(empresaId, estId, almId, productoId);

				if (it.Delta > 0m)
				{
					stock.Ingresar(cant);
					tipoMov ??= TipoMovimiento.AjustePositivo;
				}
				else
				{
					// Egreso por ajuste negativo
					stock.Egresar(cant);
					tipoMov ??= TipoMovimiento.AjusteNegativo;
				}
				await _stockRepo.GuardarAsync(stock, ct);
				lineas.Add(LineaMovimiento.Crear(productoId, cant));
			}

			if (lineas.Count == 0)
				throw new BusinessRuleException("No hay items de ajuste con delta distinto de cero.");

			var motivo = MotivoMovimiento.Ajuste;
			if (!string.IsNullOrWhiteSpace(req.Items[0].Motivo) && Enum.TryParse<MotivoMovimiento>(req.Items[0].Motivo, true, out var parsed))
				motivo = parsed;

			var movimiento = MovimientoInventario.Registrar(empresaId, estId, almId, req.Fecha, tipoMov!.Value, motivo, lineas);
			await _movRepo.GuardarAsync(movimiento, ct);
			await _uow.CommitAsync(ct);
			return new Response(movimiento.MovimientoId, lineas.Count);
		}
	}
}

