using System.Collections.Generic;
using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    public sealed class RegistrarConfiguracionEmpresaInputDto
    {
        // Requeridos
        public Ruc Ruc { get; init; } = null!;
        public string RazonSocial { get; init; } = string.Empty;
        public DomicilioFiscal DireccionFiscal { get; init; } = null!;

        // Opcional (por defecto PEN)
        public Moneda? MonedaBase { get; init; }

        // Preferencias iniciales (opcionales)
        public string? NombreComercial { get; init; }
        public Telefono? Telefono { get; init; }
        public List<Email>? Emails { get; init; }
        public PieDePagina? PieDePagina { get; init; }
        public LogoImagen? Logo { get; init; }
        public bool? MostrarImagenEnComprobanteImpresa { get; init; }

        // Defaults de catálogos (opcionales)
        public string? FormaDePagoPorDefectoNombre { get; init; }       // "Contado", "Efectivo", "Crédito 30 días"
        public string? UnidadDeMedidaPorDefectoCodigo { get; init; }    // "NIU", "KGM", "ZZ", ...

        // Visibilidad inicial (opcional)
        public IEnumerable<string>? FormasDePagoVisibles { get; init; }     // por Nombre
        public IEnumerable<string>? UnidadesDeMedidaVisibles { get; init; } // por Código
    }
}
