using System;
using System.Text.RegularExpressions;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object de Moneda (ISO 4217).
    /// Igualdad por valor (código ISO de 3 letras).
    /// Invariantes:
    ///  - Código obligatorio, exactamente 3 letras A–Z.
    ///  - Se normaliza a mayúsculas (p. ej., "pen" -> "PEN").
    /// Reglas:
    ///  - No contiene símbolo ni tipo de cambio (eso pertenece a otras capas/BC).
    /// </summary>
    public sealed record Moneda
    {
        private static readonly Regex Iso4217Regex = new(@"^[A-Z]{3}$", RegexOptions.Compiled);

        /// <summary>Código ISO 4217 (ej. "PEN", "USD").</summary>
        public string Codigo { get; }

        /// <summary>
        /// Crea una Moneda validando el código ISO (3 letras).
        /// </summary>
        /// <param name="codigo">Código ISO 4217 (3 letras).</param>
        /// <exception cref="ArgumentException">Si el código es nulo, vacío o no cumple el formato.</exception>
        public Moneda(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código de moneda es obligatorio.", nameof(codigo));

            var normalizado = codigo.Trim().ToUpperInvariant();
            if (!Iso4217Regex.IsMatch(normalizado))
                throw new ArgumentException("Código de moneda inválido. Debe tener exactamente 3 letras (ISO 4217).", nameof(codigo));

            Codigo = normalizado;
        }

        /// <summary>Fábrica explícita (alias del constructor).</summary>
        public static Moneda Crear(string codigo) => new(codigo);

        public override string ToString() => Codigo;
    }
}