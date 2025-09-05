using System;

namespace ConfiguracionSistemaBC.Application.UseCases
{
    /// <summary>
    /// Resumen de la empresa creada + datos útiles para la UI.
    /// </summary>
    public sealed class RegistrarConfiguracionEmpresaOutputDto
    {

    /// <summary>Identificador único de la empresa (string UUID).</summary>
    public string EmpresaId { get; init; } = string.Empty;
    /// <summary>RUC de la empresa registrada.</summary>
    public string Ruc { get; init; } = string.Empty;
    /// <summary>Razón social legal.</summary>
    public string RazonSocial { get; init; } = string.Empty;
    /// <summary>Nombre comercial (opcional).</summary>
    public string? NombreComercial { get; init; }

    /// <summary>Ambiente actual de emisión electrónica (PRUEBA o PRODUCCION).</summary>
    public string Ambiente { get; init; } = "PRUEBA";
    /// <summary>Código ISO 4217 de la moneda base (ej: PEN, USD).</summary>
    public string MonedaBaseCodigo { get; init; } = "PEN";

    /// <summary>Dirección fiscal principal.</summary>
    public DireccionFiscalOut DireccionFiscal { get; init; } = new DireccionFiscalOut();

    /// <summary>Establecimiento principal creado (puede ser null si no aplica).</summary>
    public EstablecimientoOut? EstablecimientoPrincipal { get; init; }

    /// <summary>Cantidad de formas de pago precargadas.</summary>
    public int FormasDePagoPreCreadas { get; init; }
    /// <summary>Cantidad de unidades de medida precargadas.</summary>
    public int UnidadesDeMedidaPreCreadas { get; init; }

        public sealed class DireccionFiscalOut
        {
            /// <summary>Código de país (por defecto PE).</summary>
            public string PaisCodigo { get; init; } = "PE";
            /// <summary>Código de ubigeo SUNAT (opcional).</summary>
            public string Ubigeo { get; init; } = string.Empty;
            /// <summary>Dirección fiscal (texto libre).</summary>
            public string Direccion { get; init; } = string.Empty;
        }

        public sealed class EstablecimientoOut
        {
            /// <summary>Identificador único del establecimiento (UUID).</summary>
            public Guid Id { get; init; }
            /// <summary>Código del establecimiento (ej: 01).</summary>
            public string Codigo { get; init; } = string.Empty;
            /// <summary>Nombre del establecimiento.</summary>
            public string Nombre { get; init; } = string.Empty;
            /// <summary>Dirección asociada al establecimiento.</summary>
            public string Direccion { get; init; } = string.Empty;
            /// <summary>Código de ubigeo SUNAT asociado al establecimiento.</summary>
            public string Ubigeo { get; init; } = string.Empty;
        }
    }
}
