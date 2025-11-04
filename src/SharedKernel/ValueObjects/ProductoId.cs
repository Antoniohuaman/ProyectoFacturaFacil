using System;

namespace SharedKernel.ValueObjects
{
    /// <summary>Identidad opaca de Producto entre bounded contexts.</summary>
    public readonly record struct ProductoId
    {
        // Backing value for the product identity
        public Guid Value { get; }

        // Single validating constructor
        public ProductoId(Guid value)
        {
            Value = Validate(value);
        }

        public static ProductoId New() => new ProductoId(Guid.NewGuid());

        private static Guid Validate(Guid value)
            => value == Guid.Empty
                ? throw new ArgumentException("ProductoId no puede ser Guid.Empty", nameof(value))
                : value;

        public static ProductoId From(Guid value) => new ProductoId(value);

        public static ProductoId FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ProductoId no puede ser nulo o vacío.", nameof(value));
            return Guid.TryParse(value, out var g)
                ? new ProductoId(g)
                : throw new ArgumentException("ProductoId inválido: formato GUID no válido.", nameof(value));
        }

        public static bool TryParse(string? value, out ProductoId id)
        {
            id = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (!Guid.TryParse(value, out var g) || g == Guid.Empty) return false;
            id = new ProductoId(g);
            return true;
        }

        // Conversión explícita desde Guid (evita asignaciones accidentales)
        public static explicit operator ProductoId(Guid value) => From(value);

        // Conversión implícita a Guid (persistencia/serialización)
        public static implicit operator Guid(ProductoId id) => id.Value;

        public override string ToString() => Value.ToString();
    }
}
