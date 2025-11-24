using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;
using IndicadoresNegocioBC.Application.Interfaces;
using IndicadoresNegocioBC.Application.DTOs;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.Notificaciones
{
	/// <summary>
	/// Caso de uso: Actualizar campos configurables de una notificación.
	/// Campos soportados: Asunto, Horario, Rango Fechas, Días Semana, Destinatario.
	/// Medio no se expone porque el método de dominio es internal; dejar TODO para exponer si se requiere.
	/// Publica los eventos generados por cada cambio efectivo.
	/// Idempotencia: si ningún campo cambia no se publica nada.
	/// </summary>
	public sealed class ActualizarNotificacionIndicadorUseCase
	{
		private readonly INotificacionIndicadorRepository _repository;
		private readonly ITenantContext _tenant;
		private readonly IEventPublisher _eventPublisher;
		private readonly IUnitOfWork _uow;

		public ActualizarNotificacionIndicadorUseCase(
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

		public async Task<ActualizarNotificacionIndicadorOutputDto> ExecuteAsync(
			ActualizarNotificacionIndicadorInputDto input,
			CancellationToken ct = default)
		{
			if (input is null) throw new ArgumentNullException(nameof(input));

			var agregado = await _repository.GetByIdAsync(input.Id);
			if (agregado is null || agregado.EmpresaId != _tenant.EmpresaId)
				throw new NotFoundException("NOTIFICACION_NO_ENCONTRADA", "No se encontró la notificación indicada para la empresa actual.");

			int eventosIniciales = agregado.DomainEvents.Count;

			// Aplicar solo cambios efectivos.
			if (input.NuevoAsunto is not null && input.NuevoAsunto != agregado.Asunto)
				agregado.CambiarAsunto(input.NuevoAsunto);

			if (input.NuevoHorario is not null && !ReferenceEquals(input.NuevoHorario, agregado.HorarioEnvio))
				agregado.CambiarHorario(input.NuevoHorario);

			if (input.NuevaFechaInicio != agregado.FechaInicio || input.NuevaFechaFin != agregado.FechaFin)
				agregado.CambiarRangoFechas(input.NuevaFechaInicio, input.NuevaFechaFin);

			if (input.NuevosDiasSemana is not null)
			{
				// Comparación simple de arrays (orden irrelevante). Si difieren, aplicar.
				bool difieren = agregado.DiasSemana is null || agregado.DiasSemana.Length != input.NuevosDiasSemana.Length;
				if (!difieren)
				{
					for (int i = 0; i < agregado.DiasSemana!.Length; i++)
					{
						if (agregado.DiasSemana[i] != input.NuevosDiasSemana[i]) { difieren = true; break; }
					}
				}
				if (difieren)
					agregado.CambiarDiasSemana(input.NuevosDiasSemana);
			}

			if (input.NuevoDestinatario is not null && input.NuevoDestinatario.ToString() != agregado.Destinatario.ToString())
				agregado.CambiarDestinatario(input.NuevoDestinatario);

			bool huboCambios = agregado.DomainEvents.Count > eventosIniciales;
			if (huboCambios)
			{
				await _repository.UpdateAsync(agregado);
				await _uow.CommitAsync(ct);
				foreach (var evt in agregado.DomainEvents.Skip(eventosIniciales))
					await _eventPublisher.PublishAsync(evt, ct);
			}

			return new ActualizarNotificacionIndicadorOutputDto(
				id: agregado.Id,
				huboCambios: huboCambios,
				activo: agregado.Activo,
				asunto: agregado.Asunto,
				horario: agregado.HorarioEnvio.ToString(),
				fechaInicio: agregado.FechaInicio,
				fechaFin: agregado.FechaFin,
				diasSemana: agregado.DiasSemana,
				medio: agregado.Medio.Valor,
				destinatario: agregado.Destinatario.ToString()
			);
		}
	}
}

