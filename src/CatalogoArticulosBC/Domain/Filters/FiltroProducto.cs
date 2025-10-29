using CatalogoArticulosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace CatalogoArticulosBC.Domain.Filters
{
    public class FiltroProducto
    {
        /// <summary>
        /// Empresa (tenant) sobre la que se ejecuta la consulta. Obligatorio para evitar fuga entre empresas.
        /// </summary>
        public EmpresaId? EmpresaId { get; set; }
        public string? Nombre { get; set; }
        public Categoria? Categoria { get; set; }
        public bool? Habilitado { get; set; }
        public decimal? PrecioMin { get; set; }
        public decimal? PrecioMax { get; set; }
        // Puedes agregar más propiedades según los filtros necesarios
    }
}
