using System;
using System.Collections.Generic;
using System.Globalization;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object Dinero = Monto (decimal) + Moneda (VO).
    /// Uso: representar importes monetarios en indicadores y entidades (no es un filtro).
    ///
    /// Invariantes:
    ///  - Moneda obligatoria (no nula).
    ///  - Monto se normaliza a 2 decimales con redondeo bancario (MidpointToEven).
    ///
    /// Reglas:
    ///  - Suma y resta sólo entre la MISMA moneda (si no, se lanza excepción).
    ///  - Multiplicación/División solo por escalares (decimal), re-redondeando a 2 decimales.
    ///  - Se permiten montos negativos (ej.: anulaciones/ajustes).
    ///
    /// Notas:
    ///  - Para mostrar símbolo/formato regional usa la capa de presentación; aquí se expone ToString simple.
    /// </summary>
    public sealed record class Dinero
    {
        public const int Escala = 2;
    public const MidpointRounding ModoRedondeo = MidpointRounding.AwayFromZero;

        /// <summary>Monto monetario normalizado a 2 decimales.</summary>
        public decimal Monto { get; }

        /// <summary>Moneda del importe (VO Moneda definido en el dominio).</summary>
        public Moneda Moneda { get; }

        private Dinero(decimal monto, Moneda moneda)
        {
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda));
            Monto  = Math.Round(monto, Escala, ModoRedondeo);
        }

        /// <summary>Fábrica principal.</summary>
        public static Dinero Crear(decimal monto, Moneda moneda) => new(monto, moneda);

        /// <summary>Dinero con valor 0 en la moneda indicada.</summary>
        public static Dinero Cero(Moneda moneda) => new(0m, moneda);

        public bool EsCero     => Monto == 0m;
        public bool EsNegativo => Monto < 0m;
        public bool EsPositivo => Monto > 0m;

        /// <summary>Valor absoluto (mismo Moneda).</summary>
        public Dinero Abs() => new(Math.Abs(Monto), Moneda);

        /// <summary>Suma dos importes de la misma moneda.</summary>
        public Dinero Sumar(Dinero otro)
        {
            AsegurarMismaMoneda(otro);
            return new(Monto + otro.Monto, Moneda);
        }

        /// <summary>Resta dos importes de la misma moneda.</summary>
        public Dinero Restar(Dinero otro)
        {
            AsegurarMismaMoneda(otro);
            return new(Monto - otro.Monto, Moneda);
        }

        /// <summary>Multiplica el importe por un escalar y re-redondea a 2 decimales.</summary>
        public Dinero Multiplicar(decimal factor) =>
            new(Math.Round(Monto * factor, Escala, ModoRedondeo), Moneda);

        /// <summary>Divide el importe por un escalar y re-redondea a 2 decimales.</summary>
        public Dinero Dividir(decimal divisor)
        {
            if (divisor == 0m) throw new DivideByZeroException();
            return new(Math.Round(Monto / divisor, Escala, ModoRedondeo), Moneda);
        }

        /// <summary>Cambia el signo del importe.</summary>
        public Dinero Negativo() => new(-Monto, Moneda);

        /// <summary>
        /// Prorratea el importe en N partes respetando centavos.
        /// La suma de las partes siempre es igual al importe original.
        /// </summary>
        /// <param name="partes">Cantidad de partes (debe ser &gt; 0).</param>
        public IReadOnlyList<Dinero> Prorratear(int partes)
        {
            if (partes <= 0) throw new ArgumentOutOfRangeException(nameof(partes), "Partes debe ser > 0.");

            // Trabajar en centavos para evitar errores de coma flotante.
            var signo = Math.Sign(Monto);
            var centavosAbs = (long)Math.Round(Math.Abs(Monto) * 100m, 0, ModoRedondeo);

            var baseParte = centavosAbs / partes;
            var resto = centavosAbs % partes;

            var lista = new List<Dinero>(partes);
            for (int i = 0; i < partes; i++)
            {
                var centavos = baseParte + (i < resto ? 1 : 0);
                var monto = signo * (centavos / 100m);
                lista.Add(new Dinero(monto, Moneda));
            }
            return lista;
        }

        // ---------- Operadores convenientes ----------
        public static Dinero operator +(Dinero a, Dinero b) => a.Sumar(b);
        public static Dinero operator -(Dinero a, Dinero b) => a.Restar(b);
        public static Dinero operator -(Dinero a) => a.Negativo();
        public static Dinero operator *(Dinero a, decimal factor) => a.Multiplicar(factor);
        public static Dinero operator *(decimal factor, Dinero a) => a.Multiplicar(factor);
        public static Dinero operator /(Dinero a, decimal divisor) => a.Dividir(divisor);

        /// <summary>Representación simple: "PEN 1234.56".</summary>
        public override string ToString() =>
            $"{Moneda} {Monto.ToString($"F{Escala}", CultureInfo.InvariantCulture)}";

        // ---------- Helpers ----------
        private void AsegurarMismaMoneda(Dinero otro)
        {
            if (!Equals(Moneda, otro.Moneda))
                throw new InvalidOperationException($"Monedas distintas: {Moneda} vs {otro.Moneda}.");
        }
    }
}





