using System;
using SharedKernel.ValueObjects;

namespace ComprobantesElectronicosBC.Domain.Services
{
    /// <summary>
    /// Helper puro para conversiones entre monedas usando un TipoCambio del SharedKernel.
    /// No tiene dependencias con infraestructura.
    /// </summary>
    public static class MonedaConversionService
    {
        /// <summary>
        /// Convierte un monto expresado en <paramref name="origen"/> hacia <paramref name="destino"/>
        /// empleando el <paramref name="tc"/> proporcionado. Redondea según los decimales de la moneda
        /// de destino (AwayFromZero).
        /// </summary>
        public static decimal Convertir(decimal monto, Moneda origen, Moneda destino, TipoCambio tc)
        {
            if (tc is null) throw new ArgumentNullException(nameof(tc));
            if (origen == null) throw new ArgumentNullException(nameof(origen));
            if (destino == null) throw new ArgumentNullException(nameof(destino));

            if (tc.EsPar(origen, destino))
            {
                var conv = monto * tc.Valor;
                return Math.Round(conv, destino.Decimales, MidpointRounding.AwayFromZero);
            }

            if (tc.EsPar(destino, origen))
            {
                var conv = monto / tc.Valor;
                return Math.Round(conv, destino.Decimales, MidpointRounding.AwayFromZero);
            }

            throw new InvalidOperationException($"El tipo de cambio {tc} no cubre el par {origen.Codigo}/{destino.Codigo}.");
        }
    }
}
