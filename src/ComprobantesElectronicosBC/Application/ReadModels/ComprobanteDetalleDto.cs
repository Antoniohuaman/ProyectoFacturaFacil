using System;
using System.Collections.Generic;

namespace ComprobantesElectronicosBC.Application.ReadModels
{
    /// <summary>
    /// Proyección completa para “ver detalle” de un comprobante.
    /// Puede ser el output de ConsultarComprobanteUseCase.
    /// </summary>
    public sealed record ComprobanteDetalleDto
    {
        // --------- Identidad y tipo ----------
        public Guid ComprobanteId { get; init; }
        public string TipoCodigo { get; init; } = default!; // "01"/"03"
        public string Serie { get; init; } = default!;
        public int Numero { get; init; }
        public string SerieNumero { get; init; } = default!; // SERIE-00000000

        // --------- Fechas ----------
        public DateOnly FechaEmision { get; init; }
        public TimeOnly HoraEmision { get; init; }
        public DateOnly FechaVencimiento { get; init; }

        // --------- Emisor (snapshot) ----------
        public string EmisorRuc { get; init; } = default!;
        public string EmisorRazonSocial { get; init; } = default!;
        public string? EmisorNombreComercial { get; init; }
        public string EmisorPaisCodigoIso { get; init; } = "PE"; // para UBL
        public string? EmisorDomicilioLinea { get; init; }
        public string? EmisorUbigeo { get; init; }
        public string? EmisorDepartamento { get; init; }
        public string? EmisorProvincia { get; init; }
        public string? EmisorDistrito { get; init; }
        public string? EmisorAddressTypeCode { get; init; }

        // --------- Cliente (snapshot) ----------
        public string ClienteDocTipo { get; init; } = default!; // Cat.06 (6,1,A,B, ...)
        public string ClienteDocNumero { get; init; } = default!;
        public string ClienteNombre { get; init; } = default!;
        public string ClientePaisCodigoIso { get; init; } = "PE";
        public string? ClienteDomicilioLinea { get; init; }
        public string? ClienteUbigeo { get; init; }
        public string? ClienteDepartamento { get; init; }
        public string? ClienteProvincia { get; init; }
        public string? ClienteDistrito { get; init; }

        // --------- Pago ----------
        /// <summary>"10" Contado / "20" Crédito.</summary>
        public string FormaDePagoCode { get; init; } = default!;
        /// <summary>Método para CONTADO (EFECTIVO/TRANSFERENCIA/...); null en CRÉDITO.</summary>
        public string? FormaDePagoMetodo { get; init; }
        public string? FormaDePagoEtiqueta { get; init; }
        public int? DiasDeCredito { get; init; } // si aplica

        // --------- Referencias opcionales ----------
        public string? NumeroGuiaRemision { get; init; }
        public string? NumeroOrdenCompra { get; init; }

        // --------- Moneda y totales ----------
        public string Moneda { get; init; } = default!; // ISO-4217
        public decimal SubtotalBaseImponible { get; init; }
        public decimal DescuentoGlobalMonto { get; init; }
        public string? DescuentoGlobalModo { get; init; } // Ninguno/Porcentaje/Monto (texto)
        public decimal IgvTotal { get; init; }
        public decimal Total { get; init; }

        // --------- Texto visible ----------
        public string? Observaciones { get; init; }

        // --------- Líneas ----------
        public IReadOnlyList<LineaDto> Lineas { get; init; } = Array.Empty<LineaDto>();

        // --------- Audit/estado ----------
        public string Estado { get; init; } = default!; // Borrador/Emitido/Anulado/etc.
        public string? UsuarioCodigo { get; init; }
        public string? UsuarioNombreCompleto { get; init; }
        public string? UsuarioRol { get; init; }

        // ======= DTO de línea =======
        public sealed record LineaDto
        {
            public int Item { get; init; }                    // 1..N
            public string? Sku { get; init; }                 // SellersItemIdentification
            public string DescripcionNombre { get; init; } = default!; // DescripcionProducto.Nombre
            public string? DescripcionDetalle { get; init; }  // DescripcionProducto.Detalle
            public decimal Cantidad { get; init; }            // Cantidad.Value
            public string? UnidadMedida { get; init; }        // si la manejas en tu línea
            public decimal PrecioUnitario { get; init; }      // en moneda del doc
            public bool PrecioIncluyeIgv { get; init; }
            public string AfectacionCodigo { get; init; } = default!;  // Cat.07 (10,20,21,30,..,17)
            public decimal TasaImpuesto { get; init; }        // fracción (0.18 => 0.18)
            public decimal BaseImponible { get; init; }
            public decimal DescuentoMonto { get; init; }
            public decimal IgvMonto { get; init; }
            public decimal TotalLinea { get; init; }
        }
    }
}
