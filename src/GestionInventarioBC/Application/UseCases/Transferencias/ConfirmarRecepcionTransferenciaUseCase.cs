using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.Transferencias
{
	/// <summary>
	/// Confirma una transferencia: egresa del origen e ingresa en el destino, y cambia el estado a Confirmada.
	/// </summary>
	public sealed class ConfirmarRecepcionTransferenciaUseCase
	{
		public readonly record struct Request(Guid TransferenciaId);
		public readonly record struct Response(bool Ok);

		private readonly ITransferenciaInventarioRepository _tRepo;
		private readonly IStockPorAlmacenRepository _stockRepo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public ConfirmarRecepcionTransferenciaUseCase(ITransferenciaInventarioRepository tRepo, IStockPorAlmacenRepository stockRepo, ITenantContext tenant, IUnitOfWork uow)
		{
			_tRepo = tRepo ?? throw new ArgumentNullException(nameof(tRepo));
			_stockRepo = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var t = await _tRepo.ObtenerAsync(empresaId, req.TransferenciaId, ct) ?? throw new NotFoundException("Transferencia no encontrada.");

			// Egreso en origen
			var stockOrigen = await _stockRepo.ObtenerAsync(empresaId, t.OrigenEstablecimientoId, t.OrigenAlmacenId, t.Sku, ct)
							 ?? throw new NotFoundException("Stock de origen no encontrado.");
			stockOrigen.Egresar(t.Cantidad);
			await _stockRepo.GuardarAsync(stockOrigen, ct);

			// Ingreso en destino
			var stockDestino = await _stockRepo.ObtenerAsync(empresaId, t.DestinoEstablecimientoId, t.DestinoAlmacenId, t.Sku, ct)
							  ?? Domain.Aggregates.StockPorAlmacen.CrearNuevo(empresaId, t.DestinoEstablecimientoId, t.DestinoAlmacenId, t.Sku);
			stockDestino.Ingresar(t.Cantidad);
			await _stockRepo.GuardarAsync(stockDestino, ct);

			t.Confirmar();
			await _tRepo.GuardarAsync(t, ct);
			await _uow.CommitAsync(ct);
			return new Response(true);
		}
	}
}

