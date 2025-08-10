using System;
using System.Globalization;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// VO de Moneda (ISO 4217) para el comprobante (UBL: <cbc:DocumentCurrencyCode>).
    /// - Inmutable, igualdad por valor.
    /// - Hoy soporta explícitamente PEN (S/.) y USD (US$).
    /// - Expone decimales (minor units) y helpers de redondeo/formato.
    /// - El tipo de cambio se maneja en OTRO VO (p.ej. TipoDeCambio), no aquí.
    /// </summary>
    public sealed record Moneda
    {
        /// <summary>Código ISO 4217 en MAYÚSCULAS, p.ej. "PEN", "USD".</summary>

    public string Codigo { get; init; }
    public string Simbolo { get; init; }
    public byte Decimales { get; init; }
    public string? Nombre { get; init; }


        [System.Text.Json.Serialization.JsonConstructor]
        public Moneda(string codigo, string simbolo, byte decimales, string? nombre)
        {
            Codigo = codigo;
            Simbolo = simbolo;
            Decimales = decimales;
            Nombre = nombre;
        }

        // ===================== Fábricas principales (casos actuales) =====================

        /// <summary>PEN (Soles) – 2 decimales.</summary>
        public static Moneda PEN() => new("PEN", "S/.", 2, "Soles");

        /// <summary>USD (Dólares) – 2 decimales.</summary>
        public static Moneda USD() => new("USD", "US$", 2, "Dólares");

        /// <summary>
        /// Crea desde código ISO 4217. Por ahora sólo admite "PEN" y "USD"
        /// (tu UI está limitada a esas opciones). Si necesitas más, usa CreateCustom.
        /// </summary>
        public static Moneda Create(string codigo)
        {
            var iso = NormalizeIso(codigo);
            return iso switch
            {
                "PEN" => PEN(),
                "USD" => USD(),
                _ => throw new ArgumentException("Moneda no soportada (use PEN o USD).", nameof(codigo))
            };
        }

        /// <summary>
        /// Fábrica extensible por si más adelante habilitas otras monedas en configuración.
        /// Mantiene las mismas invariantes de formato.
        /// </summary>
        public static Moneda CreateCustom(string codigo, string simbolo, byte decimales, string? nombre = null)
        {
            var iso = NormalizeIso(codigo);
            if (string.IsNullOrWhiteSpace(simbolo))
                throw new ArgumentException("El símbolo de moneda es obligatorio.", nameof(simbolo));
            if (decimales > 4) // conservador
                throw new ArgumentOutOfRangeException(nameof(decimales), "Los decimales deben ser 0..4.");

            return new Moneda(iso, simbolo.Trim(), decimales, string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim());
        }

        // ===================== Consultas de conveniencia =====================

        public bool EsPEN => Codigo == "PEN";
        public bool EsUSD => Codigo == "USD";

        /// <summary>
        /// Redondea un monto a la cantidad de decimales de la moneda usando
        /// MidpointRounding.AwayFromZero (regla típica contable).
        /// </summary>
        public decimal Redondear(decimal monto)
            => Math.Round(monto, Decimales, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Formatea un monto con el símbolo y los decimales de la moneda.
        /// Usa InvariantCulture para no depender de la cultura del servidor.
        /// Ej.: "S/. 1,234.56" o "US$ 1,234.56".
        /// </summary>
        public string Formatear(decimal monto, bool incluirSeparadores = true)
        {
            var dec = Decimales;
            var formato = incluirSeparadores ? "N" + dec : "F" + dec; // N = con separadores, F = sin
            var s = monto.ToString(formato, CultureInfo.InvariantCulture);
            return $"{Simbolo} {s}";
        }

        public override string ToString() => Nombre is null ? Codigo : $"{Codigo} ({Nombre})";

        // ===================== Helpers internos =====================

        private static string NormalizeIso(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código de moneda es obligatorio.", nameof(codigo));
            var iso = codigo.Trim().ToUpperInvariant();
            if (iso.Length != 3 || !IsAtoZ(iso))
                throw new ArgumentException("El código de moneda debe ser ISO 4217 (tres letras).", nameof(codigo));
            return iso;
        }

        private static bool IsAtoZ(string s)
        {
            foreach (var ch in s)
                if (ch is < 'A' or > 'Z') return false;
            return true;
        }
    }
}
