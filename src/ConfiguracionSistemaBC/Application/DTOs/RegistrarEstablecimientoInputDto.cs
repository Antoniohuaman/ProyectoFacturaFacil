using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases.DTOs
{
    /// <summary>Entrada para registrar un establecimiento.</summary>
    public sealed record RegistrarEstablecimientoInputDto(
        string Ruc,
        string Codigo,
        string Nombre,
        DomicilioFiscal Direccion,
        Telefono? Telefono,
        Email? Email
    );
}
