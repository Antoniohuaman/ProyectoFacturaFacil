using System;
using System.Collections.Generic;
using System.Linq;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Centro de costo de un producto o servicio.
    /// <para>Campo adjunto: opcional; si se proporciona, no puede estar vacío.</para>
    /// </summary>
    public sealed class CentroCosto : IEquatable<CentroCosto>
    {
        /// <summary>
        /// Código o nombre del centro de costo, normalizado (trim + uppercase).
        /// </summary>
        public string Valor { get; }

        /// <summary>
        /// Crea un nuevo <see cref="CentroCosto"/>.
        /// </summary>
        /// <param name="valor">
        /// Código o nombre; no puede ser nulo, vacío ni exceder 100 caracteres.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="valor"/> es nulo, vacío o su longitud tras trim excede 100 caracteres.
        /// </exception>
        public CentroCosto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException(
                    "El centro de costo es obligatorio cuando se proporciona.",
                    nameof(valor));

            var trimmed = valor.Trim();
            if (trimmed.Length > 100)
                throw new ArgumentException(
                    "El centro de costo no puede exceder 100 caracteres.",
                    nameof(valor));

            Valor = trimmed.ToUpperInvariant();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as CentroCosto);

        /// <inheritdoc/>
        public bool Equals(CentroCosto? other) =>
            other is not null && Valor == other.Valor;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Valor.GetHashCode(StringComparison.InvariantCulture);

        /// <inheritdoc/>
        public override string ToString() => Valor;
    }
}
