using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida para "ObtenerCantidadPorTipoComprobante".
    /// Incluye metadatos del indicador, filtros aplicados y la cantidad total.
    /// </summary>
    public sealed class ObtenerCantidadPorTipoComprobanteOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }

        public string TipoComprobante { get; }
        public DateOnly? Desde { get; }
        public DateOnly? Hasta { get; }
        public EstablecimientoId? EstablecimientoId { get; }

        public int Cantidad { get; }

        public ObtenerCantidadPorTipoComprobanteOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            string tipoComprobante,
            DateOnly? desde,
            DateOnly? hasta,
            EstablecimientoId? establecimientoId,
            int cantidad)
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
            Cantidad = cantidad;
        }
    }
}
