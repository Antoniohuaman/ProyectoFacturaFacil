using System;
using System.Diagnostics;
using SharedKernel.ValueObjects; // Dinero, Moneda, (opcional) AfectacionImpuesto

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>
    /// Valor de precio para una columna de la lista (importe + flag de impuestos).
    /// - <see cref="Importe"/>: monto y moneda (VO Dinero del SharedKernel).
    /// - <see cref="IncluyeImpuesto"/>: si el importe fue ingresado con impuesto incluido.
    ///
    /// No asume una tasa fija: los helpers para neto/bruto reciben la tasa (fracción) y
    /// si el artículo grava o no impuesto (bool o AfectacionImpuesto).
    /// </summary>
    [DebuggerDisplay("{Importe} (IncluyeImpuesto={IncluyeImpuesto})")]
    public sealed class ValorPrecio : IEquatable<ValorPrecio>
    {
        /// <summary>Importe monetario (monto &amp; moneda).</summary>
        public Dinero Importe { get; }

        /// <summary>Si <c>true</c>, el importe está ingresado con impuesto incluido.</summary>
        public bool IncluyeImpuesto { get; }

        private ValorPrecio(Dinero importe, bool incluyeImpuesto)
        {
            Importe = importe ?? throw new ArgumentNullException(nameof(importe));
            if (Importe.Monto < 0m)
                throw new ArgumentOutOfRangeException(nameof(importe), "El importe no puede ser negativo.");
            IncluyeImpuesto = incluyeImpuesto;
        }

        /// <summary>Crea un valor a partir de un Dinero ya construido.</summary>
        public static ValorPrecio Crear(Dinero importe, bool incluyeImpuesto = true)
            => new(importe, incluyeImpuesto);

        /// <summary>Crea un valor a partir de un monto y una moneda.</summary>
        /// <summary>
        /// Crea un valor a partir de un monto y una moneda.
        /// El monto se normaliza según la moneda:
        /// - Para PEN, se redondea a 2 decimales con MidpointRounding.AwayFromZero.
        /// - Para otras monedas, se respeta la escala definida en Moneda.
        /// </summary>
        public static ValorPrecio DesdeMonto(decimal monto, Moneda moneda, bool incluyeImpuesto = true)
        {
            if (moneda == null) throw new ArgumentNullException(nameof(moneda));
            decimal normalizado = monto;
            if (moneda.Codigo == "PEN")
                normalizado = Math.Round(monto, 2, MidpointRounding.AwayFromZero);
            return new ValorPrecio(new Dinero(normalizado, moneda), incluyeImpuesto);
        }

        /// <summary>Devuelve una nueva instancia marcando <see cref="IncluyeImpuesto"/> = true.</summary>
        public ValorPrecio ConImpuesto()  => new ValorPrecio(Importe, true);

        /// <summary>Devuelve una nueva instancia marcando <see cref="IncluyeImpuesto"/> = false.</summary>
        public ValorPrecio SinImpuesto()  => new ValorPrecio(Importe, false);

        /// <summary>Cambia el importe (moneda y monto), conservando el flag de impuestos.</summary>
        public ValorPrecio ConImporte(Dinero nuevoImporte) => new ValorPrecio(nuevoImporte, IncluyeImpuesto);

        /// <summary>
        /// Obtiene el valor NETO (sin impuesto) para una tasa (fracción, ej. 0.18) y si el artículo grava impuesto.
        /// </summary>
        public Dinero Neto(decimal tasaImpuestoFraccion, bool gravaImpuesto)
        {
            var monto = Importe.Monto;

            if (!gravaImpuesto) // exonerado / inafecto
                return Redondear(monto);

            if (IncluyeImpuesto)
                return Redondear(monto / (1 + tasaImpuestoFraccion));

            return Redondear(monto); // ya es neto
        }

        /// <summary>
        /// Obtiene el valor BRUTO (con impuesto) para una tasa (fracción, ej. 0.18) y si el artículo grava impuesto.
        /// </summary>
        public Dinero Bruto(decimal tasaImpuestoFraccion, bool gravaImpuesto)
        {
            var monto = Importe.Monto;

            if (!gravaImpuesto) // exonerado / inafecto
                return Redondear(monto);

            if (IncluyeImpuesto)
                return Redondear(monto); // ya es bruto

            return Redondear(monto * (1 + tasaImpuestoFraccion));
        }

        /// <summary>
        /// Versión que acepta <see cref="AfectacionImpuesto"/> del SharedKernel.
        /// </summary>
        public Dinero Neto(AfectacionImpuesto afectacion, decimal tasaImpuestoFraccion)
            => Neto(tasaImpuestoFraccion, afectacion?.GravaImpuesto ?? false);

        /// <summary>
        /// Versión que acepta <see cref="AfectacionImpuesto"/> del SharedKernel.
        /// </summary>
        public Dinero Bruto(AfectacionImpuesto afectacion, decimal tasaImpuestoFraccion)
            => Bruto(tasaImpuestoFraccion, afectacion?.GravaImpuesto ?? false);

        private Dinero Redondear(decimal monto)
            => new Dinero(decimal.Round(monto, 2, MidpointRounding.AwayFromZero), Importe.Moneda);

        #region Igualdad
        public bool Equals(ValorPrecio? other)
            => other is not null
               && IncluyeImpuesto == other.IncluyeImpuesto
               && Importe.Equals(other.Importe);

        public override bool Equals(object? obj) => Equals(obj as ValorPrecio);

        public override int GetHashCode() => HashCode.Combine(Importe, IncluyeImpuesto);
        #endregion

        public override string ToString()
            => $"{Importe} {(IncluyeImpuesto ? "(Inc. Impuesto)" : "(Sin impuesto)")}";
            
            // 1) Propiedad de conveniencia para que los tests puedan leer .Monto directamente
public decimal Monto => Importe.Monto;

// 2) Sobrecarga de conveniencia para que los tests/aggregate puedan usar DesdeMonto(monto)
//    sin especificar moneda (elige tu default; asumo PEN).
public static ValorPrecio DesdeMonto(decimal monto, bool incluyeImpuesto = true)
    => DesdeMonto(monto, Moneda.PEN(), incluyeImpuesto);

    }
}