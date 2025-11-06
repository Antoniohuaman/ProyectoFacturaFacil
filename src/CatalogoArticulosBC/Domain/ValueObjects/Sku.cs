#nullable enable
using System;
using System.Text.RegularExpressions;

namespace Dominio.CatalogoArticulosBC.ValueObjects
{
    /// <summary>
    /// SKU (Stock Keeping Unit) según la práctica de SUNAT en UBL 2.1:
    /// - Representa el código de producto propio del emisor (SellersItemIdentification → cbc:ID).
    /// - Formato normativo: alfanumérico hasta 30 caracteres (an..30).
    /// - Permitimos caracteres comunes en “an”: letras A–Z, dígitos 0–9, espacio, '-', '/', '.'.
    ///   (Ejemplo en guías: "Cap-258963"). Se normaliza en mayúsculas y sin espacios extremos.
    /// - NO es el "Código de producto SUNAT" (UNSPSC n..8, catálogo 25).
    /// </summary>
    [Obsolete("SKU no es identidad; usar ProductoId. SKU vive en Catálogo y en Application/ReadModel.")]
    public sealed class Sku : IEquatable<Sku>
    {
        /// <summary>Tamaño máximo normativo para SellersItemIdentification/cbc:ID.</summary>
        public const int MaxLength = 30;

        // Debe iniciar en alfanumérico; luego puede contener alfanumérico/espacio/-/./
        // y '/' hasta completar MaxLength. Se usa Ordinal para evitar reglas culturales.
        private static readonly Regex PatronPermitido =
            new(@"^[A-Z0-9][A-Z0-9 \-\/\.]{0,29}$", RegexOptions.Compiled);

        /// <summary>Valor normalizado (Trim → UpperInvariant → compactar espacios).</summary>
    public string Valor { get; }

    private Sku(string valor) => Valor = valor;

        /// <summary>Crea un SKU validando la norma SUNAT (lanza excepción si es inválido).</summary>
        public static Sku Crear(string? valor)
        {
            if (!TryCrear(valor, out var sku, out var error))
                throw new ArgumentException(error ?? "Sku inválido.", nameof(valor));
            return sku!;
        }

        /// <summary>Intenta crear un SKU; devuelve false y un mensaje de error si es inválido.</summary>
        public static bool TryCrear(string? valor, out Sku? sku, out string? error)
        {
            sku = null;
            error = null;

            if (string.IsNullOrWhiteSpace(valor))
            {
                error = "El Sku no puede estar vacío.";
                return false;
            }

            var normalizado = Normalizar(valor);

            if (normalizado.Length is < 1 or > MaxLength)
            {
                error = $"El Sku debe tener de 1 a {MaxLength} caracteres.";
                return false;
            }

            if (!PatronPermitido.IsMatch(normalizado))
            {
                error = "El Sku sólo puede contener A–Z, 0–9, espacio, '-', '/', '.' y debe iniciar con letra o dígito.";
                return false;
            }

            sku = new Sku(normalizado);
            return true;
        }

        /// <summary>Trim, mayúsculas invariables y compactar toda secuencia de espacio a un solo espacio.</summary>
        private static string Normalizar(string s)
        {
            var t = s.Trim().ToUpperInvariant();
            // compactar espacios internos (evita duplicados por espacios múltiples)
            t = Regex.Replace(t, @"\s+", " ");
            return t;
        }

        // ------------------- Igualdad / utilidades -------------------

        public override bool Equals(object? obj) => obj is Sku other && Equals(other);

        public bool Equals(Sku? other) =>
            other is not null &&
            string.Equals(Valor, other.Valor, StringComparison.Ordinal);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Valor);

        public override string ToString() => Valor;

    public static implicit operator string(Sku sku) => sku.Valor;
    }
}

// TODO: Eliminar esta clase cuando no existan referencias fuera de CatalogoArticulosBC ni de Application/ReadModel.
