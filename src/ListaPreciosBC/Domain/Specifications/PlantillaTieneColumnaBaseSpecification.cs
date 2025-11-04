using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Specifications;
using System.Collections.Generic;
using System.Linq;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para verificar si la plantilla tiene al menos una columna base.
    /// </summary>
    public class PlantillaTieneColumnaBaseSpecification : IBooleanSpecification<IEnumerable<ConfiguracionColumnaPrecio>>
    {
        public bool IsSatisfiedBy(IEnumerable<ConfiguracionColumnaPrecio> columnas)
        {
            return columnas.Any(c => c.EsBase);
        }
    }
}
