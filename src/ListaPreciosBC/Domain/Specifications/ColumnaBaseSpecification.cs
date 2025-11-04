using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Specifications;

namespace ListaPreciosBC.Domain.Specifications
{
    /// <summary>
    /// Especificación para determinar si una columna de precio es la columna base.
    /// </summary>
    public class ColumnaBaseSpecification : IBooleanSpecification<ConfiguracionColumnaPrecio>
    {
        public bool IsSatisfiedBy(ConfiguracionColumnaPrecio columna)
        {
            // Se considera columna base si la propiedad EsBase es true
            return columna.EsBase;
        }
    }
}
