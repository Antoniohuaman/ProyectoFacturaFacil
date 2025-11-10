using System;
using SharedKernel.Events;
using ConfiguracionSistemaBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ConfiguracionSistemaBC.Domain.Events
{
    /// <summary>
    /// Evento de dominio: se actualizan los datos legales o configuración principal de la empresa.
    /// </summary>
    public sealed class ConfiguracionEmpresaActualizada : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public Ruc Ruc { get; }
        public string RazonSocial { get; }
        public DomicilioFiscal DireccionFiscal { get; }
        public string? NombreComercial { get; }
        public Moneda MonedaBase { get; }
        public AmbienteFe Ambiente { get; }

        public ConfiguracionEmpresaActualizada(
            EmpresaId empresaId,
            Ruc ruc,
            string razonSocial,
            DomicilioFiscal direccionFiscal,
            string? nombreComercial,
            Moneda monedaBase,
            AmbienteFe ambiente,
            DateTime? occurredOnUtc = null,
            Guid? eventId = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            Ruc = ruc;
            RazonSocial = razonSocial;
            DireccionFiscal = direccionFiscal;
            NombreComercial = nombreComercial;
            MonedaBase = monedaBase;
            Ambiente = ambiente;
        }
    }
}
