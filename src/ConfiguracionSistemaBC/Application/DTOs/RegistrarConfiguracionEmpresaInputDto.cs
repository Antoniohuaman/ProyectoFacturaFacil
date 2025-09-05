using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Datos necesarios para registrar la configuración inicial de una empresa.
    /// Incluye opciones para personalizar el establecimiento principal y preferencias básicas.
    /// </summary>
    public sealed class RegistrarConfiguracionEmpresaInputDto
    {
        // --- Identidad legal ---
    public string Ruc { get; init; } = string.Empty;            // debe ser ingresado por el usuario
    public string RazonSocial { get; init; } = string.Empty;
        public string? NombreComercial { get; init; }

        // --- Dirección fiscal (PE) ---
        public DireccionFiscalDto DireccionFiscal { get; init; } = new();

        // --- Parámetros base ---
        public string MonedaCodigo { get; init; } = "PEN";           // PEN por defecto
        public string? Ambiente { get; init; } = "PRUEBA";           // "PRUEBA" | "PRODUCCION"

        // --- Preferencias opcionales ---
        public string? Telefono { get; init; }
        public string[]? Emails { get; init; }
        public string? PieDePagina { get; init; }
        public byte[]? LogoBytes { get; init; }

        // --- Personalización del Establecimiento Principal (creado por bootstrap) ---
        public string EstablecimientoCodigo { get; init; } = "01";
        public string EstablecimientoNombre { get; init; } = "Establecimiento Principal";

        // --- Futuro (no aplicado aún en aggregate actual) ---
        public string? ModoPrecio { get; init; } = "INCLUYE_IGV"; // por defecto precios incluyen IGV

        public sealed class DireccionFiscalDto
        {
            public string PaisCodigo { get; init; } = "PE";           // fijo: Perú
            public string Ubigeo { get; init; } = string.Empty;        // debe ser ingresado/autocompletado
            public string Direccion { get; init; } = string.Empty;     // debe ser ingresado/autocompletado
            public string? Referencia { get; init; }
        }
    }
}