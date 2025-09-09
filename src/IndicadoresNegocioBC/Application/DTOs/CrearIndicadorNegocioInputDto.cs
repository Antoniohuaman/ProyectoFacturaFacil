using System;
using IndicadoresNegocioBC.Domain.Aggregates;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para crear un IndicadorNegocio.
    /// La clave natural es: Tipo + Periodo (alineado) + Segmento.
    /// EmpresaId es opcional si el scope multi-tenant se gestiona por infraestructura.
    /// </summary>
    public sealed class CrearIndicadorNegocioInputDto
    {
        public IndicadoresNegocioBC.Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public EmpresaId? EmpresaId { get; }
        /// <summary>Fecha/hora UTC a usar como CreadoEn para pruebas/replays. Si es null, se usa UtcNow.</summary>
        public DateTimeOffset? AhoraUtc { get; }

        public CrearIndicadorNegocioInputDto(
            IndicadoresNegocioBC.Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EmpresaId? empresaId = null,
            DateTimeOffset? ahoraUtc = null)
        {
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            EmpresaId = empresaId; // puede ser null si el repo aísla por contexto
            AhoraUtc = ahoraUtc;
        }
    }
}
