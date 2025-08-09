using System;

namespace ComprobantesElectronicosBC.Domain.ValueObjects
{
    /// <summary>
    /// Descuento global aplicado al DOCUMENTO completo (no por línea).
    /// - OPCIONAL: si no se usa, el agregado puede dejarlo en null o usar DescuentoGlobal.None.
    /// - Porcentaje: Valor es fracción (0.10 = 10%) y se aplica sobre la base imponible del documento.
    /// - Monto: Valor es un importe fijo (no puede exceder la base imponible).
    ///
    /// UBL 2.1 / CPE 2.0:
    ///   * Se modela como cac:AllowanceCharge con cbc:ChargeIndicator=false.
    ///   * Porcentaje → cbc:MultiplierFactorNumeric (fracción), siempre cbc:Amount (monto del descuento).
    ///   * LegalMonetaryTotal/AllowanceTotalAmount debe sumar los descuentos globales.
    /// </summary>
    public enum DescuentoGlobalModo
    {
        Ninguno    = 0,
        Porcentaje = 1, // Valor en fracción: (0, 1]
        Monto      = 2  // Valor en importe: ≥ 0
    }

    /// <summary>
    /// Value Object inmutable que encapsula reglas de descuento global.
    /// Identidad por valor: (Modo, Valor).
    /// </summary>
    public sealed record DescuentoGlobal
    {
        public DescuentoGlobalModo Modo { get; }
        /// <summary>
        /// Si Modo=Porcentaje → fracción (0.10 = 10%). Si Modo=Monto → importe fijo.
        /// </summary>
        public decimal Valor { get; }

        private DescuentoGlobal(DescuentoGlobalModo modo, decimal valor)
        {
            Modo  = modo;
            Valor = valor;
        }

        /// <summary>Instancia “sin descuento”. Útil para defaults.</summary>
        public static DescuentoGlobal None { get; } = new(DescuentoGlobalModo.Ninguno, 0m);

        /// <summary>Fábrica para % usando ENTEROS (10 → 10%).</summary>
        public static DescuentoGlobal FromPorcentaje(decimal porcentajeEntero)
        {
            if (porcentajeEntero <= 0m || porcentajeEntero > 100m)
                throw new ArgumentOutOfRangeException(nameof(porcentajeEntero), "Porcentaje debe estar en (0, 100].");

            var fraccion = Math.Round(porcentajeEntero / 100m, 6); // precisión razonable
            return new DescuentoGlobal(DescuentoGlobalModo.Porcentaje, fraccion);
        }

        /// <summary>Fábrica para % ya en FRACCIÓN (0.10 = 10%).</summary>
        public static DescuentoGlobal FromFraccion(decimal fraccion)
        {
            if (fraccion <= 0m || fraccion > 1m)
                throw new ArgumentOutOfRangeException(nameof(fraccion), "Fracción debe estar en (0, 1].");

            return new DescuentoGlobal(DescuentoGlobalModo.Porcentaje, Math.Round(fraccion, 6));
        }

        /// <summary>Fábrica para MONTO fijo (≥ 0). El límite ≤ subtotal se valida al aplicar.</summary>
        public static DescuentoGlobal FromMonto(decimal monto)
        {
            if (monto < 0m)
                throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser ≥ 0.");

            return new DescuentoGlobal(DescuentoGlobalModo.Monto, Math.Round(monto, 2, MidpointRounding.AwayFromZero));
        }

        /// <summary>Conveniencia: ¿no hay descuento?</summary>
        public bool EsNinguno => Modo == DescuentoGlobalModo.Ninguno;

        /// <summary>
        /// Calcula el MONTO descontado a partir de la base imponible del documento.
        /// Redondea a 2 decimales. Valida que no exceda el subtotal.
        /// </summary>
        public decimal CalcularMontoDescuento(decimal subtotalBaseImponible)
        {
            if (EsNinguno) return 0m;

            if (subtotalBaseImponible <= 0m)
                throw new ArgumentOutOfRangeException(nameof(subtotalBaseImponible), "El subtotal debe ser > 0.");

            decimal monto = Modo switch
            {
                DescuentoGlobalModo.Porcentaje => subtotalBaseImponible * Valor, // Valor es fracción
                DescuentoGlobalModo.Monto      => Valor,
                _                              => 0m
            };

            // Evitar sobre-descontar por redondeos
            if (monto > subtotalBaseImponible + 0.0000001m)
                throw new InvalidOperationException("El descuento global no puede exceder el subtotal.");

            return Math.Round(monto, 2);
        }

        /// <summary>
        /// Base imponible neta después del descuento global.
        /// </summary>
        public decimal CalcularBaseLuegoDeDescuento(decimal subtotalBaseImponible)
        {
            var desc = CalcularMontoDescuento(subtotalBaseImponible);
            var baseNeta = subtotalBaseImponible - desc;
            return Math.Round(baseNeta, 2);
        }

        /// <summary>DTO mínimo para mapear a UBL (cac:AllowanceCharge).</summary>
        public readonly record struct AllowanceChargeDto(
            bool ChargeIndicator,            // false para descuento
            decimal Amount,                  // cbc:Amount
            decimal? MultiplierFactorNumeric // cbc:MultiplierFactorNumeric (fracción) si corresponde
        );

        /// <summary>
        /// Genera el DTO UBL del descuento global.
        /// </summary>
        public AllowanceChargeDto ToAllowanceCharge(decimal subtotalBaseImponible)
        {
            if (EsNinguno) return new(false, 0m, null);

            var monto = CalcularMontoDescuento(subtotalBaseImponible);
            var factor = Modo == DescuentoGlobalModo.Porcentaje ? Valor : (decimal?)null;

            return new AllowanceChargeDto(
                ChargeIndicator: false,
                Amount: monto,
                MultiplierFactorNumeric: factor
            );
        }

        public override string ToString() =>
            Modo switch
            {
                DescuentoGlobalModo.Ninguno    => "Sin descuento",
                DescuentoGlobalModo.Porcentaje => $"{(Valor * 100m).ToString("0.##")}%",
                DescuentoGlobalModo.Monto      => $"Monto: {Valor:0.00}",
                _                              => $"Descuento ({Modo})"
            };
    }
}
