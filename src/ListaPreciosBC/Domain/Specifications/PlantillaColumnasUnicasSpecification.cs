using ListaPreciosBC.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para verificar que todas las columnas de una plantilla sean únicas por identificador.
    /// </summary>
    public class PlantillaColumnasUnicasSpecification : ISpecification<IEnumerable<ConfiguracionColumnaPrecio>>
    {
        public bool IsSatisfiedBy(IEnumerable<ConfiguracionColumnaPrecio> columnas)
        {
            var ids = columnas.Select(c => c.Id).ToList();
            return ids.Count == ids.Distinct().Count();
        }
    }
}
