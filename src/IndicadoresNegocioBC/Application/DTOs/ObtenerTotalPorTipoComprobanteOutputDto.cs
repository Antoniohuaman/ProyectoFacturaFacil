using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida para "ObtenerTotalPorTipoComprobante".
    /// Incluye metadatos del indicador, filtros aplicados y el total (Dinero).
    /// </summary>
    public sealed class ObtenerTotalPorTipoComprobanteOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }

        public string TipoComprobante { get; }
        public DateOnly? Desde { get; }
        public DateOnly? Hasta { get; }
        public EstablecimientoId? EstablecimientoId { get; }

        public Dinero Total { get; }

        public ObtenerTotalPorTipoComprobanteOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            string tipoComprobante,
            DateOnly? desde,
            DateOnly? hasta,
            EstablecimientoId? establecimientoId,
            Dinero total)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            if (string.IsNullOrWhiteSpace(tipoComprobante))
                throw new ArgumentNullException(nameof(tipoComprobante));

            TipoComprobante = tipoComprobante;
            Desde = desde;
            Hasta = hasta;
            EstablecimientoId = establecimientoId;
            Total = total ?? throw new ArgumentNullException(nameof(total));
        }
    }
}
