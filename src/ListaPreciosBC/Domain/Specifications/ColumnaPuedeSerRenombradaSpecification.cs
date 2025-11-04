using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para determinar si una columna puede ser renombrada (no es base y es visible).
    /// </summary>
    public class ColumnaPuedeSerRenombradaSpecification : IBooleanSpecification<ConfiguracionColumnaPrecio>
    {
        public bool IsSatisfiedBy(ConfiguracionColumnaPrecio columna)
        {
            // Puede ser renombrada si no es base y es visible
            return !columna.EsBase && columna.Visible;
        }
    }
}
