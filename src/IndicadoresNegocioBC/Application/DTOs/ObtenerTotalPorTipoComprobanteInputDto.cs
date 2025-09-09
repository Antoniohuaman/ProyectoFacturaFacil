using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para obtener el total por tipo de comprobante.
    /// Clave natural: Tipo + Periodo (alineado) + Segmento.
    /// Puede consultarse:
    /// - Todo el periodo (si no pasas rango), o
    /// - Un rango [Desde..Hasta] (inclusive) dentro del Periodo.
    /// Filtro opcional por EstablecimientoId.
    /// EmpresaId es opcional si el aislamiento multi-tenant lo maneja infraestructura.
    /// </summary>
    public sealed class ObtenerTotalPorTipoComprobanteInputDto
    {
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }

        /// <summary>“Factura”, “Boleta”, etc. Se normaliza en el agregado.</summary>
        public string TipoComprobante { get; }

        /// <summary>Rango opcional (inclusive). Si se omite, se usa todo el periodo.</summary>
        public DateOnly? Desde { get; }
        public DateOnly? Hasta { get; }

        /// <summary>Filtro opcional por establecimiento.</summary>
        public EstablecimientoId? EstablecimientoId { get; }

        public EmpresaId? EmpresaId { get; }

        public ObtenerTotalPorTipoComprobanteInputDto(
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            string tipoComprobante,
            DateOnly? desde = null,
            DateOnly? hasta = null,
            EstablecimientoId? establecimientoId = null,
            EmpresaId? empresaId = null)
        {
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            if (string.IsNullOrWhiteSpace(tipoComprobante))
                throw new ArgumentNullException(nameof(tipoComprobante));
            if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
                throw new ArgumentException("El rango de fechas es inválido: Desde no puede ser mayor que Hasta.");

            TipoComprobante = tipoComprobante;
            Desde = desde;
            Hasta = hasta;
            EstablecimientoId = establecimientoId;
            EmpresaId = empresaId;
        }
    }
}
