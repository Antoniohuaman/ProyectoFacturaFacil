using System;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces;
using GestionInventarioBC.Domain.Repositories;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionInventarioBC.Application.UseCases.Transferencias
{
	/// <summary>
	/// Cancela una transferencia pendiente (no afecta stock si no fue confirmada).
	/// </summary>
	public sealed class CancelarTransferenciaPendienteUseCase
	{
		public readonly record struct Request(Guid TransferenciaId);
		public readonly record struct Response(bool Ok);

		private readonly ITransferenciaInventarioRepository _repo;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public CancelarTransferenciaPendienteUseCase(ITransferenciaInventarioRepository repo, ITenantContext tenant, IUnitOfWork uow)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<Response> Handle(Request req, CancellationToken ct)
		{
			var empresaId = _tenant.EmpresaId ?? throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");
			var t = await _repo.ObtenerAsync(empresaId, req.TransferenciaId, ct) ?? throw new NotFoundException("Transferencia no encontrada.");
			t.Cancelar();
			await _repo.GuardarAsync(t, ct);
			await _uow.CommitAsync(ct);
			return new Response(true);
		}
	}
}

