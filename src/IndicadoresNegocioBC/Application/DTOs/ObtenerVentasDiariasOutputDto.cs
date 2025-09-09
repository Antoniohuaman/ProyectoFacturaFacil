using System;
using System.Collections.Generic;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida para "ObtenerVentasDiarias".
    /// Incluye metadatos del indicador, rango consultado, la lista de días y un resumen del rango.
    /// </summary>
    public sealed class ObtenerVentasDiariasOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public DateOnly Desde { get; }
        public DateOnly Hasta { get; }
        public IReadOnlyList<Item> VentasDiarias { get; }
        public Dinero TotalVentasRango { get; }
        public Dinero TotalIgvRango { get; }
        public int TotalComprobantesRango { get; }

        public ObtenerVentasDiariasOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateOnly desde,
            DateOnly hasta,
            IReadOnlyList<Item> ventasDiarias,
            Dinero totalVentasRango,
            Dinero totalIgvRango,
            int totalComprobantesRango)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Desde = desde;
            Hasta = hasta;
            VentasDiarias = ventasDiarias ?? throw new ArgumentNullException(nameof(ventasDiarias));
            TotalVentasRango = totalVentasRango ?? throw new ArgumentNullException(nameof(totalVentasRango));
            TotalIgvRango = totalIgvRango ?? throw new ArgumentNullException(nameof(totalIgvRango));
            TotalComprobantesRango = totalComprobantesRango;
        }

        public sealed class Item
        {
            public DateOnly Fecha { get; }
            public Dinero TotalVentas { get; }
            public Dinero TotalIgv { get; }
            public int NroComprobantes { get; }

            public Item(DateOnly fecha, Dinero totalVentas, Dinero totalIgv, int nroComprobantes)
            {
                Fecha = fecha;
                TotalVentas = totalVentas ?? throw new ArgumentNullException(nameof(totalVentas));
                TotalIgv = totalIgv ?? throw new ArgumentNullException(nameof(totalIgv));
                NroComprobantes = nroComprobantes;
            }
        }
    }
}
