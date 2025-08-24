using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Domain.Specifications
{
    public class ProductoActivoSpecification : Specification<ProductoSimple>
    {
        public override SpecificationResult IsSatisfiedBy(ProductoSimple producto)
        {
            if (!producto.Habilitado)
                return SpecificationResult.Failure("RN-CA-002", "activo", "El producto está inhabilitado.");
            return SpecificationResult.Success();
        }
    }
}