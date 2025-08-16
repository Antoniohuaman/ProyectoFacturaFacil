using System;
using System.Diagnostics;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Value Object TipoCambio (snapshot) para conversión monetaria:
    ///   1 {Origen.Codigo} = {Valor} {Destino.Codigo} en la fecha {Fecha}.
    /// Reglas acordadas:
    /// - Se utiliza para congelar el TC del comprobante (SUNAT/SBS) y convertir montos.
    /// - No expone "Fuente" ni campos editables en la UI (esto vive en la capa de aplicación/infra).
    /// - Invariantes: Origen ≠ Destino, Valor > 0.
    /// - Redondeo de resultados a los decimales de la moneda destino (AwayFromZero).
    /// </summary>
    [DebuggerDisplay("1 {Origen.Codigo} = {Valor} {Destino.Codigo} ({Fecha})")]
    public sealed record TipoCambio
    {
        /// <summary>Moneda de origen del tipo de cambio (p. ej., USD).</summary>
        public Moneda Origen { get; init; }

        /// <summary>Moneda de destino del tipo de cambio (p. ej., PEN).</summary>
        public Moneda Destino { get; init; }

        /// <summary>
        /// Valor del tipo de cambio: 1 {Origen} = {Valor} {Destino}.
        /// Ej.: 1 USD = 3.537 PEN => Valor = 3.537
        /// </summary>
        public decimal Valor { get; init; }

        /// <summary>Fecha aplicada del tipo de cambio (normalmente fecha de emisión del comprobante o último día hábil previo).</summary>
        public DateOnly Fecha { get; init; }

        private const int MAX_ESCALA_TC = 6; // precisión razonable para TC (SUNAT publica con 3~6 decimales)

        private TipoCambio(Moneda origen, Moneda destino, decimal valor, DateOnly fecha)
        {
            Origen  = origen  ?? throw new ArgumentNullException(nameof(origen));
            Destino = destino ?? throw new ArgumentNullException(nameof(destino));

            if (Origen == Destino)
                throw new ArgumentException("Origen y Destino no pueden ser la misma moneda.");

            if (valor <= 0m)
                throw new ArgumentOutOfRangeException(nameof(valor), "El tipo de cambio debe ser > 0.");

            Valor = Math.Round(valor, MAX_ESCALA_TC, MidpointRounding.AwayFromZero);
            Fecha = fecha;
        }

        /// <summary>
        /// Crea un tipo de cambio dirigido: 1 {origen} = {valor} {destino} en {fecha}.
        /// </summary>
        public static TipoCambio Create(Moneda origen, Moneda destino, decimal valor, DateOnly fecha)
            => new(origen, destino, valor, fecha);

        /// <summary>
        /// Crea el tipo de cambio inverso: 1 {Destino} = (1/Valor) {Origen} para la misma fecha.
        /// </summary>
        public TipoCambio Invertir()
        {
            var inverso = 1m / Valor;
            return new TipoCambio(Destino, Origen, inverso, Fecha);
        }

        /// <summary>
        /// Convierte un importe entre Origen y Destino (en ambos sentidos).
        /// Si importe.Moneda = Origen, multiplica por Valor y redondea a Destino.Decimales.
        /// Si importe.Moneda = Destino, divide por Valor y redondea a Origen.Decimales.
        /// En otro caso, lanza excepción (el VO no adivina rutas intermedias).
        /// </summary>
        public Dinero Convertir(Dinero importe)
        {
            if (importe is null) throw new ArgumentNullException(nameof(importe));

            if (importe.Moneda == Origen)
            {
                var monto = importe.Monto * Valor;
                return Dinero.Create(Round(monto, Destino.Decimales), Destino);
            }

            if (importe.Moneda == Destino)
            {
                var monto = importe.Monto / Valor;
                return Dinero.Create(Round(monto, Origen.Decimales), Origen);
            }

            throw new InvalidOperationException(
                $"La moneda del importe ({importe.Moneda.Codigo}) no coincide con el par {Origen.Codigo}/{Destino.Codigo}.");
        }

        /// <summary>
        /// Convierte un importe explícitamente hacia una moneda objetivo,
        /// siempre que el importe esté en Origen o en Destino y la moneda objetivo sea la otra.
        /// Si objetivo == importe.Moneda, retorna el mismo importe.
        /// </summary>
        public Dinero ConvertirHacia(Dinero importe, Moneda objetivo)
        {
            if (importe is null) throw new ArgumentNullException(nameof(importe));
            if (objetivo is null) throw new ArgumentNullException(nameof(objetivo));

            if (objetivo == importe.Moneda)
                return importe;

            if (objetivo == Destino && importe.Moneda == Origen)
            {
                var monto = importe.Monto * Valor;
                return Dinero.Create(Round(monto, Destino.Decimales), Destino);
            }

            if (objetivo == Origen && importe.Moneda == Destino)
            {
                var monto = importe.Monto / Valor;
                return Dinero.Create(Round(monto, Origen.Decimales), Origen);
            }

            throw new InvalidOperationException(
                $"No se puede convertir de {importe.Moneda.Codigo} a {objetivo.Codigo} con el par {Origen.Codigo}/{Destino.Codigo}.");
        }

        /// <summary>
        /// Indica si el tipo de cambio aplica directamente al par indicado (dirección exacta).
        /// </summary>
        public bool EsPar(Moneda origen, Moneda destino) => Origen == origen && Destino == destino;

        /// <summary>
        /// Indica si el tipo de cambio cubre el par indicado en cualquier dirección (directo o inverso).
        /// </summary>
        public bool CubrePar(Moneda a, Moneda b)
            => (Origen == a && Destino == b) || (Origen == b && Destino == a);

        public override string ToString()
            => $"1 {Origen.Codigo} = {Valor} {Destino.Codigo} (Fecha: {Fecha:yyyy-MM-dd})";

        private static decimal Round(decimal value, byte decimals)
            => Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }
}
