using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Nombre visible y editable de una columna de la lista de precios.
    /// Es un Value Object inmutable con igualdad por valor (texto normalizado).
    ///
    /// Normalización:
    /// - Trim a ambos lados.
    /// - Colapso de espacios en blanco internos (uno solo).
    /// - Se rechazan caracteres de control.
    /// - Se respeta el case del usuario (igualdad case-insensitive).
    /// </summary>
    [DebuggerDisplay("{Valor}")]
    public sealed class NombreColumnaPrecio :
        IEquatable<NombreColumnaPrecio>, IComparable<NombreColumnaPrecio>
    {
        /// <summary>Longitud mínima permitida (tras normalizar).</summary>
        public const int MinLongitud = 1;

        /// <summary>Longitud máxima permitida (tras normalizar).</summary>
        public const int MaxLongitud = 30;

        /// <summary>
        /// Texto normalizado que se muestra al usuario (p.ej., "Precio mayorista").
        /// </summary>
        public string Valor { get; }

        private NombreColumnaPrecio(string valorNormalizado)
        {
            Valor = valorNormalizado;
        }

        /// <summary>
        /// Crea una instancia aplicando normalización y validaciones.
        /// </summary>
        public static NombreColumnaPrecio Crear(string texto)
        {
            if (texto is null) throw new ArgumentNullException(nameof(texto));

            var normalizado = Normalizar(texto);
            Validar(normalizado);

            return new NombreColumnaPrecio(normalizado);
        }

        /// <summary>
        /// Intenta crear una instancia sin lanzar excepciones.
        /// </summary>
        public static bool TryCrear(string? texto, out NombreColumnaPrecio? nombre)
        {
            nombre = null;
            if (texto is null) return false;

            try
            {
                var n = Normalizar(texto);
                Validar(n);
                nombre = new NombreColumnaPrecio(n);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Normaliza el texto: trim + colapso de espacios (incluye tabs y saltos),
        /// y reemplaza cualquier espacio en blanco por el espacio simple ' '.
        /// </summary>
        private static string Normalizar(string texto)
        {
            // Recorremos y:
            // - convertimos cualquier whitespace a ' '
            // - colapsamos runs de whitespace a un solo ' '
            // - hacemos trim final
            Span<char> buffer = stackalloc char[texto.Length];
            int len = 0;
            bool prevWhite = false;

            foreach (var ch in texto)
            {
                bool isWs = char.IsWhiteSpace(ch);
                if (isWs)
                {
                    if (!prevWhite)
                    {
                        buffer[len++] = ' ';
                        prevWhite = true;
                    }
                }
                else
                {
                    buffer[len++] = ch;
                    prevWhite = false;
                }
            }

            // Trim manual (puede haber espacio al inicio o fin por la lógica anterior)
            int start = 0;
            while (start < len && buffer[start] == ' ') start++;
            int end = len - 1;
            while (end >= start && buffer[end] == ' ') end--;

            var normalized = (start <= end) ? new string(buffer.Slice(start, end - start + 1)) : string.Empty;
            return normalized;
        }

        private static void Validar(string normalizado)
        {
            if (normalizado.Length < MinLongitud)
                throw new ArgumentOutOfRangeException(nameof(normalizado), $"El nombre no puede estar vacío.");

            if (normalizado.Length > MaxLongitud)
                throw new ArgumentOutOfRangeException(nameof(normalizado),
                    $"El nombre no puede superar {MaxLongitud} caracteres.");

            // No se permiten caracteres de control
            if (normalizado.Any(char.IsControl))
                throw new ArgumentOutOfRangeException(nameof(normalizado), "El nombre contiene caracteres no válidos.");

            // (Opcional) Evitar nombres sólo de puntuación (.,-_/...)
            // Si quisieras reforzar: exigir que haya al menos una letra o dígito.
            // if (!normalizado.Any(c => char.IsLetterOrDigit(c)))
            //     throw new ArgumentOutOfRangeException(nameof(normalizado), "El nombre debe contener letras o números.");
        }

        #region Igualdad y orden
        public bool Equals(NombreColumnaPrecio? other)
            => other is not null && string.Equals(Valor, other.Valor, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => Equals(obj as NombreColumnaPrecio);

        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(Valor);

        /// <summary>
        /// Orden alfabético case-insensitive (útil para listados).
        /// </summary>
        public int CompareTo(NombreColumnaPrecio? other)
            => other is null ? 1 : StringComparer.OrdinalIgnoreCase.Compare(Valor, other.Valor);
        #endregion

        public override string ToString() => Valor;

        // Conversión explícita opcional para comodidad (evita implícitas para mantener el tipo fuerte)
        public static explicit operator string(NombreColumnaPrecio n) => n.Valor;
    }
}