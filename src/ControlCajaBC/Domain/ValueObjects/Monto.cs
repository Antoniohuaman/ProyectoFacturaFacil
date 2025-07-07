using System;

namespace ControlCajaBC.Domain.ValueObjects
{
    /// <summary>
    /// Representa un importe monetario.
    /// </summary>
    public sealed record Monto
    {
        public decimal Value { get; }

        public Monto(decimal value)
        {
            if (value < 0)
                throw new ArgumentException("El monto no puede ser negativo.", nameof(value));

            Value = value;
        }

        public static Monto Zero => new(0m);

        public Monto Add(Monto otro) => new(Value + otro.Value);
        public Monto Subtract(Monto otro)
        {
            var result = Value - otro.Value;
            if (result < 0) throw new InvalidOperationException("Resultado de monto negativo.");
            return new Monto(result);
        }
    }
}
