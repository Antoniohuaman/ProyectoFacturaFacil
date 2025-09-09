using System;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de entrada para obtener el Top de clientes.
    /// Clave natural: Tipo + Periodo (alineado) + Segmento.
    /// Puede consultarse:
    /// - Todo el periodo (si no pasas rango), o
    /// - Un rango [Desde..Hasta] (inclusive) dentro del Periodo.
    /// EmpresaId es opcional si el scope multi-tenant lo aísla la infraestructura.
    /// </summary>
    public sealed class ObtenerTopClientesInputDto
    {
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }

        /// <summary>Rango opcional (inclusive). Si se omite, se usa el Top global del agregado.</summary>
        public DateOnly? Desde { get; }
        public DateOnly? Hasta { get; }

        /// <summary>Límite opcional. Si se omite y no hay rango, el caso de uso aplica un valor por defecto.</summary>
        public LimiteTop? Limite { get; }

        public EmpresaId? EmpresaId { get; }

        public ObtenerTopClientesInputDto(
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateOnly? desde = null,
            DateOnly? hasta = null,
            LimiteTop? limite = null,
            EmpresaId? empresaId = null)
        {
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));

            if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
                throw new ArgumentException("El rango de fechas es inválido: Desde no puede ser mayor que Hasta.");

            Desde = desde;
            Hasta = hasta;
            Limite = limite;
            EmpresaId = empresaId;
        }
    }
}
