using System.Collections.Generic;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases.DTOs
{
    /// <summary>
    /// Campos opcionales: solo se aplica lo que venga informado.
    /// </summary>
    public sealed record ActualizarConfiguracionEmpresaInputDto(
        // Identificador actual del agregado
        string Ruc,

        // ---- Datos legales (opcionales) ----
        string? NuevoRuc = null,
        string? NuevoRazonSocial = null,
        DomicilioFiscal? NuevaDireccionFiscal = null,
        string? NuevoNombreComercial = null,

        // ---- Preferencias (opcionales) ----
        Moneda? NuevaMonedaBase = null,
        Telefono? NuevoTelefono = null,
        List<Email>? NuevosEmails = null,
        PieDePagina? NuevoPieDePagina = null,
        bool? MostrarImagenEnComprobanteImpresa = null,

        // Logo: se controla explícitamente si se quiere reemplazar (para permitir limpiar con null)
        bool ReemplazarLogo = false,
        LogoImagen? NuevoLogo = null
    );
}
