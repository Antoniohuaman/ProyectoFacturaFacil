using System;

namespace CatalogoArticulosBC.Application.UseCases.ListarProductos
{
    /// <summary>
    /// Resultado paginado de la lista de productos.
    /// </summary>
    public sealed class ListarProductosOutputDto
    {
        // Contexto
        public string EmpresaId { get; init; } = string.Empty;

        // Paginación y ordenamiento
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages { get; init; }
        public string OrdenarPor { get; init; } = "nombre";
        public string Direccion { get; init; } = "asc";

        // Items
        public Item[] Items { get; init; } = Array.Empty<Item>();

        public sealed class Item
        {
            // Identidad y estado
            public Guid ProductoId { get; init; }
            public bool Habilitado { get; init; }

            // Clave de negocio
            public string Sku { get; init; } = string.Empty;

            // Datos
            public string Nombre { get; init; } = string.Empty;
            public string? CategoriaId { get; init; }
            public string? CategoriaNombre { get; init; }
            public string? CategoriaColor { get; init; }
            public string? Marca { get; init; }

            // Precio y moneda
            public decimal? PrecioVenta { get; init; }
            public string Moneda { get; init; } = string.Empty;

            // Nuevos opcionales
            public decimal? PrecioCompraMonto { get; init; }
            public string? PrecioCompraMoneda { get; init; }
            public decimal? PorcentajeGanancia { get; init; }     // 0..100
            public string? Alias { get; init; }

            // Tipo / inventario
            public string TipoProducto { get; init; } = string.Empty;
            public string TipoExistencia { get; init; } = string.Empty;

            // Imagen
            public Guid? ImagenPrincipalId { get; init; }
        }
    }
}
