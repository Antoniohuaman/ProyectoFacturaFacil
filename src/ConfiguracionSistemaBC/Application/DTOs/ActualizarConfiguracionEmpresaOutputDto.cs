using System.Collections.Generic;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases.DTOs
{
    /// <summary>Resultado luego de aplicar los cambios.</summary>
    public sealed record ActualizarConfiguracionEmpresaOutputDto(
        string EmpresaId,
        Ruc Ruc,
        string RazonSocial,
        string? NombreComercial,
        DomicilioFiscal DireccionFiscal,
        Moneda MonedaBase,
        AmbienteFe Ambiente,
        Telefono Telefono,
        List<Email> Emails,
        PieDePagina PieDePagina,
        LogoImagen? Logo,
        bool MostrarImagenEnComprobanteImpresa,
        int Version
    );
}
