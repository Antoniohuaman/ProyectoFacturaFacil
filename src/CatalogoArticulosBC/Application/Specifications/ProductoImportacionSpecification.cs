using CatalogoArticulosBC.Application.DTOs;
using CatalogoArticulosBC.Domain.Specifications;
using SharedKernel.Specifications;

namespace CatalogoArticulosBC.Application.Specifications
{
	/// <summary>
	/// Valida los datos mínimos obligatorios para importar un producto desde archivo (Excel, CSV, etc).
	/// </summary>
	public sealed class ProductoImportacionSpecification : Specification<ProductoImportacionDto>
	{
		public override SpecificationResult IsSatisfiedBy(ProductoImportacionDto dto)
		{
			if (dto == null)
				return SpecificationResult.Failure("RN-CA-100", "producto", "El registro de importación no puede ser nulo.");

			if (string.IsNullOrWhiteSpace(dto.Sku))
				return SpecificationResult.Failure("RN-CA-101", "sku", "El SKU es obligatorio.");

			if (string.IsNullOrWhiteSpace(dto.Nombre))
				return SpecificationResult.Failure("RN-CA-102", "nombre", "El nombre es obligatorio.");

			if (string.IsNullOrWhiteSpace(dto.UnidadMedida))
				return SpecificationResult.Failure("RN-CA-103", "unidadMedida", "La unidad de medida es obligatoria.");

			if (string.IsNullOrWhiteSpace(dto.AfectacionImpuesto))
				return SpecificationResult.Failure("RN-CA-104", "afectacionImpuesto", "La afectación de impuesto es obligatoria.");

			if (string.IsNullOrWhiteSpace(dto.Categoria))
				return SpecificationResult.Failure("RN-CA-105", "categoria", "La categoría es obligatoria.");

			if (dto.AlmacenesAsignados == null || !dto.AlmacenesAsignados.Any())
				return SpecificationResult.Failure("RN-CA-106", "almacenesAsignados", "Debe asignar al menos un almacén.");

			// Validación específica para importación: TipoProducto (enum)
			if (!System.Enum.IsDefined(typeof(TipoProductoImportacion), dto.TipoProducto))
				return SpecificationResult.Failure("RN-CA-107", "tipoProducto", "El valor de TipoProducto debe ser 'Producto' o 'Servicio'.");

			// Puedes agregar más validaciones específicas de importación aquí

			return SpecificationResult.Success();
		}
	}
}
