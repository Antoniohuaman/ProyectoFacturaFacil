// src/CatalogoArticulosBC/Domain/ValueObjects/CodigoSUNAT.cs
using System;

namespace CatalogoArticulosBC.Domain<ValueObjects>
{
    public sealed record CodigoSUNAT
    {
        public string Value { get; }

        public CodigoSUNAT(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}