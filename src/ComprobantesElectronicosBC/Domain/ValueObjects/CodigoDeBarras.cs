using System;
using System.Linq;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO para códigos de barras escaneados o tipeados.
    /// Soporta:
    /// - EAN-13 : 13 dígitos con dígito verificador (pesos 1/3)
    /// - UPC-A  : 12 dígitos con dígito verificador (pesos 3/1)
    /// - EAN-8  : 8  dígitos con dígito verificador
    /// - CODE128: texto ASCII imprimible (32..126). El DV lo calcula la lib/impresora, no el dato.
    ///
    /// Igualdad por valor (record). La unicidad global se garantiza en el agregado/repositorio.
    /// </summary>
    public sealed record CodigoDeBarras
    {
        public const string EAN13   = "EAN13";
        public const string UPCA    = "UPCA";
        public const string EAN8    = "EAN8";
        public const string CODE128 = "CODE128";

        /// <summary>Tipo de simbología (EAN13/UPCA/EAN8/CODE128).</summary>
        public string Tipo { get; }

        /// <summary>Valor canónico. Numéricos: solo dígitos; Code128: texto recortado.</summary>
        public string Valor { get; }

        /// <summary>Texto de presentación (por ahora igual a <see cref="Valor"/>).</summary>
        public string Mostrar { get; }

        private CodigoDeBarras(string tipo, string valor, string mostrar)
        {
            Tipo = tipo;
            Valor = valor;
            Mostrar = mostrar;
        }

        // ================== Fábricas para códigos numéricos ==================

        public static CodigoDeBarras CreateEan13(string digits13)
        {
            var d = OnlyDigits(digits13, expectedLength: 13, nameof(digits13));
            var dv = ComputeEan13Dv(d.AsSpan(0, 12)); // 12 datos
            if (dv != d[12] - '0') throw new ArgumentException("EAN-13 con dígito verificador inválido.", nameof(digits13));
            return new(EAN13, d, d);
        }

        public static CodigoDeBarras CreateUpcA(string digits12)
        {
            var d = OnlyDigits(digits12, expectedLength: 12, nameof(digits12));
            var dv = ComputeUpcDv(d.AsSpan(0, 11));   // 11 datos
            if (dv != d[11] - '0') throw new ArgumentException("UPC-A con dígito verificador inválido.", nameof(digits12));
            return new(UPCA, d, d);
        }

        public static CodigoDeBarras CreateEan8(string digits8)
        {
            var d = OnlyDigits(digits8, expectedLength: 8, nameof(digits8));
            var dv = ComputeEan8Dv(d.AsSpan(0, 7));   // 7 datos
            if (dv != d[7] - '0') throw new ArgumentException("EAN-8 con dígito verificador inválido.", nameof(digits8));
            return new(EAN8, d, d);
        }

        // ================== Fábrica para CODE128 (texto) ==================

        /// <summary>
        /// Crea un Code128 “texto” (sin DV a nivel de dato).
        /// Acepta ASCII 32..126 y longitud 1..maxLen (por defecto 48).
        /// </summary>
        public static CodigoDeBarras CreateCode128(string text, int maxLen = 48)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var t = text.Trim();
            if (t.Length < 1 || t.Length > maxLen)
                throw new ArgumentException($"El texto CODE128 debe tener 1..{maxLen} caracteres.", nameof(text));

            foreach (char ch in t)
                if (ch < 32 || ch > 126)
                    throw new ArgumentException("CODE128 solo admite caracteres ASCII imprimibles (32..126).", nameof(text));

            return new(CODE128, t, t);
        }

        // ================== Autodetección desde el escáner ==================

        /// <summary>
        /// Detecta por longitud/contenido:
        /// 13 dígitos → EAN-13; 12 dígitos → UPC-A; 8 dígitos → EAN-8; otro → CODE128.
        /// </summary>
        public static CodigoDeBarras FromScan(string scan)
        {
            if (string.IsNullOrWhiteSpace(scan))
                throw new ArgumentException("El código escaneado no puede ser vacío.", nameof(scan));

            var trimmed = scan.Trim();

            // ¿Solo dígitos?
            bool allDigits = trimmed.All(char.IsDigit);
            if (allDigits)
            {
                return trimmed.Length switch
                {
                    13 => CreateEan13(trimmed),
                    12 => CreateUpcA(trimmed),
                    8  => CreateEan8(trimmed),
                    _  => throw new ArgumentException("Longitud numérica inválida. Use 8, 12 o 13 dígitos.", nameof(scan))
                };
            }

            // Si trae letras u otros, lo tratamos como Code128 (texto)
            return CreateCode128(trimmed);
        }

        public static bool TryFromScan(string? scan, out CodigoDeBarras? codigo)
        {
            try { codigo = FromScan(scan!); return true; }
            catch { codigo = null; return false; }
        }

        // ================== Helpers internos ==================

        private static string OnlyDigits(string? s, int expectedLength, string paramName)
        {
            if (s is null) throw new ArgumentNullException(paramName);
            // Limpia separadores comunes (espacios, guiones, etc.)
            var digits = new string(s.Where(char.IsDigit).ToArray());
            if (digits.Length != expectedLength)
                throw new ArgumentException($"Se esperaban exactamente {expectedLength} dígitos.", paramName);
            return digits;
        }

        // EAN-13: 12 datos, pesos 1/3 alternados desde la izquierda
        private static int ComputeEan13Dv(ReadOnlySpan<char> data12)
        {
            if (data12.Length != 12) throw new ArgumentException("Se requieren 12 dígitos.", nameof(data12));
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int d = data12[i] - '0';
                sum += (i % 2 == 0) ? d : d * 3;
            }
            int mod = sum % 10;
            return (10 - mod) % 10;
        }

        // UPC-A: 11 datos, suma posiciones impares*3 + pares*1
        private static int ComputeUpcDv(ReadOnlySpan<char> data11)
        {
            if (data11.Length != 11) throw new ArgumentException("Se requieren 11 dígitos.", nameof(data11));
            int sumOdd = 0, sumEven = 0; // i=0 ⇒ posición 1 (impar)
            for (int i = 0; i < 11; i++)
            {
                int d = data11[i] - '0';
                if ((i % 2) == 0) sumOdd += d; else sumEven += d;
            }
            int sum = sumOdd * 3 + sumEven;
            int mod = sum % 10;
            return (10 - mod) % 10;
        }

        // EAN-8: 7 datos, pesos 3/1 alternados desde la izquierda
        private static int ComputeEan8Dv(ReadOnlySpan<char> data7)
        {
            if (data7.Length != 7) throw new ArgumentException("Se requieren 7 dígitos.", nameof(data7));
            int sum = 0;
            for (int i = 0; i < 7; i++)
            {
                int d = data7[i] - '0';
                sum += (i % 2 == 0) ? d * 3 : d;
            }
            int mod = sum % 10;
            return (10 - mod) % 10;
        }

        public override string ToString() => $"{Tipo}:{Valor}";
    }
}
