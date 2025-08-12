using System;
using System.Collections.Generic;

namespace IndicadoresNegocioBC.Application.Contracts.Inbound
{
    /// <summary>
    /// Evento entrante: comprobante emitido y ACEPTADO por el BC de Comprobantes.
    /// Todos los montos están en la misma moneda (ISO 4217) indicada en Moneda.
    /// </summary>
    public sealed record ComprobanteEmitidoAceptado(
        Guid ComprobanteId,
        DateTimeOffset FechaEmisionUtc,
        Guid EmpresaId,
        Guid? EstablecimientoId,
        string Moneda,                                     // "PEN", "USD", ...
        Guid? ClienteId,
        decimal Total,                                     // total del comprobante
        decimal Igv,                                       // IGV contenido en Total
        IReadOnlyList<ComprobanteEmitidoAceptadoItem> Items
    );

    /// <summary>Ítem del comprobante (mismos supuestos de moneda que el encabezado).</summary>
    public sealed record ComprobanteEmitidoAceptadoItem(
        string ProductoId,
        decimal Cantidad,
        decimal Subtotal                                   // en la misma moneda del evento
    );
}