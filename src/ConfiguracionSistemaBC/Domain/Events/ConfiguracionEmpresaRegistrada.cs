using System;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio: se registra una nueva empresa/tenant en el sistema.
    /// </summary>
    public sealed class ConfiguracionEmpresaRegistrada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public Ruc Ruc { get; }
        public string RazonSocial { get; }
        public DomicilioFiscal DireccionFiscal { get; }
        public Moneda MonedaBase { get; }

        public ConfiguracionEmpresaRegistrada(
            EmpresaId empresaId,
            Ruc ruc,
            string razonSocial,
            DomicilioFiscal direccionFiscal,
            Moneda monedaBase,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            Ruc = ruc;
            RazonSocial = razonSocial;
            DireccionFiscal = direccionFiscal;
            MonedaBase = monedaBase;
        }
    }
}
