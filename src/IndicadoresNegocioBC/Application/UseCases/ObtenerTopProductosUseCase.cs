using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Exceptions;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Obtiene el Top de productos (por monto o cantidad) de un IndicadorNegocio
    /// identificado por su clave natural (Tipo + Periodo + Segmento), ya sea para
    /// todo el periodo o para un rango [Desde..Hasta] específico.
    /// No publica eventos (consulta pura).
    /// </summary>
    public sealed class ObtenerTopProductosUseCase
    {
        private readonly IIndicadorNegocioRepository _repository;

        public ObtenerTopProductosUseCase(IIndicadorNegocioRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ObtenerTopProductosOutputDto> ExecuteAsync(
            ObtenerTopProductosInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar agregado por clave natural (respetando overload con EmpresaId si viene)
            Domain.Aggregates.IndicadorNegocio? agregado =
                input.EmpresaId is not null
                    ? await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, input.EmpresaId, ct)
                    : await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, ct);

            if (agregado is null)
            {
                var key = $"Tipo={input.Tipo}, Periodo=[{input.Periodo.Inicio:yyyy-MM-dd}..{input.Periodo.FinInclusive:yyyy-MM-dd}], Segmento={input.Segmento}";
                throw new NotFoundException("IndicadorNegocio", key, "IndicadorNegocio no encontrado para la clave natural especificada.");
            }

            // 2) Consultar Top
            var items =
                (input.Desde.HasValue && input.Hasta.HasValue)
                    ? agregado.ObtenerRankingProductosPorRango(input.Desde.Value, input.Hasta.Value, input.Limite, input.Criterio)
                    : agregado.ObtenerTopProductos(input.Limite ?? Domain.ValueObjects.LimiteTop.Crear(10), input.Criterio);

            // 3) Mapear salida
            var salidaItems = items.Select(x => new ObtenerTopProductosOutputDto.Item(
                productoId: x.ProductoId,
                cantidad: x.Cantidad,
                totalVendido: x.TotalVendido
            )).ToList();

            return new ObtenerTopProductosOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                criterio: input.Criterio,
                desde: input.Desde,
                hasta: input.Hasta,
                items: salidaItems
            );
        }
    }
}
