using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para obtener las ventas registradas en un rango [Desde..Hasta] (inclusive)
    /// de un IndicadorNegocio identificado por su clave natural: Tipo + Periodo (alineado) + Segmento.
    /// EmpresaId es opcional si el aislamiento multi-tenant lo maneja la infraestructura.
    /// </summary>
    public sealed class ObtenerVentasPorRangoInputDto
    {
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public DateOnly Desde { get; }
        public DateOnly Hasta { get; }
        public EmpresaId? EmpresaId { get; }

        public ObtenerVentasPorRangoInputDto(
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateOnly desde,
            DateOnly hasta,
            EmpresaId? empresaId = null)
        {
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));

            if (desde > hasta)
                throw new ArgumentException("El rango de fechas es inválido: Desde no puede ser mayor que Hasta.");

            // Si deseas forzar que el rango esté contenido dentro del Periodo, habilita:
            // if (desde < periodo.Desde || hasta > periodo.Hasta)
            //     throw new ArgumentException("El rango debe estar contenido en el Periodo del indicador.");

            Desde = desde;
            Hasta = hasta;
            EmpresaId = empresaId;
        }
    }
}
