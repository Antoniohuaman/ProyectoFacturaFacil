using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Application.DTOs
{
    /// <summary>
    /// Entrada para guardar un comprobante en estado BORRADOR.
    /// Nota: en borrador NO se asignan Serie/Número.
    /// </summary>
    public sealed record GuardarBorradorInput(
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

        // Emisor
        string EmisorRuc, string EmisorRazonSocial,
        string EmisorUbigeo, string EmisorDireccion,
        string? EmisorDepartamento, string? EmisorProvincia, string? EmisorDistrito,

        // Cliente
        string ClienteDocTipo, string ClienteDocNumero, string ClienteNombre,
        string? ClienteUbigeo, string? ClienteDireccion,
        string? ClienteDepartamento, string? ClienteProvincia, string? ClienteDistrito,

        // Descuento global (opcional; uno u otro)
        decimal? DescuentoGlobalPorcentaje,
        decimal? DescuentoGlobalMonto,

        // Líneas (puede ser vacío en borrador)
        IReadOnlyList<GuardarBorradorLineaInput> Lineas
    );

    public sealed record GuardarBorradorLineaInput(
        string Nombre, string? Detalle,
        string UmCodigo, string? UmNombre,
        decimal Cantidad,
        decimal PrecioUnitario,
        bool PrecioIncluyeIgv,
        string AfectacionCode,               // "10","20","21","30"..,"40"
        decimal? IgvRate,                    // 0.10 / 0.18 si "10"
        decimal? DescuentoPorcentaje,
        decimal? DescuentoMonto
    );

    /// <summary>Salida de GuardarBorrador.</summary>
    public sealed record GuardarBorradorOutput(
        Guid ComprobanteId,
        string TipoCodigo,
        DateOnly FechaEmision,
        string Estado,                       // "DRAFT"
        decimal SubtotalBase,
        decimal DescuentoGlobal,
        decimal IgvTotal,
        decimal Total
    );
}
