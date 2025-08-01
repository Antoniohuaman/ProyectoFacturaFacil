using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Define las formas de aplicar el Impuesto Selectivo al Consumo (ISC).
    /// </summary>
    public enum TipoAplicacionISC
    {
        /// <summary>
        /// El ISC se calcula como un porcentaje sobre la base imponible.
        /// </summary>
        AlValor,

        /// <summary>
        /// El ISC se aplica como un monto fijo por unidad.
        /// </summary>
        MontoFijo
    }

    /// <summary>
    /// Value Object que encapsula la configuración opcional del Impuesto Selectivo al Consumo (ISC)
    /// para un ProductoSimple.
    /// </summary>
    public sealed class ImpuestoSelectivoConsumo : IEquatable<ImpuestoSelectivoConsumo>
    {
        /// <summary>
        /// Indica si el producto aplica ISC.
        /// </summary>
        public bool Aplica { get; }

        /// <summary>
        /// Modo de aplicación del ISC: porcentaje o monto fijo.
        /// </summary>
        public TipoAplicacionISC Tipo { get; }

        /// <summary>
        /// Porcentaje de ISC (0–100) cuando <see cref="Tipo"/> == <c>AlValor</c>.
        /// </summary>
        public decimal ValorPorcentaje { get; }

        /// <summary>
        /// Monto fijo de ISC en moneda local cuando <see cref="Tipo"/> == <c>MontoFijo</c>.
        /// </summary>
        public decimal MontoFijo { get; }

        /// <summary>
        /// Crea una nueva configuración de ISC.
        /// </summary>
        /// <param name="aplica">True si aplica ISC; false en caso contrario.</param>
        /// <param name="tipo">Tipo de aplicación (porcentaje o monto fijo).</param>
        /// <param name="valorPorcentaje">
        /// Valor de porcentaje (0–100) si <paramref name="tipo"/> es <c>AlValor</c>.
        /// </param>
        /// <param name="montoFijo">
        /// Monto fijo en moneda local si <paramref name="tipo"/> es <c>MontoFijo</c>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Si se configuran valores inválidos cuando <paramref name="aplica"/> es true.
        /// </exception>
        public ImpuestoSelectivoConsumo(
            bool aplica,
            TipoAplicacionISC tipo = TipoAplicacionISC.AlValor,
            decimal valorPorcentaje = 0m,
            decimal montoFijo = 0m)
        {
            Aplica = aplica;

            if (!Aplica)
            {
                // Normalizamos al estado “no aplica”
                Tipo = TipoAplicacionISC.AlValor;
                ValorPorcentaje = 0m;
                MontoFijo = 0m;
                return;
            }

            // Cuando aplica, validamos según el tipo seleccionado
            switch (tipo)
            {
                case TipoAplicacionISC.AlValor:
                    if (valorPorcentaje <= 0m || valorPorcentaje > 100m)
                        throw new ArgumentException(
                            "El porcentaje de ISC debe ser mayor que 0 y menor o igual a 100.",
                            nameof(valorPorcentaje));
                    Tipo = TipoAplicacionISC.AlValor;
                    ValorPorcentaje = valorPorcentaje;
                    MontoFijo = 0m;
                    break;

                case TipoAplicacionISC.MontoFijo:
                    if (montoFijo <= 0m)
                        throw new ArgumentException(
                            "El monto fijo de ISC debe ser mayor que 0.",
                            nameof(montoFijo));
                    Tipo = TipoAplicacionISC.MontoFijo;
                    ValorPorcentaje = 0m;
                    MontoFijo = montoFijo;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(tipo),
                        tipo,
                        "Tipo de aplicación de ISC desconocido.");
            }
        }

        public override bool Equals(object? obj) =>
            Equals(obj as ImpuestoSelectivoConsumo);

        public bool Equals(ImpuestoSelectivoConsumo? other) =>
            other is not null
            && Aplica == other.Aplica
            && Tipo == other.Tipo
            && ValorPorcentaje == other.ValorPorcentaje
            && MontoFijo == other.MontoFijo;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Aplica.GetHashCode();
                hash = hash * 23 + Tipo.GetHashCode();
                hash = hash * 23 + ValorPorcentaje.GetHashCode();
                hash = hash * 23 + MontoFijo.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Representación legible de la configuración ISC.
        /// </summary>
        public override string ToString()
        {
            if (!Aplica)
            {
                return "ISC no aplica";
            }

            return Tipo switch
            {
                TipoAplicacionISC.AlValor   => $"ISC {ValorPorcentaje:#.##}%",
                TipoAplicacionISC.MontoFijo => $"ISC fijo S/ {MontoFijo:#.##}",
                _                           => "ISC desconocido"
            };
        }
    }
}
