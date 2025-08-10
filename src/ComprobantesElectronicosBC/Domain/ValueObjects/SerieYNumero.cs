using System;
using System.Text.RegularExpressions;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para la identidad visible del comprobante:
    /// SERIE + CORRELATIVO (8 dígitos para UBL/SUNAT).
    ///
    /// - Serie: 1..4, [A-Z0-9], normalizada a MAYÚSCULAS. Ej.: F001, B001, E001, NC01, ND01.
    /// - Número: 1..99,999,999 (se mostrará con 8 dígitos: 00000001).
    ///
    /// NOTAS:
    /// - Este VO NO asigna correlativos; solo valida y formatea.
    /// - Para UBL, el <cbc:ID> es "SERIE-00000000".
    /// - EsCompatibleConTipo() permite una verificación rápida en UI (F↔01, B↔03).
    /// </summary>
    public sealed record SerieYNumero
    {
        /// <summary>Serie normalizada (MAYÚSCULAS).</summary>
        public string Serie { get; }

        /// <summary>Número entero (1..99'999'999).</summary>
        public int Numero { get; }

        private SerieYNumero(string serie, int numero)
        {
            Serie = serie;
            Numero = numero;
        }

        // ---------------- Fábricas ----------------

        /// <summary>
        /// Crea un VO validando normas mínimas: serie [A-Z0-9]{1,4} y número 1..99,999,999.
        /// </summary>
        public static SerieYNumero Create(string serie, int numero)
        {
            if (serie is null) throw new ArgumentNullException(nameof(serie));
            var s = serie.Trim().ToUpperInvariant();
            if (!Regex.IsMatch(s, "^[A-Z0-9]{1,4}$"))
                throw new ArgumentException("La serie debe ser 1..4 caracteres alfanuméricos A-Z/0-9.", nameof(serie));

            if (numero < 1 || numero > 99_999_999)
                throw new ArgumentOutOfRangeException(nameof(numero), "El número debe estar entre 1 y 99,999,999.");

            return new SerieYNumero(s, numero);
        }

        /// <summary>
        /// Intenta parsear "SERIE-00000000" (o "SERIE 00000000"). Devuelve false si no cumple.
        /// </summary>
        public static bool TryParse(string? text, out SerieYNumero? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var t = text.Trim().ToUpperInvariant();
            // Acepta separador "-" o espacio; requiere al menos un dígito en el número.
            var m = Regex.Match(t, @"^([A-Z0-9]{1,4})[-\s](\d{1,8})$");
            if (!m.Success) return false;

            // Validar que ambos grupos estén presentes y el número sea válido
            var serie = m.Groups[1].Value;
            var numeroStr = m.Groups[2].Value;
            if (string.IsNullOrEmpty(serie) || string.IsNullOrEmpty(numeroStr)) return false;
            if (!int.TryParse(numeroStr, out var n)) return false;
            if (n < 1 || n > 99_999_999) return false;

            value = Create(serie, n);
            return true;
        }

        // ---------------- Consultas/Helpers ----------------

        /// <summary>Número con padding de 8 dígitos (para UBL/PDF): "00000001".</summary>
        public string Numero8 => Numero.ToString("00000000");

        /// <summary>Identificador UBL: "SERIE-00000001".</summary>
        public string IdUbl => $"{Serie}-{Numero8}";

        /// <summary>
        /// Aliases de conveniencia para compatibilidad con tests o llamadas existentes.
        /// Equivalen a <see cref="IdUbl"/>.
        /// </summary>
        public string Id => IdUbl;
        public string Value => IdUbl;

        /// <summary>Conveniencia para logging/impresión.</summary>
        public override string ToString() => IdUbl;

        /// <summary>
        /// ¿La serie es compatible con el tipo de comprobante?
        /// - "01" (Factura) → Serie inicia con "F"
        /// - "03" (Boleta)  → Serie inicia con "B"
        /// Otros tipos: true (dejar reglas finas al Aggregate).
        /// </summary>
        public bool EsCompatibleConTipo(string tipoCpe)
            => tipoCpe switch
            {
                "01" => Serie.StartsWith("F", StringComparison.Ordinal),
                "03" => Serie.StartsWith("B", StringComparison.Ordinal),
                _    => true
            };

        /// <summary>
        /// Nueva instancia con el siguiente correlativo (Numero+1).
        /// Lanza si se alcanzó el máximo (99,999,999).
        /// </summary>
        public SerieYNumero Next()
        {
            if (Numero >= 99_999_999)
                throw new InvalidOperationException("Se alcanzó el máximo correlativo permitido para la serie.");
            return new SerieYNumero(Serie, Numero + 1);
        }

        /// <summary>Deconstruct (syntactic sugar).</summary>
        public void Deconstruct(out string serie, out int numero)
        {
            serie = Serie;
            numero = Numero;
        }
    }
}
