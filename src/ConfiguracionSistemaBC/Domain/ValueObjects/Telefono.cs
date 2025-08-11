using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para teléfonos de contacto de la empresa (0 a 3 números).
    /// - Entrada en un solo campo; admite separar números con "/", "|", ";", ",",
    ///   con " - " (guion rodeado de espacios) o con múltiples espacios.
    /// - Cada número puede venir con espacios, guiones o paréntesis; se normaliza
    ///   a una forma canónica (E.164-like): opcional "+" al inicio y dígitos.
    /// - Longitud válida: 6–15 dígitos (o 8–15 si empieza con "+").
    /// - Se eliminan duplicados (por forma canónica). Igualdad ignora el orden.
    /// - Este VO solo modela el valor; la visibilidad en PDF o etiquetas
    ///   ("Celular", "WhatsApp") se manejan en la capa de UI/aplicación.
    /// </summary>
    [DebuggerDisplay("{UnirParaMostrar()}")]
    public sealed class Telefono
    {
        public const int MaxNumeros = 3;

        /// <summary>Representa un número individual.</summary>
        public sealed class Numero
        {
            /// <summary>Forma canónica: "+" opcional y solo dígitos.</summary>
            public string Canonico { get; }

            /// <summary>Texto para mostrar (limpio pero manteniendo formato legible).</summary>
            public string Mostrar { get; }

            internal Numero(string canonico, string mostrar)
            {
                Canonico = canonico;
                Mostrar = mostrar;
            }

            public override string ToString() => Mostrar;
        }

        /// <summary>Lista inmutable de números (0 a 3).</summary>
        public IReadOnlyList<Numero> Numeros { get; }

        public bool EsVacio => Numeros.Count == 0;

        private Telefono(IReadOnlyList<Numero> numeros)
        {
            Numeros = numeros;
        }

        public static Telefono Vacio { get; } = new(Array.Empty<Numero>());

        // ----------------------------- Fábricas -----------------------------

        /// <summary>
        /// Crea desde un texto de entrada (campo único). Si es nulo o en blanco, retorna <see cref="Vacio"/>.
        /// </summary>
        public static Telefono FromTexto(string? entrada)
        {
            if (string.IsNullOrWhiteSpace(entrada)) return Vacio;

            var partes = PartirEntrada(entrada);
            var numeros = new List<Numero>(partes.Count);
            var vistos = new HashSet<string>(StringComparer.Ordinal);

            foreach (var p in partes)
            {
                var (canonico, mostrar) = NormalizarYValidar(p);

                // deduplicar por forma canónica
                if (!vistos.Add(canonico)) continue;

                numeros.Add(new Numero(canonico, mostrar));

                if (numeros.Count > MaxNumeros)
                    throw new ArgumentOutOfRangeException(nameof(entrada),
                        $"Solo se permiten hasta {MaxNumeros} teléfonos.");
            }

            return numeros.Count == 0 ? Vacio : new Telefono(numeros);
        }

        /// <summary>
        /// Crea desde una lista de textos (cada item es un número). Útil para formularios por campos separados.
        /// </summary>
        public static Telefono FromLista(IEnumerable<string?> entradas)
        {
            if (entradas is null) throw new ArgumentNullException(nameof(entradas));

            var numeros = new List<Numero>(MaxNumeros);
            var vistos = new HashSet<string>(StringComparer.Ordinal);

            foreach (var e in entradas)
            {
                if (string.IsNullOrWhiteSpace(e)) continue;

                var (canonico, mostrar) = NormalizarYValidar(e);

                if (!vistos.Add(canonico)) continue;

                numeros.Add(new Numero(canonico, mostrar));

                if (numeros.Count > MaxNumeros)
                    throw new ArgumentOutOfRangeException(nameof(entradas),
                        $"Solo se permiten hasta {MaxNumeros} teléfonos.");
            }

            return numeros.Count == 0 ? Vacio : new Telefono(numeros);
        }

        /// <summary>
        /// Intenta crear desde texto; devuelve false si algún número es inválido
        /// o si se excede el máximo permitido.
        /// </summary>
        public static bool TryFromTexto(string? entrada, out Telefono? telefonos)
        {
            telefonos = null;
            try
            {
                telefonos = FromTexto(entrada);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ----------------------------- Helpers -----------------------------

        /// <summary>Devuelve una cadena unida para mostrar, p. ej., "999 888 777 / (01) 234 5678".</summary>
        public string UnirParaMostrar(string separador = " / ")
            => Numeros.Count == 0 ? string.Empty : string.Join(separador, Numeros.Select(n => n.Mostrar));

        private static List<string> PartirEntrada(string entrada)
        {
            // Normalizar separadores:
            // - " - " (guion rodeado de espacios) => separador
            // - dos o más espacios => separador
            var s = entrada.Replace("\r\n", " ").Trim();
            s = Regex.Replace(s, @"\s-\s", "/");
            s = Regex.Replace(s, @"\s{2,}", "/");

            var partes = s.Split(new[] { '/', '|', ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(x => x.Trim())
                          .Where(x => x.Length > 0)
                          .ToList();

            return partes;
        }

        private static (string canonico, string mostrar) NormalizarYValidar(string texto)
        {
            // Limpiar para mostrar: colapsar espacios internos
            var mostrar = Regex.Replace(texto.Trim(), @"\s{2,}", " ");

            // Construir forma canónica:
            // - permitir '+' solo al inicio
            // - eliminar espacios, guiones, puntos, paréntesis
            // - resto deben ser dígitos
            var tmp = mostrar.Replace(" ", string.Empty)
                             .Replace("-", string.Empty)
                             .Replace(".", string.Empty)
                             .Replace("(", string.Empty)
                             .Replace(")", string.Empty);

            string canonico;
            if (tmp.StartsWith("+"))
            {
                var digits = tmp.Substring(1);
                if (!TodosDigitos(digits))
                    throw new ArgumentOutOfRangeException(nameof(texto), "El teléfono contiene caracteres no válidos.");
                if (digits.Length < 8 || digits.Length > 15)
                    throw new ArgumentOutOfRangeException(nameof(texto), "El teléfono internacional debe tener entre 8 y 15 dígitos.");
                canonico = "+" + digits;
            }
            else
            {
                if (!TodosDigitos(tmp))
                    throw new ArgumentOutOfRangeException(nameof(texto), "El teléfono contiene caracteres no válidos.");
                if (tmp.Length < 6 || tmp.Length > 15)
                    throw new ArgumentOutOfRangeException(nameof(texto), "El teléfono debe tener entre 6 y 15 dígitos.");
                canonico = tmp;
            }

            return (canonico, mostrar);
        }

        private static bool TodosDigitos(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        // ----------------------------- Igualdad por valor -----------------------------

        public override bool Equals(object? obj)
        {
            if (obj is not Telefono other) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Numeros.Count != other.Numeros.Count) return false;

            var mine = Numeros.Select(n => n.Canonico).OrderBy(x => x, StringComparer.Ordinal);
            var theirs = other.Numeros.Select(n => n.Canonico).OrderBy(x => x, StringComparer.Ordinal);
            return mine.SequenceEqual(theirs, StringComparer.Ordinal);
        }

        public override int GetHashCode()
        {
            // Hash basado en los canónicos ordenados (ignora el orden de entrada)
            unchecked
            {
                int hash = 17;
                foreach (var c in Numeros.Select(n => n.Canonico).OrderBy(x => x, StringComparer.Ordinal))
                    hash = hash * 31 + c.GetHashCode(StringComparison.Ordinal);
                return hash;
            }
        }

        public static bool operator ==(Telefono? left, Telefono? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(Telefono? left, Telefono? right) => !(left == right);

        public override string ToString() => UnirParaMostrar();
    }
}