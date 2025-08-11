using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Application.DTOs
{
    /// <summary>DTO de entrada para emitir un comprobante (Factura "01" o Boleta "03").</summary>
    public sealed record EmitirComprobanteInput(
        // Tipo y moneda
        string TipoCodigo,                   // "01" | "03"
        string MonedaCodigo,                 // "PEN" | "USD"

        // Fechas
        DateOnly FechaEmision,

        // Forma de pago
        string FormaPagoCodigo,              // "10" Contado | "20" Crédito
        string? MetodoPagoCodigo,            // si "10": "EFECTIVO", "YAPE", etc.
        string? MetodoPagoNombre,            // etiqueta opcional
        int? DiasCredito,                    // si "20": > 0

        // Identidad visible
        string Serie, int Numero,

        // Emisor
        string EmisorRuc, string EmisorRazonSocial,
        string EmisorUbigeo, string EmisorDireccion,
        string? EmisorDepartamento, string? EmisorProvincia, string? EmisorDistrito,

        // Cliente
        string ClienteDocTipo, string ClienteDocNumero, string ClienteNombre,
        string? ClienteUbigeo, string? ClienteDireccion,
        string? ClienteDepartamento, string? ClienteProvincia, string? ClienteDistrito,

        // Descuento global (uno u otro; ambos null => no aplica)
        decimal? DescuentoGlobalPorcentaje,  // 10m => 10%
        decimal? DescuentoGlobalMonto,       // >= 0

        // Líneas
        IReadOnlyList<EmitirComprobanteLineaInput> Lineas
    );

    /// <summary>DTO de una línea del comprobante.</summary>
    public sealed record EmitirComprobanteLineaInput(
        string Nombre, string? Detalle,
        string UmCodigo, string? UmNombre,
        decimal Cantidad,
        decimal PrecioUnitario,
        bool PrecioIncluyeIgv,
        string AfectacionCode,               // "10","20","21","30"..,"40"
        decimal? IgvRate,                    // 0.10m o 0.18m cuando AfectacionCode="10"
        decimal? DescuentoPorcentaje,        // 0..100 (opcional)
        decimal? DescuentoMonto              // >= 0 (opcional)
    );

    /// <summary>DTO de salida al emitir un comprobante.</summary>
    public sealed record EmitirComprobanteOutput(
        Guid ComprobanteId,
        string TipoCodigo,
        string Serie, int Numero,
        DateOnly FechaEmision,
        string Estado,            // "SENT" (Enviado)
        decimal SubtotalBase,
        decimal DescuentoGlobal,
        decimal IgvTotal,
        decimal Total
    );
}
