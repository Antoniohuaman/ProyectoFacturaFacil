using System;

namespace ComprobantesElectronicosBC.Application.UseCases.ConsultarComprobante
{
    /// <summary>
    /// DTO de salida de la consulta, con tipos simples y desacoplado de entidades/VOs.
    /// La implementación del mapper llena estos datos desde el agregado.
    /// </summary>
    public sealed record ConsultarComprobanteOutputDto
    {
        public Guid ComprobanteId { get; init; }

        /// <summary>"01" = Factura, "03" = Boleta.</summary>
        public string TipoComprobante { get; init; } = "";

        public string Serie { get; init; } = "";
        public int Numero { get; init; }

        /// <summary>Estado de negocio (ej.: Borrador, Emitido, Anulado).</summary>
        public string Estado { get; init; } = "";

        /// <summary>Fecha de emisión (día calendario).</summary>
        public DateOnly FechaEmision { get; init; }

        /// <summary>Total del documento.</summary>
        public decimal Total { get; init; }

        /// <summary>Moneda ISO-4217 (PEN, USD...).</summary>
        public string Moneda { get; init; } = "PEN";

        // Resumen de emisor y cliente para cabecera
        public string EmisorRuc { get; init; } = "";
        public string EmisorRazonSocial { get; init; } = "";
        /// <summary>Documento del cliente (texto de presentación, p.ej. "6-20123456789" o "1-12345678").</summary>
        public string ClienteDocumento { get; init; } = "";
        public string ClienteNombre { get; init; } = "";

        public bool EstaAnulado => string.Equals(Estado, "Anulado", StringComparison.OrdinalIgnoreCase);
        public string SerieNumero => $"{Serie}-{Numero:00000000}";
    }
}
