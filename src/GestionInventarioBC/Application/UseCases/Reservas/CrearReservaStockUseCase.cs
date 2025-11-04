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
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, string Sku, decimal Cantidad, DateTimeOffset? VenceEn);
		public readonly record struct Response(Guid ReservaId);

		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly IReservaStockRepository _reservaRepo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public CrearReservaStockUseCase(IStockPorAlmacenRepository stockRepo, IReservaStockRepository reservaRepo, ITenantContext tenant, IUnitOfWork uow)
		{
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_reservaRepo = reservaRepo ?? throw new ArgumentNullException(nameof(reservaRepo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);
			var sku = Sku.Crear(req.Sku);
			var cant = CantidadStock.From(req.Cantidad);

			var stock = await _stockRepo.ObtenerAsync(empresaId, estId, almId, sku, ct)
					   ?? StockPorAlmacen.CrearNuevo(empresaId, estId, almId, sku);
			var disp = DisponibilidadStock.Crear(stock.Real, stock.Reservado);
			var eval = PoliticaReserva.Evaluar(disp, cant);
			if (!eval.IsSatisfied)
				throw new BusinessRuleException(eval.Message);

			stock.Reservar(cant);
			await _stockRepo.GuardarAsync(stock, ct);

			var reserva = ReservaStock.Crear(empresaId, estId, almId, sku, cant, req.VenceEn);
			await _reservaRepo.GuardarAsync(reserva, ct);
			await _uow.CommitAsync(ct);
			return new Response(reserva.ReservaId);
		}
	}
}

