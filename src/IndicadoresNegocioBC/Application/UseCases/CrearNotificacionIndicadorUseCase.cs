using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;
using IndicadoresNegocioBC.Application.Interfaces;
using IndicadoresNegocioBC.Application.DTOs;

namespace IndicadoresNegocioBC.Application.UseCases.Notificaciones
{
	/// <summary>
	/// Caso de uso: Crear una configuración de notificación para un indicador.
	/// Publica el/los eventos generados por el agregado (incluye NotificacionIndicadorCreada).
	/// Idempotencia: actualmente no hay clave natural; si se requiriera evitar duplicados debería agregarse en el repositorio.
	/// </summary>
	public sealed class CrearNotificacionIndicadorUseCase
	{
		private readonly INotificacionIndicadorRepository _repository;
		private readonly ITenantContext _tenant;
		private readonly IEventPublisher _eventPublisher;
		private readonly IUnitOfWork _uow;

		public CrearNotificacionIndicadorUseCase(
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

		public async Task<CrearNotificacionIndicadorOutputDto> ExecuteAsync(
			CrearNotificacionIndicadorInputDto input,
			CancellationToken ct = default)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));

			var empresaId = _tenant.EmpresaId;

			// Crear el agregado delegando validaciones al dominio.
			var agregado = new NotificacionIndicador(
				empresaId: empresaId,
				indicadorId: input.IndicadorId,
				establecimientoId: input.EstablecimientoId,
				usuarioId: input.UsuarioId,
				asunto: input.Asunto,
				horarioEnvio: input.HorarioEnvio,
				medio: input.Medio,
				destinatario: input.Destinatario,
				activo: input.ActivoInicial
			);

			await _repository.AddAsync(agregado);
			await _uow.CommitAsync(ct);

			// Publicar todos los eventos generados por el agregado.
			foreach (var evt in agregado.DomainEvents)
			{
				await _eventPublisher.PublishAsync(evt, ct);
			}

			return new CrearNotificacionIndicadorOutputDto(
				id: agregado.Id,
				indicadorId: agregado.IndicadorId,
				empresaId: agregado.EmpresaId,
				establecimientoId: agregado.EstablecimientoId,
				usuarioId: agregado.UsuarioId,
				asunto: agregado.Asunto,
				horario: agregado.HorarioEnvio.ToString(),
				medio: agregado.Medio.Valor,
				destinatario: agregado.Destinatario.ToString(),
				activo: agregado.Activo,
				fechaCreacion: agregado.FechaCreacion
			);
		}
	}
}

