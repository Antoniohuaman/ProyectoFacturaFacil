using System;
using CatalogoArticulosBC.Domain.Aggregates;

namespace CatalogoArticulosBC.Domain.Specifications
{
    /// <summary>
    /// Un ProductoSimple es válido para facturar si:
    /// - Está activo
    /// - Su SKU es único
    /// (Aquí podrías inyectar y combinar más specs: precio asignado, permisos, etc.)
    /// </summary>
    public sealed class ProductoValidoParaFacturarSpecification
        : Specification<ProductoSimple>
    {
        private readonly ProductoActivoSpecification _activoSpec;
        private readonly SkuUnicoSpecification _skuSpec;

        public ProductoValidoParaFacturarSpecification(
            IValidadorUnicidadSku checker)
        {
            _activoSpec = new ProductoActivoSpecification();
            _skuSpec    = new SkuUnicoSpecification(checker);
        }

        public override SpecificationResult IsSatisfiedBy(ProductoSimple candidate)
        {
            var activoResult = _activoSpec.IsSatisfiedBy(candidate);
            if (!activoResult.IsSatisfied)
                return activoResult;

            var skuResult = _skuSpec.IsSatisfiedBy(candidate);
            if (!skuResult.IsSatisfied)
                return skuResult;

            return SpecificationResult.Success();
        }
    }
}