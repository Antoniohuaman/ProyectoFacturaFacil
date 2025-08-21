using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Policies
{
    /// <summary>
    /// Policy para validar si se puede agregar una nueva columna a la plantilla.
    /// Reglas:
    /// - No debe exceder el máximo permitido (10 columnas).
    /// </summary>
    public static class PuedeAgregarColumnaPolicy
    {
        public static bool Validar(IEnumerable<ConfiguracionColumnaPrecio> columnas)
        {
            return columnas.Count() < ConfiguracionColumnaPrecio.MaxOrden;
        }
    }
}