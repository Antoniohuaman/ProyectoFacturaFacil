using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida de RegistrarVentaAceptada.
    /// Incluye resumen de totales del agregado tras la operación.
    /// </summary>
    public sealed class RegistrarVentaAceptadaOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public EstadoIndicador Estado { get; }
        public Dinero TotalVentas { get; }
        public int TotalComprobantes { get; }
        public int Version { get; }
        public bool FueIdempotente { get; }

        public RegistrarVentaAceptadaOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EstadoIndicador estado,
            Dinero totalVentas,
            int totalComprobantes,
            int version,
            bool fueIdempotente)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Estado = estado ?? throw new ArgumentNullException(nameof(estado));
            TotalVentas = totalVentas ?? throw new ArgumentNullException(nameof(totalVentas));
            TotalComprobantes = totalComprobantes;
            Version = version;
            FueIdempotente = fueIdempotente;
        }
    }
}
