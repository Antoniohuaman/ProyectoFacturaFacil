using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object de dinero: <b>Monto</b> + <b>Moneda</b> (ISO 4217) con decimales definidos por la moneda.
    /// - Redondeo único: MidpointRounding.AwayFromZero a Moneda.Decimales.
    /// - Sin conversión de moneda (eso va fuera del VO).
    /// - Operaciones seguras: suma/resta sólo si la moneda coincide.
    /// - Uso típico: precio unitario, base imponible, IGV y totales (línea/cabecera).
    /// </summary>
    public sealed record ImporteMonetario
    {
        /// <summary>Monto normalizado a <see cref="SharedKernel.ValueObjects.Moneda.Decimales"/>.</summary>
        public decimal Monto { get; }
        // Guarda el monto original sin redondear para operaciones aritméticas
        private readonly decimal _montoOriginal;

        /// <summary>Moneda del importe (incluye código, símbolo y decimales).</summary>
            public SharedKernel.ValueObjects.Moneda Moneda { get; }

        // --------------------------- Construcción ---------------------------

        // Constructor canónico (privado) que garantiza normalización siempre.
            private ImporteMonetario(decimal monto, SharedKernel.ValueObjects.Moneda moneda)
        {
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda));
            _montoOriginal = monto;
            Monto  = Math.Round(monto, Moneda.Decimales, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Constructor para (de)serialización JSON. Mantiene la normalización.
        /// </summary>
        [JsonConstructor]
            public ImporteMonetario(SharedKernel.ValueObjects.Moneda moneda, decimal monto) : this(monto, moneda) { }

        /// <summary>
        /// Fábrica con regla de no-negativos (útil para importes que no deben ser &lt; 0).
        /// </summary>
            public static ImporteMonetario Create(decimal monto, SharedKernel.ValueObjects.Moneda moneda)
        {
            if (monto < 0m) throw new ArgumentOutOfRangeException(nameof(monto), "El monto no puede ser negativo.");
            return new(monto, moneda);
        }

        /// <summary>
        /// Fábrica sin restricción de signo (útil para notas de crédito/ajustes).
        /// </summary>
            public static ImporteMonetario CreateLibre(decimal monto, SharedKernel.ValueObjects.Moneda moneda) => new(monto, moneda);

        /// <summary>Crea un importe cero en la moneda indicada.</summary>
            public static ImporteMonetario Zero(SharedKernel.ValueObjects.Moneda moneda) => new(0m, moneda);

        /// <summary>¿El monto es exactamente 0?</summary>
        public bool EsCero => Monto == 0m;

        /// <summary>Devuelve una nueva instancia con el mismo <see cref="Moneda"/> y un monto nuevo.</summary>
        public ImporteMonetario ConMonto(decimal nuevoMonto) => new(nuevoMonto, Moneda);

        // --------------------------- Operaciones ---------------------------

        /// <summary>Multiplica por un factor (cantidad, proporción), con redondeo al final.</summary>
        public ImporteMonetario Multiplicar(decimal factor)
        {
            // Multiplica el monto original (sin redondear) y redondea solo una vez
            var resultado = Math.Round(_montoOriginal * factor, Moneda.Decimales, MidpointRounding.AwayFromZero);
            return new ImporteMonetario(resultado, Moneda);
        }

        /// <summary>Suma importes de la misma moneda. Lanza si las monedas difieren.</summary>
        public ImporteMonetario Sumar(ImporteMonetario otro)
        {
            EnsureMismaMoneda(otro);
                return new(Monto + otro.Monto, Moneda);
        }

        /// <summary>Resta importes de la misma moneda. Lanza si las monedas difieren.</summary>
        public ImporteMonetario Restar(ImporteMonetario otro)
        {
            EnsureMismaMoneda(otro);
                return new(Monto - otro.Monto, Moneda);
        }

        /// <summary>Intenta sumar sin lanzar cuando la moneda difiere.</summary>
        public bool TrySumar(ImporteMonetario otro, out ImporteMonetario? resultado)
        {
            if (!MismaMoneda(otro)) { resultado = null; return false; }
                resultado = new(Monto + otro.Monto, Moneda);
            return true;
        }

        /// <summary>Intenta restar sin lanzar cuando la moneda difiere.</summary>
        public bool TryRestar(ImporteMonetario otro, out ImporteMonetario? resultado)
        {
            if (!MismaMoneda(otro)) { resultado = null; return false; }
                resultado = new(Monto - otro.Monto, Moneda);
            return true;
        }

        // --------------------------- Minor Units ---------------------------

        /// <summary>
        /// Convierte el monto a “minor units” (centavos) según los decimales de la moneda.
        /// Ej.: 12.34 con decimales=2 → 1234.
        /// </summary>
        public long AMinorUnits()
            => (long)Math.Round(Monto * Pow10(Moneda.Decimales), 0, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Crea desde “minor units” (centavos) según los decimales de la moneda.
        /// Ej.: 1234 con decimales=2 → 12.34.
        /// </summary>
        public static ImporteMonetario DesdeMinorUnits(SharedKernel.ValueObjects.Moneda moneda, long minorUnits)
            => new(minorUnits / Pow10(moneda.Decimales), moneda);

        // --------------------------- Representación ---------------------------

        /// <summary>
        /// Representación legible e invariante a cultura. Usa el formateo provisto por <see cref="Moneda"/>.
        /// Ej.: "S/. 1,234.56" o "US$ 1,234.56".
        /// </summary>
        public override string ToString()
        {
            // Formatea el monto con separadores y símbolo
            var montoFormateado = Monto.ToString($"N{Moneda.Decimales}", CultureInfo.InvariantCulture);
            var simbolo = Moneda.Codigo == "PEN" && !Moneda.Simbolo.Contains(".") ? "S/." : Moneda.Simbolo;
            return $"{simbolo} {montoFormateado}";
        }

        // Operadores de conveniencia
        public static ImporteMonetario operator +(ImporteMonetario a, ImporteMonetario b) => a.Sumar(b);
        public static ImporteMonetario operator -(ImporteMonetario a, ImporteMonetario b) => a.Restar(b);
        public static ImporteMonetario operator *(ImporteMonetario a, decimal factor) => a.Multiplicar(factor);
        public static ImporteMonetario operator *(decimal factor, ImporteMonetario a) => a.Multiplicar(factor);

        // --------------------------- Internos ---------------------------

        private static decimal Pow10(int exp)
        {
            // Evita Math.Pow(double) para no introducir imprecisiones binarias.
            decimal r = 1m;
            for (int i = 0; i < exp; i++) r *= 10m;
            return r;
        }

        private bool MismaMoneda(ImporteMonetario otro) => Moneda.Equals(otro.Moneda);

        private void EnsureMismaMoneda(ImporteMonetario otro)
        {
            if (!MismaMoneda(otro))
                throw new ComprobantesElectronicosBC.Domain.Exceptions.ReglaDeNegocioException($"No se puede operar entre distintas monedas: {Moneda.Codigo} vs {otro.Moneda.Codigo}.");
        }
    }
}