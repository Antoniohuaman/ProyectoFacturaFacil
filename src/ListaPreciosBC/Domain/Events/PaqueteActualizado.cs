using System;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Domain.Events
{
    /// <summary>
    /// Se emite cuando un paquete existente cambia su configuración
    /// (nombre, descripción, descuento o composición de productos).
    /// </summary>
    public sealed class PaqueteActualizado : DomainEvent
    {
        public EmpresaId EmpresaId { get; }
        public Guid PaqueteId { get; }
        public NombrePaquete Nombre { get; }
        public PorcentajeDescuentoPaquete Descuento { get; }

        public PaqueteActualizado(
            EmpresaId empresaId,
            Guid paqueteId,
            NombrePaquete nombre,
            PorcentajeDescuentoPaquete descuento,
            Guid? eventId = null,
            DateTime? occurredOnUtc = null)
            : base(eventId, occurredOnUtc)
        {
            EmpresaId = empresaId;
            PaqueteId = paqueteId;
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descuento = descuento ?? throw new ArgumentNullException(nameof(descuento));
        }
    }
}
