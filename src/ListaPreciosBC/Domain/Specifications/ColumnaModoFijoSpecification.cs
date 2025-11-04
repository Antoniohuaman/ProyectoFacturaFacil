using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para determinar si una columna de precio tiene modo fijo.
    /// </summary>
    public class ColumnaModoFijoSpecification : IBooleanSpecification<ConfiguracionColumnaPrecio>
    {
        public bool IsSatisfiedBy(ConfiguracionColumnaPrecio columna)
        {
            // Se considera modo por volumen si su modo es PorVolumen
            return columna.Modo == ModoValorizacionColumna.PorVolumen;
        }
    }
}
