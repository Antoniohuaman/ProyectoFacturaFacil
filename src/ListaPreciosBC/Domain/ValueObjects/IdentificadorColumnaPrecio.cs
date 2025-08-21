using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Identidad estable de una columna de precio en la plantilla (P1..P10).
    /// Inmutable, con igualdad por valor (por número) y orden natural por número.
    /// </summary>
    [DebuggerDisplay("{Valor}")]
    public sealed class IdentificadorColumnaPrecio :
        IEquatable<IdentificadorColumnaPrecio>, IComparable<IdentificadorColumnaPrecio>
    {
        /// <summary>Número mínimo de columna permitido.</summary>
        public const byte Min = 1;
        /// <summary>Número máximo de columna permitido.</summary>
        public const byte Max = 10;

        // Acepta P1..P10 en mayúsculas/minúsculas, con espacios alrededor (se eliminan).
        private static readonly Regex _rgx =
            new(@"^P(10|[1-9])$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>Código normalizado 'Pn' (por ejemplo: "P1", "P10").</summary>
        public string Valor { get; }
        /// <summary>Número de columna (1..10).</summary>
        public byte Numero { get; }

        private IdentificadorColumnaPrecio(string valor, byte numero)
        {
            Valor  = valor;
            Numero = numero;
        }

        /// <summary>
        /// Crea un identificador a partir de un texto (admite " p5 ", "P5", "p10").
        /// Normaliza a mayúsculas sin espacios. Valida formato y rango.
        /// </summary>
        public static IdentificadorColumnaPrecio Crear(string codigo)
        {
            if (codigo is null) throw new ArgumentNullException(nameof(codigo));

            var raw = codigo.Trim();
            if (!_rgx.IsMatch(raw))
                throw new ArgumentOutOfRangeException(nameof(codigo), "Formato inválido. Use P1..P10.");

            var num = byte.Parse(raw.AsSpan(1)); // seguro por el regex
            return new IdentificadorColumnaPrecio($"P{num}", num);
        }

        /// <summary>
        /// Crea un identificador desde el número (1..10).
        /// </summary>
        public static IdentificadorColumnaPrecio DesdeNumero(byte numero)
        {
            if (numero < Min || numero > Max)
                throw new ArgumentOutOfRangeException(nameof(numero), $"El número debe estar entre {Min} y {Max}.");
            return new IdentificadorColumnaPrecio($"P{numero}", numero);
        }

        /// <summary>
        /// Intenta crear el identificador a partir del texto. No lanza excepciones.
        /// </summary>
        public static bool TryCrear(string? codigo, out IdentificadorColumnaPrecio? id)
        {
            id = null;
            if (codigo is null) return false;

            var raw = codigo.Trim();
            if (!_rgx.IsMatch(raw)) return false;

            var num = byte.Parse(raw.AsSpan(1));
            id = new IdentificadorColumnaPrecio($"P{num}", num);
            return true;
        }

        /// <summary>
        /// Intenta crear el identificador desde un número. No lanza excepciones.
        /// </summary>
        public static bool TryDesdeNumero(byte numero, out IdentificadorColumnaPrecio? id)
        {
            id = null;
            if (numero < Min || numero > Max) return false;
            id = new IdentificadorColumnaPrecio($"P{numero}", numero);
            return true;
        }

        /// <summary>Devuelve P1..P10 ya materializados (inmutables y compartidos).</summary>
        public static IReadOnlyList<IdentificadorColumnaPrecio> Todos { get; } =
            Enumerable.Range(Min, Max - Min + 1)
                      .Select(n => new IdentificadorColumnaPrecio($"P{n}", (byte)n))
                      .ToArray();

        #region Igualdad y orden
        public bool Equals(IdentificadorColumnaPrecio? other)
            => other is not null && Numero == other.Numero;

        public override bool Equals(object? obj) => Equals(obj as IdentificadorColumnaPrecio);

        public override int GetHashCode() => Numero.GetHashCode();

        public int CompareTo(IdentificadorColumnaPrecio? other)
            => other is null ? 1 : Numero.CompareTo(other.Numero);

        public static bool operator ==(IdentificadorColumnaPrecio? a, IdentificadorColumnaPrecio? b)
            => a is null ? b is null : a.Equals(b);

        public static bool operator !=(IdentificadorColumnaPrecio? a, IdentificadorColumnaPrecio? b)
            => !(a == b);
        #endregion

        public override string ToString() => Valor;
    }
}