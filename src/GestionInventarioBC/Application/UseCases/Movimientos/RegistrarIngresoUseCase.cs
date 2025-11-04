using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Entities;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Movimientos
{
	/// <summary>
	/// Registra un ingreso de inventario (aumenta stock real) y crea el movimiento correspondiente.
	/// </summary>
	public sealed class RegistrarIngresoUseCase
	{
		public readonly record struct Linea(string Sku, decimal Cantidad);
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, DateTimeOffset Fecha, string Motivo, IReadOnlyList<Linea> Lineas);
		public readonly record struct Response(Guid MovimientoId, int LineasAfectadas);

		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly IMovimientoInventarioRepository _movRepo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public RegistrarIngresoUseCase(IStockPorAlmacenRepository stockRepo, IMovimientoInventarioRepository movRepo, ITenantContext tenant, IUnitOfWork uow)
		{
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_movRepo = movRepo ?? throw new ArgumentNullException(nameof(movRepo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var lineas = new List<LineaMovimiento>(req.Lineas.Count);

			foreach (var l in req.Lineas)
			{
				var sku = Sku.Crear(l.Sku);
				var cant = CantidadStock.From(l.Cantidad);
				// Stock
				var stock = await _stockRepo.ObtenerAsync(empresaId, estId, almId, sku, ct)
						   ?? StockPorAlmacen.CrearNuevo(empresaId, estId, almId, sku);
				stock.Ingresar(cant);
				await _stockRepo.GuardarAsync(stock, ct);

				// Línea de movimiento
				lineas.Add(LineaMovimiento.Crear(sku, cant));
			}

			var movimiento = MovimientoInventario.Registrar(
				empresaId, estId, almId, req.Fecha,
				TipoMovimiento.Ingreso,
				Enum.TryParse<MotivoMovimiento>(req.Motivo, true, out var mot) ? mot : MotivoMovimiento.Compra,
				lineas);

			await _movRepo.GuardarAsync(movimiento, ct);
			await _uow.CommitAsync(ct);

			return new Response(movimiento.MovimientoId, lineas.Count);
		}
	}
}

