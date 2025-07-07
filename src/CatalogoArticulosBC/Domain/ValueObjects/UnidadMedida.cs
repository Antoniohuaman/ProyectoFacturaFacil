// src/CatalogoArticulosBC/Domain/ValueObjects/UnidadMedida.cs
using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record UnidadMedida
    {
        public string Value { get; }

        public UnidadMedida(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}