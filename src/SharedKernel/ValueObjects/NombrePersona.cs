using System;
using System.Text.RegularExpressions;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Nombre de persona natural (Nombres + Apellidos).
    /// Reutilizable en múltiples BCs. No contiene identidad.
    /// </summary>
    public sealed record NombrePersona
    {
        public string Nombres { get; init; }
        public string Apellidos { get; init; }

        private NombrePersona(string nombres, string apellidos)
        {
            Nombres = nombres;
            Apellidos = apellidos;
        }

        /// <summary>
        /// Crea un nombre de persona validando y normalizando.
        /// - Trim
        /// - Colapsa múltiples espacios
        /// - Valida caracteres permitidos
        /// - Valida longitudes
        /// </summary>
        public static NombrePersona Crear(string nombres, string apellidos)
        {
            if (string.IsNullOrWhiteSpace(nombres))
                throw new BusinessRuleException("Nombres es obligatorio.");
            if (string.IsNullOrWhiteSpace(apellidos))
                throw new BusinessRuleException("Apellidos es obligatorio.");

            var n = Normalizar(nombres);
            var a = Normalizar(apellidos);

            ValidarLargo(n, 100, "Nombres");
            ValidarLargo(a, 120, "Apellidos");
            ValidarCaracteres(n, "Nombres");
            ValidarCaracteres(a, "Apellidos");

            return new NombrePersona(n, a);
        }

        /// <summary>Nombre completo con un espacio entre nombres y apellidos.</summary>
        public string Completo => $"{Nombres} {Apellidos}";

        public override string ToString() => Completo;

        // ----- Helpers -----

        private static string Normalizar(string s)
        {
            s = s.Trim();
            // Colapsa múltiples espacios (incluye Unicode whitespace)
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static void ValidarLargo(string s, int max, string campo)
        {
            if (s.Length > max)
                throw new BusinessRuleException($"{campo} excede el máximo de {max} caracteres.");
        }

        // Permite letras unicode, marcas, espacios, punto, apóstrofe recto/curvo y guion.
        // Ejemplos válidos: "María José", "O'Connor", "O’Connor", "Pérez-López", "St. John"
        private static readonly Regex Permitidos =
            new(@"^[\p{L}\p{M}\p{Zs}\.'’\-]+$", RegexOptions.Compiled);

        private static void ValidarCaracteres(string s, string campo)
        {
            if (!Permitidos.IsMatch(s))
                throw new BusinessRuleException($"{campo} contiene caracteres no permitidos.");
        }
    }
}
