using System;

namespace CatalogoArticulosBC.Application.DTOs
{
    public class EditarProductoSimpleDto
    {
        public Guid ProductoId { get; set; }
        public string NuevoNombre { get; set; } = default!;
        public string NuevaDescripcion { get; set; } = default!;
        public decimal NuevoPrecio { get; set; }
        public string UsuarioId { get; set; } = default!;
    }
}