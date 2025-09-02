using System;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio: se registra una nueva empresa/tenant en el sistema.
    /// </summary>
    public sealed record ConfiguracionEmpresaRegistrada(
        EmpresaId EmpresaId,
        Ruc Ruc,
        string RazonSocial,
    DomicilioFiscal DireccionFiscal,
        Moneda MonedaBase,
        DateTime OccurredOn
    ) : IDomainEvent;
}
