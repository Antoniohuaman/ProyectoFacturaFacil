// src/CatalogoArticulosBC/Application/DTOs/ProductoDetalleDto.cs
namespace CatalogoArticulosBC.Application.DTOs
{
    public sealed class ProductoDetalleDto
    {
        public ProductoDetalleDto(
            Guid   productoId,
            string sku,
            string nombre,
            string descripcion,
            string tipoProducto,
            decimal peso)
        {
            ProductoId    = productoId;
            Sku           = sku;
            Nombre        = nombre;
            Descripcion   = descripcion;
            TipoProducto  = tipoProducto;
            Peso          = peso;
        }

        public Guid   ProductoId   { get; }
        public string Sku          { get; }
        public string Nombre       { get; }
        public string Descripcion  { get; }
        public string TipoProducto { get; }
        public decimal Peso        { get; }
    }
}
