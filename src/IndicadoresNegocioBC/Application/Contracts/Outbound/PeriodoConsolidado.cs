using System;

namespace IndicadoresNegocioBC.Application.Contracts.Events.Outbound
{
    /// <summary>
    /// Evento saliente que indica que un periodo quedó CONSOLIDADO
    /// (estado final, sin más mutaciones) para un tipo de indicador y segmento.
    /// </summary>
    public sealed record PeriodoConsolidado(
        Guid EventId,                       // id del evento para idempotencia
        string Version,                     // e.g. "v1"
        string TipoIndicador,               // "VENTA_DIARIA" | "RANKING_PRODUCTOS" | ...
        Guid EmpresaId,
        Guid? EstablecimientoId,
        string Moneda,                      // ISO 4217
        DateOnly PeriodoInicio,
        DateOnly PeriodoFinInclusive,
        DateTimeOffset FechaConsolidacionUtc,
        string? Motivo = null,              // opcional: p.ej., "cierre de mes", "job scheduler"
        long? SnapshotVersion = null        // opcional: versión de snapshot/proyección publicada
    );
}