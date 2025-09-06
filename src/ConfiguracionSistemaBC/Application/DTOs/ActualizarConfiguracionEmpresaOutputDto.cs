using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Snapshot de la configuración luego de la actualización.
    /// </summary>
    public sealed class ActualizarConfiguracionEmpresaOutputDto
    {
        public string EmpresaId { get; init; } = string.Empty;
        public string Ruc { get; init; } = string.Empty;
        public string RazonSocial { get; init; } = string.Empty;
        public string? NombreComercial { get; init; }
        public string MonedaBaseCodigo { get; init; } = "PEN";
        public string Ambiente { get; init; } = "PRUEBA";

        public string Telefono { get; init; } = string.Empty;
        public string[] Emails { get; init; } = Array.Empty<string>();
        public string PieDePagina { get; init; } = string.Empty;
        public bool MostrarImagenEnComprobanteImpresa { get; init; }

        public DireccionFiscalOut DireccionFiscal { get; init; } = new();
        public EstablecimientoOut? EstablecimientoPrincipal { get; init; }

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
        }
    }
}
