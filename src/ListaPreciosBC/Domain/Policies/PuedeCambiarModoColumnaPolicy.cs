using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Policies
{
    /// <summary>
    /// Policy para validar si se puede cambiar el modo de valorización de una columna.
    /// Reglas:
    /// - El modo debe ser uno de los permitidos por el ValueObject.
    /// </summary>
    public static class PuedeCambiarModoColumnaPolicy
    {
        public static bool Validar(string nuevoModo)
        {
            return ModoValorizacionColumna.TryCrear(nuevoModo, out var _);
        }
    }
}