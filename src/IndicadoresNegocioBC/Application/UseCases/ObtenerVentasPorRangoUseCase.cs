using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndicadoresNegocioBC.Domain.Repositories;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using SharedKernel.Application.Interfaces;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// Caso de uso: Devuelve las ventas registradas por el agregado en el rango [Desde..Hasta] (inclusive).
    /// Es una consulta (no publica eventos).
    /// </summary>
    public sealed class ObtenerVentasPorRangoUseCase
    {
        private readonly IIndicadorNegocioRepository _repository;
        private readonly ITenantContext _tenant;

        public ObtenerVentasPorRangoUseCase(IIndicadorNegocioRepository repository, ITenantContext tenant)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        public async Task<ObtenerVentasPorRangoOutputDto> ExecuteAsync(
            ObtenerVentasPorRangoInputDto input,
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

            // 2) Obtener ventas por rango (el agregado ya valida rango inclusive)
            var ventas = agregado.ObtenerVentasPorRango(input.Desde, input.Hasta);

            // 3) Mapear ventas a DTO
            var items = ventas.Select(v =>
                new ObtenerVentasPorRangoOutputDto.VentaItem(
                    comprobanteId: v.ComprobanteId,
                    fecha: v.Fecha,
                    clienteId: v.ClienteId,
                    total: v.Total,
                    igv: v.Igv,
                    tipoComprobante: v.TipoComprobante,
                    vendedorId: v.VendedorId,
                    establecimientoId: v.EstablecimientoId,
                    items: v.Items.Select(it => new ObtenerVentasPorRangoOutputDto.VentaItem.Item(
                        productoId: it.ProductoId,
                        cantidad: it.Cantidad,
                        subtotal: it.Subtotal
                    )).ToList()
                )).ToList();

            // 4) Resumen del rango
            var moneda = agregado.Segmento.Moneda;
            Dinero Sumar(Dinero acc, Dinero x) => acc.Sumar(x);

            var totalVentasRango = ventas
                .Select(v => v.Total)
                .DefaultIfEmpty(Dinero.Cero(moneda))
                .Aggregate(Dinero.Cero(moneda), Sumar);

            var totalIgvRango = ventas
                .Select(v => v.Igv)
                .DefaultIfEmpty(Dinero.Cero(moneda))
                .Aggregate(Dinero.Cero(moneda), Sumar);

            var totalComprobantesRango = ventas.Count;

            // 5) Devolver salida
            return new ObtenerVentasPorRangoOutputDto(
                indicadorId: agregado.IndicadorId,
                tipo: agregado.Tipo,
                periodo: agregado.Periodo,
                segmento: agregado.Segmento,
                desde: input.Desde,
                hasta: input.Hasta,
                ventas: items,
                totalVentasRango: totalVentasRango,
                totalIgvRango: totalIgvRango,
                totalComprobantesRango: totalComprobantesRango
            );
        }
    }
}
