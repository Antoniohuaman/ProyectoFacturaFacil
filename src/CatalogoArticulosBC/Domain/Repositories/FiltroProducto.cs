namespace CatalogoArticulosBC.Domain.Repositories
{
    public class FiltroProducto
    {
        public string? Nombre { get; set; }
        public Guid? CategoriaId { get; set; }
        public bool? Habilitado { get; set; }
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }
        // Puedes agregar más propiedades según los filtros necesarios
    }
}
