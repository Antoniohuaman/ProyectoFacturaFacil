using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    public sealed class Sku : IEquatable<Sku>
    {
        public string Value { get; }               // lo que tecleó el usuario (se preserva)
        public string Canonical => Value.ToUpperInvariant(); // para comparar/indexar

        private Sku(string value) => Value = value;

        public static Sku Create(string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            var raw = input.Trim();
            if (raw.Length == 0) throw new ArgumentException("El SKU no puede estar vacío.", nameof(input));
            if (raw.Length > 30) throw new ArgumentException("El SKU no debe exceder 30 caracteres.", nameof(input));

            // Validamos con la versión en upper, pero SIN cambiar Value
            var upper = raw.ToUpperInvariant();
            if (!Regex.IsMatch(upper, @"^[A-Z0-9][A-Z0-9._-]*$"))
                throw new ArgumentException("Solo A-Z, 0-9, punto (.), guion (-) y guion bajo (_), sin espacios.", nameof(input));
            if (upper.EndsWith('.') || upper.EndsWith('_') || upper.EndsWith('-'))
                throw new ArgumentException("No debe terminar en punto, guion o guion bajo.", nameof(input));

            return new Sku(raw);
        }

        public static bool TryCreate(string? input, out Sku? sku)
        {
            try { sku = Create(input!); return true; }
            catch { sku = null; return false; }
        }

        // Igualdad por valor ignorando mayúsculas/minúsculas
        public bool Equals(Sku? other) => other is not null && Canonical == other.Canonical;
        public override bool Equals(object? obj) => obj is Sku s && Equals(s);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Canonical);

        public override string ToString() => Value;

        public static bool LooksLikeSkuToken(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.Trim().ToUpperInvariant();
            if (t.Length is < 1 or > 30) return false;
            return Regex.IsMatch(t, @"^[A-Z0-9][A-Z0-9._-]*$");
        }
    }
}
