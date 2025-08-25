using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Filters
{
    public class FiltroProducto
    {
        public string? Nombre { get; set; }
        public Categoria? Categoria { get; set; }
        public bool? Habilitado { get; set; }
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }
        // Puedes agregar más propiedades según los filtros necesarios
    }
}
