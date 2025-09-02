using System;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio: se actualizan los datos legales o configuración principal de la empresa.
    /// </summary>
    public sealed record ConfiguracionEmpresaActualizada(
    EmpresaId EmpresaId,
        Ruc Ruc,
        string RazonSocial,
    DomicilioFiscal DireccionFiscal,
        string? NombreComercial,
        Moneda MonedaBase,
        AmbienteFe Ambiente,
        DateTime OccurredOn
    ) : IDomainEvent;
}
