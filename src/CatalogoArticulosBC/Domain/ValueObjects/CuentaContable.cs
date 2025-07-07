// src/CatalogoArticulosBC/Domain/ValueObjects/CuentaContable.cs
using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record CuentaContable
    {
        public string Value { get; }

        public CuentaContable(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}