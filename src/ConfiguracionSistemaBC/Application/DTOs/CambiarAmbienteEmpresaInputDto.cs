using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases.DTOs
{
    /// <summary>Entrada para cambio de ambiente.</summary>
    public sealed record CambiarAmbienteEmpresaInputDto(
        string Ruc,
        AmbienteFe Destino
    );
}
