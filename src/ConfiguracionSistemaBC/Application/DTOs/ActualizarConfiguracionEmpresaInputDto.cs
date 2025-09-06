using System.Collections.Generic;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Petición para actualizar configuración de la empresa.
    /// Solo se actualizan los campos provistos (parciales). El RUC no se cambia.
    /// </summary>
    public sealed class ActualizarConfiguracionEmpresaInputDto
    {
        /// <summary>Empresa destino (GUID-string). Opcional: si no se envía, se toma del ITenantContext.</summary>
        public string? EmpresaId { get; init; }

        // ---- Datos legales (opcionales; el RUC NO se cambia) ----
        public string? RazonSocial { get; init; }
        public string? NombreComercial { get; init; }
        public DireccionFiscalDto? DireccionFiscal { get; init; }

        // ---- Preferencias (opcionales) ----
        public string? Telefono { get; init; }
        public List<string>? Emails { get; init; }   // reemplaza la lista completa (puede vaciarla)
        public string? PieDePagina { get; init; }
        public bool? MostrarImagenEnComprobanteImpresa { get; init; }

        // ---- Moneda base (opcional) ----
        public string? MonedaCodigo { get; init; }   // p.ej. "PEN", "USD"

        // ---- Logo (opcional) ----
        public bool? EliminarLogo { get; init; }     // true: elimina el logo actual
        public byte[]? LogoBytes { get; init; }      // si viene, se podría mapear a LogoImagen (ver comentario en UseCase)
        public string? LogoFileName { get; init; }
        public string? LogoContentType { get; init; }

        public sealed class DireccionFiscalDto
        {
            public string PaisCodigo { get; init; } = "PE";       // fijo PE
            public string Ubigeo { get; init; } = string.Empty;    // SUNAT
            public string Direccion { get; init; } = string.Empty; // línea de dirección
            public string? Referencia { get; init; }
        }
    }
}
