using System;

namespace IndicadoresNegocioBC.Application.Contracts.Events.Inbound
{
    /// <summary>
    /// Evento de integración entrante: anulación de un comprobante previamente emitido.
    /// </summary>
    public sealed record ComprobanteAnulado(
        Guid ComprobanteId,
        DateTimeOffset FechaAnulacionUtc,
        Guid EmpresaId,
        Guid? EstablecimientoId,
        string Moneda                  // para reconstruir el segmento
    );
}