using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.Repositories;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Crear un IndicadorNegocio si no existe para la clave natural (Tipo + Periodo + Segmento).
    /// Publica el evento de dominio "IndicadorNegocioCreado".
    /// </summary>
    public sealed class CrearIndicadorNegocioUseCase
    {
    private readonly IIndicadorNegocioRepository _repository;
        private readonly IEventBus _eventBus;
    private readonly ITenantContext _tenant;

        public CrearIndicadorNegocioUseCase(
            IIndicadorNegocioRepository repository,
            IEventBus eventBus,
            ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<CrearIndicadorNegocioOutputDto> ExecuteAsync(
            CrearIndicadorNegocioInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Verificar existencia por clave natural dentro del scope de empresa del tenant
            var empresaId = _tenant.EmpresaId;
            Domain.Aggregates.IndicadorNegocio? existente = await _repository.GetByClaveAsync(
                input.Tipo, input.Periodo, input.Segmento, empresaId, ct);

            if (existente is not null)
            {
                // Ya existe: violación de unicidad por clave natural.
                throw new BusinessRuleException(
                    code: "INDICADOR_YA_EXISTE",
                    message: "Ya existe un IndicadorNegocio para la clave natural especificada (Tipo + Periodo + Segmento).");
            }

            // 2) Crear el agregado (la fábrica valida Periodo alineado) con timestamp determinista si se provee
            var creadoEn = input.AhoraUtc ?? DateTimeOffset.UtcNow;
            var agregado = Domain.Aggregates.IndicadorNegocio.Crear(
                tipo: input.Tipo,
                periodo: input.Periodo,
                segmento: input.Segmento,
                ahora: creadoEn);

            // 3) Persistir
            await _repository.AddAsync(agregado, ct);

            // 4) Publicar evento de dominio (para proyecciones/outbox)
            var evt = new Domain.Events.IndicadorNegocioEvents.IndicadorNegocioCreado(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                creadoEn: agregado.CreadoEn,
                version: agregado.Version
            );
            await _eventBus.PublishAsync(evt, ct);

            // 5) Mapear salida
            return new CrearIndicadorNegocioOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                estado: agregado.Estado,
                creadoEn: agregado.CreadoEn,
                version: agregado.Version
            );
        }
    }
}
