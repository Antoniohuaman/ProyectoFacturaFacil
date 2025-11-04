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
	/// Extiende la fecha de vencimiento de una reserva pendiente.
	/// </summary>
	public sealed class ExtenderReservaStockUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId, Guid ReservaId, DateTimeOffset NuevaFechaVencimiento);
		public readonly record struct Response(bool Ok);

		private readonly IReservaStockRepository _reservaRepo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public ExtenderReservaStockUseCase(IReservaStockRepository reservaRepo, ITenantContext tenant, IUnitOfWork uow)
		{
			_reservaRepo = reservaRepo ?? throw new ArgumentNullException(nameof(reservaRepo));
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

			reserva.ExtenderHasta(req.NuevaFechaVencimiento);
			await _reservaRepo.GuardarAsync(reserva, ct);
			await _uow.CommitAsync(ct);
			return new Response(true);
		}
	}
}

