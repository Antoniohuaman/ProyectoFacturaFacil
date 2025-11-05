using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using GestionInventarioBC.Domain.Policies;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Reservas
{
	/// <summary>
	/// Crea una reserva de stock para un SKU (incrementa stock reservado) si hay disponibilidad.
	/// </summary>
	public sealed class CrearReservaStockUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, string? Sku, Guid? ProductoId, decimal Cantidad, DateTimeOffset? VenceEn);
		public readonly record struct Response(Guid ReservaId);

		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly IReservaStockRepository _reservaRepo;
		private readonly ICatalogoReadModel _catalogo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public CrearReservaStockUseCase(IStockPorAlmacenRepository stockRepo, IReservaStockRepository reservaRepo, ICatalogoReadModel catalogo, ITenantContext tenant, IUnitOfWork uow)
		{
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_reservaRepo = reservaRepo ?? throw new ArgumentNullException(nameof(reservaRepo));
			_catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);
			var cant = CantidadStock.From(req.Cantidad);

			// Resolver ProductoId y validar consistencia si llega SKU y ProductoId
			ProductoId? productoId = null;
			if (req.ProductoId.HasValue)
				productoId = ProductoId.From(req.ProductoId.Value);
			if (!string.IsNullOrWhiteSpace(req.Sku))
			{
				var resolved = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, req.Sku!, ct)
							  ?? throw new NotFoundException("No existe producto para el SKU indicado.");
				if (productoId is not null && !productoId.Value.Equals(resolved))
					throw new BusinessRuleException("SKU y ProductoId no corresponden al mismo producto.");
				productoId ??= resolved;
			}
			if (productoId is null)
				throw new ArgumentException("Debe especificar SKU o ProductoId.");

			var stock = await _stockRepo.ObtenerAsync(empresaId, estId, almId, productoId, ct)
					   ?? StockPorAlmacen.CrearNuevo(empresaId, estId, almId, productoId);
			var disp = DisponibilidadStock.Crear(stock.Real, stock.Reservado);
			var eval = PoliticaReserva.Evaluar(disp, cant);
			if (!eval.IsSatisfied)
				throw new BusinessRuleException(eval.Message);

			stock.Reservar(cant);
			await _stockRepo.GuardarAsync(stock, ct);

			var reserva = ReservaStock.Crear(empresaId, estId, almId, productoId, cant, req.VenceEn);
			await _reservaRepo.GuardarAsync(reserva, ct);
			await _uow.CommitAsync(ct);
			return new Response(reserva.ReservaId);
		}
	}
}

