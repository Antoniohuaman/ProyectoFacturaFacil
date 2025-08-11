using System;
using System.Diagnostics;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para el <b>Correlativo</b> de un comprobante.
    ///
    /// Normativa SUNAT relevante:
    /// - El <cbc:ID> del comprobante se forma como: <b>Serie</b> + "-" + <b>Correlativo de 8 dígitos</b>.
    ///   Ej.: F001-00000001
    ///
    /// Reglas de dominio:
    /// - El usuario define el correlativo inicial (típicamente 1, o la continuación si migra).
    /// - Rango permitido: [1..99,999,999] (8 dígitos máximo).
    /// - Una vez que existen comprobantes emitidos/aceptados, el correlativo evoluciona
    ///   automáticamente (Valor+1) y <b>no se debe alterar manualmente</b>.
    ///   (Esta restricción se hace cumplir en la entidad/aggregate, no en este VO).
    /// </summary>
    [DebuggerDisplay("{Valor} ({FormatoSunat8})")]
    public sealed class Correlativo
    {
        /// <summary>Valor mínimo permitido (1).</summary>
        public const int Min = 1;

        /// <summary>Valor máximo permitido (99,999,999) — cabe en 8 dígitos.</summary>
        public const int Max = 99_999_999;

        /// <summary>Cantidad de dígitos requerida por SUNAT para el correlativo en el ID.</summary>
        public const int DigitosSunat = 8;

        /// <summary>Valor numérico del correlativo (inmutable).</summary>
        public int Valor { get; }

        /// <summary>True si el correlativo alcanzó el valor máximo permitido.</summary>
        public bool EsMaximo => Valor == Max;

        /// <summary>
        /// Representación en <b>8 dígitos con ceros a la izquierda</b> (requerido para el ID SUNAT).
        /// Ej.: Valor=1 → "00000001".
        /// </summary>
        public string FormatoSunat8 => Valor.ToString("D" + DigitosSunat);

        private Correlativo(int valor)
        {
            if (valor < Min || valor > Max)
                throw new ArgumentOutOfRangeException(nameof(valor), $"El correlativo debe estar entre {Min} y {Max}.");
            Valor = valor;
        }

        // ---------------------------- Fábricas ----------------------------

        /// <summary>
        /// Crea un correlativo a partir de un entero válido [1..99,999,999].
        /// </summary>
        public static Correlativo From(int valor) => new(valor);

        /// <summary>
        /// Intenta crear un correlativo válido desde entero.
        /// </summary>
        public static bool TryFrom(int valor, out Correlativo? correlativo)
        {
            correlativo = null;
            if (valor < Min || valor > Max) return false;
            correlativo = new Correlativo(valor);
            return true;
        }

        /// <summary>
        /// Crea un correlativo a partir de texto numérico (admite ceros a la izquierda).
        /// Reglas:
        /// - Debe contener exclusivamente dígitos (0-9).
        /// - Entre 1 y 8 dígitos (SUNAT exige 8 para el ID; aquí normalizamos a entero).
        /// - El valor resultante debe estar en [1..99,999,999].
        /// Ej.: "00000001" → Valor=1; "9135" → Valor=9135.
        /// </summary>
        public static Correlativo FromString(string raw)
        {
            if (raw is null) throw new ArgumentNullException(nameof(raw));

            var s = raw.Trim();
            if (s.Length == 0)
                throw new ArgumentNullException(nameof(raw), "El correlativo no puede estar vacío.");

            if (!EsSoloDigitos(s))
                throw new ArgumentOutOfRangeException(nameof(raw), "El correlativo debe contener solo dígitos (0-9).");

            if (s.Length > DigitosSunat)
                throw new ArgumentOutOfRangeException(nameof(raw), $"El correlativo no puede exceder {DigitosSunat} dígitos.");

            // Permite "00000001" → 1; "00000000" → 0 (inválido).
            if (!int.TryParse(s, out var valor))
                throw new ArgumentOutOfRangeException(nameof(raw), "El correlativo no es un número válido.");

            if (valor < Min || valor > Max)
                throw new ArgumentOutOfRangeException(nameof(raw), $"El correlativo debe estar entre {Min} y {Max}.");

            return new Correlativo(valor);
        }

        /// <summary>
        /// Intenta crear desde texto numérico (1..8 dígitos). Devuelve false si no cumple.
        /// </summary>
        public static bool TryFromString(string? raw, out Correlativo? correlativo)
        {
            correlativo = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var s = raw.Trim();
            if (!EsSoloDigitos(s) || s.Length > DigitosSunat) return false;
            if (!int.TryParse(s, out var valor)) return false;
            if (valor < Min || valor > Max) return false;

            correlativo = new Correlativo(valor);
            return true;
        }

        // ---------------------------- Operaciones de dominio ----------------------------

        /// <summary>
        /// Devuelve el siguiente correlativo (Valor+1).
        /// Lanza excepción si ya se alcanzó el máximo permitido (overflow normativo).
        /// </summary>
        public Correlativo Siguiente()
        {
            if (Valor == Max)
                throw new InvalidOperationException($"No es posible incrementar: el correlativo ya está en {Max}.");

            return new Correlativo(Valor + 1);
        }

        // ---------------------------- Igualdad por valor ----------------------------
        public override bool Equals(object? obj)
            => obj is Correlativo other && Valor == other.Valor;

        public override int GetHashCode() => Valor.GetHashCode();

        public static bool operator ==(Correlativo? left, Correlativo? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(Correlativo? left, Correlativo? right)
            => !(left == right);

        public override string ToString() => Valor.ToString();

        /// <summary>Conversión implícita a int.</summary>
        public static implicit operator int(Correlativo value) => value.Valor;

        /// <summary>Conversión explícita desde int (valida rango).</summary>
        public static explicit operator Correlativo(int valor) => From(valor);

        /// <summary>Conversión explícita desde string (valida formato y rango).</summary>
        public static explicit operator Correlativo(string raw) => FromString(raw);

        // ---------------------------- Helpers ----------------------------
        private static bool EsSoloDigitos(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c < '0' || c > '9') return false;
            }
            return true;
        }
    }
}