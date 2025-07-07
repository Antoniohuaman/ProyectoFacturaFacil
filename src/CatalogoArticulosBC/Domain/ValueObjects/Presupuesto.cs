// src/CatalogoArticulosBC/Domain/ValueObjects/Presupuesto.cs
using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record Presupuesto
    {
        public decimal Value { get; }

        public Presupuesto(decimal value)
        {
            if (value < 0) throw new ArgumentException("Presupuesto no puede ser negativo.", nameof(value));
            Value = value;
        }
    }
}