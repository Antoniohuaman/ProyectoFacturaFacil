using System.Collections.Generic;
using System.Linq;
using CatalogoArticulosBC.Application.Interfaces;

namespace CatalogoArticulosBC.Application.Services
{
    /// <summary>
    /// Implementación mínima y centralizada del esquema de importación.
    /// Mantener aquí la lista completa de columnas soportadas por el frontend.
    /// </summary>
    public sealed class DefaultImportSchemaProvider : IImportSchemaProvider
    {
        // Algunas columnas mínimas obligatorias que el import real validará.
        private static readonly string[] Minimum = new[]
        {
            "Sku",
            "Nombre",
            "UnidadMedida",
            "AfectacionImpuesto",
            "Categoria",
            "AlmacenesAsignados"
        };

        // Columnas adicionales soportadas por el frontend (completa).
        // Nota: No incluir en esta lista columnas pertenecientes a stock, paquetes, variantes ni categorías-resolución específicas.
        private static readonly string[] Additional = new[]
        {
            "Descripcion",
            "Marca",
            "Modelo",
            "CodigoBarras",
            "CodigoFabrica",
            "CodigoSUNAT",
            "PrecioVenta",
            "Moneda",
            "Peso",
            "Descuento",
            "TipoExistencia",
            "TipoProducto"
        };

        public IReadOnlyList<string> GetBasicaHeaders()
        {
            // Básica = mínimos (en orden definido)
            return Minimum.ToArray();
        }

        public IReadOnlyList<string> GetCompletaHeaders()
        {
            // Completa = mínimos seguidos de adicionales en el orden oficial
            return Minimum.Concat(Additional).ToArray();
        }

        public IReadOnlyList<string> GetMinimumRequiredHeaders() => Minimum.ToArray();
    }
}
