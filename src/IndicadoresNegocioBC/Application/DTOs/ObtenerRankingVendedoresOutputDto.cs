using System;
using System.Collections.Generic;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida para el ranking de vendedores.
    /// Devuelve metadatos del indicador y la lista ordenada (según el agregado) de vendedores.
    /// </summary>
    public sealed class ObtenerRankingVendedoresOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public DateOnly Desde { get; }
        public DateOnly Hasta { get; }
        public IReadOnlyList<Item> Ranking { get; }

        public ObtenerRankingVendedoresOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateOnly desde,
            DateOnly hasta,
            IReadOnlyList<Item> items)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Desde = desde;
            Hasta = hasta;
            Ranking = items ?? throw new ArgumentNullException(nameof(items));
        }

        public sealed class Item
        {
            public UsuarioId VendedorId { get; }
            public Dinero TotalVendido { get; }

            public Item(UsuarioId vendedorId, Dinero totalVendido)
            {
                VendedorId = vendedorId ?? throw new ArgumentNullException(nameof(vendedorId));
                TotalVendido = totalVendido ?? throw new ArgumentNullException(nameof(totalVendido));
            }
        }
    }
}
