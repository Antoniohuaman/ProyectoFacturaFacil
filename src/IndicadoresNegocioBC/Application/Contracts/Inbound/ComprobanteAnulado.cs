using System;

namespace IndicadoresNegocioBC.Application.Contracts.Inbound
{
    /// <summary>
    /// Evento de integración entrante que indica que un comprobante fue anulado.
    /// Se usa para revertir su impacto en los indicadores.
    /// </summary>
    public sealed record ComprobanteAnulado(
        Guid ComprobanteId,
        DateTimeOffset FechaAnulacionUtc,
        Guid EmpresaId,
        Guid? EstablecimientoId,
        string Moneda,
        string? Motivo = null
    );
}