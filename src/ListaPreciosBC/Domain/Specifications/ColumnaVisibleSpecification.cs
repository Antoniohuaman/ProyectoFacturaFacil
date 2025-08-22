using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para determinar si una columna es visible.
    /// </summary>
    public class ColumnaVisibleSpecification : ISpecification<ConfiguracionColumnaPrecio>
    {
        public bool IsSatisfiedBy(ConfiguracionColumnaPrecio columna)
        {
            return columna.Visible;
        }
    }
}
