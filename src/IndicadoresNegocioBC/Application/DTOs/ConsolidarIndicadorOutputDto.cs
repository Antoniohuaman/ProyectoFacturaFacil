using System;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida del caso de uso de consolidación.
    /// </summary>
    public sealed class ConsolidarIndicadorOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public EstadoIndicador Estado { get; }
        public DateTimeOffset? ConsolidadoEn { get; }
        public int Version { get; }
        public bool FueIdempotente { get; }

        public ConsolidarIndicadorOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EstadoIndicador estado,
            DateTimeOffset? consolidadoEn,
            int version,
            bool fueIdempotente)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Estado = estado ?? throw new ArgumentNullException(nameof(estado));
            ConsolidadoEn = consolidadoEn;
            Version = version;
            FueIdempotente = fueIdempotente;
        }
    }
}
