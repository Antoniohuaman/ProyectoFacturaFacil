using System;
using System.Collections.Generic;

namespace IndicadoresNegocioBC.Application.Contracts.Events.Outbound
{
    /// <summary>
    /// Evento de integración saliente: avisa que los indicadores fueron actualizados
    /// para un periodo/segmento/tipo específico. Incluye campos opcionales de resumen
    /// para evitar una consulta inmediata del consumidor.
    /// </summary>
    public sealed record IndicadoresActualizados(
        Guid EventId,                       // id del evento para idempotencia en el bus
        string Version,                     // e.g. "v1"
        string TipoIndicador,               // "VENTA_DIARIA" | "RANKING_PRODUCTOS" | ...
        Guid EmpresaId,
        Guid? EstablecimientoId,
        string Moneda,                      // ISO 4217
        DateOnly PeriodoInicio,             // yyyy-MM-dd
        DateOnly PeriodoFinInclusive,       // yyyy-MM-dd
        DateTimeOffset FechaEventoUtc,      // marca de tiempo de publicación

        // ---- Resumen opcional (según tipo de indicador) ----
        decimal? TotalVentas = null,
        decimal? TotalIgv = null,
        int? NroComprobantes = null,
        decimal? TicketPromedio = null
    )
    {
        /// <summary>Datos adicionales no estructurados (claves conocidas por los consumidores).</summary>
        public IDictionary<string, string>? Extras { get; init; }
    }
}
