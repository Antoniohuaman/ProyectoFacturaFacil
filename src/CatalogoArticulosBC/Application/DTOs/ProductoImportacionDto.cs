
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Application.DTOs
{
	/// <summary>
	/// Enum para el tipo de producto importado.
	/// </summary>
	public enum TipoProductoImportacion
	{
		Producto = 0,
		Servicio = 1
	}

	/// <summary>
	/// DTO para la importación masiva de productos desde archivo (Excel, CSV, etc).
	/// </summary>
	public class ProductoImportacionDto
	{
		public string? Sku { get; set; }
		public string? Nombre { get; set; }
		public string? UnidadMedida { get; set; }
		public string? AfectacionImpuesto { get; set; }
		public string? Categoria { get; set; }
		public List<string>? AlmacenesAsignados { get; set; }
		public decimal? TasaImpuesto { get; set; }
		public TipoProductoImportacion TipoProducto { get; set; }

		// Campos opcionales para importación
		public string? Descripcion { get; set; }
		public string? Marca { get; set; }
		public string? Modelo { get; set; }
		public string? CodigoBarras { get; set; }
		public string? CodigoFabrica { get; set; }
		public string? CodigoSUNAT { get; set; }
		public decimal? PrecioVenta { get; set; }
		public decimal? Peso { get; set; }
		public decimal? Descuento { get; set; }
		public string? Moneda { get; set; }
		public string? TipoExistencia { get; set; }
	}
}
