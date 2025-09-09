using System;
using System.Collections.Generic;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida para el Top de productos.
    /// Incluye metadatos del indicador, el criterio aplicado y (si se usó) el rango consultado.
    /// </summary>
    public sealed class ObtenerTopProductosOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public Domain.Aggregates.IndicadorNegocio.RankingCriterio Criterio { get; }
        public DateOnly? Desde { get; }
        public DateOnly? Hasta { get; }
        public IReadOnlyList<Item> TopProductos { get; }

        public ObtenerTopProductosOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            Domain.Aggregates.IndicadorNegocio.RankingCriterio criterio,
            DateOnly? desde,
            DateOnly? hasta,
            IReadOnlyList<Item> items)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Criterio = criterio;
            Desde = desde;
            Hasta = hasta;
            TopProductos = items ?? throw new ArgumentNullException(nameof(items));
        }

        public sealed class Item
        {
            public string ProductoId { get; }
            public decimal Cantidad { get; }
            public Dinero TotalVendido { get; }

            public Item(string productoId, decimal cantidad, Dinero totalVendido)
            {
                if (string.IsNullOrWhiteSpace(productoId))
                    throw new ArgumentException("ProductoId requerido.", nameof(productoId));

                ProductoId = productoId;
                Cantidad = cantidad;
                TotalVendido = totalVendido ?? throw new ArgumentNullException(nameof(totalVendido));
            }
        }
    }
}
