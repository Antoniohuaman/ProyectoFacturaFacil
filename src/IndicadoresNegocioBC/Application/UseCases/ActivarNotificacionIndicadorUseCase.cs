using System;
using System.Linq;
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
	/// Caso de uso: Activar una notificación si está inactiva.
	/// Idempotencia: si ya está activa no se publica evento y se marca FueIdempotente.
	/// </summary>
	public sealed class ActivarNotificacionIndicadorUseCase
	{
		private readonly INotificacionIndicadorRepository _repository;
		private readonly ITenantContext _tenant;
		private readonly IEventPublisher _eventPublisher;
		private readonly IUnitOfWork _uow;

		public ActivarNotificacionIndicadorUseCase(
			INotificacionIndicadorRepository repository,
			ITenantContext tenant,
			IEventPublisher eventPublisher,
			IUnitOfWork uow)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
			_eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
			_uow = uow ?? throw new ArgumentNullException(nameof(uow));
		}

		public async Task<ActivarNotificacionIndicadorOutputDto> ExecuteAsync(
			ActivarNotificacionIndicadorInputDto input,
			CancellationToken ct = default)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));
			var agregado = await _repository.GetByIdAsync(input.Id);
			if (agregado is null || agregado.EmpresaId != _tenant.EmpresaId)
				throw new NotFoundException("NOTIFICACION_NO_ENCONTRADA", "No se encontró la notificación para la empresa actual.");

			bool idempotente = agregado.Activo;
			int eventosIniciales = agregado.DomainEvents.Count;
			if (!idempotente)
			{
				agregado.Activar();
				await _repository.UpdateAsync(agregado);
				await _uow.CommitAsync(ct);
				foreach (var evt in agregado.DomainEvents.Skip(eventosIniciales))
					await _eventPublisher.PublishAsync(evt, ct);
			}

			return new ActivarNotificacionIndicadorOutputDto(
				id: agregado.Id,
				activo: agregado.Activo,
				fueIdempotente: idempotente
			);
		}
	}
}

