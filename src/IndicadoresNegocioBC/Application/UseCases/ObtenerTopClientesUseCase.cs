using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Obtiene el Top de clientes de un IndicadorNegocio
    /// identificado por su clave natural (Tipo + Periodo + Segmento), ya sea
    /// para todo el periodo o para un rango [Desde..Hasta] específico.
    /// No publica eventos (consulta).
    /// </summary>
    public sealed class ObtenerTopClientesUseCase
    {
        private readonly IIndicadorNegocioRepository _repository;
        private readonly ITenantContext _tenant;

        public ObtenerTopClientesUseCase(IIndicadorNegocioRepository repository, ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ObtenerTopClientesOutputDto> ExecuteAsync(
            ObtenerTopClientesInputDto input,
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

            // 2) Consultar Top
            var items =
                (input.Desde.HasValue && input.Hasta.HasValue)
                    ? agregado.ObtenerRankingClientesPorRango(input.Desde.Value, input.Hasta.Value, input.Limite)
                    : agregado.ObtenerTopClientes(input.Limite ?? Domain.ValueObjects.LimiteTop.Crear(10));

            // 3) Mapear salida
            var salidaItems = items.Select(x => new ObtenerTopClientesOutputDto.Item(
                clienteId: x.ClienteId,
                frecuencia: x.Frecuencia,
                totalComprado: x.TotalComprado
            )).ToList();

            return new ObtenerTopClientesOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                desde: input.Desde,
                hasta: input.Hasta,
                items: salidaItems
            );
        }
    }
}
