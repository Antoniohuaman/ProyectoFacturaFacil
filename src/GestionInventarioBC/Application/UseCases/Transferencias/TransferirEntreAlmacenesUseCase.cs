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

namespace GestionInventarioBC.Application.UseCases.Transferencias
{
	/// <summary>
	/// Transfiere stock entre almacenes de forma inmediata (salida + entrada y movimientos).
	/// </summary>
	public sealed class TransferirEntreAlmacenesUseCase
	{
		public readonly record struct Linea(string? Sku, Guid? ProductoId, decimal Cantidad);

		public readonly record struct Request(
			Guid OrigenEstablecimientoId,
			Guid OrigenAlmacenId,
			Guid DestinoEstablecimientoId,
			Guid DestinoAlmacenId,
			DateTimeOffset? Fecha,
			IReadOnlyList<Linea> Lineas
		);

		public readonly record struct Response(Guid MovimientoSalidaId, Guid MovimientoEntradaId);

		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly IMovimientoInventarioRepository _movRepo;
		private readonly ICatalogoReadModel _catalogo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public TransferirEntreAlmacenesUseCase(
			IStockPorAlmacenRepository stockRepo,
			IMovimientoInventarioRepository movRepo,
			ICatalogoReadModel catalogo,
			ITenantContext tenant,
			IUnitOfWork uow)
		{
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_movRepo = movRepo ?? throw new ArgumentNullException(nameof(movRepo));
			_catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var oEst = EstablecimientoId.From(req.OrigenEstablecimientoId);
			var oAlm = AlmacenId.From(req.OrigenAlmacenId);
			var dEst = EstablecimientoId.From(req.DestinoEstablecimientoId);
			var dAlm = AlmacenId.From(req.DestinoAlmacenId);
			if (oEst == dEst && oAlm == dAlm)
				throw new BusinessRuleException("El origen y destino no pueden ser iguales.");

			if (req.Lineas is null || req.Lineas.Count == 0)
				throw new ArgumentException("Debe especificar al menos una línea.", nameof(req.Lineas));

			var fecha = req.Fecha ?? DateTimeOffset.UtcNow;

			// Validar disponibilidad en origen y preparar líneas de movimientos
			var lineasSalida = new List<LineaMovimiento>(req.Lineas.Count);
			var lineasEntrada = new List<LineaMovimiento>(req.Lineas.Count);

			foreach (var l in req.Lineas)
			{
				// Resolver ProductoId y validar consistencia si llega SKU y ProductoId
				ProductoId? productoId = null;
				if (l.ProductoId.HasValue)
					productoId = ProductoId.From(l.ProductoId.Value);
				if (!string.IsNullOrWhiteSpace(l.Sku))
				{
					var resolved = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, l.Sku!, ct)
								  ?? throw new NotFoundException($"No existe producto para SKU {l.Sku}.");
					if (productoId is not null && !productoId.Value.Equals(resolved))
						throw new BusinessRuleException("SKU y ProductoId no corresponden al mismo producto.");
					productoId ??= resolved;
				}
				if (productoId is null)
					throw new ArgumentException("Debe especificar SKU o ProductoId en la línea.");
				var pid = productoId.Value; // no-nullable guard
				var cant = CantidadStock.From(l.Cantidad);

				var stockOrigen = await _stockRepo.ObtenerAsync(empresaId, oEst, oAlm, pid, ct);
				if (stockOrigen is null)
					throw new NotFoundException($"No se encontró stock en el almacén de origen para el producto indicado.");

				// Egresar en origen (lanza si no alcanza)
				stockOrigen.Egresar(cant);
				await _stockRepo.GuardarAsync(stockOrigen, ct);

				// Ingresar en destino (crear si no existe)
				var stockDestino = await _stockRepo.ObtenerAsync(empresaId, dEst, dAlm, pid, ct)
								  ?? StockPorAlmacen.CrearNuevo(empresaId, dEst, dAlm, pid);
				stockDestino.Ingresar(cant);
				await _stockRepo.GuardarAsync(stockDestino, ct);

				lineasSalida.Add(LineaMovimiento.Crear(pid, cant));
				lineasEntrada.Add(LineaMovimiento.Crear(pid, cant));
			}

			// Registrar movimientos de salida/entrada
			var movSalida = MovimientoInventario.Registrar(
				empresaId, oEst, oAlm, fecha,
				TipoMovimiento.TransferenciaSalida,
				MotivoMovimiento.Transferencia,
				lineasSalida);

			var movEntrada = MovimientoInventario.Registrar(
				empresaId, dEst, dAlm, fecha,
				TipoMovimiento.TransferenciaEntrada,
				MotivoMovimiento.Transferencia,
				lineasEntrada);

			await _movRepo.GuardarAsync(movSalida, ct);
			await _movRepo.GuardarAsync(movEntrada, ct);

			await _uow.CommitAsync(ct);
			return new Response(movSalida.MovimientoId, movEntrada.MovimientoId);
		}
	}
}

