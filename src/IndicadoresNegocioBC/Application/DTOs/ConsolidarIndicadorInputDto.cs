using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para consolidar un IndicadorNegocio.
    /// Clave natural: Tipo + Periodo (alineado) + Segmento.
    /// EmpresaId es opcional si el scope multi-tenant lo aísla la infraestructura.
    /// </summary>
    public sealed class ConsolidarIndicadorInputDto
    {
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public EmpresaId? EmpresaId { get; }

        /// <summary>Permite inyectar el instante de consolidación (útil en tests/replays). Si es null, se usa UtcNow.</summary>
        public DateTimeOffset? Ahora { get; }

        public ConsolidarIndicadorInputDto(
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            EmpresaId? empresaId = null,
            DateTimeOffset? ahora = null)
        {
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            EmpresaId = empresaId;
            Ahora = ahora;
        }
    }
}
