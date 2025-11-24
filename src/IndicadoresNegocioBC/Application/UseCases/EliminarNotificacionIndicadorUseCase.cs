using System;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;
using IndicadoresNegocioBC.Application.Interfaces;
using IndicadoresNegocioBC.Application.DTOs;

namespace IndicadoresNegocioBC.Application.UseCases.Notificaciones
{
	/// <summary>
	/// Caso de uso: Eliminar una notificación.
	/// No publica eventos (podría agregarse uno en el dominio si se requiere auditoría).
	/// </summary>
	public sealed class EliminarNotificacionIndicadorUseCase
	{
		private readonly INotificacionIndicadorRepository _repository;
		private readonly ITenantContext _tenant;
		private readonly IUnitOfWork _uow;

		public EliminarNotificacionIndicadorUseCase(
			INotificacionIndicadorRepository repository,
			ITenantContext tenant,
			IUnitOfWork uow)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<EliminarNotificacionIndicadorOutputDto> ExecuteAsync(
			EliminarNotificacionIndicadorInputDto input,
			CancellationToken ct = default)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));
			var agregado = await _repository.GetByIdAsync(input.Id);
			if (agregado is null || agregado.EmpresaId != _tenant.EmpresaId)
				throw new NotFoundException("NOTIFICACION_NO_ENCONTRADA", "No se encontró la notificación para la empresa actual.");

			await _repository.DeleteAsync(agregado.Id);
			await _uow.CommitAsync(ct);

			return new EliminarNotificacionIndicadorOutputDto(agregado.Id, true);
		}
	}
}

