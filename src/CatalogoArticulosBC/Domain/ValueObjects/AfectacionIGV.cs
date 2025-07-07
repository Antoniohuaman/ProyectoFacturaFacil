// src/CatalogoArticulosBC/Domain/ValueObjects/AfectacionIGV.cs
using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record AfectacionIGV
    {
        public string Value { get; }

        public AfectacionIGV(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}