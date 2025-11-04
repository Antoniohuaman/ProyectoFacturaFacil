using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Reservas
{
	/// <summary>
	/// Libera una reserva pendiente (disminuye stock reservado).
	/// </summary>
	public sealed class LiberarReservaStockUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, Guid ReservaId);
		public readonly record struct Response(bool Ok);

		private readonly IReservaStockRepository _reservaRepo;
		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public LiberarReservaStockUseCase(IReservaStockRepository reservaRepo, IStockPorAlmacenRepository stockRepo, ITenantContext tenant, IUnitOfWork uow)
		{
			_reservaRepo = reservaRepo ?? throw new ArgumentNullException(nameof(reservaRepo));
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var reserva = await _reservaRepo.ObtenerAsync(empresaId, estId, almId, req.ReservaId, ct)
						  ?? throw new NotFoundException("Reserva no encontrada.");
			var stock = await _stockRepo.ObtenerAsync(empresaId, estId, almId, reserva.ProductoId, ct)
					   ?? throw new NotFoundException("Stock no encontrado para la reserva.");

			// Dominio: pasa a Liberada y refleja en stock como liberación
			reserva.Liberar();
			stock.LiberarReserva(reserva.Cantidad);

			await _reservaRepo.GuardarAsync(reserva, ct);
			await _stockRepo.GuardarAsync(stock, ct);
			await _uow.CommitAsync(ct);
			return new Response(true);
		}
	}
}

