// src/CatalogoArticulosBC/Domain/ValueObjects/Peso.cs
using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record Peso
   {
        public decimal Value { get; }

        public Peso(decimal value)
        {
            if (value < 0) throw new ArgumentException("Peso no puede ser negativo.", nameof(value));
            Value = value;
        }
    }
}