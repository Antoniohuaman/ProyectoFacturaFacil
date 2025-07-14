using System;
using GestionClientesBC.Domain.ValueObjects;

namespace GestionClientesBC.Domain.Entities
{
    /// <summary>
    /// Registro de transacción histórica o evento relevante.
    /// </summary>
    public class OperacionCliente
    {
        public Guid OperacionId { get; }
        public TipoOperacion TipoOperacion { get; }
        public MontoOperacion MontoOperacion { get; }
        public ReferenciaId ReferenciaId { get; }
        public DateTime FechaOperacion { get; }
        public bool EstaPendiente { get; set; }

        public OperacionCliente(Guid operacionId, TipoOperacion tipoOperacion, MontoOperacion montoOperacion, ReferenciaId referenciaId, DateTime fechaOperacion)
        {
            OperacionId = operacionId != Guid.Empty ? operacionId : throw new ArgumentException("El Id no puede ser vacío.", nameof(operacionId));
            TipoOperacion = tipoOperacion;
            MontoOperacion = montoOperacion ?? throw new ArgumentNullException(nameof(montoOperacion));
            ReferenciaId = referenciaId ?? throw new ArgumentNullException(nameof(referenciaId));
            FechaOperacion = fechaOperacion;
        }
    }
}