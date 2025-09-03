using System;

namespace ComprobantesElectronicosBC.Application.ReadModels
{
    /// <summary>
    /// Proyección ligera para listados de comprobantes.
    /// Útil para búsquedas/paginación sin cargar el agregado completo.
    /// </summary>
    public sealed record ComprobanteResumenDto
    {
        /// <summary>Id interno (Guid del agregado).</summary>
        public Guid ComprobanteId { get; init; }

        /// <summary>Código SUNAT (01=Factura, 03=Boleta).</summary>
        public string TipoCodigo { get; init; } = default!;

        /// <summary>Serie normalizada (1..4 alfanum).</summary>
        public string Serie { get; init; } = default!;

        /// <summary>Correlativo (1..99’999’999).</summary>
        public int Numero { get; init; }

        /// <summary>Identificador visible UBL: SERIE-00000000.</summary>
        public string SerieNumero { get; init; } = default!;

        /// <summary>Fecha de emisión (IssueDate).</summary>
        public DateOnly FechaEmision { get; init; }

        /// <summary>Nombre/Razón social del cliente.</summary>
        public string ClienteNombre { get; init; } = default!;

        /// <summary>Tipo de doc. cliente (Cat.06: 6=RUC, 1=DNI, etc.).</summary>
        public string ClienteDocTipo { get; init; } = default!;

        /// <summary>Número de doc. cliente.</summary>
        public string ClienteDocNumero { get; init; } = default!;

        /// <summary>Código ISO-4217 (PEN, USD, ...).</summary>
        public string Moneda { get; init; } = default!;

        /// <summary>Total del documento (ya redondeado a decimales de la moneda).</summary>
        public decimal ImporteTotal { get; init; }

        /// <summary>Estado de negocio visible (Borrador/Emitido/Anulado, etc.).</summary>
        public string Estado { get; init; } = default!;
    }
}
