using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Moneda en la que se registran los precios de productos y servicios.
    /// Campo <c>obligatorio</c>: el usuario debe elegir una de las monedas predefinidas.
    /// </summary>
    public enum Moneda
    {
        /// <summary>
        /// Soles peruanos (PEN).
        /// </summary>
        Soles = 1,

        /// <summary>
        /// Dólares estadounidenses (USD).
        /// </summary>
        Dolares = 2

        // Más monedas pueden añadirse aquí.
    }

    /// <summary>
    /// Métodos de extensión para trabajar con <see cref="Moneda"/>.
    /// </summary>
    public static class MonedaExtensions
    {
        /// <summary>
        /// Obtiene el código ISO 4217 correspondiente a la moneda.
        /// </summary>
        /// <param name="moneda">Moneda de la que se quiere obtener el código.</param>
        /// <returns>Cadena con el código ISO ("PEN", "USD", etc.).</returns>
        public static string ObtenerCodigo(this Moneda moneda) => moneda switch
        {
            Moneda.Soles   => "PEN",
            Moneda.Dolares => "USD",
            _               => throw new NotSupportedException($"Código ISO desconocido para la moneda {moneda}.")
        };

        /// <summary>
        /// Obtiene el símbolo de la moneda.
        /// </summary>
        /// <param name="moneda">Moneda de la que se quiere obtener el símbolo.</param>
        /// <returns>Cadena con el símbolo monetario ("S/", "$", etc.).</returns>
        public static string ObtenerSimbolo(this Moneda moneda) => moneda switch
        {
            Moneda.Soles   => "S/",
            Moneda.Dolares => "$",
            _               => throw new NotSupportedException($"Símbolo desconocido para la moneda {moneda}.")
        };
    }
}
