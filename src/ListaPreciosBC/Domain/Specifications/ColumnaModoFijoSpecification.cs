using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para determinar si una columna de precio tiene modo fijo.
    /// </summary>
    public class ColumnaModoFijoSpecification : ISpecification<ConfiguracionColumnaPrecio>
    {
        public bool IsSatisfiedBy(ConfiguracionColumnaPrecio columna)
        {
            // Se considera modo por volumen si su modo es PorVolumen
            return columna.Modo == ModoValorizacionColumna.PorVolumen;
        }
    }
}
