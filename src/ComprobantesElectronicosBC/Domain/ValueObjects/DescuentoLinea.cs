using System;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Representa el descuento aplicable a una LÍNEA de detalle.
    /// Modos soportados:
    ///  - Ninguno  : sin descuento
    ///  - Porcentaje: factor en fracción [0..1] (p.ej. 0.10 = 10%)
    ///  - Monto     : importe fijo >= 0 (moneda)
    /// Expone helpers para calcular el monto de descuento sobre una base
    /// y para recalcular la línea (base/igv/total) considerando el impuesto.
    /// </summary>
    public enum DescuentoLineaModo { Ninguno = 0, Porcentaje = 1, Monto = 2 }

    public sealed record DescuentoLinea
    {
        public DescuentoLineaModo Modo { get; }
        /// <summary>
        /// Si Modo=Porcentaje → Valor=fracción [0..1].
        /// Si Modo=Monto → Valor=importe (2 decimales).
        /// Si Modo=Ninguno → 0.
        /// </summary>
        public decimal Valor { get; }

        private DescuentoLinea(DescuentoLineaModo modo, decimal valor)
        {
            Modo = modo;
            Valor = valor;
        }

        // ---------- Constructores de dominio ----------
        public static DescuentoLinea None => new(DescuentoLineaModo.Ninguno, 0m);

        public static DescuentoLinea FromPorcentaje(decimal porcentaje)
        {
            // Acepta 0..100 y convierte a fracción
            if (porcentaje < 0m || porcentaje > 100m)
                throw new ArgumentOutOfRangeException(nameof(porcentaje), "El porcentaje debe estar entre 0 y 100.");
            var fraccion = porcentaje / 100m;
            return FromFraccion(fraccion);
        }

        public static DescuentoLinea FromFraccion(decimal fraccion)
        {
            if (fraccion < 0m || fraccion > 1m)
                throw new ArgumentOutOfRangeException(nameof(fraccion), "La fracción debe estar entre 0.00 y 1.00.");
            return new(DescuentoLineaModo.Porcentaje, fraccion);
        }

        public static DescuentoLinea FromMonto(decimal monto)
        {
            if (monto < 0m) throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser ≥ 0.");
            return new(DescuentoLineaModo.Monto, Round2(monto));
        }

        // ---------- Flags de conveniencia ----------
        public bool EsNinguno => Modo == DescuentoLineaModo.Ninguno;
        public bool EsPorcentaje => Modo == DescuentoLineaModo.Porcentaje;
        public bool EsMonto => Modo == DescuentoLineaModo.Monto;

        // ---------- Cálculos ----------
        /// <summary>
        /// Calcula el importe de descuento sobre una base previa a descuento.
        /// Si Modo=Monto valida que no exceda la base.
        /// </summary>
        public decimal CalcularMontoSobreBase(decimal baseAntes)
        {
            if (baseAntes < 0m) throw new ArgumentOutOfRangeException(nameof(baseAntes));
            if (EsNinguno) return 0m;

            if (EsPorcentaje)
                return Round2(baseAntes * Valor);

            // Monto
            if (Valor > baseAntes)
                throw new InvalidOperationException("El descuento por monto no puede exceder la base.");
            return Round2(Valor);
        }

        /// <summary>
        /// Recalcula la línea (base/IGV/total) aplicando el descuento.
        /// Usa el VO AfectacionImpuesto para derivar la base antes de descuento y la tasa de impuesto.
        /// </summary>
        public Resultado Aplicar(AfectacionImpuesto afectacion, decimal unitPriceEntrada, Cantidad cantidad, bool priceIncludesIgv)
        {
            // Calcular base imponible antes de descuento
            decimal baseAntes;
            if (priceIncludesIgv && afectacion.GravaImpuesto)
            {
                baseAntes = Round2(unitPriceEntrada * cantidad.Value / (1 + TasaImpuesto.IGV18.Fraccion));
            }
            else
            {
                baseAntes = Round2(unitPriceEntrada * cantidad.Value);
            }

            // Descuento sobre base imponible
            var descuento = CalcularMontoSobreBase(baseAntes);
            var baseDespues = Round2(baseAntes - descuento);

            // Recalcular IGV y Total tras descuento
            decimal igv = afectacion.GravaImpuesto
                ? Round2(baseDespues * TasaImpuesto.IGV18.Fraccion) // Puedes parametrizar la tasa si lo necesitas
                : 0m;

            var total = Round2(baseDespues + igv);

            return new Resultado(
                BaseAntes: baseAntes,
                Descuento: descuento,
                BaseDespues: baseDespues,
                Igv: igv,
                Total: total
            );
        }

        /// <summary>
        /// Estructura mínima para mapear AllowanceCharge de UBL a nivel de línea.
        /// ChargeIndicator siempre false (es descuento), Amount el monto de descuento,
        /// BaseAmount la base imponible antes de descuento, y MultiplierFactorNumeric
        /// solo cuando el modo es porcentaje.
        /// </summary>
        public AllowanceCharge ToAllowanceCharge(decimal baseAntes)
        {
            var amount = CalcularMontoSobreBase(baseAntes);
            decimal? factor = EsPorcentaje ? Valor : null;

            return new AllowanceCharge(
                ChargeIndicator: false,
                Amount: amount,
                BaseAmount: Round2(baseAntes),
                MultiplierFactorNumeric: factor,
                ChargeReasonCode: "00" // 00 = Descuento por ítem (puedes ajustar si usas otro catálogo interno)
            );
        }

        // ---------- Tipos auxiliares ----------
        public readonly record struct Resultado(
            decimal BaseAntes,
            decimal Descuento,
            decimal BaseDespues,
            decimal Igv,
            decimal Total
        );

        public readonly record struct AllowanceCharge(
            bool    ChargeIndicator,
            decimal Amount,
            decimal BaseAmount,
            decimal? MultiplierFactorNumeric,
            string? ChargeReasonCode
        );

        public override string ToString()
        {
            return Modo switch
            {
                DescuentoLineaModo.Ninguno    => "Sin descuento",
                DescuentoLineaModo.Porcentaje => $"{(Valor * 100m):0.##}%",
                DescuentoLineaModo.Monto      => $"Monto: {Valor:0.00}",
                _ => base.ToString()!
            };
        }

        private static decimal Round2(decimal v)
            => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    }
}
