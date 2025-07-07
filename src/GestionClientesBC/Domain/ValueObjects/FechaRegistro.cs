using System;

namespace GestionClientesBC.Domain.ValueObjects
{
    public sealed record FechaRegistro
    {
        public DateTime Value { get; }

        public FechaRegistro(DateTime value)
        {
            Value = value;
        }

        public static FechaRegistro Now() => new(DateTime.UtcNow);
    }
}