#nullable enable
using System;
using System.Collections.Generic;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Application.DTOs
{
    /// <summary>
    /// DTO de entrada para emitir un comprobante (Factura/Boleta, etc.).
    /// </summary>
    public sealed class EmitirComprobanteInputDto
    {
        public required string EmpresaId { get; init; }
        public required string EstablecimientoId { get; init; }

        /// <summary>Valores esperados: "FACTURA" o "BOLETA" (amplía según tus tipos).</summary>
        public required string TipoComprobante { get; init; }

        /// <summary>Si no se especifica, la política de numeración decide.</summary>
        public string? SeriePreferida { get; init; }

        /// <summary>Si es null, se usa la fecha local de hoy.</summary>
        public DateOnly? FechaEmision { get; init; }

        /// <summary>Código ISO-4217: "PEN", "USD", ...</summary>
        public required string MonedaCodigo { get; init; }

        /// <summary>Tasa en porcentaje (ej.: 18 =&gt; 18%).</summary>
        public required decimal TasaImpuestoPorcentaje { get; init; }

        public required ClienteDto Cliente { get; init; }
        public required List<ItemDto> Items { get; init; }

        public string? Observaciones { get; init; }

        // --------- Tipos anidados ---------

        public sealed class ClienteDto
        {
            public required TipoDocumento TipoDocumento { get; init; }
            public required string NumeroDocumento { get; init; }

            /// <summary>Para PJ. Para PN, usar Nombres/Apellidos.</summary>
            public string? RazonSocial { get; init; }
            public string? Nombres { get; init; }
            public string? Apellidos { get; init; }

            /// <summary>ISO-3166 alpha-2, ej. "PE".</summary>
            public required string PaisCodigoIso { get; init; }
            public string? DomicilioLinea { get; init; }
            public string? Ubigeo { get; init; }
            public string? Departamento { get; init; }
            public string? Provincia { get; init; }
            public string? Distrito { get; init; }
            public string? AddressTypeCode { get; init; }

            /// <summary>Correos separados por coma/espacio/; etc. (máx. 5).</summary>
            public string? Emails { get; init; }

            /// <summary>Teléfonos (campo único; admite "/", "|", ";", ",", múltiples espacios).</summary>
            public string? Telefonos { get; init; }
        }

        public sealed class ItemDto
        {
            public required string Sku { get; init; }
            public required string Descripcion { get; init; }
            public required string UnidadMedidaCodigo { get; init; }
            public required decimal Cantidad { get; init; }
            public required decimal PrecioUnitario { get; init; }

            /// <summary>Catálogo 07 (SUNAT): "10","20","21","30"–"36","40","17".</summary>
            public required string AfectacionCodigo { get; init; }
        }
    }
}
