using System;
using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Domain.Specifications
{
    /// <summary>
    /// Verifica que ningún otro ProductoSimple tenga el mismo SKU.
    /// </summary>
    public sealed class SkuUnicoSpecification : Specification<ProductoSimple>
    {
        private readonly IValidadorUnicidadSku _checker;

        public SkuUnicoSpecification(IValidadorUnicidadSku checker)
        {
            _checker = checker ?? 
                throw new ArgumentNullException(nameof(checker));
        }

        public override SpecificationResult IsSatisfiedBy(ProductoSimple candidate)
        {
            if (candidate == null)
                return SpecificationResult.Failure("RN-CA-001", "sku", "El producto es nulo.");

            if (_checker.IsUnique(candidate.Sku.Valor))
                return SpecificationResult.Success();

            return SpecificationResult.Failure("RN-CA-001", "sku", $"El SKU '{candidate.Sku.Valor}' ya existe en el catálogo (debe ser único).");
        }
    }
}