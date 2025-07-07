using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public sealed record SKU
    {
        public string Value { get; }

        public SKU(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("SKU no puede estar vacío.", nameof(value));
            Value = value;
        }
    }
}