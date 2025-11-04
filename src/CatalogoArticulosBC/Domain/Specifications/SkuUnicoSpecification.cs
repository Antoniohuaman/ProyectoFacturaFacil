using System;
using CatalogoArticulosBC.Domain.Aggregates;
using SharedKernel.Specifications;

namespace CatalogoArticulosBC.Domain.Specifications
{
    /// <summary>
    /// Verifica que ningún otro ProductoSimple tenga el mismo SKU.
    /// </summary>
    public sealed class SkuUnicoSpecification : Specification<ProductoSimple>
    {
        private readonly IValidadorUnicidadSku _validador;

        public SkuUnicoSpecification(IValidadorUnicidadSku validador)
        {
            _validador = validador ?? throw new ArgumentNullException(nameof(validador));
        }

        public override SpecificationResult IsSatisfiedBy(ProductoSimple candidate)
        {
            if (candidate == null)
                return SpecificationResult.Failure("RN-CA-001", "sku", "El producto es nulo.");

            if (_validador.EsUnico(candidate.Sku.Valor, candidate.EmpresaId))
                return SpecificationResult.Success();

            return SpecificationResult.Failure("RN-CA-001", "sku", $"El SKU '{candidate.Sku.Valor}' ya existe en el catálogo (debe ser único).");
        }
    }
}