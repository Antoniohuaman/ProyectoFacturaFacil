#nullable enable
using System;

namespace ComprobantesElectronicosBC.Application.DTOs
{
    /// <summary>DTO de salida para la emisión del comprobante.</summary>
    public sealed class EmitirComprobanteOutputDto
    {
        public required Guid ComprobanteId { get; init; }
        public required string TipoComprobante { get; init; }
        public required string Serie { get; init; }
        public required int Numero { get; init; }
        public string Cdp => $"{Serie}-{Numero:D8}";

        public required DateOnly FechaEmision { get; init; }
        public required DateTimeOffset EmitidoEnUtc { get; init; }

        public required string Moneda { get; init; }

        public required decimal ImporteBaseGravada { get; init; }
        public required decimal ImporteBaseNoGravada { get; init; }
        public required decimal ImporteImpuesto { get; init; }

        /// <summary>Valor de venta total (sin impuesto), excluye líneas gratuitas.</summary>
        public required decimal TotalValorVenta { get; init; }

        /// <summary>Total a pagar = Valor de venta + Impuesto.</summary>
        public required decimal ImporteTotal { get; init; }

        public required string ClienteResumen { get; init; }
        public required string Estado { get; init; }
    }
}
