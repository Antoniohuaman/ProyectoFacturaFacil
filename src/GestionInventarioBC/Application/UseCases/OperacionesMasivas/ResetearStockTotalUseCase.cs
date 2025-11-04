using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Application.UseCases.OperacionesMasivas
{
	/// <summary>
	/// Resetea a cero el stock real y reservado de todos los SKUs en un almacén.
	/// </summary>
	public sealed class ResetearStockTotalUseCase
	{
		public readonly record struct Request(Guid EstablecimientoId, Guid AlmacenId);
		public readonly record struct Response(int Afectados);

		private readonly IStockPorAlmacenRepository _repo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public ResetearStockTotalUseCase(IStockPorAlmacenRepository repo, ITenantContext tenant, IUnitOfWork uow)
		{
			_repo = repo ?? throw new System.ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new System.ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new System.ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new System.InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var estId = EstablecimientoId.From(req.EstablecimientoId);
			var almId = AlmacenId.From(req.AlmacenId);

			var lista = await _repo.ListarPorAlmacenAsync(empresaId, estId, almId, ct);
			var afectados = 0;
			foreach (var s in lista)
			{
				// Liberar todo lo reservado primero
				if (s.Reservado.Value > 0m)
				{
					s.LiberarReserva(CantidadStock.From(s.Reservado.Value));
				}
				// Egresar todo el real restante
				if (s.Real.Value > 0m)
				{
					s.Egresar(CantidadStock.From(s.Real.Value));
				}
				await _repo.GuardarAsync(s, ct);
				afectados++;
			}

			await _uow.CommitAsync(ct);
			return new Response(afectados);
		}
	}
}

