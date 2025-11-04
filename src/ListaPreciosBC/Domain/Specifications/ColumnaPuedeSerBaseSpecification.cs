using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para determinar si una columna puede ser base (no tiene dependencias y es visible).
    /// </summary>
    public class ColumnaPuedeSerBaseSpecification : IBooleanSpecification<ConfiguracionColumnaPrecio>
    {
        public bool IsSatisfiedBy(ConfiguracionColumnaPrecio columna)
        {
            // Puede ser base si es visible y no es ya base
            return columna.Visible && !columna.EsBase;
        }
    }
}
