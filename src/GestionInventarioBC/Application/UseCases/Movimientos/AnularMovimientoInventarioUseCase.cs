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
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Movimientos
{
	/// <summary>
	/// Anula lógicamente un movimiento aplicando un movimiento compensatorio inverso que revierte su efecto en stock.
	/// </summary>
	public sealed class AnularMovimientoInventarioUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, Guid MovimientoId, DateTimeOffset Fecha);
		public readonly record struct Response(Guid MovimientoCompensatorioId);

		private readonly IMovimientoInventarioRepository _movRepo;
		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public AnularMovimientoInventarioUseCase(IMovimientoInventarioRepository movRepo, IStockPorAlmacenRepository stockRepo, ITenantContext tenant, IUnitOfWork uow)
		{
			_movRepo = movRepo ?? throw new ArgumentNullException(nameof(movRepo));
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var original = await _movRepo.ObtenerAsync(empresaId, estId, almId, req.MovimientoId, ct)
						   ?? throw new NotFoundException("Movimiento no encontrado.");

			// Determinar tipo inverso
			static TipoMovimiento Inverso(TipoMovimiento t) => t switch
			{
				TipoMovimiento.Ingreso => TipoMovimiento.Egreso,
				TipoMovimiento.Egreso => TipoMovimiento.Ingreso,
				TipoMovimiento.AjustePositivo => TipoMovimiento.AjusteNegativo,
				TipoMovimiento.AjusteNegativo => TipoMovimiento.AjustePositivo,
				TipoMovimiento.TransferenciaEntrada => TipoMovimiento.TransferenciaSalida,
				TipoMovimiento.TransferenciaSalida => TipoMovimiento.TransferenciaEntrada,
				_ => TipoMovimiento.AjusteNegativo
			};

			var lineasInv = new List<LineaMovimiento>(original.Lineas.Count);
			foreach (var l in original.Lineas)
			{
				var productoId = l.ProductoId;
				var cant = l.Cantidad;
				var stock = await _stockRepo.ObtenerAsync(empresaId, estId, almId, productoId, ct)
					   ?? StockPorAlmacen.CrearNuevo(empresaId, estId, almId, productoId);

				// Aplica inverso en stock
				switch (original.Tipo)
				{
					case TipoMovimiento.Ingreso:
					case TipoMovimiento.AjustePositivo:
					case TipoMovimiento.TransferenciaEntrada:
						stock.Egresar(cant);
						break;
					case TipoMovimiento.Egreso:
					case TipoMovimiento.AjusteNegativo:
					case TipoMovimiento.TransferenciaSalida:
						stock.Ingresar(cant);
						break;
				}
				await _stockRepo.GuardarAsync(stock, ct);
				lineasInv.Add(LineaMovimiento.Crear(productoId, cant));
			}

			var compensatorio = MovimientoInventario.Registrar(
				empresaId, estId, almId, req.Fecha, Inverso(original.Tipo), MotivoMovimiento.Ajuste, lineasInv);

			await _movRepo.GuardarAsync(compensatorio, ct);
			await _uow.CommitAsync(ct);
			return new Response(compensatorio.MovimientoId);
		}
	}
}

