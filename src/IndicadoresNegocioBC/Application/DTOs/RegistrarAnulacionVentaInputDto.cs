using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para registrar la anulación de una venta en un IndicadorNegocio.
    /// La clave natural del agregado es: Tipo + Periodo (alineado) + Segmento.
    /// EmpresaId es opcional si el scope multi-tenant lo aísla la infraestructura.
    /// </summary>
    public sealed class RegistrarAnulacionVentaInputDto
    {
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public Guid ComprobanteId { get; }
        public EmpresaId? EmpresaId { get; }

        public RegistrarAnulacionVentaInputDto(
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            Guid comprobanteId,
            EmpresaId? empresaId = null)
        {
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            if (comprobanteId == Guid.Empty) throw new ArgumentException("ComprobanteId vacío.", nameof(comprobanteId));
            ComprobanteId = comprobanteId;
            EmpresaId = empresaId;
        }
    }
}
