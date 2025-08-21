using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Value Object Dinero: monto + Moneda (ISO-4217) con redondeo consistente.
    /// - Inmutable, igualdad por valor.
    /// - Todas las operaciones respetan Moneda.Decimales usando MidpointAwayFromZero.
    /// - Solo permite aritmética entre la MISMA moneda.
    /// </summary>
    [DebuggerDisplay("{Moneda.Simbolo}{Monto} {Moneda.Codigo}")]
    public sealed record Dinero
    {
        /// <summary>Monto redondeado a Moneda.Decimales.</summary>
        public decimal Monto { get; init; }

        /// <summary>Moneda ISO-4217 (PEN, USD, ...).</summary>
        public Moneda Moneda { get; init; }

    public Dinero(decimal monto, Moneda moneda)
        {
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda));
            Monto  = Round(monto, Moneda.Decimales);
        }

        /// <summary>Crea un Dinero normalizado (redondeado) en la moneda indicada.</summary>
        public static Dinero Create(decimal monto, Moneda moneda) => new(monto, moneda);

        /// <summary>Atajo para cero en una moneda.</summary>
        public static Dinero Cero(Moneda moneda) => new(0m, moneda);

        /// <summary>¿El monto es exactamente 0 en esta moneda?</summary>
        public bool EsCero => Monto == 0m;

        /// <summary>Suma (misma moneda).</summary>
        public Dinero Sumar(Dinero otro)
        {
            EnsureSameCurrency(otro);
            return new Dinero(Monto + otro.Monto, Moneda);
        }

        /// <summary>Resta (misma moneda).</summary>
        public Dinero Restar(Dinero otro)
        {
            EnsureSameCurrency(otro);
            return new Dinero(Monto - otro.Monto, Moneda);
        }

        /// <summary>Negación (cambia signo).</summary>
        public Dinero Negar() => new(-Monto, Moneda);

        /// <summary>Multiplica por un escalar (ej.: precio * cantidad).</summary>
        public Dinero Multiplicar(decimal factor) => new(Monto * factor, Moneda);

        /// <summary>Divide por un escalar (lanzará si divisor=0).</summary>
        public Dinero Dividir(decimal divisor)
        {
            if (divisor == 0m) throw new DivideByZeroException("No se puede dividir entre 0.");
            return new Dinero(Monto / divisor, Moneda);
        }

        /// <summary>
        /// Divide el monto en N partes iguales en la unidad mínima de la moneda (centavos, etc.),
        /// asegurando que la suma de partes = monto original. Útil para prorrateos simples.
        /// </summary>
        public IReadOnlyList<Dinero> DividirEnPartes(int partes)
        {
            if (partes <= 0) throw new ArgumentOutOfRangeException(nameof(partes), "Partes debe ser > 0.");

            var scale = Pow10(Moneda.Decimales);
            // Trabajamos en unidades mínimas (centavos) para repartir remainders de forma exacta.
            var unidadesTotales = ToMinorUnits(Monto, Moneda.Decimales); // puede ser negativo
            var q = unidadesTotales / partes;                            // cociente (entero)
            var r = unidadesTotales % partes;                            // resto (entero, mismo signo que unidadesTotales)

            var lista = new List<Dinero>(partes);
            // Para montos positivos, los primeros 'r' reciben +1 unidad mínima.
            // Para montos negativos, los primeros 'abs(r)' reciben -1 unidad mínima.
            var extra = r > 0 ? 1 : (r < 0 ? -1 : 0);
            var vecesExtra = Math.Abs(r);

            for (int i = 0; i < partes; i++)
            {
                var unidades = q + (i < vecesExtra ? extra : 0);
                var montoParte = (decimal)unidades / scale;
                lista.Add(new Dinero(montoParte, Moneda));
            }
            return lista;
        }

        /// <summary>Operador + (misma moneda).</summary>
        public static Dinero operator +(Dinero a, Dinero b) => a.Sumar(b);

        /// <summary>Operador - (misma moneda).</summary>
        public static Dinero operator -(Dinero a, Dinero b) => a.Restar(b);

        /// <summary>Operador unario -.</summary>
        public static Dinero operator -(Dinero a) => a.Negar();

        /// <summary>Operador * por escalar (decimal).</summary>
        public static Dinero operator *(Dinero a, decimal factor) => a.Multiplicar(factor);

        /// <summary>Operador * por escalar (decimal) (conmutativo).</summary>
        public static Dinero operator *(decimal factor, Dinero a) => a.Multiplicar(factor);

        /// <summary>Operador / por escalar (decimal).</summary>
        public static Dinero operator /(Dinero a, decimal divisor) => a.Dividir(divisor);

        /// <summary>Devuelve el absoluto.</summary>
        public Dinero Abs() => Monto < 0m ? new Dinero(-Monto, Moneda) : this;

        /// <summary>Formatea simple: "S/ 123.45" o "$ 123.45".</summary>
        public override string ToString()
            => $"{Moneda.Simbolo} {Monto.ToString($"F{Moneda.Decimales}")}";

        // ------------------------------
        // Helpers internos
        // ------------------------------
        private static decimal Round(decimal value, byte decimals)
            => Math.Round(value, decimals, MidpointRounding.AwayFromZero);

        private static decimal Pow10(byte decimales)
        {
            // 10^decimales como decimal (ej. 2 => 100m)
            decimal r = 1m;
            for (int i = 0; i < decimales; i++) r *= 10m;
            return r;
        }

        private static long ToMinorUnits(decimal amount, byte decimales)
        {
            // Convierte a unidades mínimas (centavos) preservando signo.
            // Se redondea con AwayFromZero para coincidir con la normalización de Monto.
            var scale = Pow10(decimales);
            var unidades = Math.Round(amount * scale, 0, MidpointRounding.AwayFromZero);
            return decimal.ToInt64(unidades);
        }

        private void EnsureSameCurrency(Dinero otro)
        {
            if (otro is null) throw new ArgumentNullException(nameof(otro));
            if (otro.Moneda != Moneda)
                throw new InvalidOperationException($"No se puede operar entre {Moneda.Codigo} y {otro.Moneda.Codigo}.");
        }
    }
}
