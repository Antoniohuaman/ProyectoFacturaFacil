#nullable enable
using System;
using System.Diagnostics;

namespace ListaPreciosBC.Domain.ValueObjects
{
    /// <summary>De dónde se obtuvo el precio resuelto.</summary>
    public enum PrecioResueltoOrigen
    {
        Fijo = 1,
        PorVolumen = 2
    }

    /// <summary>
    /// Value Object que representa el resultado de resolver un precio
    /// para una columna dada, una cantidad solicitada y una fecha.
    /// - No calcula impuestos ni neto/bruto (eso ocurre en otra capa/BC).
    /// - Captura el <see cref="ValorPrecio"/>, el <see cref="PrecioResueltoOrigen"/>
    ///   y la cantidad consultada.
    /// </summary>
    [DebuggerDisplay("{Origen}: {Valor} (cant={CantidadSolicitada})")]
    public sealed record class PrecioResuelto
    {
        /// <summary>Precio resuelto (monto/moneda/impuesto ya definidos en el VO).</summary>
        public ValorPrecio Valor { get; }

        /// <summary>Origen del precio: fijo o por volumen.</summary>
        public PrecioResueltoOrigen Origen { get; }

        /// <summary>Cantidad para la que se resolvió el precio (≥ 1).</summary>
        public int CantidadSolicitada { get; }

        /// <summary>
        /// Crea un <see cref="PrecioResuelto"/> validando invariantes.
        /// </summary>
        public PrecioResuelto(ValorPrecio valor, PrecioResueltoOrigen origen, int cantidadSolicitada)
        {
            Valor = valor ?? throw new ArgumentNullException(nameof(valor));
            if (cantidadSolicitada < 1)
                throw new ArgumentOutOfRangeException(nameof(cantidadSolicitada), "La cantidad solicitada debe ser ≥ 1.");

            Origen = origen;
            CantidadSolicitada = cantidadSolicitada;
        }

        public override string ToString() => $"{Origen}: {Valor} x{CantidadSolicitada}";
    }
}
