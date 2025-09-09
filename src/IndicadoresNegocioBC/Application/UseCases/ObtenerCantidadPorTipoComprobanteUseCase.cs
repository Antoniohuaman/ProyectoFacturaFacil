using System;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Exceptions;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Obtiene la cantidad de comprobantes por tipo,
    /// con filtros opcionales de rango de fechas y establecimiento.
    /// Consulta pura: no publica eventos.
    /// </summary>
    public sealed class ObtenerCantidadPorTipoComprobanteUseCase
    {
        private readonly IIndicadorNegocioRepository _repository;

        public ObtenerCantidadPorTipoComprobanteUseCase(IIndicadorNegocioRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ObtenerCantidadPorTipoComprobanteOutputDto> ExecuteAsync(
            ObtenerCantidadPorTipoComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar agregado por clave natural (usar overload con EmpresaId si viene)
            Domain.Aggregates.IndicadorNegocio? agregado =
                input.EmpresaId is not null
                    ? await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, input.EmpresaId, ct)
                    : await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, ct);

            if (agregado is null)
            {
                var key = $"Tipo={input.Tipo}, Periodo=[{input.Periodo.Inicio:yyyy-MM-dd}..{input.Periodo.FinInclusive:yyyy-MM-dd}], Segmento={input.Segmento}";
                throw new NotFoundException("IndicadorNegocio", key, "IndicadorNegocio no encontrado para la clave natural especificada.");
            }

            // 2) Delegar conteo al agregado (normaliza internamente el tipo de comprobante)
            var cantidad = agregado.ObtenerCantidadPorTipoComprobante(
                input.TipoComprobante,
                input.Desde,
                input.Hasta,
                input.EstablecimientoId
            );

            // 3) Salida
            return new ObtenerCantidadPorTipoComprobanteOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                tipoComprobante: input.TipoComprobante,
                desde: input.Desde,
                hasta: input.Hasta,
                establecimientoId: input.EstablecimientoId,
                cantidad: cantidad
            );
        }
    }
}
