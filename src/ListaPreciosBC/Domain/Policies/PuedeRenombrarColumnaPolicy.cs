using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Policies
{
    /// <summary>
    /// Policy para validar si se puede renombrar una columna.
    /// Reglas:
    /// - El nombre debe ser válido según las reglas del ValueObject.
    /// </summary>
    public static class PuedeRenombrarColumnaPolicy
    {
        public static bool Validar(string nuevoNombre)
        {
            return NombreColumnaPrecio.TryCrear(nuevoNombre, out var _);
        }
    }
}