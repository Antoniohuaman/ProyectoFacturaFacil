using System.Text.RegularExpressions;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Razón social de persona jurídica (SUNAT).
    /// Reutilizable en múltiples BCs. No contiene identidad.
    /// </summary>
    public sealed record RazonSocial
    {
        public string Valor { get; init; }

        private RazonSocial(string valor) => Valor = valor;

        /// <summary>
        /// Crea una razón social validando y normalizando:
        /// - Trim
        /// - Colapsa espacios
        /// - Longitud máxima 200
        /// - Caracteres permitidos para denominaciones habituales
        /// </summary>
        public static RazonSocial Crear(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new BusinessRuleException("La razón social es obligatoria.");

            var v = Normalizar(valor);
            ValidarLargo(v, 200, "Razón social");
            ValidarCaracteres(v);
            return new RazonSocial(v);
        }

        public override string ToString() => Valor;

        // ----- Helpers -----

        private static string Normalizar(string s)
        {
            s = s.Trim();
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static void ValidarLargo(string s, int max, string campo)
        {
            if (s.Length > max)
                throw new BusinessRuleException($"{campo} excede el máximo de {max} caracteres.");
        }

        // Letras, marcas, dígitos, espacios, &, ., -, coma, apóstrofos, comillas,
        // paréntesis, slash y numeral. Ej: "R&G 3 Hermanos S.A.C.", "Cía. de Alimentos, S.A."
        private static readonly Regex Permitidos =
            new(@"^[\p{L}\p{M}\p{N}\p{Zs}&\.\-,'’""()/#]+$", RegexOptions.Compiled);

        private static void ValidarCaracteres(string s)
        {
            if (!Permitidos.IsMatch(s))
                throw new BusinessRuleException("Razón social contiene caracteres no permitidos.");
        }
    }
}
