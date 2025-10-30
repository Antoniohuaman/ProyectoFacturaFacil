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
    /// Caso de uso: Obtiene el ranking de vendedores para un IndicadorNegocio
    /// identificado por su clave natural (Tipo + Periodo + Segmento) y un rango de fechas.
    /// No publica eventos (consulta pura).
    /// </summary>
    public sealed class ObtenerRankingVendedoresUseCase
    {
        private readonly IIndicadorNegocioRepository _repository;
        private readonly ITenantContext _tenant;

        public ObtenerRankingVendedoresUseCase(IIndicadorNegocioRepository repository, ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ObtenerRankingVendedoresOutputDto> ExecuteAsync(
            ObtenerRankingVendedoresInputDto input,
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

            // 2) Consultar ranking en el rango
            var ranking = agregado.ObtenerRankingVendedoresPorRango(input.Desde, input.Hasta, input.Limite);

            // 3) Mapear salida
            var items = ranking.Select(x =>
                new ObtenerRankingVendedoresOutputDto.Item(
                    vendedorId: x.VendedorId,
                    totalVendido: x.TotalVendido
                )).ToList();

            return new ObtenerRankingVendedoresOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                desde: input.Desde,
                hasta: input.Hasta,
                items: items
            );
        }
    }
}
