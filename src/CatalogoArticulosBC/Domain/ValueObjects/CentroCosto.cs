// src/CatalogoArticulosBC/Domain/ValueObjects/CentroCosto.cs
using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record CentroCosto
    {
        public string Value { get; }

        public CentroCosto(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}