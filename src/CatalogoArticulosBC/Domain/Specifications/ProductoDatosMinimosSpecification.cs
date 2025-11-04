using CatalogoArticulosBC.Domain.Aggregates;
using SharedKernel.Specifications;

namespace CatalogoArticulosBC.Domain.Specifications
{
    /// <summary>
    /// Valida que un ProductoSimple tenga los datos mínimos obligatorios para ser creado.
    /// </summary>
    public sealed class ProductoDatosMinimosSpecification : Specification<ProductoSimple>
    {
        public override SpecificationResult IsSatisfiedBy(ProductoSimple producto)
        {
            if (producto == null)
                return SpecificationResult.Failure("RN-CA-000", "producto", "El producto no puede ser nulo.");

            if (producto.Sku == null)
                return SpecificationResult.Failure("RN-CA-001", "sku", "El SKU es obligatorio.");

            if (string.IsNullOrWhiteSpace(producto.Nombre?.Valor))
                return SpecificationResult.Failure("RN-CA-002", "nombre", "El nombre del producto es obligatorio.");

            if (producto.UnidadMedida == null)
                return SpecificationResult.Failure("RN-CA-003", "unidadMedida", "La unidad de medida es obligatoria.");

            if (producto.AfectacionImpuesto == null)
                return SpecificationResult.Failure("RN-CA-004", "afectacionImpuesto", "La afectación de impuesto es obligatoria.");

            if (producto.CategoriaId == null)
                return SpecificationResult.Failure("RN-CA-005", "categoria", "La categoría es obligatoria.");

            if (producto.EstablecimientosAsignados == null || !producto.EstablecimientosAsignados.Any())
                return SpecificationResult.Failure("RN-CA-006", "establecimientosAsignados", "Debe asignar al menos un establecimiento.");

            // No se valida Tipo ni TipoExistencia porque no hay valor "None" en el enum

            return SpecificationResult.Success();
        }
    }
}