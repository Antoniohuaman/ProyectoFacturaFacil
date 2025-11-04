using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para determinar si una columna puede ser eliminada (no es base y no tiene dependientes).
    /// </summary>
    public class ColumnaPuedeSerEliminadaSpecification : IBooleanSpecification<ConfiguracionColumnaPrecio>
    {
        public bool IsSatisfiedBy(ConfiguracionColumnaPrecio columna)
        {
            // Puede ser eliminada si no es base
            return !columna.EsBase;
        }
    }
}
