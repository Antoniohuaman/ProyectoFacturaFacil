using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Obtiene la lista de ventas diarias de un IndicadorNegocio para un rango de fechas.
    /// No publica eventos (consulta).
    /// </summary>
    public sealed class ObtenerVentasDiariasUseCase
    {
        private readonly IIndicadorNegocioRepository _repository;
        private readonly ITenantContext _tenant;

        public ObtenerVentasDiariasUseCase(IIndicadorNegocioRepository repository, ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ObtenerVentasDiariasOutputDto> ExecuteAsync(
            ObtenerVentasDiariasInputDto input,
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

            // 2) Obtener y filtrar por rango (inclusive)
            var ventas = agregado.ObtenerVentasDiariasOrdenadas()
                .Where(v => v.Fecha >= input.Desde && v.Fecha <= input.Hasta)
                .ToList();

            // 3) Mapear lista de items
            var items = ventas
                .Select(v => new ObtenerVentasDiariasOutputDto.Item(
                    fecha: v.Fecha,
                    totalVentas: v.TotalVentas,
                    totalIgv: v.TotalIgv,
                    nroComprobantes: v.NroComprobantes))
                .ToList();

            // 4) Resumen del rango (útil para dashboards)
            var moneda = agregado.Segmento.Moneda;
            var totalVentasRango = ventas
                .Select(v => v.TotalVentas)
                .DefaultIfEmpty(Dinero.Cero(moneda))
                .Aggregate(Dinero.Cero(moneda), (a, b) => a.Sumar(b));

            var totalIgvRango = ventas
                .Select(v => v.TotalIgv)
                .DefaultIfEmpty(Dinero.Cero(moneda))
                .Aggregate(Dinero.Cero(moneda), (a, b) => a.Sumar(b));

            var totalComprobantesRango = ventas.Sum(v => v.NroComprobantes);

            // 5) Armar salida
            return new ObtenerVentasDiariasOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                desde: input.Desde,
                hasta: input.Hasta,
                ventasDiarias: items,
                totalVentasRango: totalVentasRango,
                totalIgvRango: totalIgvRango,
                totalComprobantesRango: totalComprobantesRango
            );
        }
    }
}
