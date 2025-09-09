using System;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida para el caso de uso CrearIndicadorNegocio.
    /// </summary>
    public sealed class CrearIndicadorNegocioOutputDto
    {
        public Guid IndicadorId { get; }
        public IndicadoresNegocioBC.Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public EstadoIndicador Estado { get; }
        public DateTimeOffset CreadoEn { get; }
        public int Version { get; }

        public CrearIndicadorNegocioOutputDto(
            Guid indicadorId,
            IndicadoresNegocioBC.Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EstadoIndicador estado,
            DateTimeOffset creadoEn,
            int version)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Estado = estado ?? throw new ArgumentNullException(nameof(estado));
            CreadoEn = creadoEn;
            Version = version;
        }
    }
}
