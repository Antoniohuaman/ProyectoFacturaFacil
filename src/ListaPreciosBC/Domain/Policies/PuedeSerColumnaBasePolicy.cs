using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Policies
{
    /// <summary>
    /// Policy para validar si una columna puede ser marcada como "Base" en la plantilla de precios.
    /// Reglas:
    /// - Debe estar visible.
    /// - No puede haber otra columna marcada como Base.
    /// - El modo debe ser Fijo.
    /// </summary>
    public static class PuedeSerColumnaBasePolicy
    {
        public static bool Validar(
            ConfiguracionColumnaPrecio columna,
            IEnumerable<ConfiguracionColumnaPrecio> todasColumnas)
        {
            if (!columna.Visible) return false;
            if (columna.Modo != ModoValorizacionColumna.Fijo) return false;
            if (todasColumnas.Any(c => c.EsBase && c.Id != columna.Id)) return false;
            return true;
        }
    }
}