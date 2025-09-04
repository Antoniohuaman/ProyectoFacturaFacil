using SharedKernel.ValueObjects;
using ConfiguracionSistemaBC.Domain.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases.DTOs
{
    /// <summary>Resultado del cambio de ambiente.</summary>
    public sealed record CambiarAmbienteEmpresaOutputDto(
        string EmpresaId,
        Ruc Ruc,
        AmbienteFe AmbienteAnterior,
        AmbienteFe AmbienteActual,
        int Version
    );
}
