using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Código de serie de un producto (opcional).
    /// Normaliza el valor aplicando Trim y convierte cadenas vacías en null.
    /// </summary>
    public sealed class Serie : IEquatable<Serie>
    {
        /// <summary>
        /// Valor del código de serie, o null si no se proporcionó.
        /// </summary>
        public string? Valor { get; }

        /// <summary>
        /// Crea una nueva instancia de <see cref="Serie"/>.
        /// </summary>
        /// <param name="valor">
        /// Código de serie. Si es null o solo espacios, se considera no aplicable.
        /// De lo contrario, se almacena la versión recortada.
        /// </param>
        public Serie(string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                var trimmed = valor.Trim();
                Valor = trimmed.Length > 0 ? trimmed : null;
            }
            else
            {
                Valor = null;
            }
        }

        /// <inheritdoc/>
        public override string? ToString() => Valor;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as Serie);

        /// <inheritdoc/>
        public bool Equals(Serie? other) =>
            other is not null && string.Equals(Valor, other.Valor, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor is null ? 0 : Valor.GetHashCode(StringComparison.Ordinal);
    }
}
