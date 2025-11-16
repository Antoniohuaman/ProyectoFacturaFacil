using System;
using System.Threading;
using System.Threading.Tasks;
using GestionClientesBC.Application.Interfaces; // IUnitOfWork
using GestionClientesBC.Domain.Aggregates;
using GestionClientesBC.Domain.Repositories;
using GestionClientesBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.Exceptions;

namespace GestionClientesBC.Application.Clientes.FotoPerfil.Actualizar
{
	public interface IActualizarFotoPerfilClienteUseCase
	{
		Task<ActualizarFotoPerfilClienteOutputDto> Handle(ActualizarFotoPerfilClienteInputDto input, CancellationToken ct = default);
	}

	/// <summary>
	/// Actualiza los metadatos de la foto principal del cliente.
	/// </summary>
	public sealed class ActualizarFotoPerfilClienteUseCase : IActualizarFotoPerfilClienteUseCase
	{
		private readonly IClienteRepository _repo;
		private readonly IUnitOfWork _uow;
		private readonly ITenantContext _tenant;

		public ActualizarFotoPerfilClienteUseCase(IClienteRepository repo, IUnitOfWork uow, ITenantContext tenant)
		{
			_repo = repo ?? throw new ArgumentNullException(nameof(repo));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
		}

		public async Task<ActualizarFotoPerfilClienteOutputDto> Handle(ActualizarFotoPerfilClienteInputDto input, CancellationToken ct = default)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));
			if (input.ClienteId == Guid.Empty)
				throw new BusinessRuleException("ClienteId no puede ser vacío.");

			var empresaId = _tenant.EmpresaId;
			if (empresaId is null || empresaId.IsEmpty)
				throw new BusinessRuleException("No se pudo resolver la Empresa actual.");

			var cliente = await _repo.GetByIdAsync(empresaId, input.ClienteId);
			if (cliente is null)
				throw NotFoundException.For<Cliente>(input.ClienteId);

			if (!cliente.EmpresaId.EsMismaEmpresaQue(empresaId))
				throw NotFoundException.For<Cliente>(input.ClienteId);

			bool removerFoto = string.IsNullOrWhiteSpace(input.NombreArchivo) && string.IsNullOrWhiteSpace(input.UrlPublica);
			FotoPerfilCliente? fotoPerfil = removerFoto ? null : FotoPerfilCliente.Create(input.NombreArchivo, input.UrlPublica);

			var expectedVersion = input.ExpectedVersion ?? cliente.Version;
			cliente.ActualizarFotoPerfil(fotoPerfil);

			await _repo.UpdateAsync(cliente, expectedVersion);
			await _uow.CommitAsync(ct);

			var fechaActualizacion = cliente.FechaUltimaModificacion ?? DateTime.UtcNow;

			return new ActualizarFotoPerfilClienteOutputDto
			{
				ClienteId = cliente.ClienteId,
				EmpresaId = empresaId.Value,
				TieneFoto = cliente.FotoPerfil?.TieneFoto ?? false,
				NombreArchivo = cliente.FotoPerfil?.NombreArchivo,
				UrlPublica = cliente.FotoPerfil?.UrlPublica,
				FechaActualizacionUtc = fechaActualizacion,
				Version = cliente.Version
			};
		}
	}
}
