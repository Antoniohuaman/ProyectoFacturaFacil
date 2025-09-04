using System.Collections.Generic;
using ConfiguracionSistemaBC.Domain.Aggregates;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases.DTOs
{
    /// <summary>
    /// Snapshot completo del agregado ConfiguracionEmpresa.
    /// Devuelve los ReadModels ya definidos en el dominio para evitar duplicar estructuras.
    /// </summary>
    public sealed record ConsultarConfiguracionGeneralOutputDto(
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
        ConfiguracionEmpresa.EstablecimientoRead? EstablecimientoPrincipal,
        List<ConfiguracionEmpresa.EstablecimientoRead> Establecimientos,
        List<ConfiguracionEmpresa.SerieRead> Series,
        List<ConfiguracionEmpresa.FormaPagoRead> FormasDePago,
        ConfiguracionEmpresa.FormaPagoRead? FormaDePagoPorDefecto,
        List<ConfiguracionEmpresa.UnidadMedidaRead> UnidadesDeMedida,
        ConfiguracionEmpresa.UnidadMedidaRead? UnidadDeMedidaPorDefecto,
        int Version
    );
}
