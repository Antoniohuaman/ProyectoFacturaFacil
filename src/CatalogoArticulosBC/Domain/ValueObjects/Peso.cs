// src/CatalogoArticulosBC/Domain/ValueObjects/Peso.cs
using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record Peso
    {
        public decimal Valor { get; }

        public Peso(decimal valor)
        {
            if (valor < 0) throw new ArgumentException("Peso no puede ser negativo.", nameof(valor));
            Valor = valor;
        }
    }
}