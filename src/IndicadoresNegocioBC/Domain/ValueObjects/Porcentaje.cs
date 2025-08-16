using System;
using System.Globalization;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa un porcentaje como fracción decimal.
    /// 
    /// Uso en Indicadores:
    ///  - Variación % vs periodo anterior, participación de producto/cliente,
    ///    cumplimiento de meta, descuentos, ratios, etc.
    ///
    /// Representación:
    ///  - Interno como fracción (0.12 = 12%).
    ///  - Inmutable, igualdad por valor.
    ///
    /// Invariantes:
    ///  - Se normaliza a escala fija (Escala = 6) con redondeo bancario (ToEven),
    ///    para evitar arrastres de precisión en cálculos encadenados.
    ///
    /// Notas:
    ///  - No se limita a [0..1]: se permiten valores negativos (p.ej., −25%)
    ///    y > 100% (p.ej., 150%) según el KPI.
    ///  - Para porcentajes “acotados” (p.ej., participación [0..1]), usar <see cref="Limitar"/>.
    /// </summary>
    public sealed record class Porcentaje
    {
        public const int Escala = 6;
        public const MidpointRounding ModoRedondeo = MidpointRounding.ToEven;

        /// <summary>Fracción equivalente (0.12 = 12%).</summary>
        public decimal Fraccion { get; }

        private Porcentaje(decimal fraccion)
        {
            Fraccion = Math.Round(fraccion, Escala, ModoRedondeo);
        }

        /// <summary>Crea desde fracción (0.12 = 12%).</summary>
        public static Porcentaje DesdeFraccion(decimal fraccion) => new(fraccion);

        /// <summary>Crea desde porcentaje en “por ciento” (12.34 =&gt; 0.1234).</summary>
        public static Porcentaje DesdePorCiento(decimal valorPorCiento) =>
            new(valorPorCiento / 100m);

        // ---------- Instancias comunes ----------
        public static readonly Porcentaje Cero       = new(0m);       // 0%
        public static readonly Porcentaje Cincuenta  = new(0.5m);     // 50%
        public static readonly Porcentaje Cien       = new(1m);       // 100%

        // ---------- Operaciones ----------
        /// <summary>Aplica el porcentaje a un importe monetario.</summary>
        public Dinero Aplicar(Dinero importe) =>
            importe.Multiplicar(Fraccion);

        /// <summary>Suma porcentajes (fracciones).</summary>
        public Porcentaje Sumar(Porcentaje otro) =>
            new(Fraccion + otro.Fraccion);

        /// <summary>Resta porcentajes (fracciones).</summary>
        public Porcentaje Restar(Porcentaje otro) =>
            new(Fraccion - otro.Fraccion);

        /// <summary>Escala el porcentaje por un factor escalar (útil en transformaciones).</summary>
        public Porcentaje Multiplicar(decimal factor) =>
            new(Fraccion * factor);

        /// <summary>Devuelve el porcentaje con signo invertido.</summary>
        public Porcentaje Negativo() => new(-Fraccion);

        /// <summary>Restringe el porcentaje a un rango [min, max] en fracción.</summary>
        public Porcentaje Limitar(decimal minFraccion, decimal maxFraccion)
        {
            if (minFraccion > maxFraccion)
                throw new ArgumentException("El mínimo no puede ser mayor que el máximo.");
            var f = Fraccion;
            if (f < minFraccion) f = minFraccion;
            if (f > maxFraccion) f = maxFraccion;
            return new(f);
        }

        // ---------- Conversión / Formato ----------
        /// <summary>Devuelve el valor en “por ciento” (12.34 para 12.34%).</summary>
        public decimal APorCiento(int decimales = 2) =>
            Math.Round(Fraccion * 100m, decimales, ModoRedondeo);

        /// <summary>Texto con formato fijo: “12.34 %”.</summary>
        public string Formatear(int decimales = 2)
        {
            var valor = APorCiento(decimales).ToString($"F{decimales}", CultureInfo.InvariantCulture);
            return $"{valor} %";
        }

        public override string ToString() => Formatear(2);

        // ---------- Operadores ----------
        public static Porcentaje operator +(Porcentaje a, Porcentaje b) => a.Sumar(b);
        public static Porcentaje operator -(Porcentaje a, Porcentaje b) => a.Restar(b);
        public static Porcentaje operator -(Porcentaje a) => a.Negativo();
        public static Porcentaje operator *(Porcentaje a, decimal factor) => a.Multiplicar(factor);
        public static Porcentaje operator *(decimal factor, Porcentaje a) => a.Multiplicar(factor);
    }
}