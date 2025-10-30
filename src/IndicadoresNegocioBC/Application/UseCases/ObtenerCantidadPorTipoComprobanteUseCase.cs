using System;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Exceptions;
using SharedKernel.Application.Interfaces;

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
        private readonly ITenantContext _tenant;

        public ObtenerCantidadPorTipoComprobanteUseCase(IIndicadorNegocioRepository repository, ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ObtenerCantidadPorTipoComprobanteOutputDto> ExecuteAsync(
            ObtenerCantidadPorTipoComprobanteInputDto input,
            CancellationToken ct = default)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // 1) Cargar agregado por clave natural dentro del scope de empresa del tenant
            var empresaId = _tenant.EmpresaId;
            Domain.Aggregates.IndicadorNegocio? agregado =
                await _repository.GetByClaveAsync(input.Tipo, input.Periodo, input.Segmento, empresaId, ct);

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
