using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    /// <summary>
    /// Tipo de existencia de un producto, que define cómo se gestiona su inventario.
    /// Campo <c>obligatorio</c>: el usuario debe elegir uno de los valores predefinidos.
    /// </summary>
    public enum TipoExistencia
    {
        /// <summary>
        /// Mercaderías para la venta.
        /// </summary>
        Mercaderias = 1,

        /// <summary>
        /// Productos terminados listos para despacho.
        /// </summary>
        ProductosTerminados = 2,

        /// <summary>
        /// Servicios (no generan inventario físico).
        /// </summary>
        Servicios = 3,

        /// <summary>
        /// Materias primas para fabricación.
        /// </summary>
        MateriasPrimas = 4,

        /// <summary>
        /// Envases usados para empaque.
        /// </summary>
        Envases = 5,

        /// <summary>
        /// Materiales auxiliares de producción.
        /// </summary>
        MaterialesAuxiliares = 6,

        /// <summary>
        /// Suministros (artículos consumibles).
        /// </summary>
        Suministros = 7,

        /// <summary>
        /// Repuestos o piezas de recambio.
        /// </summary>
        Repuestos = 8,

        /// <summary>
        /// Embalajes para transporte.
        /// </summary>
        Embalajes = 9,

        /// <summary>
        /// Otros tipos de existencia no categorizados.
        /// </summary>
        Otros = 10
    }

    /// <summary>
    /// Extensiones útiles para <see cref="TipoExistencia"/>, como descripciones legibles.
    /// </summary>
    public static class TipoExistenciaExtensions
    {
        /// <summary>
        /// Devuelve una descripción legible del tipo de existencia.
        /// </summary>
        public static string Descripcion(this TipoExistencia tipo) => tipo switch
        {
            TipoExistencia.Mercaderias           => "Mercaderías",
            TipoExistencia.ProductosTerminados   => "Productos Terminados",
            TipoExistencia.Servicios             => "Servicios",
            TipoExistencia.MateriasPrimas        => "Materias Primas",
            TipoExistencia.Envases               => "Envases",
            TipoExistencia.MaterialesAuxiliares  => "Materiales Auxiliares",
            TipoExistencia.Suministros           => "Suministros",
            TipoExistencia.Repuestos             => "Repuestos",
            TipoExistencia.Embalajes             => "Embalajes",
            TipoExistencia.Otros                 => "Otros",
            _                                     => throw new NotSupportedException($"Tipo de existencia desconocido: {tipo}")
        };
    }
}
