using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para obtener las ventas diarias de un IndicadorNegocio.
    /// Clave natural: Tipo + Periodo (alineado) + Segmento.
    /// Rango: [Desde..Hasta] (inclusive) dentro del Periodo.
    /// EmpresaId es opcional si el scope multi-tenant lo aísla la infraestructura.
    /// </summary>
    public sealed class ObtenerVentasDiariasInputDto
    {
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public DateOnly Desde { get; }
        public DateOnly Hasta { get; }
        public EmpresaId? EmpresaId { get; }

        public ObtenerVentasDiariasInputDto(
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

            // Si deseas ser estricto a que el rango esté contenido en Periodo, habilita:
            // if (desde < periodo.Desde || hasta > periodo.Hasta)
            //     throw new ArgumentException("El rango debe estar contenido en el Periodo del indicador.");

            Desde = desde;
            Hasta = hasta;
            EmpresaId = empresaId;
        }
    }
}
