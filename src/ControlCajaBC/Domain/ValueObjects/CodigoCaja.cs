using System;

namespace ControlCajaBC.Domain.ValueObjects
{
    /// <summary>
    /// Identificador único de la caja.
    /// </summary>
    public sealed record CodigoCaja
    {
        public Guid Value { get; }

        public CodigoCaja(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("El código de caja no puede ser vacío.", nameof(value));

            Value = value;
        }

        public static CodigoCaja New() => new(Guid.NewGuid());
    }
}
