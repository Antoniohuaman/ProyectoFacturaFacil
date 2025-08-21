using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Policies
{
    /// <summary>
    /// Policy para validar si se puede eliminar una columna de la plantilla.
    /// Reglas:
    /// - Debe quedar al menos una columna visible.
    /// - Debe quedar una columna marcada como Base.
    /// </summary>
    public static class PuedeEliminarColumnaPolicy
    {
        public static bool Validar(
            IdentificadorColumnaPrecio columnaAEliminar,
            IEnumerable<ConfiguracionColumnaPrecio> columnas)
        {
            var restantes = columnas.Where(c => c.Id != columnaAEliminar).ToList();
            bool hayVisible = restantes.Any(c => c.Visible);
            bool hayBase = restantes.Any(c => c.EsBase);
            return hayVisible && hayBase && restantes.Count >= ConfiguracionColumnaPrecio.MinOrden;
        }
    }
}