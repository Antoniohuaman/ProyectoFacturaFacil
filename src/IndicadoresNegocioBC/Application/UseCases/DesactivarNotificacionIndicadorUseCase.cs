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
	/// Caso de uso: Desactivar una notificación si está activa.
	/// Idempotencia: si ya está inactiva no se publica evento.
	/// </summary>
	public sealed class DesactivarNotificacionIndicadorUseCase
	{
		private readonly INotificacionIndicadorRepository _repository;
		private readonly ITenantContext _tenant;
		private readonly IEventPublisher _eventPublisher;
		private readonly IUnitOfWork _uow;

		public DesactivarNotificacionIndicadorUseCase(
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

		public async Task<DesactivarNotificacionIndicadorOutputDto> ExecuteAsync(
			DesactivarNotificacionIndicadorInputDto input,
			CancellationToken ct = default)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));
			var agregado = await _repository.GetByIdAsync(input.Id);
			if (agregado is null || agregado.EmpresaId != _tenant.EmpresaId)
				throw new NotFoundException("NOTIFICACION_NO_ENCONTRADA", "No se encontró la notificación para la empresa actual.");

			bool idempotente = !agregado.Activo;
			int eventosIniciales = agregado.DomainEvents.Count;
			if (!idempotente)
			{
				agregado.Desactivar();
				await _repository.UpdateAsync(agregado);
				await _uow.CommitAsync(ct);
				foreach (var evt in agregado.DomainEvents.Skip(eventosIniciales))
					await _eventPublisher.PublishAsync(evt, ct);
			}

			return new DesactivarNotificacionIndicadorOutputDto(
				id: agregado.Id,
				activo: agregado.Activo,
				fueIdempotente: idempotente
			);
		}
	}
}

