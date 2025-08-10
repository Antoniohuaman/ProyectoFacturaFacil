using System;
using System.Text;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Observaciones visibles para el cliente (se imprimen en el PDF/nota del comprobante).
    /// - Campo OPCIONAL: si el usuario no escribe nada, simplemente no se incluye.
    /// - Texto libre (permite múltiples líneas), pensado para mensajes cortos: p.ej.
    ///   “Entrega coordinada”, “Descuento por campaña”, “Gracias por su compra”, etc.
    /// - Normaliza y sanea para evitar caracteres inválidos en XML/PDF.
    /// - Longitud sugerida: ≤ 500 caracteres.
    /// </summary>
    public sealed record Observaciones
    {
        public const int MaxLength = 500;

        /// <summary>Texto normalizado y saneado listo para persistir/imprimir.</summary>
        public string Texto { get; }

        private Observaciones(string texto) => Texto = texto;

        /// <summary>
        /// Crea un valor obligatorio (1..500). Lanza excepción si está vacío o excede longitud.
        /// </summary>
        public static Observaciones Create(string texto)
        {
            if (texto is null) throw new ArgumentNullException(nameof(texto));

            // 1) trim y normalización de saltos de línea
            var t = Normalize(texto);

            // 2) validaciones
            if (t.Length == 0)
                throw new ArgumentException("Las observaciones no pueden ser vacías.", nameof(texto));
            if (t.Length > MaxLength)
                throw new ArgumentException($"Las observaciones no deben exceder {MaxLength} caracteres.", nameof(texto));

            return new Observaciones(t);
        }

        /// <summary>
        /// Versión para campos opcionales en la entidad:
        /// - null, vacío o solo espacios ⇒ devuelve null
        /// - caso contrario ⇒ Observaciones Create(...)
        /// </summary>
        public static Observaciones? FromOptional(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;

            return Create(texto);
        }

        public override string ToString() => Texto;

        // ==================== Helpers internos ====================

        private static string Normalize(string raw)
        {
            // Trim extremos
            var s = raw.Trim();

            // Unificar CRLF/CR a LF (para almacenar consistente)
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");

            // Saneamos: quitamos controles no imprimibles excepto \n y \t
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsControl(ch) && ch != '\n' && ch != '\t')
                    continue;
                sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
