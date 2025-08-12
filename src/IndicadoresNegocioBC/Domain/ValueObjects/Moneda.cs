using System;
using System.Text.RegularExpressions;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object de Moneda (ISO 4217) usado como filtro en Indicadores.
    /// - Inmutable, igualdad por valor.
    /// - No normaliza ni transforma entradas: exige que el código ya venga correcto.
    /// - Invariantes: exactamente 3 letras A–Z (ASCII).
    /// </summary>
    public sealed record Moneda
    {
        private static readonly Regex Iso4217Regex = new(@"^[A-Z]{3}$", RegexOptions.Compiled);

        /// <summary>Código ISO 4217 (ej.: "PEN", "USD").</summary>
        public string Codigo { get; }

        public Moneda(string codigo)
        {
            if (codigo is null)
                throw new ArgumentNullException(nameof(codigo));

            // Sin Trim ni ToUpper: asumimos que la capa de aplicación/Configuración ya entrega "PEN"/"USD".
            if (!Iso4217Regex.IsMatch(codigo))
                throw new ArgumentException("Código de moneda inválido (se esperan 3 letras A–Z, ISO 4217).", nameof(codigo));

            Codigo = codigo;
        }

        public static Moneda Crear(string codigo) => new(codigo);

        public override string ToString() => Codigo;
    }
}