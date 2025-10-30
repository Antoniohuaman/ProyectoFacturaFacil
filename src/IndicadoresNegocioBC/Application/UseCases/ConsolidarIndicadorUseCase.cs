using System;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;

using SharedKernel.Events;
using SharedKernel.Exceptions;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Marca el periodo del IndicadorNegocio como CONSOLIDADO.
    /// - Es idempotente a nivel de caso de uso (si ya está CONSOLIDADO, no persiste ni publica eventos).
    /// - Publica evento de cambio de estado (IndicadorNegocioActualizado) cuando hay transición efectiva.
    /// </summary>
    public sealed class ConsolidarIndicadorUseCase
    {
    private readonly IIndicadorNegocioRepository _repository;
        private readonly IEventBus _eventBus;
    private readonly ITenantContext _tenant;

        public ConsolidarIndicadorUseCase(
            IIndicadorNegocioRepository repository,
            IEventBus eventBus,
            ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ConsolidarIndicadorOutputDto> ExecuteAsync(
            ConsolidarIndicadorInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar el agregado por clave natural dentro del scope de empresa del tenant
            var empresaId = _tenant.EmpresaId;
            Domain.Aggregates.IndicadorNegocio? agregado =
                await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, empresaId, ct);

            if (agregado is null)
            {
                var key = $"Tipo={input.Tipo}, Periodo=[{input.Periodo.Inicio:yyyy-MM-dd}..{input.Periodo.FinInclusive:yyyy-MM-dd}], Segmento={input.Segmento}";
                throw new NotFoundException("IndicadorNegocio", key, "IndicadorNegocio no encontrado para la clave natural especificada.");
            }

            // 2) Si ya está consolidado, devolvemos idempotente sin mutaciones.
            if (Equals(agregado.Estado, EstadoIndicador.Consolidado))
            {
                return new ConsolidarIndicadorOutputDto(
                    indicadorId: agregado.IndicadorId,
                    tipo: agregado.Tipo,
                    periodo: agregado.Periodo,
                    segmento: agregado.Segmento,
                    estado: agregado.Estado,
                    consolidadoEn: agregado.ConsolidadoEn,
                    version: agregado.Version,
                    fueIdempotente: true
                );
            }

            var estadoAntes = agregado.Estado;
            var versionAntes = agregado.Version;

            // 3) Consolidar (mutación de dominio)
            agregado.ConsolidarPeriodo(input.Ahora);

            // 4) Persistir y publicar evento de actualización de estado
            await _repository.UpdateAsync(agregado, ct);

            var evtEstado = new Domain.Events.IndicadorNegocioEvents.IndicadorNegocioActualizado(
                indicadorId: agregado.IndicadorId,
                estadoAnterior: estadoAntes,
                estadoNuevo: agregado.Estado,
                version: agregado.Version
            );
            await _eventBus.PublishAsync(evtEstado, ct);

            // 5) Salida
            return new ConsolidarIndicadorOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                estado: agregado.Estado,
                consolidadoEn: agregado.ConsolidadoEn,
                version: agregado.Version,
                fueIdempotente: agregado.Version == versionAntes // debería ser false si consolidó
            );
        }
    }
}
