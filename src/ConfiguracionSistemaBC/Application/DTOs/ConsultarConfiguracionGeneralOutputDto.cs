using System;
using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Snapshot de la configuración general de la empresa para la UI.
    /// </summary>
    public record ConsultarConfiguracionGeneralOutputDto
    {
        // --- Identidad / legales ---
        public string EmpresaId { get; init; } = string.Empty;
        public string Ruc { get; init; } = string.Empty;
        public string RazonSocial { get; init; } = string.Empty;
        public string? NombreComercial { get; init; }

        // --- Parámetros base / ambiente ---
        public string Ambiente { get; init; } = "PRUEBA";
        public string MonedaBaseCodigo { get; init; } = "PEN";

        // --- Dirección fiscal ---
        public DireccionFiscalOut DireccionFiscal { get; init; } = new();

        // --- Preferencias ---
        public string? Telefono { get; init; }
        public IReadOnlyList<string> Emails { get; init; } = Array.Empty<string>();
        public string? PieDePagina { get; init; }
        public bool MostrarImagenEnComprobanteImpresa { get; init; }
        public bool TieneLogo { get; init; }

        // --- Establecimientos / Catálogos ---
        public IReadOnlyList<EstablecimientoOut> Establecimientos { get; init; } = Array.Empty<EstablecimientoOut>();
        public Guid? EstablecimientoPrincipalId { get; init; }

        public IReadOnlyList<FormaPagoOut> FormasDePago { get; init; } = Array.Empty<FormaPagoOut>();
        public Guid? FormaPagoDefaultId { get; init; }

        public IReadOnlyList<UnidadMedidaOut> UnidadesDeMedida { get; init; } = Array.Empty<UnidadMedidaOut>();
        public Guid? UnidadDeMedidaDefaultId { get; init; }

        // ---- Types ----
        public sealed class DireccionFiscalOut
        {
            public string PaisCodigo { get; init; } = "PE";
            public string Ubigeo { get; init; } = string.Empty;
            public string Direccion { get; init; } = string.Empty;
        }

        public sealed class EstablecimientoOut
        {
            public Guid Id { get; init; }
            public string Codigo { get; init; } = string.Empty;
            public string Nombre { get; init; } = string.Empty;
            public string Direccion { get; init; } = string.Empty;
            public string Ubigeo { get; init; } = string.Empty;
            public bool Habilitado { get; init; }
            public bool EsPrincipal { get; init; }
        }

        public sealed class FormaPagoOut
        {
            public Guid Id { get; init; }
            public string PaymentMeansCode { get; init; } = string.Empty; // "10" / "20"
            public string? MetodoCodigo { get; init; } // para contado
            public string Nombre { get; init; } = string.Empty;
            public bool Visible { get; init; }
            public bool EsPorDefecto { get; init; }
            public bool EsSistema { get; init; }
            public int Orden { get; init; }
        }

        public sealed class UnidadMedidaOut
        {
            public Guid Id { get; init; }
            public string Codigo { get; init; } = string.Empty; // "NIU", "KGM", etc.
            public string Nombre { get; init; } = string.Empty;
            public bool Visible { get; init; }
            public bool EsPorDefecto { get; init; }
            public bool EsSistema { get; init; }
            public int Orden { get; init; }
        }
    }
}
