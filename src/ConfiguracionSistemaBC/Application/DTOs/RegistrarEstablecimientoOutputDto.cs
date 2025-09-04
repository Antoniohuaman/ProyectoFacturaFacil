using System;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Application.UseCases.DTOs
{
    /// <summary>Resultado del registro de establecimiento.</summary>
    public sealed record RegistrarEstablecimientoOutputDto(
        Guid Id,
        string EmpresaId,
        string Codigo,
        string Nombre,
        DomicilioFiscal Direccion,
        bool Habilitado
    );
}
